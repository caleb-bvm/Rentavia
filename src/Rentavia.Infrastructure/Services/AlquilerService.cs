using System.Data;
using Microsoft.EntityFrameworkCore;
using Rentavia.Application.Services;
using Rentavia.Domain.Entities;
using Rentavia.Infrastructure.Data;

namespace Rentavia.Infrastructure.Services;

/// <summary>
/// Operación de renta: entrega del vehículo, devolución y pagos internos.
/// Usa el mismo bloqueo de vehículo que AgendaService para mantener la agenda consistente.
/// </summary>
public class AlquilerService(RentaviaDbContext db, AgendaService agenda)
{
    /// <summary>Convierte una reserva confirmada en alquiler y registra la inspección de entrega.</summary>
    public async Task<int> EntregarAsync(int reservaId, byte[] version, int kilometraje, int combustible,
        string observaciones, string actor)
    {
        var vehiculoId = await db.Reservas.AsNoTracking().Where(r => r.Id == reservaId).Select(r => (int?)r.VehiculoId).SingleOrDefaultAsync()
            ?? throw new ReglaNegocioException("La reserva no existe.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var vehiculo = await agenda.BloquearVehiculoAsync(vehiculoId);
        var reserva = await db.Reservas.Include(r => r.Cliente).SingleAsync(r => r.Id == reservaId);
        ReglasAlquiler.VersionActual(reserva.Version, version);
        var ahora = DateTimeOffset.UtcNow;
        if (reserva.Estado != EstadoReserva.Confirmada || vehiculo.Estado != EstadoVehiculo.Activo
            || !reserva.Cliente.Activo
            || reserva.InicioUtc > ahora.AddMinutes(5) || reserva.FinUtc <= ahora)
            throw new ReglaNegocioException("La reserva debe estar confirmada, vigente y próxima a iniciar; cliente y vehículo deben estar habilitados.");
        if (kilometraje < vehiculo.Kilometraje || combustible is < 0 or > 100 || observaciones.Length > 500)
            throw new ReglaNegocioException("Revisa el kilometraje, combustible y observaciones.");
        if (await db.Alquileres.AnyAsync(a => a.VehiculoId == vehiculoId && a.Estado == EstadoAlquiler.Abierto))
            throw new ReglaNegocioException("El vehículo ya tiene un alquiler abierto.");
        await agenda.ComprobarLibreAsync(vehiculoId, reserva.InicioUtc, reserva.FinUtc, reservaId);
        reserva.Estado = EstadoReserva.Convertida;
        var alquiler = new Alquiler
        {
            ReservaId = reservaId,
            VehiculoId = vehiculoId,
            EntregaUtc = ahora,
            KilometrajeInicial = kilometraje,
            CombustibleInicial = combustible,
            ObservacionesEntrega = observaciones.Trim(),
            MontoTotal = reserva.TotalEstimado
        };
        db.Alquileres.Add(alquiler);
        vehiculo.Kilometraje = kilometraje;
        // Reutilizamos la ocupación de la reserva; no insertamos otra agenda.
        await db.SaveChangesAsync();
        db.Registrar(actor, "Entregar", "Alquiler", alquiler.Id);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return alquiler.Id;
    }

    /// <summary>Cierra el alquiler, registra la inspección final y libera la agenda.</summary>
    public async Task DevolverAsync(int id, byte[] version, int kilometraje, int combustible,
        string observaciones, decimal cargo, string? concepto, string actor)
    {
        var vehiculoId = await db.Alquileres.AsNoTracking().Where(a => a.Id == id).Select(a => (int?)a.VehiculoId).SingleOrDefaultAsync()
            ?? throw new ReglaNegocioException("El alquiler no existe.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var vehiculo = await agenda.BloquearVehiculoAsync(vehiculoId);
        var alquiler = await db.Alquileres.Include(a => a.Reserva).Include(a => a.Pagos).SingleAsync(a => a.Id == id);
        ReglasAlquiler.VersionActual(alquiler.Version, version);
        if (alquiler.Estado != EstadoAlquiler.Abierto) throw new ReglaNegocioException("El alquiler ya está cerrado.");
        if (kilometraje < alquiler.KilometrajeInicial || kilometraje < vehiculo.Kilometraje
            || combustible is < 0 or > 100 || cargo is < 0 or > 999999 || observaciones.Length > 500
            || (cargo > 0 && string.IsNullOrWhiteSpace(concepto)) || concepto?.Length > 150)
            throw new ReglaNegocioException("Revisa kilometraje, combustible y motivo del cargo adicional.");
        alquiler.KilometrajeFinal = kilometraje;
        alquiler.CombustibleFinal = combustible;
        alquiler.ObservacionesDevolucion = observaciones.Trim();
        alquiler.CargoAdicional = cargo;
        alquiler.ConceptoCargo = concepto?.Trim();
        alquiler.MontoTotal = alquiler.Reserva.TotalEstimado + cargo;
        if (alquiler.Pagos.Sum(p => p.Monto) > alquiler.MontoTotal)
            throw new ReglaNegocioException("El total no puede ser inferior a los pagos registrados.");
        alquiler.DevolucionUtc = DateTimeOffset.UtcNow;
        alquiler.Estado = EstadoAlquiler.Cerrado;
        vehiculo.Kilometraje = kilometraje;
        (await db.Ocupaciones.SingleAsync(o => o.ReservaId == alquiler.ReservaId)).Activa = false;
        db.Registrar(actor, "Devolver", "Alquiler", id);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>Registra un pago interno sin superar el saldo pendiente.</summary>
    public async Task RegistrarPagoAsync(int id, decimal monto, string metodo, string? referencia, string actor)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var alquiler = await db.Alquileres.FromSqlInterpolated($"SELECT * FROM Alquileres WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
            .Include(a => a.Pagos).SingleOrDefaultAsync() ?? throw new ReglaNegocioException("El alquiler no existe.");
        if (monto <= 0 || monto > alquiler.MontoTotal - alquiler.Pagos.Sum(p => p.Monto)
            || metodo is not ("Efectivo" or "Transferencia" or "Tarjeta") || referencia?.Length > 100)
            throw new ReglaNegocioException("El pago debe ser positivo y no superar el saldo. Revisa método y referencia.");
        db.Pagos.Add(new Pago { AlquilerId = id, Monto = monto, Metodo = metodo, Referencia = referencia?.Trim() });
        db.Registrar(actor, "RegistrarPago", "Alquiler", id);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
