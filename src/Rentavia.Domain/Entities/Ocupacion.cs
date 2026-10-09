namespace Rentavia.Domain.Entities;

/// <summary>
/// Intervalo [InicioUtc, FinUtc) en que un vehículo está ocupado. Una sola tabla reúne
/// reservas, alquileres (reutilizan la ocupación de su reserva) y mantenimientos.
/// </summary>
public class Ocupacion
{
    public int Id { get; set; }
    public int VehiculoId { get; set; }
    public Vehiculo Vehiculo { get; set; } = null!;
    public int? ReservaId { get; set; }
    public Reserva? Reserva { get; set; }
    public int? MantenimientoId { get; set; }
    public Mantenimiento? Mantenimiento { get; set; }
    public DateTimeOffset InicioUtc { get; set; }
    public DateTimeOffset FinUtc { get; set; }
    public bool Activa { get; set; } = true;
}
