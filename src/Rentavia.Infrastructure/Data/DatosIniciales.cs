using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rentavia.Domain.Entities;

namespace Rentavia.Infrastructure.Data;

public static class DatosIniciales
{
    public static async Task CrearAdministradorAsync(UserManager<IdentityUser> users,
        RoleManager<IdentityRole> roles, string correo, string clave)
    {
        foreach (var rol in new[] { "Administrador", "Empleado" })
        {
            if (!await roles.RoleExistsAsync(rol))
            {
                var creado = await roles.CreateAsync(new IdentityRole(rol));
                if (!creado.Succeeded) throw new InvalidOperationException("No fue posible crear los roles.");
            }
        }
        if (await users.FindByEmailAsync(correo) is not null)
            throw new InvalidOperationException("La cuenta ya existe; no se reemplazan credenciales ni permisos.");
        var user = new IdentityUser { UserName = correo, Email = correo };
        var result = await users.CreateAsync(user, clave);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(x => x.Description)));
        var assigned = await users.AddToRoleAsync(user, "Administrador");
        if (!assigned.Succeeded) throw new InvalidOperationException("La cuenta se creó, pero no se asignó el rol; revisar configuración.");
    }

    public static async Task CargarDatosInicialesAsync(RentaviaDbContext db)
    {
        if (await db.Vehiculos.AnyAsync() || await db.Clientes.AnyAsync())
            throw new InvalidOperationException("La carga de datos iniciales solo se permite si no hay vehículos ni clientes.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var categoria = new Categoria { Nombre = "Familiar", Descripcion = "Sedanes y SUV para familias y viajes cortos" };
        var sucursal = new Sucursal { Nombre = "Agencia Central", Direccion = "Boulevard de los Héroes, San Salvador", Activa = true };
        db.Categorias.Add(categoria);
        db.Sucursales.Add(sucursal);
        db.Clientes.Add(new Cliente
        {
            Nombre = "Carlos",
            Apellido = "Hernández",
            TipoDocumento = "PASAPORTE",
            NumeroDocumento = "A01234567",
            Telefono = "7012-3456",
            FechaRegistroUtc = DateTimeOffset.UtcNow
        });
        db.Vehiculos.AddRange(
            new Vehiculo { Categoria = categoria, Sucursal = sucursal, Placa = "P512-301", Marca = "Toyota", Modelo = "Corolla", Anio = 2024, CapacidadPasajeros = 5, CapacidadMaletas = 2, Transmision = Transmision.Automatica, TarifaDiaria = 40 },
            new Vehiculo { Categoria = categoria, Sucursal = sucursal, Placa = "P648-115", Marca = "Hyundai", Modelo = "Tucson", Anio = 2024, CapacidadPasajeros = 5, CapacidadMaletas = 4, Transmision = Transmision.Automatica, TarifaDiaria = 65 },
            new Vehiculo { Categoria = categoria, Sucursal = sucursal, Placa = "P237-904", Marca = "Kia", Modelo = "Rio", Anio = 2023, CapacidadPasajeros = 5, CapacidadMaletas = 2, Transmision = Transmision.Manual, TarifaDiaria = 35 });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
