using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rentavia.Domain.Entities;
using Rentavia.Infrastructure.Data;
using Rentavia.Web.Models;

namespace Rentavia.Web.Controllers;

/// <summary>CRUD de sucursales. Una sucursal inactiva deja de ofrecer sus vehículos.</summary>
public class SucursalesController(RentaviaDbContext db) : ProyectoController
{
    public async Task<IActionResult> Index() =>
        View(await db.Sucursales.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync());

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Editar(int id = 0)
    {
        if (id == 0) return View(new SucursalForm());
        var s = await db.Sucursales.FindAsync(id);
        return s is null ? NotFound() : View(new SucursalForm
        {
            Id = s.Id,
            Nombre = s.Nombre,
            Direccion = s.Direccion,
            Telefono = s.Telefono,
            Activa = s.Activa
        });
    }

    [HttpPost, Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Editar(SucursalForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var s = form.Id == 0 ? new Sucursal() : await db.Sucursales.FindAsync(form.Id);
        if (s is null) return NotFound();

        s.Nombre = form.Nombre.Trim();
        s.Direccion = form.Direccion.Trim();
        s.Telefono = form.Telefono?.Trim();
        s.Activa = form.Activa;
        if (form.Id == 0) db.Sucursales.Add(s);
        try
        {
            await db.GuardarConAuditoriaAsync(Actor, form.Id == 0 ? "Crear" : "Editar", "Sucursal", () => s.Id);
            TempData["Exito"] = "Sucursal guardada.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            ErrorFormulario(ex);
            return View(form);
        }
    }

    [HttpPost, Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var s = await db.Sucursales.FindAsync(id);
        if (s is null) return NotFound();
        try
        {
            db.Sucursales.Remove(s);
            db.Registrar(Actor, "Eliminar", "Sucursal", id);
            await db.SaveChangesAsync();
            TempData["Exito"] = "Sucursal eliminada.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "La sucursal está referenciada. Puede desactivarse desde Editar.";
        }
        return RedirectToAction(nameof(Index));
    }
}
