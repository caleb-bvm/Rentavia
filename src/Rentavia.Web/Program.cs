using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Rentavia.Application.Recommendations;
using Rentavia.Infrastructure.Data;
using Rentavia.Infrastructure.Services;

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("es-SV");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("es-SV");
var builder = WebApplication.CreateBuilder(args);
if (builder.Configuration["DataProtection:Ruta"] is string rutaClaves)
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(rutaClaves));
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddDbContext<RentaviaDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Rentavia")));
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 10;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
}).AddEntityFrameworkStores<RentaviaDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Cuenta/Ingresar";
    options.AccessDeniedPath = "/Cuenta/SinPermiso";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(1));
builder.Services.AddScoped<AgendaService>();
builder.Services.AddScoped<AlquilerService>();
builder.Services.Configure<OpenAiOptions>(options =>
{
    options.ApiKey = builder.Configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
    options.Modelo = builder.Configuration["OpenAI:Modelo"] ?? "gpt-4.1-mini";
});
builder.Services.AddHttpClient<IRecomendador, OpenAiRecomendador>(http => http.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ia", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();

// Las tareas administrativas son explícitas; la web nunca migra al arrancar.
if (args.Contains("--migrate") || args.Contains("--create-admin") || args.Contains("--seed-demo"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RentaviaDbContext>();
    if (args.Contains("--migrate")) await db.Database.MigrateAsync();
    if (args.Contains("--create-admin"))
    {
        var correo = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? throw new InvalidOperationException("Configura ADMIN_EMAIL.");
        var clave = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? throw new InvalidOperationException("Configura ADMIN_PASSWORD.");
        await DatosIniciales.CrearAdministradorAsync(scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>(),
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(), correo, clave);
    }
    if (args.Contains("--seed-demo")) await DatosIniciales.CargarDemoAsync(db);
    Console.WriteLine("Operación administrativa completada.");
    return;
}

app.UseExceptionHandler("/Home/Error");
if (builder.Configuration.GetValue("Seguridad:RedirigirHttps", !app.Environment.IsDevelopment()))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles(); // Fotografías cargadas después de compilar.
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapStaticAssets();
app.MapGet("/health", async (RentaviaDbContext db) =>
    await db.Database.CanConnectAsync() ? Results.Ok(new { estado = "Disponible" }) : Results.StatusCode(503)).AllowAnonymous();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.Run();

public partial class Program;
