using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rentavia.Application.Services;
using Rentavia.Domain.Entities;
using Rentavia.Infrastructure.Data;
using Rentavia.Infrastructure.Services;
using Rentavia.Web.Models;

namespace Rentavia.Web.Controllers;

/// <summary>Crear, modificar y cancelar reservas. La validación de cruces la hace AgendaService.</summary>
public class ReservasController(RentaviaDbContext db, AgendaService agenda) : ProyectoController
{
    /// <summary>Listado filtrado por estado. Por defecto muestra las reservas por entregar, la más próxima primero.</summary>
    public async Task<IActionResult> Index(string? estado)
    {
        estado ??= "por-entregar";
        var conteo = await db.Reservas.GroupBy(r => r.Estado)
            .Select(g => new { g.Key, Total = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Total);
        int Total(EstadoReserva e) => conteo.GetValueOrDefault(e);
        ViewBag.Pestanas = new Pestanas(estado,
        [
            new("por-entregar", "Por entregar", Total(EstadoReserva.Confirmada)),
            new("entregadas", "Entregadas", Total(EstadoReserva.Convertida)),
            new("canceladas", "Canceladas", Total(EstadoReserva.Cancelada)),
            new("todas", "Todas", conteo.Values.Sum())
        ]);
        EstadoReserva? filtro = estado switch
        {
            "por-entregar" => EstadoReserva.Confirmada,
            "entregadas" => EstadoReserva.Convertida,
            "canceladas" => EstadoReserva.Cancelada,
            _ => null
        };
        var query = db.Reservas.AsNoTracking().Include(r => r.Cliente).Include(r => r.Vehiculo)
            .Where(r => filtro == null || r.Estado == filtro);
        var reservas = await (filtro == EstadoReserva.Confirmada
            ? query.OrderBy(r => r.InicioUtc)
            : query.OrderByDescending(r => r.CreadaUtc)).ToListAsync();
        // Las reservas entregadas enlazan con su contrato de alquiler
        var ids = reservas.Where(r => r.Estado == EstadoReserva.Convertida).Select(r => r.Id).ToList();
        ViewBag.Contratos = await db.Alquileres.Where(a => ids.Contains(a.ReservaId))
            .ToDictionaryAsync(a => a.ReservaId, a => a.Id);
        return View(reservas);
    }

    /// <summary>Formulario de reserva. Desde Disponibilidad llegan el vehículo y las fechas; desde Nuevo cliente, el cliente.</summary>
    public async Task<IActionResult> Editar(int id = 0, int vehiculoId = 0, int clienteId = 0, DateTime? inicio = null, DateTime? fin = null)
    {
        await OpcionesAsync();
        if (id == 0)
        {
            var ahora = Presentacion.Local(DateTimeOffset.UtcNow);
            return View(new ReservaForm { ClienteId = clienteId, VehiculoId = vehiculoId, Inicio = inicio ?? ahora, Fin = fin ?? ahora.AddDays(1) });
        }
        var r = await db.Reservas.FindAsync(id);
        return r is null ? NotFound() : View(new ReservaForm
        {
            Id = r.Id,
            ClienteId = r.ClienteId,
            VehiculoId = r.VehiculoId,
            Inicio = Presentacion.Local(r.InicioUtc),
            Fin = Presentacion.Local(r.FinUtc),
            Version = Convert.ToBase64String(r.Version)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(ReservaForm form)
    {
        await OpcionesAsync();
        if (!ModelState.IsValid) return View(form);
        try
        {
            await agenda.GuardarReservaAsync(form.Id == 0 ? null : form.Id, form.ClienteId, form.VehiculoId,
                Presentacion.Utc(form.Inicio), Presentacion.Utc(form.Fin), Presentacion.Version(form.Version), Actor);
            TempData["Exito"] = form.Id == 0 ? "Reserva confirmada. Cuando el cliente llegue, pulsa Entregar." : "Reserva actualizada.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is ReglaNegocioException or DbUpdateException)
        {
            ErrorFormulario(ex);
            return View(form);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Cancelar(int id, string version)
    {
        try
        {
            await agenda.CancelarReservaAsync(id, Presentacion.Version(version), Actor);
            TempData["Exito"] = "Reserva cancelada.";
        }
        catch (Exception ex) when (ex is ReglaNegocioException or DbUpdateException)
        {
            TempData["Error"] = ex is ReglaNegocioException ? ex.Message : "No se pudo cancelar. Recarga la página.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task OpcionesAsync()
    {
        var clientes = await db.Clientes.Where(x => x.Activo).OrderBy(x => x.Nombre)
            .Select(x => new { x.Id, Nombre = x.Nombre + " " + x.Apellido }).ToListAsync();
        var vehiculos = await db.Vehiculos.OrderBy(x => x.Placa)
            .Select(x => new { x.Id, Nombre = x.Placa + " - " + x.Marca + " " + x.Modelo }).ToListAsync();
        ViewBag.Clientes = new SelectList(clientes, "Id", "Nombre");
        ViewBag.Vehiculos = new SelectList(vehiculos, "Id", "Nombre");
    }
}
