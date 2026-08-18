using Microsoft.Playwright;

namespace Licitaciones.FunctionalTests.Infraestructura;

/// <summary>
/// Base de las pruebas funcionales de extremo a extremo ejecutadas con un navegador real.
/// </summary>
/// <remarks>
/// A diferencia de las pruebas de integracion, estas no levantan la aplicacion en memoria:
/// atacan una instancia ya desplegada, la misma que produce <c>docker compose up --build</c>.
/// Es lo que hace que verifiquen de verdad el requisito 12.3, porque ejercitan el HTML servido,
/// las hojas de estilo locales y los guiones del navegador, no solo la capa de servidor.
/// <para>
/// La direccion se toma de la variable de entorno <c>URL_BASE_PRUEBAS</c>. En la integracion
/// continua apunta al servicio levantado por Docker Compose; en local, a la instancia de
/// desarrollo.
/// </para>
/// </remarks>
public abstract class PruebaNavegadorBase : IAsyncLifetime
{
    /// <summary>Nombre de la variable de entorno que indica la direccion de la aplicacion.</summary>
    public const string VariableUrlBase = "URL_BASE_PRUEBAS";

    /// <summary>Direccion usada cuando la variable de entorno no esta definida.</summary>
    public const string UrlBasePorDefecto = "http://localhost:8080";

    private IPlaywright? _playwright;
    private IBrowser? _navegador;

    /// <summary>Direccion base de la aplicacion bajo prueba.</summary>
    protected static string UrlBase =>
        Environment.GetEnvironmentVariable(VariableUrlBase) ?? UrlBasePorDefecto;

    /// <summary>Contexto de navegador aislado de la prueba actual.</summary>
    protected IBrowserContext Contexto { get; private set; } = null!;

    /// <summary>Pagina sobre la que actua la prueba.</summary>
    protected IPage Pagina { get; private set; } = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();

        _navegador = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        // Cada prueba recibe un contexto propio: las cookies de tema y de moneda no deben
        // filtrarse de una prueba a otra, porque justamente algunas de ellas las modifican.
        Contexto = await _navegador.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = UrlBase,
            Locale = "es-CR",
            ViewportSize = new ViewportSize { Width = 1366, Height = 900 }
        });

        Pagina = await Contexto.NewPageAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await Contexto.CloseAsync();

        if (_navegador is not null)
        {
            await _navegador.CloseAsync();
        }

        _playwright?.Dispose();
    }

    /// <summary>Genera un sufijo unico para evitar colisiones de unicidad entre ejecuciones.</summary>
    /// <returns>Cadena corta y unica.</returns>
    /// <remarks>
    /// Los codigos de licitacion y los nombres de proveedor son unicos. Sin un sufijo, la
    /// segunda ejecucion de la misma prueba fallaria por duplicado en lugar de por un defecto real.
    /// </remarks>
    protected static string SufijoUnico() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    /// <summary>Cambia el tamano de la ventana para probar el diseno adaptable.</summary>
    /// <param name="ancho">Ancho en pixeles.</param>
    /// <param name="alto">Alto en pixeles.</param>
    /// <returns>Tarea que se completa cuando la ventana cambio de tamano.</returns>
    protected Task UsarPantallaAsync(int ancho, int alto)
        => Pagina.SetViewportSizeAsync(ancho, alto);
}
