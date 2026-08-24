using Licitaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.PostgreSql;

namespace Licitaciones.IntegrationTests.Infraestructura;

/// <summary>
/// Levanta un contenedor de PostgreSQL real y compartido para toda la coleccion de pruebas.
/// </summary>
/// <remarks>
/// El enunciado (seccion 11 y 12.2) exige ejecutar las pruebas de integracion contra PostgreSQL
/// real y prohibe sustituirlo por SQLite. La razon es concreta: los indices unicos parciales,
/// las restricciones CHECK, la columna de sistema <c>xmin</c> y el comportamiento de
/// <c>timestamptz</c> son especificos de PostgreSQL. Una prueba contra un motor en memoria
/// pasaria sin verificar nada de eso.
/// <para>
/// El contenedor se comparte entre todas las clases de prueba mediante una coleccion de xUnit:
/// arrancar uno por clase multiplicaria el tiempo total sin aportar aislamiento real, porque
/// cada prueba ya limpia sus propios datos.
/// </para>
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _contenedor = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("licitaciones_pruebas")
        .WithUsername("pruebas")
        .WithPassword("pruebas")
        .Build();

    /// <summary>Cadena de conexion al contenedor en ejecucion.</summary>
    public string CadenaConexion => _contenedor.GetConnectionString();

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();

        // Aplicar las migraciones aqui verifica de paso que el esquema versionado se puede
        // construir desde cero, que es uno de los puntos de la seccion 12.2.
        await using LicitacionesDbContext contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    /// <inheritdoc />
    public Task DisposeAsync() => _contenedor.DisposeAsync().AsTask();

    /// <summary>Crea un contexto nuevo apuntando al contenedor.</summary>
    /// <returns>Un contexto listo para usarse.</returns>
    /// <remarks>
    /// Cada prueba usa su propio contexto. Compartir uno haria que el rastreo de entidades de
    /// una prueba contaminara la siguiente y ocultaria errores de persistencia reales.
    /// </remarks>
    public LicitacionesDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<LicitacionesDbContext>()
            .UseNpgsql(CadenaConexion, npgsql =>
                npgsql.MigrationsAssembly(typeof(LicitacionesDbContext).Assembly.FullName))
            // Mismo criterio que en RegistroServiciosInfraestructura: la migracion escrita a mano
            // es la fuente de verdad del esquema, no la instantanea de diseno.
            .ConfigureWarnings(avisos => avisos.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new LicitacionesDbContext(opciones);
    }

    /// <summary>Borra los datos de las tablas transaccionales entre pruebas.</summary>
    /// <returns>Tarea que se completa cuando las tablas quedan limpias.</returns>
    /// <remarks>
    /// Se usa TRUNCATE con CASCADE en lugar de recrear la base: es mucho mas rapido y conserva
    /// el esquema migrado. Los niveles de aprobacion y el tipo de cambio semilla se conservan
    /// porque son datos de configuracion, no datos de prueba.
    /// </remarks>
    public async Task LimpiarDatosTransaccionalesAsync()
    {
        await using LicitacionesDbContext contexto = CrearContexto();

        await contexto.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE ofertas, licitaciones, proveedores RESTART IDENTITY CASCADE;");
    }
}

/// <summary>Define la coleccion de xUnit que comparte el contenedor de PostgreSQL.</summary>
[CollectionDefinition(Nombre)]
public sealed class ColeccionPostgres : ICollectionFixture<PostgresFixture>
{
    /// <summary>Nombre de la coleccion, referenciado por cada clase de prueba.</summary>
    public const string Nombre = "PostgreSQL";
}
