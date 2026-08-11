using System.Globalization;

namespace AssetManagmentApp.Models;

public static class MonedaExtensions
{
    private static readonly CultureInfo CulturaMoneda = CultureInfo.GetCultureInfo("en-US");

    public static string ToMoneda(this decimal valor) => valor.ToString("C2", CulturaMoneda);
}
