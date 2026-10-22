using System.ComponentModel.DataAnnotations;
using Rentavia.Domain.Entities;

namespace Rentavia.Web.Models;

public class CategoriaForm
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Escribe el nombre."), StringLength(50)] public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
}

public class SucursalForm
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(100)] public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(200)] public string Direccion { get; set; } = "";
    [StringLength(20)] public string? Telefono { get; set; }
    public bool Activa { get; set; } = true;
}

/// <summary>
/// El cliente se identifica con DUI (formato 00000000-0) o con pasaporte (formato libre).
/// Solo se llena el campo del tipo elegido; el otro queda desactivado en pantalla.
/// </summary>
public class ClienteForm : IValidatableObject
{
    public const string Dui = "DUI";
    public const string Pasaporte = "PASAPORTE";

    public int Id { get; set; }
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(50)] public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(50)] public string Apellido { get; set; } = "";
    [Required(ErrorMessage = "Este campo es obligatorio."), Display(Name = "Tipo de documento")] public string TipoDocumento { get; set; } = Dui;
    [RegularExpression(@"^\d{8}-\d$", ErrorMessage = "El DUI debe tener el formato 00000000-0."), Display(Name = "DUI")] public string? NumeroDui { get; set; }
    [StringLength(30), Display(Name = "Pasaporte")] public string? NumeroPasaporte { get; set; }
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(20), Display(Name = "Teléfono")] public string Telefono { get; set; } = "";
    [EmailAddress, StringLength(100)] public string? Correo { get; set; }
    public bool Activo { get; set; } = true;
    public string Version { get; set; } = "";

    /// <summary>Número que se guarda según el tipo elegido.</summary>
    public string NumeroDocumento => (TipoDocumento == Dui ? NumeroDui : NumeroPasaporte)?.Trim().ToUpperInvariant() ?? "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TipoDocumento != Dui && TipoDocumento != Pasaporte)
            yield return new ValidationResult("Selecciona DUI o Pasaporte.", [nameof(TipoDocumento)]);
        else if (string.IsNullOrWhiteSpace(NumeroDocumento))
            yield return new ValidationResult("Escribe el número de documento.", [TipoDocumento == Dui ? nameof(NumeroDui) : nameof(NumeroPasaporte)]);
    }
}

public class VehiculoForm
{
    public int Id { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Categoría")] public int CategoriaId { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Sucursal")] public int SucursalId { get; set; }
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(15),
     RegularExpression(@"^[Pp][A-Za-z0-9]{3}-[A-Za-z0-9]{3}$", ErrorMessage = "Usa el formato Pxxx-xxx con letras o números, por ejemplo P123-ABC.")]
    public string Placa { get; set; } = "";
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
