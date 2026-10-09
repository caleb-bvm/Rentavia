using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rentavia.Domain.Entities;

namespace Rentavia.Infrastructure.Data;

// Identity administra usuarios y contraseñas; no creamos una tabla de acceso propia.
public class RentaviaDbContext(DbContextOptions<RentaviaDbContext> options)
    : IdentityDbContext(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Ocupacion> Ocupaciones => Set<Ocupacion>();
    public DbSet<Alquiler> Alquileres => Set<Alquiler>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Mantenimiento> Mantenimientos => Set<Mantenimiento>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<Recomendacion> Recomendaciones => Set<Recomendacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ModeloRentas.Configurar(modelBuilder);

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.Property(c => c.Nombre).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Descripcion).HasMaxLength(150);
            entity.HasIndex(c => c.Nombre).IsUnique();
        });

        modelBuilder.Entity<Sucursal>(entity =>
        {
            entity.Property(s => s.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Direccion).HasMaxLength(200).IsRequired();
            entity.Property(s => s.Telefono).HasMaxLength(20);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.Property(c => c.Nombre).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Apellido).HasMaxLength(50).IsRequired();
            entity.Property(c => c.TipoDocumento).HasMaxLength(20).IsRequired();
            entity.Property(c => c.NumeroDocumento).HasMaxLength(30).IsRequired();
            entity.Property(c => c.Licencia).HasMaxLength(30).IsRequired();
            entity.Property(c => c.Telefono).HasMaxLength(20).IsRequired();
            entity.Property(c => c.Correo).HasMaxLength(100);
            entity.Property(c => c.FechaRegistroUtc).HasColumnType("datetimeoffset(0)");
            entity.Property(c => c.Version).IsRowVersion();
            entity.HasIndex(c => new { c.TipoDocumento, c.NumeroDocumento }).IsUnique();
        });

        modelBuilder.Entity<Vehiculo>(entity =>
        {
            entity.Property(v => v.Placa).HasMaxLength(15).IsRequired();
            entity.Property(v => v.Marca).HasMaxLength(50).IsRequired();
            entity.Property(v => v.Modelo).HasMaxLength(50).IsRequired();
            entity.Property(v => v.Transmision).HasConversion<string>().HasMaxLength(20);
            entity.Property(v => v.Estado).HasConversion<string>().HasMaxLength(20);
            entity.Property(v => v.Version).IsRowVersion();
            entity.HasIndex(v => v.Placa).IsUnique();
            entity.HasOne(v => v.Categoria).WithMany().HasForeignKey(v => v.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(v => v.Sucursal).WithMany().HasForeignKey(v => v.SucursalId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("Vehiculos", table =>
            {
                table.HasCheckConstraint("CK_Vehiculos_Pasajeros", "[CapacidadPasajeros] > 0");
                table.HasCheckConstraint("CK_Vehiculos_Maletas", "[CapacidadMaletas] >= 0");
                table.HasCheckConstraint("CK_Vehiculos_Kilometraje", "[Kilometraje] >= 0");
                table.HasCheckConstraint("CK_Vehiculos_Estado", "[Estado] IN ('Activo', 'FueraDeServicio', 'Retirado')");
                table.HasCheckConstraint("CK_Vehiculos_Transmision", "[Transmision] IN ('Manual', 'Automatica')");
            });
        });
    }
}
