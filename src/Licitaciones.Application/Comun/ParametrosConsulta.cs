namespace Licitaciones.Application.Comun;

/// <summary>
/// Parametros comunes de paginacion, filtrado y ordenamiento de los listados.
/// </summary>
/// <remarks>
/// El tamano de pagina se acota por arriba para que un cliente no pueda pedir el listado
/// completo y degradar la base de datos, y por abajo para evitar consultas sin sentido.
/// </remarks>
public sealed class ParametrosConsulta
{
    /// <summary>Tamano de pagina utilizado cuando el cliente no indica ninguno.</summary>
    public const int TamanoPaginaPorDefecto = 20;

    /// <summary>Tamano de pagina maximo admitido.</summary>
    public const int TamanoPaginaMaximo = 100;

    private int _pagina = 1;
    private int _tamanoPagina = TamanoPaginaPorDefecto;

    /// <summary>Numero de pagina solicitado, empezando en 1.</summary>
    public int Pagina
    {
        get => _pagina;
        set => _pagina = value < 1 ? 1 : value;
    }

    /// <summary>Cantidad de elementos por pagina, acotada entre 1 y <see cref="TamanoPaginaMaximo"/>.</summary>
    public int TamanoPagina
    {
        get => _tamanoPagina;
        set => _tamanoPagina = value switch
        {
            < 1 => TamanoPaginaPorDefecto,
            > TamanoPaginaMaximo => TamanoPaginaMaximo,
            _ => value
        };
    }

    /// <summary>Texto libre de busqueda. Se aplica a los campos descriptivos de cada modulo.</summary>
    public string? Buscar { get; set; }

    /// <summary>Campo por el que se ordena. Cada servicio define los valores que acepta.</summary>
    public string? OrdenarPor { get; set; }

    /// <summary>Indica si el orden es descendente.</summary>
    public bool Descendente { get; set; }

    /// <summary>Indica si el listado debe incluir los registros eliminados logicamente.</summary>
    public bool IncluirEliminados { get; set; }

    /// <summary>Cantidad de elementos que deben omitirse para llegar a la pagina solicitada.</summary>
    public int Omitir => (Pagina - 1) * TamanoPagina;
}
