using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Rentavia.Application.Recommendations;
using Rentavia.Application.Services;
using Rentavia.Domain.Entities;
using Rentavia.Infrastructure.Data;
using Rentavia.Infrastructure.Services;

var resultados = new List<object>();
var fallos = 0;
async Task Probar(string nombre, Func<Task> prueba)
{
    try { await prueba(); resultados.Add(new { nombre, estado = "Correcto" }); Console.WriteLine("OK: " + nombre); }
    catch (Exception ex) { fallos++; resultados.Add(new { nombre, estado = "Fallo", error = ex.Message }); Console.WriteLine("FALLO: " + nombre + " - " + ex.Message); }
}
void Verificar(bool condicion, string mensaje) { if (!condicion) throw new Exception(mensaje); }
async Task DebeRechazar(Func<Task> operacion)
{
    try { await operacion(); }
    catch (ReglaNegocioException) { return; }
    throw new Exception("Se aceptó una operación que debía rechazarse.");
}
var ahora = DateTimeOffset.UtcNow;
await Probar("Tarifa por 24 horas y redondeo", () =>
{
    Verificar(ReglasAlquiler.Dias(ahora, ahora.AddDays(1).AddMinutes(1)) == 2, "Redondeo incorrecto.");
    Verificar(ReglasAlquiler.Total(ahora, ahora.AddHours(2), 40) == 40, "Total incorrecto."); return Task.CompletedTask;
});
await Probar("Intervalos adyacentes no se cruzan", () =>
{
    Verificar(!ReglasAlquiler.SeCruzan(ahora, ahora.AddDays(1), ahora.AddDays(1), ahora.AddDays(2)), "Adyacencia rechazada.");
    Verificar(ReglasAlquiler.SeCruzan(ahora, ahora.AddDays(2), ahora.AddDays(1), ahora.AddDays(3)), "Cruce no detectado."); return Task.CompletedTask;
});
await Probar("Rango inválido rechazado", () => DebeRechazar(() => { ReglasAlquiler.Dias(ahora, ahora); return Task.CompletedTask; }));
await Probar("Versión anterior rechazada", () => DebeRechazar(() => { ReglasAlquiler.VersionActual([1], [2]); return Task.CompletedTask; }));
var candidatos = new List<CandidatoVehiculo> { new(1, "Toyota Corolla", 5, 2, "Manual", 40), new(2, "Hyundai Tucson", 5, 4, "Automatica", 60) };
var preferencias = new PreferenciasViaje(4, 2, 100, "viaje familiar");
OpenAiRecomendador Adaptador(HttpMessageHandler handler, string clave = "clave-de-prueba") =>
    new(new HttpClient(handler), Options.Create(new OpenAiOptions { ApiKey = clave }), NullLogger<OpenAiRecomendador>.Instance);
await Probar("OpenAI valida salida y contrato HTTP", async () =>
{
    var handler = new RespuestaHttp(async request =>
    {
        Verificar(request.RequestUri?.ToString() == "https://api.openai.com/v1/responses", "Endpoint incorrecto.");
        Verificar(request.Headers.Authorization?.Scheme == "Bearer", "Falta autenticación.");
        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
        Verificar(!body.RootElement.GetProperty("store").GetBoolean(), "Persistencia remota activada.");
        Verificar(body.RootElement.GetProperty("text").GetProperty("format").GetProperty("strict").GetBoolean(), "Falta esquema estricto.");
        return RespuestaHttp.Json("{\"status\":\"completed\",\"output\":[{\"type\":\"message\",\"content\":[{\"type\":\"output_text\",\"text\":\"{\\\"sugerencias\\\":[{\\\"vehiculoId\\\":1,\\\"motivo\\\":\\\"Capacidad adecuada\\\"}]}\"}]}]}");
    });
    var resultado = await Adaptador(handler).RecomendarAsync(preferencias, candidatos);
    Verificar(resultado.Origen == "OpenAI" && resultado.Sugerencias.Single().VehiculoId == 1, "No se interpretó la salida.");
});
await Probar("ID ajeno de IA provoca respaldo explícito", async () =>
{
    var json = JsonSerializer.Serialize(new { status = "completed", output = new[] { new { type = "message", content = new[] { new { type = "output_text", text = "{\"sugerencias\":[{\"vehiculoId\":999,\"motivo\":\"Inventado\"}]}" } } } } });
    var r = await Adaptador(new RespuestaHttp(_ => Task.FromResult(RespuestaHttp.Json(json)))).RecomendarAsync(preferencias, candidatos);
    Verificar(r.Origen == "Reglas (sin IA)" && r.Sugerencias.All(s => s.VehiculoId != 999), "Se aceptó un ID inventado.");
});
await Probar("HTTP 429 y JSON defectuoso no rompen el recomendador", async () =>
{
    foreach (var response in new[] { new HttpResponseMessage(HttpStatusCode.TooManyRequests), RespuestaHttp.Json("no es JSON") })
    {
        var r = await Adaptador(new RespuestaHttp(_ => Task.FromResult(response))).RecomendarAsync(preferencias, candidatos);
        Verificar(r.Origen == "Reglas (sin IA)", "No se activó respaldo.");
    }
});
await Probar("Timeout e identificador fuera de rango provocan respaldo", async () =>
{
    var timeout = Adaptador(new RespuestaHttp(_ => throw new TaskCanceledException()));
    Verificar((await timeout.RecomendarAsync(preferencias, candidatos)).Origen == "Reglas (sin IA)", "No manejó timeout.");
    var json = JsonSerializer.Serialize(new { status = "completed", output = new[] { new { type = "message", content = new[] { new { type = "output_text", text = "{\"sugerencias\":[{\"vehiculoId\":999999999999999999,\"motivo\":\"Inválido\"}]}" } } } } });
    var r = await Adaptador(new RespuestaHttp(_ => Task.FromResult(RespuestaHttp.Json(json)))).RecomendarAsync(preferencias, candidatos);
    Verificar(r.Origen == "Reglas (sin IA)", "No rechazó número fuera de rango.");
});
await Probar("Sin clave o sin candidatos no se consume API", async () =>
{
    var handler = new RespuestaHttp(_ => throw new Exception("No debe llamar al servicio."));
    Verificar((await Adaptador(handler, "").RecomendarAsync(preferencias, candidatos)).Origen == "Reglas (sin IA)", "Origen incorrecto.");
    Verificar((await Adaptador(handler).RecomendarAsync(preferencias, [])).Sugerencias.Count == 0, "Debe quedar vacío.");
});

if (args.Contains("--sql"))
{
    var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("RENTAVIA_TEST_CONNECTION")
        ?? "Server=.\\SQLEXPRESS;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True");
    var database = "Rentavia_Pruebas_" + Guid.NewGuid().ToString("N");
    builder.InitialCatalog = database;
    var options = new DbContextOptionsBuilder<RentaviaDbContext>().UseSqlServer(builder.ConnectionString).Options;
    RentaviaDbContext Contexto() => new(options);
    try
    {
        await using (var db = Contexto())
        {
            await db.Database.MigrateAsync();
            await DatosIniciales.CargarDatosInicialesAsync(db);
        }
        int vehiculoId, clienteId;
        await using (var db = Contexto()) { vehiculoId = await db.Vehiculos.MinAsync(v => v.Id); clienteId = await db.Clientes.MinAsync(c => c.Id); }
        await Probar("SQL: dos conexiones no confirman reservas superpuestas", async () =>
        {
            async Task<bool> Reservar()
            {
                await using var db = Contexto();
                try { await new AgendaService(db).GuardarReservaAsync(null, clienteId, vehiculoId, ahora.AddDays(2), ahora.AddDays(3), [], "prueba"); return true; }
                catch (ReglaNegocioException) { return false; }
            }
            var resultadosReserva = await Task.WhenAll(Reservar(), Reservar());
            Verificar(resultadosReserva.Count(x => x) == 1, "Deben confirmar una sola reserva.");
        });
        await Probar("SQL: tarifa histórica, edición y cancelación", async () =>
        {
            await using var db = Contexto(); var agenda = new AgendaService(db);
            var r = await db.Reservas.SingleAsync(); var tarifa = r.TarifaDiaria;
            (await db.Vehiculos.FindAsync(vehiculoId))!.TarifaDiaria = 90; await db.SaveChangesAsync();
            r = await agenda.GuardarReservaAsync(r.Id, clienteId, vehiculoId, ahora.AddDays(2), ahora.AddDays(4), r.Version, "prueba");
            Verificar(r.TarifaDiaria == tarifa && r.TotalEstimado == tarifa * 2, "Cambió la tarifa histórica.");
            await agenda.CancelarReservaAsync(r.Id, r.Version, "prueba");
            Verificar(await agenda.Disponibles(ahora.AddDays(2), ahora.AddDays(4)).AnyAsync(v => v.Id == vehiculoId), "No liberó disponibilidad.");
        });
        await Probar("SQL: mantenimiento bloquea y luego libera la agenda", async () =>
        {
            await using var db = Contexto(); var agenda = new AgendaService(db);
            var m = await agenda.ProgramarMantenimientoAsync(vehiculoId, ahora.AddDays(2), ahora.AddDays(3), "Preventivo", "Cambio de aceite", 20, "prueba");
            await DebeRechazar(() => agenda.GuardarReservaAsync(null, clienteId, vehiculoId, ahora.AddDays(2), ahora.AddDays(3), [], "prueba"));
            await agenda.FinalizarMantenimientoAsync(m.Id, "prueba");
            Verificar(await agenda.Disponibles(ahora.AddDays(2), ahora.AddDays(3)).AnyAsync(v => v.Id == vehiculoId), "No liberó agenda.");
        });
        await Probar("SQL: entrega, pago, devolución, versión y auditoría", async () =>
        {
            await using var db = Contexto(); var agenda = new AgendaService(db); var alquileres = new AlquilerService(db, agenda);
            var r = await agenda.GuardarReservaAsync(null, clienteId, vehiculoId, DateTimeOffset.UtcNow.AddMinutes(1), DateTimeOffset.UtcNow.AddDays(1), [], "prueba");
            var id = await alquileres.EntregarAsync(r.Id, r.Version, 10, 100, "Sin daños", "prueba");
            await DebeRechazar(() => alquileres.EntregarAsync(r.Id, r.Version, 10, 100, "", "prueba"));
            var a = await db.Alquileres.FindAsync(id); var version = a!.Version.ToArray();
            await alquileres.RegistrarPagoAsync(id, 20, "Efectivo", "REF-001", "prueba");
            await DebeRechazar(() => alquileres.RegistrarPagoAsync(id, 100000, "Efectivo", null, "prueba"));
            await DebeRechazar(() => alquileres.DevolverAsync(id, version, 5, 100, "", 0, null, "prueba"));
            await alquileres.DevolverAsync(id, version, 100, 80, "Inspección final", 10, "Limpieza interior", "prueba");
            Verificar(a.Estado == EstadoAlquiler.Cerrado && a.MontoTotal == r.TotalEstimado + 10, "Cierre incorrecto.");
            Verificar(await db.Ocupaciones.CountAsync(o => o.ReservaId == r.Id) == 1, "Duplicó ocupación.");
            Verificar(!await db.Ocupaciones.AnyAsync(o => o.ReservaId == r.Id && o.Activa), "Agenda no liberada.");
            Verificar(await db.Auditorias.AnyAsync(e => e.Accion == "Devolver" && e.RegistroId == id), "Falta auditoría.");
        });
        await Probar("SQL: mantenimiento vencido sin finalizar no se ofrece", async () =>
        {
            await using var db = Contexto(); var agenda = new AgendaService(db);
            var m = await agenda.ProgramarMantenimientoAsync(vehiculoId, ahora.AddDays(10), ahora.AddDays(11), "Correctivo", "Prueba de retraso", 0, "prueba");
            // Simulamos que pasó el tiempo sin finalizar el servicio.
            m.InicioUtc = ahora.AddDays(-2); m.FinUtc = ahora.AddDays(-1);
            var ocupacion = await db.Ocupaciones.SingleAsync(o => o.MantenimientoId == m.Id);
            ocupacion.InicioUtc = m.InicioUtc; ocupacion.FinUtc = m.FinUtc; await db.SaveChangesAsync();
            Verificar(!await agenda.Disponibles(ahora.AddDays(20), ahora.AddDays(21)).AnyAsync(v => v.Id == vehiculoId), "Ofreció vehículo aún en taller.");
            await agenda.FinalizarMantenimientoAsync(m.Id, "prueba");
        });
        await Probar("SQL: edición concurrente protegida por rowversion", async () =>
        {
            await using var db1 = Contexto(); await using var db2 = Contexto();
            var v1 = await db1.Vehiculos.FindAsync(vehiculoId); var v2 = await db2.Vehiculos.FindAsync(vehiculoId);
            v1!.Modelo = "Edición uno"; await db1.SaveChangesAsync(); v2!.Modelo = "Edición dos";
            try { await db2.SaveChangesAsync(); throw new Exception("Sobrescribió una versión anterior."); }
            catch (DbUpdateConcurrencyException) { }
        });
    }
    finally
    {
        // Solo eliminamos la base temporal creada por esta ejecución.
        if (!database.StartsWith("Rentavia_Pruebas_") || database.Length != "Rentavia_Pruebas_".Length + 32) throw new Exception("Nombre temporal inesperado.");
        await using var db = Contexto(); await db.Database.EnsureDeletedAsync();
    }
}
var output = Environment.GetEnvironmentVariable("RENTAVIA_TEST_RESULT");
if (!string.IsNullOrWhiteSpace(output))
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
    await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { fechaUtc = DateTimeOffset.UtcNow, fallos, pruebas = resultados }, new JsonSerializerOptions { WriteIndented = true }));
}
Console.WriteLine($"Resultado: {resultados.Count - fallos} correctas, {fallos} fallos.");
return fallos == 0 ? 0 : 1;

class RespuestaHttp(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);
    public static HttpResponseMessage Json(string texto) => new(HttpStatusCode.OK) { Content = new StringContent(texto, Encoding.UTF8, "application/json") };
}
