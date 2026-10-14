using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rentavia.Infrastructure.Data;
using Rentavia.Web.Models;

namespace Rentavia.Web.Controllers;

/// <summary>Gestión de cuentas internas con ASP.NET Core Identity. Solo para administradores.</summary>
[Authorize(Roles = "Administrador")]
public class UsuariosController(UserManager<IdentityUser> users, RoleManager<IdentityRole> roles,
    RentaviaDbContext db) : ProyectoController
{
    public async Task<IActionResult> Index() => View(await users.Users.OrderBy(x => x.Email).ToListAsync());

    public IActionResult Crear() => View(new UsuarioForm());

    [HttpPost]
    public async Task<IActionResult> Crear(UsuarioForm form)
    {
        if (form.Rol is not ("Administrador" or "Empleado"))
            ModelState.AddModelError(nameof(form.Rol), "Rol inválido.");
        if (!ModelState.IsValid) return View(form);

        // Identity usa el mismo contexto, así cuenta, rol y auditoría quedan en una transacción.
        await using var tx = await db.Database.BeginTransactionAsync();
        if (!await roles.RoleExistsAsync(form.Rol))
        {
            var roleResult = await roles.CreateAsync(new IdentityRole(form.Rol));
            if (!roleResult.Succeeded)
            {
                ModelState.AddModelError("", "No se pudo crear el rol.");
                return View(form);
            }
        }
        var user = new IdentityUser { UserName = form.Correo.Trim(), Email = form.Correo.Trim() };
        var result = await users.CreateAsync(user, form.Clave);
        if (result.Succeeded) result = await users.AddToRoleAsync(user, form.Rol);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            form.Clave = "";
            return View(form);
        }
        db.Registrar(Actor, "CrearUsuario", "Identity", 0, user.Id);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        TempData["Exito"] = "Usuario creado.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Bloquea o desbloquea una cuenta. Cambiar el security stamp cierra sus sesiones abiertas.</summary>
    [HttpPost]
    public async Task<IActionResult> Bloquear(string id)
    {
        if (id == Actor)
        {
            TempData["Error"] = "No puedes bloquear tu propia cuenta.";
            return RedirectToAction(nameof(Index));
        }
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();

        await using var tx = await db.Database.BeginTransactionAsync();
        var bloqueado = user.LockoutEnd > DateTimeOffset.UtcNow;
        var enabled = await users.SetLockoutEnabledAsync(user, true);
        var result = enabled.Succeeded
            ? await users.SetLockoutEndDateAsync(user, bloqueado ? null : DateTimeOffset.UtcNow.AddYears(10))
            : enabled;
        if (result.Succeeded) result = await users.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = "No se pudo cambiar el acceso.";
            return RedirectToAction(nameof(Index));
        }
        db.Registrar(Actor, bloqueado ? "DesbloquearUsuario" : "BloquearUsuario", "Identity", 0, user.Id);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        TempData["Exito"] = "Acceso actualizado.";
        return RedirectToAction(nameof(Index));
    }
}
