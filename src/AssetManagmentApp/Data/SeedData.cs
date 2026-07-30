using Microsoft.AspNetCore.Identity;

namespace AssetManagmentApp.Data;

public static class SeedData
{
    public static readonly string[] Roles = ["Admin", "Consultor"];

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }
    }
}
