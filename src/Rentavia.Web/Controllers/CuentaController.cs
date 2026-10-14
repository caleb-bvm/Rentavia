using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rentavia.Web.Models;

namespace Rentavia.Web.Controllers;

/// <summary>Inicio y cierre de sesión. No existe registro público: las cuentas las crea un administrador.</summary>
public class CuentaController(SignInManager<IdentityUser> signIn) : Controller
{
    [AllowAnonymous]
    public IActionResult Ingresar(string? returnUrl) => View(new LoginForm { ReturnUrl = returnUrl });
    [HttpPost, AllowAnonymous, EnableRateLimiting("login")]
    public async Task<IActionResult> Ingresar(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var resultado = await signIn.PasswordSignInAsync(form.Correo.Trim(), form.Clave, false, lockoutOnFailure: true);
        if (resultado.Succeeded) return LocalRedirect(Url.IsLocalUrl(form.ReturnUrl) ? form.ReturnUrl! : "/");
        ModelState.AddModelError("", resultado.IsLockedOut ? "La cuenta está temporalmente bloqueada. Inténtalo más tarde." : "Correo o contraseña incorrectos.");
        form.Clave = "";
        return View(form);
    }
    [HttpPost, Authorize]
    public async Task<IActionResult> Salir()
    {
        await signIn.SignOutAsync();
        return RedirectToAction(nameof(Ingresar));
    }

    public IActionResult SinPermiso()
    {
        Response.StatusCode = 403;
        return View();
    }
}
