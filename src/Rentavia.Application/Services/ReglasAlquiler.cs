namespace Rentavia.Application.Services;

public static class ReglasAlquiler
{
    public static int Dias(DateTimeOffset inicio, DateTimeOffset fin)
    {
        if (fin <= inicio) throw new ReglaNegocioException("La fecha final debe ser posterior al inicio.");
        if (fin - inicio > TimeSpan.FromDays(365))
            throw new ReglaNegocioException("El período máximo del prototipo es de 365 días.");
        // Parámetro del prototipo: días de 24 horas, redondeados hacia arriba.
        return (int)Math.Ceiling((fin - inicio).TotalDays);
    }

    public static decimal Total(DateTimeOffset inicio, DateTimeOffset fin, decimal tarifa)
    {
        if (tarifa <= 0) throw new ReglaNegocioException("La tarifa debe ser mayor que cero.");
        return decimal.Round(Dias(inicio, fin) * tarifa, 2);
    }

    public static bool SeCruzan(DateTimeOffset inicioA, DateTimeOffset finA,
        DateTimeOffset inicioB, DateTimeOffset finB) => inicioA < finB && inicioB < finA;

    public static void VersionActual(byte[] actual, byte[] recibida)
    {
        if (!actual.SequenceEqual(recibida))
            throw new ReglaNegocioException("Otro usuario modificó este registro. Recarga la página e inténtalo nuevamente.");
    }
}

public class ReglaNegocioException(string mensaje) : Exception(mensaje);
