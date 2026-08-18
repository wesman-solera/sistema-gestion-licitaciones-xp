using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;

namespace Licitaciones.Web.Modelos;

/// <summary>
/// Modelo de vista comun a todos los listados.
/// </summary>
/// <typeparam name="T">Tipo de los elementos listados.</typeparam>
/// <remarks>
/// Agrupa la pagina de resultados con los parametros que la produjeron para que la vista pueda
/// reconstruir los enlaces de paginacion y de ordenamiento conservando los filtros vigentes.
/// </remarks>
public sealed class ListadoViewModel<T>
{
    /// <summary>Pagina de resultados devuelta por el servicio.</summary>
    public required PaginaResultado<T> Pagina { get; init; }

    /// <summary>Parametros de consulta que generaron la pagina.</summary>
    public required ParametrosConsultaApi Parametros { get; init; }

    /// <summary>Parametros adicionales propios del modulo que deben conservarse en los enlaces.</summary>
    /// <remarks>
    /// El tipo del valor no admite nulos porque el ayudante <c>asp-all-route-data</c> espera
    /// <c>IDictionary&lt;string, string&gt;</c>. Un filtro sin valor se representa con la cadena
    /// vacia, que el generador de enlaces omite igual que un nulo.
    /// </remarks>
    public IDictionary<string, string> FiltrosExtra { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Construye el diccionario de valores de ruta para un enlace del listado.</summary>
    /// <param name="pagina">Pagina destino, o <c>null</c> para conservar la actual.</param>
    /// <param name="ordenarPor">Campo de ordenamiento, o <c>null</c> para conservar el actual.</param>
    /// <param name="descendente">Direccion del orden, o <c>null</c> para conservar la actual.</param>
    /// <returns>Diccionario listo para pasarse al ayudante de enlaces.</returns>
    public IDictionary<string, string> ValoresRuta(
        int? pagina = null,
        string? ordenarPor = null,
        bool? descendente = null)
    {
        var valores = new Dictionary<string, string>
        {
            ["pagina"] = (pagina ?? Parametros.Pagina).ToString(),
            ["tamanoPagina"] = Parametros.TamanoPagina.ToString(),
            ["buscar"] = Parametros.Buscar ?? string.Empty,
            ["ordenarPor"] = ordenarPor ?? Parametros.OrdenarPor ?? string.Empty,
            ["descendente"] = (descendente ?? Parametros.Descendente).ToString().ToLowerInvariant(),
            ["incluirEliminados"] = Parametros.IncluirEliminados.ToString().ToLowerInvariant()
        };

        foreach (var filtro in FiltrosExtra)
        {
            valores[filtro.Key] = filtro.Value;
        }

        return valores;
    }

    /// <summary>
    /// Calcula la direccion que debe aplicarse al pulsar el encabezado de una columna.
    /// </summary>
    /// <param name="campo">Campo de la columna.</param>
    /// <returns><c>true</c> si el proximo clic debe ordenar de forma descendente.</returns>
    /// <remarks>
    /// Pulsar la columna ya activa invierte la direccion; pulsar otra columna empieza ascendente.
    /// </remarks>
    public bool ProximaDireccion(string campo)
        => string.Equals(Parametros.OrdenarPor, campo, StringComparison.OrdinalIgnoreCase)
           && !Parametros.Descendente;
}
