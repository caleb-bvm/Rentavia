namespace Rentavia.Domain.Entities;

/// <summary>Pago interno registrado por un empleado. No se conecta con bancos ni pasarelas.</summary>
public class Pago
{
    public int Id { get; set; }
    public int AlquilerId { get; set; }
    public Alquiler Alquiler { get; set; } = null!;
    public DateTimeOffset FechaUtc { get; set; } = DateTimeOffset.UtcNow;
    public decimal Monto { get; set; }
    public string Metodo { get; set; } = "Efectivo";
    public string? Referencia { get; set; }
}
