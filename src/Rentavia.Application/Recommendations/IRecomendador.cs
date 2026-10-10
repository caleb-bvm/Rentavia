namespace Rentavia.Application.Recommendations;

public record PreferenciasViaje(int Pasajeros, int Maletas, decimal PresupuestoDiario, string Preferencias);
public record CandidatoVehiculo(int Id, string Nombre, int Pasajeros, int Maletas,
    string Transmision, decimal TarifaDiaria);
public record SugerenciaVehiculo(int VehiculoId, string Motivo);
public record ResultadoRecomendacion(List<SugerenciaVehiculo> Sugerencias, string Origen, string Mensaje);

// Una interfaz para poder probar y reemplazar el servicio externo.
public interface IRecomendador
{
    Task<ResultadoRecomendacion> RecomendarAsync(PreferenciasViaje preferencias,
        List<CandidatoVehiculo> candidatos, CancellationToken cancellationToken = default);
}
