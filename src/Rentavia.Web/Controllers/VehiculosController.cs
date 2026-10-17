using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rentavia.Application.Services;
using Rentavia.Domain.Entities;
using Rentavia.Infrastructure.Data;
using Rentavia.Web.Models;

namespace Rentavia.Web.Controllers;

/// <summary>
/// CRUD de la flota con fotografía opcional. Un vehículo con historial se retira en lugar de eliminarse.
/// </summary>
public class VehiculosController(RentaviaDbContext db, IWebHostEnvironment entorno) : ProyectoController
{
    public async Task<IActionResult> Index(string? buscar) => View(await db.Vehiculos.AsNoTracking()
        .Include(v => v.Categoria).Include(v => v.Sucursal)
        .Where(v => buscar == null || v.Placa.Contains(buscar) || v.Marca.Contains(buscar) || v.Modelo.Contains(buscar))
        .OrderBy(v => v.Placa).ToListAsync());

    public async Task<IActionResult> Editar(int id = 0)
    {
        await OpcionesAsync();
        if (id == 0) return View(new VehiculoForm());
        var v = await db.Vehiculos.FindAsync(id);
        return v is null ? NotFound() : View(new VehiculoForm
        {
            Id = v.Id,
            CategoriaId = v.CategoriaId,
            SucursalId = v.SucursalId,
            Placa = v.Placa,
            Marca = v.Marca,
            Modelo = v.Modelo,
            Anio = v.Anio,
            CapacidadPasajeros = v.CapacidadPasajeros,
            CapacidadMaletas = v.CapacidadMaletas,
            Kilometraje = v.Kilometraje,
            Transmision = v.Transmision,
            Estado = v.Estado,
            TarifaDiaria = v.TarifaDiaria,
            Version = Convert.ToBase64String(v.Version),
            FotoRuta = v.FotoRuta
        });
    }

    [HttpPost, RequestSizeLimit(3_000_000)]
    public async Task<IActionResult> Editar(VehiculoForm form)
    {
        await OpcionesAsync();
        if (!await db.Categorias.AnyAsync(c => c.Id == form.CategoriaId) || !await db.Sucursales.AnyAsync(s => s.Id == form.SucursalId))
            ModelState.AddModelError("", "Selecciona categoría y sucursal existentes.");
        if (form.Anio > DateTime.Today.Year + 1)
            ModelState.AddModelError(nameof(form.Anio), "Revisa el año del vehículo.");
        if (!ModelState.IsValid) return View(form);

        var v = form.Id == 0 ? new Vehiculo() : await db.Vehiculos.FindAsync(form.Id);
        if (v is null) return NotFound();
        string? fotoNueva = null;
        try
        {
            if (form.Id != 0)
            {
                ReglasAlquiler.VersionActual(v.Version, Presentacion.Version(form.Version));
                if (form.Kilometraje < v.Kilometraje)
                    throw new ReglaNegocioException("El kilometraje no puede disminuir.");
            }
            if (form.Foto is not null) fotoNueva = await GuardarFotoAsync(form.Foto);

            v.CategoriaId = form.CategoriaId;
            v.SucursalId = form.SucursalId;
            v.Placa = form.Placa.Trim().ToUpperInvariant();
            v.Marca = form.Marca.Trim();
            v.Modelo = form.Modelo.Trim();
            v.Anio = form.Anio;
            v.CapacidadPasajeros = form.CapacidadPasajeros;
            v.CapacidadMaletas = form.CapacidadMaletas;
            v.Kilometraje = form.Kilometraje;
            v.Transmision = form.Transmision;
            v.Estado = form.Estado;
            v.TarifaDiaria = form.TarifaDiaria;
            if (fotoNueva is not null) v.FotoRuta = fotoNueva;
            if (form.Id == 0) db.Vehiculos.Add(v);

            await db.GuardarConAuditoriaAsync(Actor, form.Id == 0 ? "Crear" : "Editar", "Vehiculo", () => v.Id);
            TempData["Exito"] = "Vehículo guardado.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is DbUpdateException or ReglaNegocioException)
        {
            // Si no se guardó el registro, se borra la foto recién subida.
            if (fotoNueva is not null)
                System.IO.File.Delete(Path.Combine(entorno.WebRootPath, fotoNueva.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
            form.FotoRuta = v.FotoRuta == fotoNueva ? null : v.FotoRuta;
            ErrorFormulario(ex);
            return View(form);
        }
    }

    [HttpPost, Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Eliminar(int id, string version)
    {
        var v = await db.Vehiculos.FindAsync(id);
        if (v is null) return NotFound();
        try
        {
            ReglasAlquiler.VersionActual(v.Version, Presentacion.Version(version));
            if (await db.Reservas.AnyAsync(r => r.VehiculoId == id) || await db.Mantenimientos.AnyAsync(m => m.VehiculoId == id))
            {
                v.Estado = EstadoVehiculo.Retirado;
                TempData["Exito"] = "Vehículo retirado; se conserva su historial.";
            }
            else
            {
                db.Vehiculos.Remove(v);
                TempData["Exito"] = "Vehículo eliminado.";
            }
            db.Registrar(Actor, "EliminarORetirar", "Vehiculo", id);
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException or ReglaNegocioException)
        {
            TempData["Error"] = "No se pudo eliminar. Recarga la página o retira el vehículo desde Editar.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task OpcionesAsync()
    {
        ViewBag.Categorias = new SelectList(await db.Categorias.OrderBy(x => x.Nombre).ToListAsync(), "Id", "Nombre");
        ViewBag.Sucursales = new SelectList(await db.Sucursales.OrderBy(x => x.Nombre).ToListAsync(), "Id", "Nombre");
    }

    /// <summary>
    /// Acepta PNG o JPEG de hasta 2 MB. El tipo se comprueba por los primeros bytes del archivo
    /// y se guarda con un nombre aleatorio, nunca con el nombre que envió el usuario.
    /// </summary>
    private async Task<string> GuardarFotoAsync(IFormFile foto)
    {
        if (foto.Length is < 8 or > 2_000_000)
            throw new ReglaNegocioException("La fotografía debe pesar hasta 2 MB y ser PNG o JPEG.");
        using var buffer = new MemoryStream();
        await foto.CopyToAsync(buffer);
        var bytes = buffer.ToArray();
        var png = bytes.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpg = bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255;
        if (!png && !jpg) throw new ReglaNegocioException("Solo se permiten imágenes PNG y JPEG.");

        var carpeta = Path.Combine(entorno.WebRootPath, "uploads", "vehiculos");
        Directory.CreateDirectory(carpeta);
        var nombre = Guid.NewGuid().ToString("N") + (png ? ".png" : ".jpg");
        await System.IO.File.WriteAllBytesAsync(Path.Combine(carpeta, nombre), bytes);
        return "/uploads/vehiculos/" + nombre;
    }
}
