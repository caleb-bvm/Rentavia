namespace Rentavia.Web.Models;

public static class Presentacion
{
    // La agencia piloto usa UTC-6, sin horario de verano.
    public static DateTimeOffset Utc(DateTime local)
    {
        if (local.Year is < 2000 or > 2100)
            throw new Rentavia.Application.Services.ReglaNegocioException("Selecciona fechas entre los años 2000 y 2100.");
        return new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), TimeSpan.FromHours(-6)).ToUniversalTime();
    }
    public static DateTime Local(DateTimeOffset utc) => utc.ToOffset(TimeSpan.FromHours(-6)).DateTime;
    public static string Fecha(DateTimeOffset utc) => Local(utc).ToString("dd/MM/yyyy HH:mm");
    public static string Dinero(decimal monto) => $"USD {monto:N2}";
    public static byte[] Version(string? texto)
    {
        try { return Convert.FromBase64String(texto ?? ""); }
        catch (FormatException) { return []; }
    }
}

/// <summary>Pestaña de filtro por estado en los listados; la clave viaja en ?estado=.</summary>
public record Pestana(string Clave, string Texto, int Cantidad);
public record Pestanas(string Actual, IReadOnlyList<Pestana> Opciones);
