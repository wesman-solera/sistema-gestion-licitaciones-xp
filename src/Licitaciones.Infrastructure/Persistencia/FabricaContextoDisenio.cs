using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Construye el contexto para las herramientas de linea de comandos de Entity Framework Core.
/// </summary>
/// <remarks>
/// Comandos como <c>dotnet ef migrations add</c> necesitan instanciar el contexto sin arrancar
/// la aplicacion web. Sin esta fabrica habria que apuntar el comando al proyecto de arranque y
/// depender de su configuracion completa.
/// <para>
/// La cadena de conexion se toma de la variable de entorno <c>ConnectionStrings__Licitaciones</c>
/// y solo cae en un valor local de desarrollo si no esta definida. En el repositorio no hay
/// credenciales reales (seccion 11 y 14.2).
/// </para>
/// </remarks>
public sealed class FabricaContextoDisenio : IDesignTimeDbContextFactory<LicitacionesDbContext>
{
    /// <summary>Nombre de la variable de entorno que aporta la cadena de conexion.</summary>
    public const string VariableCadenaConexion = "ConnectionStrings__Licitaciones";

    /// <inheritdoc />
    public LicitacionesDbContext CreateDbContext(string[] args)
    {
        string cadena =
            Environment.GetEnvironmentVariable(VariableCadenaConexion)
            ?? "Host=localhost;Port=5432;Database=licitaciones;Username=postgres;Password=postgres";

        var opciones = new DbContextOptionsBuilder<LicitacionesDbContext>()
            .UseNpgsql(cadena, npgsql =>
                npgsql.MigrationsAssembly(typeof(LicitacionesDbContext).Assembly.FullName))
            .Options;

        return new LicitacionesDbContext(opciones);
    }
}
