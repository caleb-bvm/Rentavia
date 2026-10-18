using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rentavia.Application.Services;
using Rentavia.Infrastructure.Data;
using Rentavia.Infrastructure.Services;
using Rentavia.Web.Models;

namespace Rentavia.Web.Controllers;

/// <summary>Entrega, devolución y pagos. Las reglas viven en AlquilerService.</summary>
public class AlquileresController(RentaviaDbContext db, AlquilerService servicio) : ProyectoController
{
    public async Task<IActionResult> Index() => View(await db.Alquileres.AsNoTracking()
        .Include(a => a.Vehiculo).Include(a => a.Reserva).ThenInclude(r => r.Cliente).Include(a => a.Pagos)
        .OrderByDescending(a => a.Id).ToListAsync());

    public async Task<IActionResult> Detalle(int id)
    {
        var alquiler = await db.Alquileres.AsNoTracking()
            .Include(a => a.Vehiculo).Include(a => a.Reserva).ThenInclude(r => r.Cliente).Include(a => a.Pagos)
            .SingleOrDefaultAsync(a => a.Id == id);
        return alquiler is null ? NotFound() : View(alquiler);
    }

    public async Task<IActionResult> Entregar(int reservaId)
    {
        var r = await db.Reservas.Include(r => r.Vehiculo).SingleOrDefaultAsync(r => r.Id == reservaId);
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
        var a = await db.Alquileres.Include(a => a.Vehiculo).SingleOrDefaultAsync(a => a.Id == id);
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
}
