namespace Rentavia.Domain.Entities;

/// <summary>Clasificación de vehículos (familiar, económico, etc.).</summary>
public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
