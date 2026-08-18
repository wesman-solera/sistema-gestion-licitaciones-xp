using Licitaciones.IntegrationTests.Infraestructura;
using Licitaciones.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Licitaciones.IntegrationTests.Api;

/// <summary>
/// Levanta la aplicacion completa en memoria apuntando al contenedor de PostgreSQL.
/// </summary>
/// <remarks>
/// La aplicacion arranca con su tuberia real: enrutamiento, enlace de modelo, validacion,
/// manejo global de excepciones y acceso a datos. Lo unico que se sustituye es la cadena de
/// conexion, que apunta al contenedor de pruebas en lugar de a una base de desarrollo.
/// </remarks>
public sealed class FabricaAplicacion : WebApplicationFactory<Licitaciones.Web.Program>
{
    private readonly string _cadenaConexion;

    /// <summary>Inicializa la fabrica con la cadena de conexion del contenedor.</summary>
    /// <param name="cadenaConexion">Cadena de conexion a PostgreSQL.</param>
    public FabricaAplicacion(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, configuracion) =>
        {
            configuracion.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{RegistroServiciosInfraestructura.NombreCadenaConexion}"] =
                    _cadenaConexion
            });
        });
    }
}

/// <summary>Base de las pruebas de API que necesitan la aplicacion levantada.</summary>
[Collection(ColeccionPostgres.Nombre)]
public abstract class PruebaApiBase : IAsyncLifetime
{
    /// <summary>Contenedor de PostgreSQL compartido.</summary>
    protected PostgresFixture Postgres { get; }

    /// <summary>Fabrica de la aplicacion bajo prueba.</summary>
    protected FabricaAplicacion Fabrica { get; }

    /// <summary>Cliente HTTP conectado a la aplicacion en memoria.</summary>
    protected HttpClient Cliente { get; }

    /// <summary>Inicializa la prueba levantando la aplicacion.</summary>
    /// <param name="postgres">Contenedor de PostgreSQL.</param>
    protected PruebaApiBase(PostgresFixture postgres)
    {
        ArgumentNullException.ThrowIfNull(postgres);

        Postgres = postgres;
        Fabrica = new FabricaAplicacion(postgres.CadenaConexion);
        Cliente = Fabrica.CreateClient();
    }

    /// <inheritdoc />
    public Task InitializeAsync() => Postgres.LimpiarDatosTransaccionalesAsync();

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        Cliente.Dispose();
        Fabrica.Dispose();

        return Task.CompletedTask;
    }
}
