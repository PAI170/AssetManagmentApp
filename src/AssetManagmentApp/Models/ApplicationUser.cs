using Microsoft.AspNetCore.Identity;

namespace AssetManagmentApp.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
