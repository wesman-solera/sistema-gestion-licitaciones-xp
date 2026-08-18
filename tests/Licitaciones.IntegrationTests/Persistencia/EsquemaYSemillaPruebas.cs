using Licitaciones.IntegrationTests.Infraestructura;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.IntegrationTests.Persistencia;

/// <summary>Verifica que la migracion cree el esquema y los datos semilla esperados.</summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class EsquemaYSemillaPruebas
{
    private readonly PostgresFixture _postgres;

    /// <summary>Inicializa la prueba con el contenedor compartido.</summary>
    /// <param name="postgres">Contenedor de PostgreSQL.</param>
    public EsquemaYSemillaPruebas(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Migraciones_SeAplicanSinDejarPendientes()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        var pendientes = await contexto.Database.GetPendingMigrationsAsync();

        pendientes.Should().BeEmpty();
    }

    [Fact]
    public async Task Migraciones_CreanLasCincoTablasDelModelo()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        var tablas = await contexto.Database
            .SqlQuery<string>($@"
                SELECT table_name AS ""Value""
                FROM information_schema.tables
                WHERE table_schema = 'public' AND table_type = 'BASE TABLE'")
            .ToListAsync();

        tablas.Should().Contain(
        [
            "licitaciones",
            "proveedores",
            "ofertas",
            "niveles_aprobacion",
            "tipos_cambio"
        ]);
    }

    /// <summary>
    /// La semilla debe reproducir exactamente los tres rangos de la seccion 8.7 del enunciado.
    /// </summary>
    [Fact]
    public async Task Semilla_CargaLosTresNivelesDeAprobacionDelEnunciado()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        var niveles = await contexto.NivelesAprobacion
            .OrderBy(n => n.MontoMinimoCrc)
            .ToListAsync();

        niveles.Should().HaveCount(3);

        niveles[0].MontoMinimoCrc.Should().Be(0.01m);
        niveles[0].MontoMaximoCrc.Should().Be(999_999.99m);
        niveles[0].Aprobador.Should().Be("Encargado de area");

        niveles[1].MontoMinimoCrc.Should().Be(1_000_000.00m);
        niveles[1].MontoMaximoCrc.Should().Be(9_999_999.99m);
        niveles[1].Aprobador.Should().Be("Gerencia");

        niveles[2].MontoMinimoCrc.Should().Be(10_000_000.00m);
        niveles[2].MontoMaximoCrc.Should().BeNull("es el unico rango abierto");
        niveles[2].Aprobador.Should().Be("Junta Directiva");
    }

    [Fact]
    public async Task Semilla_CargaUnTipoDeCambioActivo()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        var activos = await contexto.TiposCambio.Where(t => t.Activo).ToListAsync();

        activos.Should().HaveCount(1);
        activos[0].CrcPorUsd.Should().BeGreaterThan(0m);
    }

    /// <summary>
    /// Los montos deben persistirse como numeric(18,2). Si la columna fuera de punto flotante,
    /// esta comprobacion fallaria.
    /// </summary>
    [Theory]
    [InlineData("licitaciones", "presupuesto_estimado_crc")]
    [InlineData("ofertas", "monto_ofertado_crc")]
    [InlineData("niveles_aprobacion", "monto_minimo_crc")]
    [InlineData("tipos_cambio", "crc_por_usd")]
    public async Task ColumnasMonetarias_UsanNumericConPrecisionExplicita(string tabla, string columna)
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        var tipos = await contexto.Database
            .SqlQuery<string>($@"
                SELECT data_type || '(' || numeric_precision || ',' || numeric_scale || ')' AS ""Value""
                FROM information_schema.columns
                WHERE table_name = {tabla} AND column_name = {columna}")
            .ToListAsync();

        tipos.Should().ContainSingle().Which.Should().Be("numeric(18,2)");
    }

    [Fact]
    public async Task IndicesUnicos_ExistenEnLaBaseDeDatos()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        var indices = await contexto.Database
            .SqlQuery<string>($@"
                SELECT indexname AS ""Value""
                FROM pg_indexes
                WHERE schemaname = 'public'")
            .ToListAsync();

        indices.Should().Contain(
        [
            "ux_licitaciones_codigo_normalizado",
            "ux_proveedores_nombre_normalizado",
            "ux_ofertas_licitacion_proveedor",
            "ux_tipos_cambio_unico_activo"
        ]);
    }
}
