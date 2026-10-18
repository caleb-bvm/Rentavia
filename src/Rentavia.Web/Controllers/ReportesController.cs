using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rentavia.Infrastructure.Data;
using Rentavia.Web.Models;

namespace Rentavia.Web.Controllers;

/// <summary>Reportes por rango de fechas y bitácora de auditoría. Solo administradores.</summary>
[Authorize(Roles = "Administrador")]
public class ReportesController(RentaviaDbContext db) : ProyectoController
{
    public async Task<IActionResult> Index(DateOnly? desde, DateOnly? hasta)
    {
        var model = new ReporteModel { Desde = desde ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30)), Hasta = hasta ?? DateOnly.FromDateTime(DateTime.Today) };
        if (!ModelState.IsValid || model.Desde.Year < 2000 || model.Hasta.Year > 2100
            || model.Hasta < model.Desde || model.Hasta.DayNumber - model.Desde.DayNumber > 365)
        { ModelState.AddModelError("", "Selecciona un período válido de hasta un año."); return View(model); }
        var inicio = Presentacion.Utc(model.Desde.ToDateTime(TimeOnly.MinValue));
        var fin = Presentacion.Utc(model.Hasta.AddDays(1).ToDateTime(TimeOnly.MinValue));
        model.Pagos = await db.Pagos.AsNoTracking().Where(p => p.FechaUtc >= inicio && p.FechaUtc < fin).OrderByDescending(p => p.FechaUtc).ToListAsync();
        model.Ingresos = model.Pagos.Sum(p => p.Monto);
        model.Reservas = await db.Reservas.CountAsync(r => r.CreadaUtc >= inicio && r.CreadaUtc < fin);
        model.Alquileres = await db.Alquileres.CountAsync(a => a.EntregaUtc >= inicio && a.EntregaUtc < fin);
        model.CostoMantenimiento = await db.Mantenimientos.Where(m => m.InicioUtc >= inicio && m.InicioUtc < fin).SumAsync(m => m.Costo);
        var vehiculos = await db.Vehiculos.AsNoTracking().OrderBy(v => v.Placa).ToListAsync();
        var rentas = await db.Alquileres.AsNoTracking().Where(a => a.EntregaUtc < fin && (a.DevolucionUtc == null || a.DevolucionUtc > inicio)).ToListAsync();
        foreach (var vehiculo in vehiculos)
        {
            var horas = rentas.Where(a => a.VehiculoId == vehiculo.Id).Sum(a =>
            {
                var desdeUso = a.EntregaUtc > inicio ? a.EntregaUtc : inicio;
                var hastaUso = a.DevolucionUtc ?? DateTimeOffset.UtcNow;
                if (hastaUso > fin) hastaUso = fin;
                return Math.Max(0, (hastaUso - desdeUso).TotalHours);
            });
            model.Uso.Add(new UsoVehiculo(vehiculo.Placa, decimal.Round((decimal)horas, 2),
                decimal.Round((decimal)(horas / (fin - inicio).TotalHours * 100), 2)));
        }
        return View(model);
    }
    public async Task<IActionResult> Auditoria() => View(await db.Auditorias.AsNoTracking().OrderByDescending(x => x.FechaUtc).Take(200).ToListAsync());
}
