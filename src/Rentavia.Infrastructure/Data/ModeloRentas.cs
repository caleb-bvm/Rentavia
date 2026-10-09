using Microsoft.EntityFrameworkCore;
using Rentavia.Domain.Entities;

namespace Rentavia.Infrastructure.Data;

internal static class ModeloRentas
{
    public static void Configurar(ModelBuilder model)
    {
        model.Entity<Cliente>().Property(x => x.Activo).HasDefaultValue(true);
        model.Entity<Vehiculo>().Property(x => x.TarifaDiaria).HasPrecision(12, 2);
        model.Entity<Vehiculo>().Property(x => x.FotoRuta).HasMaxLength(200);
        model.Entity<Vehiculo>().ToTable(t => t.HasCheckConstraint("CK_Vehiculos_Tarifa", "[TarifaDiaria] >= 0"));

        model.Entity<Reserva>(e =>
        {
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Version).IsRowVersion();
            e.Property(x => x.TarifaDiaria).HasPrecision(12, 2);
            e.Property(x => x.TotalEstimado).HasPrecision(12, 2);
            e.HasOne(x => x.Cliente).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Vehiculo).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.VehiculoId, x.InicioUtc, x.FinUtc });
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Reservas_Fechas", "[FinUtc] > [InicioUtc]");
                t.HasCheckConstraint("CK_Reservas_Total", "[TarifaDiaria] > 0 AND [TotalEstimado] > 0 AND [Dias] > 0");
            });
        });
        model.Entity<Ocupacion>(e =>
        {
            e.HasOne(x => x.Vehiculo).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Reserva).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Mantenimiento).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.VehiculoId, x.Activa, x.InicioUtc, x.FinUtc });
            e.HasIndex(x => x.ReservaId).IsUnique().HasFilter("[ReservaId] IS NOT NULL");
            e.HasIndex(x => x.MantenimientoId).IsUnique().HasFilter("[MantenimientoId] IS NOT NULL");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Ocupaciones_Fechas", "[FinUtc] > [InicioUtc]");
                t.HasCheckConstraint("CK_Ocupaciones_Origen", "([ReservaId] IS NOT NULL AND [MantenimientoId] IS NULL) OR ([ReservaId] IS NULL AND [MantenimientoId] IS NOT NULL)");
            });
        });
        model.Entity<Alquiler>(e =>
        {
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Version).IsRowVersion();
            e.Property(x => x.ObservacionesEntrega).HasMaxLength(500);
            e.Property(x => x.ObservacionesDevolucion).HasMaxLength(500);
            e.Property(x => x.ConceptoCargo).HasMaxLength(150);
            e.Property(x => x.CargoAdicional).HasPrecision(12, 2);
            e.Property(x => x.MontoTotal).HasPrecision(12, 2);
            e.HasOne(x => x.Reserva).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Vehiculo).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ReservaId).IsUnique();
            e.HasIndex(x => x.VehiculoId).IsUnique().HasFilter("[Estado] = 'Abierto'");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Alquileres_Importes", "[CargoAdicional] >= 0 AND [MontoTotal] >= 0");
                t.HasCheckConstraint("CK_Alquileres_Km", "[KilometrajeInicial] >= 0 AND ([KilometrajeFinal] IS NULL OR [KilometrajeFinal] >= [KilometrajeInicial])");
                t.HasCheckConstraint("CK_Alquileres_Combustible", "[CombustibleInicial] BETWEEN 0 AND 100 AND ([CombustibleFinal] IS NULL OR [CombustibleFinal] BETWEEN 0 AND 100)");
            });
        });
        model.Entity<Pago>(e =>
        {
            e.Property(x => x.Monto).HasPrecision(12, 2);
            e.Property(x => x.Metodo).HasMaxLength(30);
            e.Property(x => x.Referencia).HasMaxLength(100);
            e.HasOne(x => x.Alquiler).WithMany(x => x.Pagos).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Pagos_Monto", "[Monto] > 0"));
        });
        model.Entity<Mantenimiento>(e =>
        {
            e.Property(x => x.Tipo).HasMaxLength(30);
            e.Property(x => x.Descripcion).HasMaxLength(500);
            e.Property(x => x.Costo).HasPrecision(12, 2);
            e.HasOne(x => x.Vehiculo).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Mantenimientos_Datos", "[FinUtc] > [InicioUtc] AND [Costo] >= 0"));
        });
        model.Entity<Auditoria>(e =>
        {
            e.Property(x => x.UsuarioId).HasMaxLength(450);
            e.Property(x => x.Accion).HasMaxLength(50);
            e.Property(x => x.Entidad).HasMaxLength(50);
            e.Property(x => x.RegistroClave).HasMaxLength(450);
        });
        model.Entity<Recomendacion>(e =>
        {
            e.Property(x => x.UsuarioId).HasMaxLength(450);
            e.Property(x => x.Origen).HasMaxLength(30);
            e.Property(x => x.Modelo).HasMaxLength(100);
            e.Property(x => x.VersionPrompt).HasMaxLength(30);
        });
    }
}
