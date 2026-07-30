using AssetManagmentApp.Models;
using Microsoft.AspNetCore.Identity;

namespace AssetManagmentApp.Data;

public static class SeedData
{
    public static readonly string[] Roles = ["Admin", "Consultor"];

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }

        var adminEmail = configuration["SeedAdmin:Email"] ?? "admin@rodcast.local";
        var adminPassword = configuration["SeedAdmin:Password"]
            ?? throw new InvalidOperationException("SeedAdmin:Password no configurada. Definirla en appsettings o variable de entorno antes de correr el seed.");

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                Nombre = "Administrador",
                Activo = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
