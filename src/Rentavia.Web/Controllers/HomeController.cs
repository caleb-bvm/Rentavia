using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Rentavia.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Rentavia.Domain.Entities;
using Rentavia.Infrastructure.Data;

namespace Rentavia.Web.Controllers;

/// <summary>Panel de inicio con cifras consultadas en la base y próximas devoluciones.</summary>
public class HomeController(RentaviaDbContext db) : ProyectoController
{
    public async Task<IActionResult> Index()
    {
        return View(new PanelModel
        {
            VehiculosActivos = await db.Vehiculos.CountAsync(v => v.Estado == EstadoVehiculo.Activo),
            ClientesActivos = await db.Clientes.CountAsync(c => c.Activo),
            ReservasConfirmadas = await db.Reservas.CountAsync(r => r.Estado == EstadoReserva.Confirmada),
            AlquileresAbiertos = await db.Alquileres.CountAsync(a => a.Estado == EstadoAlquiler.Abierto),
            IngresosRegistrados = await db.Pagos.SumAsync(p => p.Monto),
            Devoluciones = await db.Alquileres.AsNoTracking().Include(a => a.Vehiculo).Include(a => a.Reserva)
                .Where(a => a.Estado == EstadoAlquiler.Abierto).OrderBy(a => a.Reserva.FinUtc).Take(8).ToListAsync()
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
