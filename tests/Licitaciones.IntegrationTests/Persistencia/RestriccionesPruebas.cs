using FluentAssertions;
using Licitaciones.Domain.Entidades;
using Licitaciones.IntegrationTests.Infraestructura;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Licitaciones.IntegrationTests.Persistencia;

/// <summary>
/// Comprueba que PostgreSQL rechace por su cuenta lo que las capas superiores ya validan.
/// </summary>
/// <remarks>
/// Estas pruebas no duplican las unitarias: verifican la ultima linea de defensa. Escriben
/// directamente contra el contexto, saltandose los servicios de aplicacion, para comprobar que
/// ni siquiera un camino que evite la validacion pueda dejar datos invalidos en la base.
/// </remarks>
[Collection(ColeccionPostgres.Nombre)]
public sealed class RestriccionesPruebas : IAsyncLifetime
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgresFixture _postgres;

    /// <summary>Inicializa la prueba con el contenedor compartido.</summary>
    /// <param name="postgres">Contenedor de PostgreSQL.</param>
    public RestriccionesPruebas(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    /// <inheritdoc />
    public Task InitializeAsync() => _postgres.LimpiarDatosTransaccionalesAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task IndiceUnico_RechazaDosProveedoresConNombreEquivalente()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        contexto.Proveedores.Add(Proveedor.Crear("Empresa Central", Ahora));
        await contexto.SaveChangesAsync();

        // La segunda escritura usa otra grafia; la forma normalizada colisiona igual.
        contexto.Proveedores.Add(Proveedor.Crear("  empresa   central  ", Ahora));

        Func<Task> accion = () => contexto.SaveChangesAsync();

        var excepcion = await accion.Should().ThrowAsync<DbUpdateException>();
        excepcion.WithInnerException<DbUpdateException, PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task IndiceUnico_RechazaDosLicitacionesConCodigoEquivalente()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        contexto.Licitaciones.Add(
            Licitacion.Crear("LIC-2026-050", "Titulo", 1_000_000m, Ahora.AddDays(7), Ahora));
        await contexto.SaveChangesAsync();

        contexto.Licitaciones.Add(
            Licitacion.Crear("  lic-2026-050  ", "Otro titulo", 500_000m, Ahora.AddDays(9), Ahora));

        Func<Task> accion = () => contexto.SaveChangesAsync();

        var excepcion = await accion.Should().ThrowAsync<DbUpdateException>();
        excepcion.WithInnerException<DbUpdateException, PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    /// <summary>
    /// Indice unico compuesto exigido por la seccion 8.3: un proveedor no puede ofertar dos
    /// veces en la misma licitacion.
    /// </summary>
    [Fact]
    public async Task IndiceUnicoCompuesto_RechazaDosOfertasDelMismoProveedorEnLaMismaLicitacion()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        Licitacion licitacion =
            Licitacion.Crear("LIC-2026-060", "Titulo", 1_000_000m, Ahora.AddDays(7), Ahora);
        licitacion.Publicar(Ahora);

        Proveedor proveedor = Proveedor.Crear("Distribuidora del Norte", Ahora);

        contexto.Licitaciones.Add(licitacion);
        contexto.Proveedores.Add(proveedor);
        await contexto.SaveChangesAsync();

        contexto.Ofertas.Add(Oferta.Registrar(licitacion, proveedor.Id, 800_000m, Ahora));
        await contexto.SaveChangesAsync();

        contexto.Ofertas.Add(Oferta.Registrar(licitacion, proveedor.Id, 700_000m, Ahora));

        Func<Task> accion = () => contexto.SaveChangesAsync();

        var excepcion = await accion.Should().ThrowAsync<DbUpdateException>();
        excepcion.WithInnerException<DbUpdateException, PostgresException>()
            .Which.ConstraintName.Should().Be("ux_ofertas_licitacion_proveedor");
    }

    /// <summary>
    /// El indice unico parcial mantiene la invariante de un unico tipo de cambio activo
    /// (seccion 8.8) incluso ante una escritura que evite la capa de aplicacion.
    /// </summary>
    [Fact]
    public async Task IndiceUnicoParcial_RechazaUnSegundoTipoDeCambioActivo()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        contexto.TiposCambio.Add(TipoCambio.Crear(520m, Ahora, activo: true, Ahora));

        Func<Task> accion = () => contexto.SaveChangesAsync();

        // La semilla ya dejo uno activo, asi que este segundo debe ser rechazado.
        var excepcion = await accion.Should().ThrowAsync<DbUpdateException>();
        excepcion.WithInnerException<DbUpdateException, PostgresException>()
            .Which.ConstraintName.Should().Be("ux_tipos_cambio_unico_activo");
    }

    [Fact]
    public async Task IndiceUnicoParcial_AdmiteVariosTiposDeCambioInactivos()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        contexto.TiposCambio.Add(TipoCambio.Crear(510m, Ahora.AddDays(-2), activo: false, Ahora));
        contexto.TiposCambio.Add(TipoCambio.Crear(515m, Ahora.AddDays(-1), activo: false, Ahora));

        Func<Task> accion = () => contexto.SaveChangesAsync();

        await accion.Should().NotThrowAsync();
    }

    /// <summary>
    /// Clave foranea con RESTRICT: borrar una licitacion con ofertas debe fallar en la base de
    /// datos, no solo en la capa de aplicacion (seccion 8.9).
    /// </summary>
    [Fact]
    public async Task ClaveForanea_ImpideBorrarUnaLicitacionConOfertas()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        Licitacion licitacion =
            Licitacion.Crear("LIC-2026-070", "Titulo", 1_000_000m, Ahora.AddDays(7), Ahora);
        licitacion.Publicar(Ahora);

        Proveedor proveedor = Proveedor.Crear("Consorcio Sur", Ahora);

        contexto.Licitaciones.Add(licitacion);
        contexto.Proveedores.Add(proveedor);
        await contexto.SaveChangesAsync();

        contexto.Ofertas.Add(Oferta.Registrar(licitacion, proveedor.Id, 800_000m, Ahora));
        await contexto.SaveChangesAsync();

        contexto.Licitaciones.Remove(licitacion);

        Func<Task> accion = () => contexto.SaveChangesAsync();

        var excepcion = await accion.Should().ThrowAsync<DbUpdateException>();
        excepcion.WithInnerException<DbUpdateException, PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
    }

    /// <summary>
    /// La restriccion CHECK protege el presupuesto positivo aunque se escriba SQL directamente.
    /// </summary>
    [Fact]
    public async Task RestriccionCheck_RechazaUnPresupuestoNoPositivoEscritoEnSql()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        Func<Task> accion = () => contexto.Database.ExecuteSqlRawAsync(@"
            INSERT INTO licitaciones
                (id, codigo, codigo_normalizado, titulo, estado, fecha_cierre,
                 presupuesto_estimado_crc, created_at, updated_at)
            VALUES
                (gen_random_uuid(), 'LIC-CHECK', 'LIC-CHECK', 'Titulo', 0, now() + interval '7 days',
                 0, now(), now());");

        var excepcion = await accion.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task RestriccionCheck_RechazaUnMontoDeOfertaNoPositivo()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        Licitacion licitacion =
            Licitacion.Crear("LIC-2026-080", "Titulo", 1_000_000m, Ahora.AddDays(7), Ahora);
        licitacion.Publicar(Ahora);
        Proveedor proveedor = Proveedor.Crear("Grupo Este", Ahora);

        contexto.Licitaciones.Add(licitacion);
        contexto.Proveedores.Add(proveedor);
        await contexto.SaveChangesAsync();

        Func<Task> accion = () => contexto.Database.ExecuteSqlRawAsync($@"
            INSERT INTO ofertas
                (id, licitacion_id, proveedor_id, monto_ofertado_crc, fecha_registro, updated_at)
            VALUES
                (gen_random_uuid(), '{licitacion.Id}', '{proveedor.Id}', -1, now(), now());");

        var excepcion = await accion.Should().ThrowAsync<PostgresException>();
        excepcion.Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }

    /// <summary>
    /// Los montos deben conservar sus dos decimales exactos al persistirse y recuperarse.
    /// Con un tipo de punto flotante, el valor volveria alterado.
    /// </summary>
    [Fact]
    public async Task Montos_ConservanLaPrecisionDecimalAlPersistirYRecuperar()
    {
        Guid id;

        await using (LicitacionesDbContext escritura = _postgres.CrearContexto())
        {
            Licitacion licitacion = Licitacion.Crear(
                "LIC-2026-090",
                "Titulo",
                1_234_567.89m,
                Ahora.AddDays(7),
                Ahora);

            escritura.Licitaciones.Add(licitacion);
            await escritura.SaveChangesAsync();

            id = licitacion.Id;
        }

        await using LicitacionesDbContext lectura = _postgres.CrearContexto();
        Licitacion recuperada = await lectura.Licitaciones.SingleAsync(l => l.Id == id);

        recuperada.PresupuestoEstimadoCrc.Should().Be(1_234_567.89m);
    }
}
