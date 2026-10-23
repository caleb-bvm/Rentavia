using Rentavia.Domain.Entities;

namespace Rentavia.Web.Models;

public class PanelModel
{
    public int VehiculosLibres { get; set; }
    public int ReservasPorEntregar { get; set; }
    public int AlquileresAbiertos { get; set; }
    public int DevolucionesAtrasadas { get; set; }
    public List<Reserva> Entregas { get; set; } = [];
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
