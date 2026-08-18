using System.Globalization;
using Licitaciones.Application.Dtos;

namespace Licitaciones.Web.Servicios;

/// <summary>
/// Da formato cultural a los montos segun la moneda que el usuario eligio ver.
/// </summary>
/// <remarks>
/// El formato de colones usa la cultura <c>es-CR</c>, tal como exige el requisito 9. Se
/// centraliza aqui para que ninguna vista construya su propio formato y aparezcan separadores
/// distintos entre pantallas.
/// </remarks>
public sealed class FormateadorMonto
{
    private static readonly CultureInfo CulturaCostaRica = CultureInfo.GetCultureInfo("es-CR");
    private static readonly CultureInfo CulturaEstadosUnidos = CultureInfo.GetCultureInfo("en-US");

    private readonly PreferenciasUsuario _preferencias;

    /// <summary>Inicializa el formateador.</summary>
    /// <param name="preferencias">Preferencias de presentacion del usuario.</param>
    public FormateadorMonto(PreferenciasUsuario preferencias)
    {
        _preferencias = preferencias;
    }

    /// <summary>Formatea un monto en la moneda que el usuario eligio ver.</summary>
    /// <param name="monto">Monto con sus dos representaciones.</param>
    /// <returns>El texto listo para mostrar.</returns>
    /// <remarks>
    /// Si el usuario pidio dolares pero no hay tipo de cambio activo, se muestra el valor en
    /// colones con una nota: es preferible mostrar el dato correcto en otra moneda que dejar
    /// la celda vacia.
    /// </remarks>
    public string Formatear(MontoDto? monto)
    {
        if (monto is null)
        {
            return "-";
        }

        if (_preferencias.MostrarEnDolares)
        {
            return monto.Usd is decimal usd
                ? usd.ToString("C2", CulturaEstadosUnidos)
                : $"{monto.Crc.ToString("C2", CulturaCostaRica)} (sin tipo de cambio)";
        }

        return monto.Crc.ToString("C2", CulturaCostaRica);
    }

    /// <summary>Formatea un monto expresado directamente en colones.</summary>
    /// <param name="montoCrc">Monto en colones.</param>
    /// <returns>El texto con formato costarricense.</returns>
    public static string FormatearColones(decimal montoCrc)
        => montoCrc.ToString("C2", CulturaCostaRica);

    /// <summary>Codigo de la moneda que el usuario eligio ver.</summary>
    public string MonedaVigente => _preferencias.Moneda;
}
