namespace AssetManagmentApp.Services;

public class SidebarState
{
    public bool Colapsado { get; private set; }

    public event Action? OnChange;

    public void Alternar()
    {
        Colapsado = !Colapsado;
        OnChange?.Invoke();
    }

    public void Cerrar()
    {
        if (Colapsado)
        {
            Colapsado = false;
            OnChange?.Invoke();
        }
    }
}
