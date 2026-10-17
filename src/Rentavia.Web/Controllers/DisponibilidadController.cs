using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rentavia.Application.Services;
using Rentavia.Infrastructure.Data;
using Rentavia.Infrastructure.Services;
using Rentavia.Web.Models;

namespace Rentavia.Web.Controllers;

/// <summary>Búsqueda de vehículos libres por fechas, capacidad, categoría y sucursal.</summary>
public class DisponibilidadController(RentaviaDbContext db, AgendaService agenda) : ProyectoController
{
    public async Task<IActionResult> Index([Bind("Inicio,Fin,Pasajeros,Maletas,CategoriaId,SucursalId")] DisponibilidadForm form)
    {
        ViewBag.Categorias = new SelectList(await db.Categorias.OrderBy(x => x.Nombre).ToListAsync(), "Id", "Nombre");
        ViewBag.Sucursales = new SelectList(await db.Sucursales.Where(x => x.Activa).OrderBy(x => x.Nombre).ToListAsync(), "Id", "Nombre");
        if (ModelState.IsValid)
        {
            try
            {
                var query = agenda.Disponibles(Presentacion.Utc(form.Inicio), Presentacion.Utc(form.Fin))
                    .Where(v => v.CapacidadPasajeros >= form.Pasajeros && v.CapacidadMaletas >= form.Maletas);
                if (form.CategoriaId.HasValue) query = query.Where(v => v.CategoriaId == form.CategoriaId);
                if (form.SucursalId.HasValue) query = query.Where(v => v.SucursalId == form.SucursalId);
                form.Vehiculos = await query.OrderBy(v => v.TarifaDiaria).ToListAsync();
            }
            catch (ReglaNegocioException ex) { ErrorFormulario(ex); }
        }
        return View(form);
    }
}
