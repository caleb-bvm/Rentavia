using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Rentavia.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Rentavia.Domain.Entities;
using Rentavia.Infrastructure.Data;
using Rentavia.Infrastructure.Services;

namespace Rentavia.Web.Controllers;

/// <summary>Panel de inicio: lo que necesita atención hoy (entregas, devoluciones y atrasos) y el ciclo de renta.</summary>
public class HomeController(RentaviaDbContext db, AgendaService agenda) : ProyectoController
{
    public async Task<IActionResult> Index()
    {
        var ahora = DateTimeOffset.UtcNow;
        return View(new PanelModel
        {
            VehiculosLibres = await agenda.Disponibles(ahora, ahora.AddHours(1)).CountAsync(),
            ReservasPorEntregar = await db.Reservas.CountAsync(r => r.Estado == EstadoReserva.Confirmada),
            AlquileresAbiertos = await db.Alquileres.CountAsync(a => a.Estado == EstadoAlquiler.Abierto),
            DevolucionesAtrasadas = await db.Alquileres.CountAsync(a => a.Estado == EstadoAlquiler.Abierto && a.Reserva.FinUtc < ahora),
            Entregas = await db.Reservas.AsNoTracking().Include(r => r.Vehiculo).Include(r => r.Cliente)
                .Where(r => r.Estado == EstadoReserva.Confirmada).OrderBy(r => r.InicioUtc).Take(6).ToListAsync(),
            Devoluciones = await db.Alquileres.AsNoTracking().Include(a => a.Vehiculo).Include(a => a.Reserva)
                .Where(a => a.Estado == EstadoAlquiler.Abierto).OrderBy(a => a.Reserva.FinUtc).Take(6).ToListAsync()
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous, IgnoreAntiforgeryToken]
    public IActionResult Error()
    {
        Response.StatusCode = 500;
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
