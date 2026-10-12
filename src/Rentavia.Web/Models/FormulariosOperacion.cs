using System.ComponentModel.DataAnnotations;
using Rentavia.Application.Recommendations;
using Rentavia.Domain.Entities;

namespace Rentavia.Web.Models;

public class DisponibilidadForm
{
    [Required(ErrorMessage = "Este campo es obligatorio."), DataType(DataType.DateTime)] public DateTime Inicio { get; set; } = Presentacion.Local(DateTimeOffset.UtcNow).AddDays(1);
    [Required(ErrorMessage = "Este campo es obligatorio."), DataType(DataType.DateTime)] public DateTime Fin { get; set; } = Presentacion.Local(DateTimeOffset.UtcNow).AddDays(2);
    [Range(1, 50)] public int Pasajeros { get; set; } = 1;
    [Range(0, 50)] public int Maletas { get; set; }
    [Display(Name = "Categoría")] public int? CategoriaId { get; set; }
    [Display(Name = "Sucursal")] public int? SucursalId { get; set; }
    public List<Vehiculo> Vehiculos { get; set; } = [];
}

public class ReservaForm
{
    public int Id { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Cliente")] public int ClienteId { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Vehículo")] public int VehiculoId { get; set; }
    [Required(ErrorMessage = "Este campo es obligatorio."), DataType(DataType.DateTime)] public DateTime Inicio { get; set; } = Presentacion.Local(DateTimeOffset.UtcNow);
    [Required(ErrorMessage = "Este campo es obligatorio."), DataType(DataType.DateTime)] public DateTime Fin { get; set; } = Presentacion.Local(DateTimeOffset.UtcNow).AddDays(1);
    public string Version { get; set; } = "";
}

public class EntregaForm
{
    public int ReservaId { get; set; }
    public string Version { get; set; } = "";
    [Range(0, 10000000)] public int Kilometraje { get; set; }
    [Range(0, 100), Display(Name = "Combustible (%)")] public int Combustible { get; set; } = 100;
    [StringLength(500)] public string Observaciones { get; set; } = "";
}

public class DevolucionForm
{
    public int Id { get; set; }
    public string Version { get; set; } = "";
    [Range(0, 10000000)] public int Kilometraje { get; set; }
    [Range(0, 100), Display(Name = "Combustible (%)")] public int Combustible { get; set; }
    [StringLength(500)] public string Observaciones { get; set; } = "";
    [Range(typeof(decimal), "0", "999999"), Display(Name = "Cargo adicional USD")] public decimal Cargo { get; set; }
    [StringLength(150), Display(Name = "Concepto del cargo")] public string? Concepto { get; set; }
}

public class PagoForm
{
    public int AlquilerId { get; set; }
    [Range(typeof(decimal), "0.01", "999999")] public decimal Monto { get; set; }
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(30)] public string Metodo { get; set; } = "Efectivo";
    [StringLength(100)] public string? Referencia { get; set; }
}

public class MantenimientoForm
{
    [Range(1, int.MaxValue), Display(Name = "Vehículo")] public int VehiculoId { get; set; }
    [DataType(DataType.DateTime)] public DateTime Inicio { get; set; } = Presentacion.Local(DateTimeOffset.UtcNow);
    [DataType(DataType.DateTime)] public DateTime Fin { get; set; } = Presentacion.Local(DateTimeOffset.UtcNow).AddDays(1);
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(30)] public string Tipo { get; set; } = "Preventivo";
    [Required(ErrorMessage = "Este campo es obligatorio."), StringLength(500), Display(Name = "Descripción")] public string Descripcion { get; set; } = "";
    [Range(typeof(decimal), "0", "999999")] public decimal Costo { get; set; }
}

public class RecomendacionForm : DisponibilidadForm
{
    [Range(typeof(decimal), "0.01", "999999"), Display(Name = "Presupuesto diario USD")] public decimal PresupuestoDiario { get; set; } = 100;
    [StringLength(1000)] public string Preferencias { get; set; } = "";
    public ResultadoRecomendacion? Resultado { get; set; }
}

public class LoginForm
{
    [Required(ErrorMessage = "Este campo es obligatorio."), EmailAddress] public string Correo { get; set; } = "";
    [Required(ErrorMessage = "Este campo es obligatorio."), DataType(DataType.Password), Display(Name = "Contraseña")] public string Clave { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public class UsuarioForm
{
    [Required(ErrorMessage = "Este campo es obligatorio."), EmailAddress] public string Correo { get; set; } = "";
    [Required(ErrorMessage = "Este campo es obligatorio."), DataType(DataType.Password), MinLength(10), Display(Name = "Contraseña inicial")] public string Clave { get; set; } = "";
    [Required(ErrorMessage = "Este campo es obligatorio.")] public string Rol { get; set; } = "Empleado";
}
