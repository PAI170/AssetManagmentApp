using AssetManagmentApp.Components;
using AssetManagmentApp.Data;
using AssetManagmentApp.Models;
using AssetManagmentApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no configurada.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MariaDbServerVersion(new Version(10, 11, 0))));

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
    {
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/acceso-denegado";
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

builder.Services.AddScoped<ProformaService>();

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

app.MapGet("/proformas/{id:int}/pdf", async (int id, AppDbContext db) =>
{
    var proforma = await db.Proformas
        .Include(p => p.Proyecto)
        .Include(p => p.Detalles).ThenInclude(d => d.Activo)
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
