namespace Rentavia.Domain.Entities;

public enum EstadoAlquiler { Abierto, Cerrado }

/// <summary>Renta en curso o cerrada. Guarda la inspección de entrega y devolución, el cargo adicional y el total.</summary>
public class Alquiler
{
    public int Id { get; set; }
    public int ReservaId { get; set; }
    public Reserva Reserva { get; set; } = null!;
    public int VehiculoId { get; set; }
    public Vehiculo Vehiculo { get; set; } = null!;
    public DateTimeOffset EntregaUtc { get; set; }
    public DateTimeOffset? DevolucionUtc { get; set; }
    public int KilometrajeInicial { get; set; }
    public int? KilometrajeFinal { get; set; }
    public int CombustibleInicial { get; set; }
    public int? CombustibleFinal { get; set; }
    public string ObservacionesEntrega { get; set; } = "";
    public string? ObservacionesDevolucion { get; set; }
    public decimal CargoAdicional { get; set; }
    public string? ConceptoCargo { get; set; }
    public decimal MontoTotal { get; set; }
    public EstadoAlquiler Estado { get; set; } = EstadoAlquiler.Abierto;
    public byte[] Version { get; set; } = [];
    public List<Pago> Pagos { get; set; } = [];
}
