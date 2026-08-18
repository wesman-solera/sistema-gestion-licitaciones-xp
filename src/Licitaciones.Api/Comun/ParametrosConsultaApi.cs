using Licitaciones.Application.Comun;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Comun;

/// <summary>
/// Parametros de paginacion, filtrado y ordenamiento tal como llegan en la cadena de consulta.
/// </summary>
/// <remarks>
/// Se mantiene separado de <see cref="ParametrosConsulta"/> para que la capa de aplicacion no
/// dependa de los atributos de enlace de ASP.NET Core y para poder documentar cada parametro
/// en OpenAPI con el nombre exacto que espera el cliente.
/// </remarks>
public sealed class ParametrosConsultaApi
{
    /// <summary>Numero de pagina solicitado, empezando en 1.</summary>
    [FromQuery(Name = "pagina")]
    public int Pagina { get; set; } = 1;

    /// <summary>Cantidad de elementos por pagina. Se acota al maximo admitido por el servidor.</summary>
    [FromQuery(Name = "tamanoPagina")]
    public int TamanoPagina { get; set; } = ParametrosConsulta.TamanoPaginaPorDefecto;

    /// <summary>Texto libre de busqueda.</summary>
    [FromQuery(Name = "buscar")]
    public string? Buscar { get; set; }

    /// <summary>Campo por el que ordenar.</summary>
    [FromQuery(Name = "ordenarPor")]
    public string? OrdenarPor { get; set; }

    /// <summary>Indica si el orden es descendente.</summary>
    [FromQuery(Name = "descendente")]
    public bool Descendente { get; set; }

    /// <summary>Indica si deben incluirse los registros eliminados logicamente.</summary>
    [FromQuery(Name = "incluirEliminados")]
    public bool IncluirEliminados { get; set; }

    /// <summary>Convierte los parametros al tipo que consume la capa de aplicacion.</summary>
    /// <returns>Los parametros ya normalizados y acotados.</returns>
    public ParametrosConsulta AParametrosConsulta() => new()
    {
        Pagina = Pagina,
        TamanoPagina = TamanoPagina,
        Buscar = Buscar,
        OrdenarPor = OrdenarPor,
        Descendente = Descendente,
        IncluirEliminados = IncluirEliminados
    };
}
