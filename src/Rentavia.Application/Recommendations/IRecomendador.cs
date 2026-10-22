namespace Rentavia.Application.Recommendations;

// Guarda las preferencias que ingresa el usuario para buscar un vehículo.
// Incluye la cantidad de pasajeros, maletas, presupuesto diario y preferencias adicionales.
public record PreferenciasViaje(int Pasajeros, int Maletas, decimal PresupuestoDiario, string Preferencias);

// Representa los vehículos disponibles que pueden ser recomendados.
// Contiene el identificador, nombre, capacidad, transmisión y tarifa diaria.
public record CandidatoVehiculo(int Id, string Nombre, int Pasajeros, int Maletas,
    string Transmision, decimal TarifaDiaria);

// Guarda el vehículo recomendado y el motivo por el cual fue seleccionado.
public record SugerenciaVehiculo(int VehiculoId, string Motivo);

// Almacena el resultado de la recomendación.
// Incluye la lista de vehículos sugeridos, el origen de la recomendación y un mensaje.
public record ResultadoRecomendacion(List<SugerenciaVehiculo> Sugerencias, string Origen, string Mensaje);

// Define las funciones que debe cumplir el servicio de recomendaciones.
// Permite utilizar o reemplazar el servicio de inteligencia artificial.
public interface IRecomendador
{
    // Recibe las preferencias del usuario y la lista de vehículos disponibles.
    // Devuelve las recomendaciones de forma asíncrona.
    // CancellationToken permite cancelar la operación si es necesario.
    Task<ResultadoRecomendacion> RecomendarAsync(PreferenciasViaje preferencias,
        List<CandidatoVehiculo> candidatos, CancellationToken cancellationToken = default);
}
