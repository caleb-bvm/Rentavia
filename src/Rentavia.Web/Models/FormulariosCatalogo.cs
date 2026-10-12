using System.ComponentModel.DataAnnotations;
using Rentavia.Domain.Entities;

namespace Rentavia.Web.Models;

public class CategoriaForm
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Escribe el nombre."), StringLength(50)] public string Nombre { get; set; } = "";
    [StringLength(150)] public string? Descripcion { get; set; }
}

public class SucursalForm
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(100)] public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(200)] public string Direccion { get; set; } = "";
    [StringLength(20)] public string? Telefono { get; set; }
    public bool Activa { get; set; } = true;
}

public class ClienteForm
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(50)] public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(50)] public string Apellido { get; set; } = "";
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(20), Display(Name = "Tipo de documento")] public string TipoDocumento { get; set; } = "DEMO";
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(30), Display(Name = "Número de documento")] public string NumeroDocumento { get; set; } = "";
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(30)] public string Licencia { get; set; } = "";
    [DataType(DataType.Date), Display(Name = "Vencimiento de licencia")] public DateOnly VencimientoLicencia { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(1));
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(20), Display(Name = "Teléfono")] public string Telefono { get; set; } = "";
    [EmailAddress, StringLength(100)] public string? Correo { get; set; }
    public bool Activo { get; set; } = true;
    public string Version { get; set; } = "";
}

public class VehiculoForm
{
    public int Id { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Categoría")] public int CategoriaId { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Sucursal")] public int SucursalId { get; set; }
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(15)] public string Placa { get; set; } = "";
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(50)] public string Marca { get; set; } = "";
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(50)] public string Modelo { get; set; } = "";
    [Range(1980, 2100), Display(Name = "Año")] public int Anio { get; set; } = DateTime.Today.Year;
    [Range(1, 50), Display(Name = "Pasajeros")] public int CapacidadPasajeros { get; set; } = 5;
    [Range(0, 50), Display(Name = "Maletas")] public int CapacidadMaletas { get; set; } = 2;
    [Range(0, 10000000)] public int Kilometraje { get; set; }
    [EnumDataType(typeof(Transmision)), Display(Name = "Transmisión")] public Transmision Transmision { get; set; }
    [EnumDataType(typeof(EstadoVehiculo))] public EstadoVehiculo Estado { get; set; }
    [Range(typeof(decimal), "0.01", "999999"), Display(Name = "Tarifa diaria USD")] public decimal TarifaDiaria { get; set; } = 40;
    public string Version { get; set; } = "";
    public string? FotoRuta { get; set; }
    public IFormFile? Foto { get; set; }
}
