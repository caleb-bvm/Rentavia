using Rentavia.Domain.Entities;

namespace Rentavia.Infrastructure.Data;

/// <summary>
/// Registro de auditoría compartido por servicios y controladores.
/// </summary>
public static class AuditoriaExtensions
{
    /// <summary>
    /// Agrega un evento de auditoría al contexto. Se guarda en el mismo SaveChanges
    /// que la operación, así ambos quedan confirmados o se descartan juntos.
    /// </summary>
    public static void Registrar(this RentaviaDbContext db, string actor, string accion,
        string entidad, int id, string? clave = null)
    {
        db.Auditorias.Add(new Auditoria
        {
            UsuarioId = actor,
            Accion = accion,
            Entidad = entidad,
            RegistroId = id,
            RegistroClave = clave
        });
    }

    /// <summary>
    /// Guarda un registro de catálogo y su auditoría en una sola transacción.
    /// Se guarda dos veces porque un registro nuevo recibe su Id hasta el primer SaveChanges.
    /// </summary>
    public static async Task GuardarConAuditoriaAsync(this RentaviaDbContext db, string actor,
        string accion, string entidad, Func<int> obtenerId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.SaveChangesAsync();
        db.Registrar(actor, accion, entidad, obtenerId());
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
