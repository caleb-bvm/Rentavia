using System.Data;
using Microsoft.EntityFrameworkCore;
using Rentavia.Application.Services;
using Rentavia.Domain.Entities;
using Rentavia.Infrastructure.Data;

namespace Rentavia.Infrastructure.Services;

/// <summary>
/// Administra la agenda de cada vehículo: reservas y mantenimientos.
/// Todas las escrituras bloquean el vehículo dentro de una transacción serializable,
/// así dos empleados no pueden ocupar el mismo intervalo al mismo tiempo.
/// </summary>
public class AgendaService(RentaviaDbContext db)
{
    /// <summary>
    /// Vehículos activos, con tarifa y sin ocupaciones que se crucen con [inicio, fin).
    /// </summary>
    public IQueryable<Vehiculo> Disponibles(DateTimeOffset inicio, DateTimeOffset fin, int? excluirReserva = null)
    {
        ReglasAlquiler.Dias(inicio, fin);
        return db.Vehiculos.AsNoTracking().Include(x => x.Categoria).Include(x => x.Sucursal)
            .Where(v => v.Estado == EstadoVehiculo.Activo && v.Sucursal.Activa && v.TarifaDiaria > 0)
            .Where(v => !db.Ocupaciones.Any(o => o.VehiculoId == v.Id && o.Activa
                && (excluirReserva == null || o.ReservaId != excluirReserva)
                && o.InicioUtc < fin && inicio < o.FinUtc))
            // Un alquiler vencido que sigue abierto requiere intervención: no se ofrece el vehículo.
            .Where(v => !db.Alquileres.Any(a => a.VehiculoId == v.Id
                && a.Estado == EstadoAlquiler.Abierto && a.Reserva.FinUtc < DateTimeOffset.UtcNow))
            // Un mantenimiento iniciado y sin finalizar deja el vehículo en taller hasta que se finalice,
            // aunque ya pasó o aún no llega la fecha estimada de fin.
            .Where(v => !db.Mantenimientos.Any(m => m.VehiculoId == v.Id
                && !m.Finalizado && m.InicioUtc <= DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Crea una reserva (id nulo) o modifica sus fechas y cliente. Al editar se conserva la tarifa acordada.
    /// </summary>
    public async Task<Reserva> GuardarReservaAsync(int? id, int clienteId, int vehiculoId,
        DateTimeOffset inicio, DateTimeOffset fin, byte[] version, string actor)
    {
        var dias = ReglasAlquiler.Dias(inicio, fin);
        if (inicio < DateTimeOffset.UtcNow.AddMinutes(-5))
            throw new ReglaNegocioException("La reserva no puede comenzar en el pasado.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var vehiculo = await BloquearVehiculoAsync(vehiculoId);
        if (vehiculo.Estado != EstadoVehiculo.Activo
            || !await db.Sucursales.AnyAsync(s => s.Id == vehiculo.SucursalId && s.Activa))
            throw new ReglaNegocioException("El vehículo o su sucursal no están activos.");

        if (!await db.Clientes.AnyAsync(c => c.Id == clienteId && c.Activo))
            throw new ReglaNegocioException("Selecciona un cliente activo.");

        var esNueva = !id.HasValue;
        var reserva = id.HasValue ? await db.Reservas.FindAsync(id.Value) : new Reserva();
        if (reserva is null) throw new ReglaNegocioException("La reserva no existe.");
        if (!esNueva)
        {
            ReglasAlquiler.VersionActual(reserva.Version, version);
            if (reserva.Estado != EstadoReserva.Confirmada || reserva.VehiculoId != vehiculoId)
                throw new ReglaNegocioException("Solo se edita una reserva confirmada del mismo vehículo. Cancela y crea otra para cambiarlo.");
        }

        await ComprobarLibreAsync(vehiculoId, inicio, fin, id);

        reserva.ClienteId = clienteId;
        reserva.VehiculoId = vehiculoId;
        reserva.InicioUtc = inicio;
        reserva.FinUtc = fin;
        if (esNueva) reserva.TarifaDiaria = vehiculo.TarifaDiaria;
        reserva.Dias = dias;
        reserva.TotalEstimado = ReglasAlquiler.Total(inicio, fin, reserva.TarifaDiaria);

        var ocupacion = esNueva
            ? new Ocupacion { Reserva = reserva, VehiculoId = vehiculoId }
            : await db.Ocupaciones.SingleAsync(o => o.ReservaId == id);
        ocupacion.InicioUtc = inicio;
        ocupacion.FinUtc = fin;
        if (esNueva)
        {
            db.Reservas.Add(reserva);
            db.Ocupaciones.Add(ocupacion);
        }

        await db.SaveChangesAsync();
        db.Registrar(actor, esNueva ? "Crear" : "Editar", "Reserva", reserva.Id);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return reserva;
    }

    /// <summary>Cancela una reserva confirmada y libera su ocupación.</summary>
    public async Task CancelarReservaAsync(int id, byte[] version, string actor)
    {
        var vehiculoId = await db.Reservas.AsNoTracking().Where(r => r.Id == id)
            .Select(r => (int?)r.VehiculoId).SingleOrDefaultAsync()
            ?? throw new ReglaNegocioException("La reserva no existe.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await BloquearVehiculoAsync(vehiculoId);
        var reserva = await db.Reservas.SingleAsync(r => r.Id == id);
        ReglasAlquiler.VersionActual(reserva.Version, version);
        if (reserva.Estado != EstadoReserva.Confirmada)
            throw new ReglaNegocioException("Solo se cancela una reserva confirmada.");

        reserva.Estado = EstadoReserva.Cancelada;
        (await db.Ocupaciones.SingleAsync(o => o.ReservaId == id)).Activa = false;
        db.Registrar(actor, "Cancelar", "Reserva", id);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>Programa un mantenimiento y bloquea la agenda del vehículo en ese período.</summary>
    public async Task<Mantenimiento> ProgramarMantenimientoAsync(int vehiculoId, DateTimeOffset inicio,
        DateTimeOffset fin, string tipo, string descripcion, decimal costo, string actor)
    {
        ReglasAlquiler.Dias(inicio, fin);
        if (inicio < DateTimeOffset.UtcNow.AddMinutes(-5) || costo < 0 || costo > 999999)
            throw new ReglaNegocioException("Revisa las fechas y el costo del mantenimiento.");
        if (tipo is not ("Preventivo" or "Correctivo") || string.IsNullOrWhiteSpace(descripcion) || descripcion.Length > 500)
            throw new ReglaNegocioException("Indica tipo y descripción del mantenimiento.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var vehiculo = await BloquearVehiculoAsync(vehiculoId);
        if (vehiculo.Estado == EstadoVehiculo.Retirado)
            throw new ReglaNegocioException("El vehículo está retirado.");
        await ComprobarLibreAsync(vehiculoId, inicio, fin);

        var mantenimiento = new Mantenimiento
        {
            VehiculoId = vehiculoId,
            InicioUtc = inicio,
            FinUtc = fin,
            Tipo = tipo,
            Descripcion = descripcion.Trim(),
            Costo = costo
        };
        db.Mantenimientos.Add(mantenimiento);
        db.Ocupaciones.Add(new Ocupacion { VehiculoId = vehiculoId, Mantenimiento = mantenimiento, InicioUtc = inicio, FinUtc = fin });
        await db.SaveChangesAsync();
        db.Registrar(actor, "Programar", "Mantenimiento", mantenimiento.Id);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return mantenimiento;
    }

    /// <summary>Marca el mantenimiento como finalizado y libera la agenda.</summary>
    public async Task FinalizarMantenimientoAsync(int id, string actor)
    {
        var vehiculoId = await db.Mantenimientos.AsNoTracking().Where(m => m.Id == id)
            .Select(m => (int?)m.VehiculoId).SingleOrDefaultAsync()
            ?? throw new ReglaNegocioException("El mantenimiento no existe.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await BloquearVehiculoAsync(vehiculoId);
        var mantenimiento = await db.Mantenimientos.SingleAsync(m => m.Id == id);
        if (mantenimiento.Finalizado)
            throw new ReglaNegocioException("El mantenimiento ya finalizó.");

        mantenimiento.Finalizado = true;
        (await db.Ocupaciones.SingleAsync(o => o.MantenimientoId == id)).Activa = false;
        db.Registrar(actor, "Finalizar", "Mantenimiento", id);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>
    /// Lee el vehículo con UPDLOCK y HOLDLOCK. Otra transacción que intente bloquear
    /// el mismo vehículo espera hasta que esta confirme o se revierta.
    /// </summary>
    internal async Task<Vehiculo> BloquearVehiculoAsync(int id) => await db.Vehiculos
        .FromSqlInterpolated($"SELECT * FROM Vehiculos WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}").SingleOrDefaultAsync()
        ?? throw new ReglaNegocioException("El vehículo no existe.");

    /// <summary>Lanza una excepción de negocio si el vehículo no está libre en [inicio, fin).</summary>
    internal async Task ComprobarLibreAsync(int vehiculoId, DateTimeOffset inicio, DateTimeOffset fin, int? excluirReserva = null)
    {
        if (await db.Ocupaciones.AnyAsync(o => o.VehiculoId == vehiculoId && o.Activa
            && (excluirReserva == null || o.ReservaId != excluirReserva)
            && o.InicioUtc < fin && inicio < o.FinUtc))
            throw new ReglaNegocioException("El vehículo ya tiene una reserva, alquiler o mantenimiento en ese período.");
        if (await db.Alquileres.AnyAsync(a => a.VehiculoId == vehiculoId && a.Estado == EstadoAlquiler.Abierto
            && a.Reserva.FinUtc < DateTimeOffset.UtcNow))
            throw new ReglaNegocioException("El vehículo tiene una devolución pendiente; resuélvela antes de programar otra operación.");
        if (await db.Mantenimientos.AnyAsync(m => m.VehiculoId == vehiculoId && !m.Finalizado
            && m.InicioUtc <= DateTimeOffset.UtcNow))
            throw new ReglaNegocioException("El vehículo está en mantenimiento; finalízalo antes de programar otra operación.");
    }
}
