namespace Rentavia.Domain.Entities;

/// <summary>Servicio preventivo o correctivo. Mientras no finaliza, bloquea la agenda del vehículo.</summary>
public class Mantenimiento
{
    public int Id { get; set; }
    public int VehiculoId { get; set; }
    public Vehiculo Vehiculo { get; set; } = null!;
    public DateTimeOffset InicioUtc { get; set; }
    public DateTimeOffset FinUtc { get; set; }
    public string Tipo { get; set; } = "Preventivo";
    public string Descripcion { get; set; } = "";
    public decimal Costo { get; set; }
    public bool Finalizado { get; set; }
}
