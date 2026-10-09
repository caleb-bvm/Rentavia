namespace Rentavia.Domain.Entities;

public enum EstadoVehiculo { Activo, FueraDeServicio, Retirado }
public enum Transmision { Manual, Automatica }

/// <summary>Vehículo de la flota. El estado indica si puede rentarse; la disponibilidad por fechas sale de Ocupaciones.</summary>
public class Vehiculo
{
    public int Id { get; set; }
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
    public int SucursalId { get; set; }
    public Sucursal Sucursal { get; set; } = null!;
    public string Placa { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Anio { get; set; }
    public int CapacidadPasajeros { get; set; }
    public int CapacidadMaletas { get; set; }
    public int Kilometraje { get; set; }
    public Transmision Transmision { get; set; }
    public EstadoVehiculo Estado { get; set; } = EstadoVehiculo.Activo;
    public byte[] Version { get; set; } = [];
    public decimal TarifaDiaria { get; set; }
    public string? FotoRuta { get; set; }
}
