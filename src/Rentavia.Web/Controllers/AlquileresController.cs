using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rentavia.Application.Services;
using Rentavia.Domain.Entities;
using Rentavia.Infrastructure.Data;
using Rentavia.Infrastructure.Services;
using Rentavia.Web.Models;

namespace Rentavia.Web.Controllers;

/// <summary>Entrega, devolución y pagos. Las reglas viven en AlquilerService.</summary>
public class AlquileresController(RentaviaDbContext db, AlquilerService servicio) : ProyectoController
{
    /// <summary>Listado filtrado por estado. Por defecto muestra los alquileres en curso, la devolución más próxima primero.</summary>
    public async Task<IActionResult> Index(string? estado)
    {
        estado ??= "en-curso";
        var ahora = DateTimeOffset.UtcNow;
        var abiertos = db.Alquileres.Where(a => a.Estado == EstadoAlquiler.Abierto);
        ViewBag.Pestanas = new Pestanas(estado,
        [
            new("en-curso", "En curso", await abiertos.CountAsync()),
            new("atrasados", "Atrasados", await abiertos.CountAsync(a => a.Reserva.FinUtc < ahora)),
            new("cerrados", "Cerrados", await db.Alquileres.CountAsync(a => a.Estado == EstadoAlquiler.Cerrado)),
            new("todos", "Todos", await db.Alquileres.CountAsync())
        ]);
        var query = db.Alquileres.AsNoTracking()
            .Include(a => a.Vehiculo).Include(a => a.Reserva).ThenInclude(r => r.Cliente).Include(a => a.Pagos).AsQueryable();
        query = estado switch
        {
            "en-curso" => query.Where(a => a.Estado == EstadoAlquiler.Abierto).OrderBy(a => a.Reserva.FinUtc),
            "atrasados" => query.Where(a => a.Estado == EstadoAlquiler.Abierto && a.Reserva.FinUtc < ahora).OrderBy(a => a.Reserva.FinUtc),
            "cerrados" => query.Where(a => a.Estado == EstadoAlquiler.Cerrado).OrderByDescending(a => a.Id),
            _ => query.OrderByDescending(a => a.Id)
        };
        return View(await query.ToListAsync());
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var alquiler = await db.Alquileres.AsNoTracking()
            .Include(a => a.Vehiculo).Include(a => a.Reserva).ThenInclude(r => r.Cliente).Include(a => a.Pagos)
            .SingleOrDefaultAsync(a => a.Id == id);
        return alquiler is null ? NotFound() : View(alquiler);
    }

    public async Task<IActionResult> Entregar(int reservaId)
    {
        var r = await ReservaAsync(reservaId);
        return r is null ? NotFound() : View(new EntregaForm
        {
            ReservaId = r.Id,
            Version = Convert.ToBase64String(r.Version),
            Kilometraje = r.Vehiculo.Kilometraje
        });
    }

    [HttpPost]
    public async Task<IActionResult> Entregar(EntregaForm form)
    {
        await ReservaAsync(form.ReservaId);
        if (!ModelState.IsValid) return View(form);
        try
        {
            var id = await servicio.EntregarAsync(form.ReservaId, Presentacion.Version(form.Version),
                form.Kilometraje, form.Combustible, form.Observaciones, Actor);
            TempData["Exito"] = "Entrega registrada.";
            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (Exception ex) when (ex is ReglaNegocioException or DbUpdateException)
        {
            ErrorFormulario(ex);
            return View(form);
        }
    }

    public async Task<IActionResult> Devolver(int id)
    {
        var a = await AlquilerAsync(id);
        return a is null ? NotFound() : View(new DevolucionForm
        {
            Id = id,
            Version = Convert.ToBase64String(a.Version),
            Kilometraje = a.Vehiculo.Kilometraje,
            Combustible = a.CombustibleInicial
        });
    }

    [HttpPost]
    public async Task<IActionResult> Devolver(DevolucionForm form)
    {
        await AlquilerAsync(form.Id);
        if (!ModelState.IsValid) return View(form);
        try
        {
            await servicio.DevolverAsync(form.Id, Presentacion.Version(form.Version), form.Kilometraje,
                form.Combustible, form.Observaciones, form.Cargo, form.Concepto, Actor);
            TempData["Exito"] = "Devolución registrada; alquiler cerrado.";
            return RedirectToAction(nameof(Detalle), new { id = form.Id });
        }
        catch (Exception ex) when (ex is ReglaNegocioException or DbUpdateException)
        {
            ErrorFormulario(ex);
            return View(form);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Pago(PagoForm form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revisa monto, método y referencia del pago.";
            return RedirectToAction(nameof(Detalle), new { id = form.AlquilerId });
        }
        try
        {
            await servicio.RegistrarPagoAsync(form.AlquilerId, form.Monto, form.Metodo, form.Referencia, Actor);
            TempData["Exito"] = "Pago registrado.";
        }
        catch (Exception ex) when (ex is ReglaNegocioException or DbUpdateException)
        {
            TempData["Error"] = ex is ReglaNegocioException ? ex.Message : "No se pudo registrar el pago.";
        }
        return RedirectToAction(nameof(Detalle), new { id = form.AlquilerId });
    }

    // Resumen que acompaña a los formularios de entrega y devolución: quién, qué vehículo y cuándo.
    // Sin seguimiento, para no interferir con la verificación de versión que hace AlquilerService.
    private async Task<Reserva?> ReservaAsync(int id)
    {
        var r = await db.Reservas.AsNoTracking().Include(r => r.Vehiculo).Include(r => r.Cliente).SingleOrDefaultAsync(r => r.Id == id);
        ViewBag.Reserva = r;
        return r;
    }

    private async Task<Alquiler?> AlquilerAsync(int id)
    {
        var a = await db.Alquileres.AsNoTracking().Include(a => a.Vehiculo)
            .Include(a => a.Reserva).ThenInclude(r => r.Cliente)
            .Include(a => a.Reserva).ThenInclude(r => r.Vehiculo)
            .SingleOrDefaultAsync(a => a.Id == id);
        ViewBag.Alquiler = a;
        return a;
    }
}
