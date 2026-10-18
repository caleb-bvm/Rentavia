using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rentavia.Application.Services;
using Rentavia.Infrastructure.Data;
using Rentavia.Infrastructure.Services;
using Rentavia.Web.Models;

namespace Rentavia.Web.Controllers;

/// <summary>Mantenimientos preventivos y correctivos. Programar uno bloquea la agenda del vehículo.</summary>
public class MantenimientosController(RentaviaDbContext db, AgendaService agenda) : ProyectoController
{
    public async Task<IActionResult> Index() => View(await db.Mantenimientos.AsNoTracking()
        .Include(m => m.Vehiculo).OrderByDescending(m => m.Id).ToListAsync());

    public async Task<IActionResult> Crear()
    {
        await OpcionesAsync();
        return View(new MantenimientoForm());
    }

    [HttpPost]
    public async Task<IActionResult> Crear(MantenimientoForm form)
    {
        await OpcionesAsync();
        if (!ModelState.IsValid) return View(form);
        try
        {
            await agenda.ProgramarMantenimientoAsync(form.VehiculoId, Presentacion.Utc(form.Inicio),
                Presentacion.Utc(form.Fin), form.Tipo, form.Descripcion, form.Costo, Actor);
            TempData["Exito"] = "Mantenimiento programado; agenda bloqueada.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is ReglaNegocioException or DbUpdateException)
        {
            ErrorFormulario(ex);
            return View(form);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Finalizar(int id)
    {
        try
        {
            await agenda.FinalizarMantenimientoAsync(id, Actor);
            TempData["Exito"] = "Mantenimiento finalizado; agenda liberada.";
        }
        catch (Exception ex) when (ex is ReglaNegocioException or DbUpdateException)
        {
            TempData["Error"] = ex is ReglaNegocioException ? ex.Message : "No se pudo finalizar.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task OpcionesAsync()
    {
        var vehiculos = await db.Vehiculos.OrderBy(x => x.Placa)
            .Select(x => new { x.Id, Nombre = x.Placa + " - " + x.Modelo }).ToListAsync();
        ViewBag.Vehiculos = new SelectList(vehiculos, "Id", "Nombre");
    }
}
