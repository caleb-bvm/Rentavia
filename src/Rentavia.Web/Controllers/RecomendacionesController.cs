using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rentavia.Application.Recommendations;
using Rentavia.Application.Services;
using Rentavia.Domain.Entities;
using Rentavia.Infrastructure.Data;
using Rentavia.Infrastructure.Services;
using Rentavia.Web.Models;

namespace Rentavia.Web.Controllers;

/// <summary>
/// Recomendador IA: filtra candidatos disponibles, consulta OpenAI y guarda entrada,
/// candidatos y resultado para trazabilidad. La sugerencia nunca crea una reserva.
/// </summary>
public class RecomendacionesController(RentaviaDbContext db, AgendaService agenda, IRecomendador recomendador,
    IOptions<OpenAiOptions> options) : ProyectoController
{
    public IActionResult Index() => View(new RecomendacionForm());
    [HttpPost, EnableRateLimiting("ia")]
    public async Task<IActionResult> Index([Bind("Inicio,Fin,Pasajeros,Maletas,PresupuestoDiario,Preferencias")] RecomendacionForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(form);
        try
        {
            // La IA recibe hasta veinte candidatos que ya cumplen restricciones objetivas.
            form.Vehiculos = await agenda.Disponibles(Presentacion.Utc(form.Inicio), Presentacion.Utc(form.Fin))
                .Where(v => v.CapacidadPasajeros >= form.Pasajeros && v.CapacidadMaletas >= form.Maletas && v.TarifaDiaria <= form.PresupuestoDiario)
                .OrderBy(v => v.TarifaDiaria).Take(20).ToListAsync(cancellationToken);
            var candidatos = form.Vehiculos.Select(v => new CandidatoVehiculo(v.Id, v.Marca + " " + v.Modelo,
                v.CapacidadPasajeros, v.CapacidadMaletas, v.Transmision.ToString(), v.TarifaDiaria)).ToList();
            var preferencias = new PreferenciasViaje(form.Pasajeros, form.Maletas, form.PresupuestoDiario, form.Preferencias);
            form.Resultado = await recomendador.RecomendarAsync(preferencias, candidatos, cancellationToken);
            db.Recomendaciones.Add(new Recomendacion
            {
                UsuarioId = Actor,
                EntradaJson = JsonSerializer.Serialize(new { InicioUtc = Presentacion.Utc(form.Inicio), FinUtc = Presentacion.Utc(form.Fin), preferencias }),
                CandidatosJson = JsonSerializer.Serialize(candidatos),
                ResultadoJson = JsonSerializer.Serialize(form.Resultado),
                Origen = form.Resultado.Origen,
                Modelo = form.Resultado.Origen == "OpenAI" ? options.Value.Modelo : "No utilizado"
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (ReglaNegocioException ex) { ErrorFormulario(ex); }
        return View(form);
    }
}
