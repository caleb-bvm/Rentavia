namespace Rentavia.Domain.Entities;

/// <summary>Historial de cada consulta al recomendador: entrada, candidatos, resultado y origen (OpenAI o reglas).</summary>
public class Recomendacion
{
    public int Id { get; set; }
    public DateTimeOffset FechaUtc { get; set; } = DateTimeOffset.UtcNow;
    public string UsuarioId { get; set; } = "";
    public string EntradaJson { get; set; } = "";
    public string CandidatosJson { get; set; } = "";
    public string ResultadoJson { get; set; } = "";
    public string Origen { get; set; } = "";
    public string Modelo { get; set; } = "";
    public string VersionPrompt { get; set; } = "rentavia-v1";
}
