namespace Licitaciones.Web.Modelos;

/// <summary>
/// Datos que necesita la vista parcial de paginacion.
/// </summary>
/// <remarks>
/// Se calcula en un solo lugar y se reutiliza en los cinco listados, de modo que el texto
/// "Mostrando X a Y de Z" y los enlaces se comportan igual en todas las pantallas.
/// </remarks>
public sealed class PaginacionViewModel
{
    /// <summary>Numero de la pagina que se esta mostrando.</summary>
    public required int PaginaActual { get; init; }

    /// <summary>Cantidad total de paginas disponibles.</summary>
    public required int TotalPaginas { get; init; }

    /// <summary>Cantidad total de registros que cumplen el filtro.</summary>
    public required int TotalElementos { get; init; }

    /// <summary>Numero del primer registro visible en la pagina actual.</summary>
    public required int Desde { get; init; }

    /// <summary>Numero del ultimo registro visible en la pagina actual.</summary>
    public required int Hasta { get; init; }

    /// <summary>Valores de ruta del enlace a la pagina anterior.</summary>
    public required IDictionary<string, string> RutaAnterior { get; init; }

    /// <summary>Valores de ruta del enlace a la pagina siguiente.</summary>
    public required IDictionary<string, string> RutaSiguiente { get; init; }

    /// <summary>Indica si existe una pagina anterior.</summary>
    public bool TienePrevia => PaginaActual > 1;

    /// <summary>Indica si existe una pagina siguiente.</summary>
    public bool TieneSiguiente => PaginaActual < TotalPaginas;

    /// <summary>Construye el modelo a partir de un listado ya resuelto.</summary>
    /// <typeparam name="T">Tipo de los elementos listados.</typeparam>
    /// <param name="listado">Listado con su pagina y sus parametros.</param>
    /// <returns>El modelo listo para la vista parcial.</returns>
    /// <remarks>
    /// Se llama Crear y no Desde porque este tipo ya expone una propiedad Desde, que es el
    /// numero del primer registro visible en la pagina.
    /// </remarks>
    public static PaginacionViewModel Crear<T>(ListadoViewModel<T> listado)
    {
        ArgumentNullException.ThrowIfNull(listado);

        var pagina = listado.Pagina;

        int primero = pagina.TotalElementos == 0
            ? 0
            : ((pagina.Pagina - 1) * pagina.TamanoPagina) + 1;

        int ultimo = Math.Min(pagina.Pagina * pagina.TamanoPagina, pagina.TotalElementos);

        return new PaginacionViewModel
        {
            PaginaActual = pagina.Pagina,
            TotalPaginas = pagina.TotalPaginas,
            TotalElementos = pagina.TotalElementos,
            Desde = primero,
            Hasta = ultimo,
            RutaAnterior = listado.ValoresRuta(pagina: pagina.Pagina - 1),
            RutaSiguiente = listado.ValoresRuta(pagina: pagina.Pagina + 1)
        };
    }
}
