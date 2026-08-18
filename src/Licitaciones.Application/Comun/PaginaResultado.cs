namespace Licitaciones.Application.Comun;

/// <summary>
/// Pagina de resultados devuelta por los listados (requisito 10.2: paginacion).
/// </summary>
/// <typeparam name="T">Tipo de los elementos de la pagina.</typeparam>
/// <param name="Elementos">Elementos de la pagina actual.</param>
/// <param name="Pagina">Numero de pagina solicitado, empezando en 1.</param>
/// <param name="TamanoPagina">Cantidad maxima de elementos por pagina.</param>
/// <param name="TotalElementos">Cantidad total de elementos que cumplen el filtro.</param>
public sealed record PaginaResultado<T>(
    IReadOnlyList<T> Elementos,
    int Pagina,
    int TamanoPagina,
    int TotalElementos)
{
    /// <summary>Cantidad total de paginas disponibles.</summary>
    public int TotalPaginas => TamanoPagina <= 0
        ? 0
        : (int)Math.Ceiling(TotalElementos / (double)TamanoPagina);

    /// <summary>Indica si existe una pagina anterior.</summary>
    public bool TieneAnterior => Pagina > 1;

    /// <summary>Indica si existe una pagina siguiente.</summary>
    public bool TieneSiguiente => Pagina < TotalPaginas;

    /// <summary>Crea una pagina vacia conservando los parametros de consulta.</summary>
    /// <param name="pagina">Numero de pagina solicitado.</param>
    /// <param name="tamanoPagina">Tamano de pagina solicitado.</param>
    /// <returns>Una pagina sin elementos.</returns>
    public static PaginaResultado<T> Vacia(int pagina, int tamanoPagina)
        => new([], pagina, tamanoPagina, 0);
}
