using AssetManagmentApp.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AssetManagmentApp.Services;

public class CurrentUserService(AuthenticationStateProvider authenticationStateProvider, UserManager<ApplicationUser> userManager)
{
    public async Task<ApplicationUser> ObtenerUsuarioActualAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var email = authState.User.Identity?.Name
            ?? throw new InvalidOperationException("No hay usuario autenticado.");
        return await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException("Usuario autenticado no encontrado.");
    }

    public async Task<int> ObtenerUsuarioActualIdAsync()
    {
        var usuario = await ObtenerUsuarioActualAsync();
        return usuario.Id;
    }
}
