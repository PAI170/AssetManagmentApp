using AssetManagmentApp.Components;
using AssetManagmentApp.Data;
using AssetManagmentApp.Models;
using AssetManagmentApp.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no configurada.");

void ConfigureDbContext(DbContextOptionsBuilder options) =>
    options.UseMySql(connectionString, new MariaDbServerVersion(new Version(10, 11, 0)));

// Se registra solo la fábrica (Singleton) y el AppDbContext con ámbito de request se crea
// a partir de ella, en vez de llamar AddDbContext por separado: registrar ambas formas
// directamente duplica/choca el registro de DbContextOptions<AppDbContext> (Scoped vs.
// Singleton) y rompe la validación de servicios de EF Core en tiempo de diseño.
// La fábrica también permite que componentes que viven junto a la página (como UserMenu)
// usen su propio AppDbContext aislado, sin competir por la misma instancia con ámbito de
// circuito de la página (Blazor Server comparte un único DI scope por circuito).
builder.Services.AddDbContextFactory<AppDbContext>(ConfigureDbContext);
builder.Services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<TurnstileService>();

// Llaves de cifrado en memoria (no en disco): al reiniciar el proceso, las cookies de
// sesion ya emitidas quedan invalidas y todos los usuarios tienen que volver a loguearse.
// Sin esto, ASP.NET Core persiste las llaves en disco por defecto y una cookie "recuerdame"
// sigue siendo valida despues de un reinicio del servidor.
builder.Services.AddDataProtection()
    .UseEphemeralDataProtectionProvider();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 3;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/acceso-denegado";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    options.SlidingExpiration = true;
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

builder.Services.AddScoped<ProformaService>();
builder.Services.AddScoped<TipoCambioService>();
builder.Services.AddScoped<SidebarState>();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<SolicitudCorreccionService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

// Las llaves de Data Protection son efímeras (ver arriba): al reiniciar el proceso, los
// tokens antiforgery de cookies/paginas ya emitidos quedan invalidos. Sin esto, esa
// excepcion no tiene manejador y el usuario ve una pantalla en blanco en vez de volver
// al login.
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
    {
        foreach (var key in context.Request.Cookies.Keys)
        {
            context.Response.Cookies.Delete(key);
        }

        context.Response.Redirect("/login");
    }
});

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/Account/Logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.LocalRedirect("/");
});

// Ping desde js/sessionTimeout.js mientras hay actividad en la pagina. El circuito de
// Blazor Server no genera peticiones HTTP por si solo, asi que sin este ping la cookie
// (SlidingExpiration) nunca se renovaria mientras el usuario interactua dentro de una
// sola pagina sin navegar.
app.MapPost("/account/keep-alive", () => Results.Ok())
    .RequireAuthorization();

app.MapGet("/proformas/{id:int}/pdf", async (int id, AppDbContext db) =>
{
    var proforma = await db.Proformas
        .Include(p => p.Proyecto)
        .Include(p => p.Detalles)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (proforma is null)
    {
        return Results.NotFound();
    }

    var pdfBytes = ProformaPdfService.Generar(proforma);
    return Results.File(pdfBytes, "application/pdf", $"Proforma-{proforma.Numero}.pdf");
}).RequireAuthorization();

using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

app.Run();
