using Rentavia.Domain.Entities;

namespace Rentavia.Web.Models;

public class PanelModel
{
    public int VehiculosActivos { get; set; }
    public int ClientesActivos { get; set; }
    public int ReservasConfirmadas { get; set; }
    public int AlquileresAbiertos { get; set; }
    public decimal IngresosRegistrados { get; set; }
    public List<Alquiler> Devoluciones { get; set; } = [];
}

public class ReporteModel
{
    // Últimos 30 días contando hoy, igual que el atajo "Últimos 30 días" del filtro
    public DateOnly Desde { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-29));
    public DateOnly Hasta { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public decimal Ingresos { get; set; }
    public int Reservas { get; set; }
    public int Alquileres { get; set; }
    public decimal CostoMantenimiento { get; set; }
    public List<Pago> Pagos { get; set; } = [];
    public List<UsoVehiculo> Uso { get; set; } = [];
}

public record UsoVehiculo(string Placa, decimal Horas, decimal Porcentaje);
