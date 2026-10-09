namespace Rentavia.Domain.Entities;

public enum EstadoReserva { Confirmada, Cancelada, Convertida }

/// <summary>Reserva de un vehículo para un cliente. Conserva la tarifa acordada aunque cambie el catálogo.</summary>
public class Reserva
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public int VehiculoId { get; set; }
    public Vehiculo Vehiculo { get; set; } = null!;
    public DateTimeOffset InicioUtc { get; set; }
    public DateTimeOffset FinUtc { get; set; }
    public DateTimeOffset CreadaUtc { get; set; } = DateTimeOffset.UtcNow;
    public decimal TarifaDiaria { get; set; }
    public int Dias { get; set; }
    public decimal TotalEstimado { get; set; }
    public EstadoReserva Estado { get; set; } = EstadoReserva.Confirmada;
    public byte[] Version { get; set; } = [];
}
