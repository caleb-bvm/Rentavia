using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rentavia.Application.Services;
using Rentavia.Domain.Entities;
using Rentavia.Infrastructure.Data;
using Rentavia.Web.Models;

namespace Rentavia.Web.Controllers;

/// <summary>
/// CRUD de clientes. El documento (DUI o pasaporte) se guarda en mayúsculas para evitar duplicados.
/// Un cliente con reservas se desactiva en lugar de eliminarse.
/// </summary>
public class ClientesController(RentaviaDbContext db) : ProyectoController
{
    public async Task<IActionResult> Index(string? buscar) => View(await db.Clientes.AsNoTracking()
        .Where(c => buscar == null || c.Nombre.Contains(buscar) || c.Apellido.Contains(buscar) || c.NumeroDocumento.Contains(buscar))
        .OrderBy(c => c.Apellido).ThenBy(c => c.Nombre).ToListAsync());

    /// <summary>
    /// Formulario de cliente. Con <paramref name="volver"/> (por ejemplo, desde una reserva nueva)
    /// al guardar se regresa a esa pantalla con el cliente ya elegido.
    /// </summary>
    public async Task<IActionResult> Editar(int id = 0, string? volver = null)
    {
        ViewBag.Volver = Url.IsLocalUrl(volver) ? volver : null;
        if (id == 0) return View(new ClienteForm());
        var c = await db.Clientes.FindAsync(id);
        return c is null ? NotFound() : View(new ClienteForm
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Apellido = c.Apellido,
            TipoDocumento = c.TipoDocumento == ClienteForm.Dui ? ClienteForm.Dui : ClienteForm.Pasaporte,
            NumeroDui = c.TipoDocumento == ClienteForm.Dui ? c.NumeroDocumento : null,
            NumeroPasaporte = c.TipoDocumento == ClienteForm.Dui ? null : c.NumeroDocumento,
            Telefono = c.Telefono,
            Correo = c.Correo,
            Activo = c.Activo,
            Version = Convert.ToBase64String(c.Version)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(ClienteForm form, string? volver)
    {
        ViewBag.Volver = Url.IsLocalUrl(volver) ? volver : null;
        if (!ModelState.IsValid) return View(form);
        var c = form.Id == 0 ? new Cliente { FechaRegistroUtc = DateTimeOffset.UtcNow } : await db.Clientes.FindAsync(form.Id);
        if (c is null) return NotFound();
        try
        {
            // Si otro usuario guardó primero, la versión ya no coincide y se avisa.
            if (form.Id != 0) ReglasAlquiler.VersionActual(c.Version, Presentacion.Version(form.Version));

            c.Nombre = form.Nombre.Trim();
            c.Apellido = form.Apellido.Trim();
            c.TipoDocumento = form.TipoDocumento;
            c.NumeroDocumento = form.NumeroDocumento;
            c.Telefono = form.Telefono.Trim();
            c.Correo = form.Correo?.Trim();
            c.Activo = form.Activo;
            if (form.Id == 0) db.Clientes.Add(c);

            await db.GuardarConAuditoriaAsync(Actor, form.Id == 0 ? "Crear" : "Editar", "Cliente", () => c.Id);
            if (ViewBag.Volver is string destino)
            {
                TempData["Exito"] = "Cliente guardado; ya está seleccionado.";
                return LocalRedirect(destino + (destino.Contains('?') ? "&" : "?") + "clienteId=" + c.Id);
            }
            TempData["Exito"] = "Cliente guardado.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is DbUpdateException or ReglaNegocioException)
        {
            ErrorFormulario(ex);
            return View(form);
        }
    }

    [HttpPost, Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Eliminar(int id, string version)
    {
        var c = await db.Clientes.FindAsync(id);
        if (c is null) return NotFound();
        try
        {
            ReglasAlquiler.VersionActual(c.Version, Presentacion.Version(version));
            if (await db.Reservas.AnyAsync(r => r.ClienteId == id))
            {
                c.Activo = false;
                TempData["Exito"] = "Cliente desactivado; se conserva su historial.";
            }
            else
            {
                db.Clientes.Remove(c);
                TempData["Exito"] = "Cliente eliminado.";
            }
            db.Registrar(Actor, "EliminarODesactivar", "Cliente", id);
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException or ReglaNegocioException)
        {
            TempData["Error"] = "No se pudo eliminar. Recarga la página o desactiva el cliente.";
        }
        return RedirectToAction(nameof(Index));
    }
}
