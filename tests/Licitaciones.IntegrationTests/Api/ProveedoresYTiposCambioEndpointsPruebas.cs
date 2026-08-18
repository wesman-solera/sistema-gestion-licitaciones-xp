using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Constantes;
using Licitaciones.IntegrationTests.Infraestructura;

namespace Licitaciones.IntegrationTests.Api;

/// <summary>Endpoints de proveedores y de tipos de cambio contra infraestructura real.</summary>
public sealed class ProveedoresYTiposCambioEndpointsPruebas : PruebaApiBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Inicializa la prueba.</summary>
    /// <param name="postgres">Contenedor de PostgreSQL.</param>
    public ProveedoresYTiposCambioEndpointsPruebas(PostgresFixture postgres)
        : base(postgres)
    {
    }

    [Fact]
    public async Task Post_Proveedor_ConNombreValido_DevuelveCreated()
    {
        HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new CrearProveedorRequest("Servicios Tecnicos S.A."),
            Json);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        ProveedorDto? creado = await respuesta.Content.ReadFromJsonAsync<ProveedorDto>(Json);

        creado!.Nombre.Should().Be("Servicios Tecnicos S.A.");
        creado.NombreNormalizado.Should().Be("SERVICIOS TECNICOS S.A.");
    }

    /// <summary>
    /// Los tres nombres del ejemplo de la seccion 8.3 deben colisionar entre si.
    /// </summary>
    [Theory]
    [InlineData(" empresa central")]
    [InlineData("EMPRESA  CENTRAL")]
    [InlineData("EmPrEsA CeNtRaL")]
    public async Task Post_Proveedor_ConNombreEquivalente_DevuelveConflict(string nombreEquivalente)
    {
        await Cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new CrearProveedorRequest("Empresa Central"),
            Json);

        HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new CrearProveedorRequest(nombreEquivalente),
            Json);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);

        JsonDocument problema = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        problema.RootElement.GetProperty("codigoError").GetString()
            .Should().Be(CodigosError.NombreProveedorDuplicado);
    }

    [Theory]
    [InlineData("Empresa @ Central")]
    [InlineData("Proveedor #1")]
    [InlineData("Servicios & Mas")]
    public async Task Post_Proveedor_ConCaracteresNoPermitidos_DevuelveBadRequest(string nombre)
    {
        HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new CrearProveedorRequest(nombre),
            Json);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_TipoCambioActivo_DevuelveElDeLaSemilla()
    {
        TipoCambioDto? activo =
            await Cliente.GetFromJsonAsync<TipoCambioDto>("/api/v1/tipos-cambio/activo", Json);

        activo.Should().NotBeNull();
        activo!.Activo.Should().BeTrue();
        activo.CrcPorUsd.Should().BeGreaterThan(0m);
    }

    /// <summary>
    /// Activar un tipo de cambio nuevo debe desactivar el anterior en la misma transaccion, de
    /// modo que en ningun momento haya cero ni dos activos (seccion 8.8 y 11).
    /// </summary>
    [Fact]
    public async Task Patch_ActivarTipoCambio_DesactivaElAnterior()
    {
        TipoCambioDto anterior =
            (await Cliente.GetFromJsonAsync<TipoCambioDto>("/api/v1/tipos-cambio/activo", Json))!;

        HttpResponseMessage creacion = await Cliente.PostAsJsonAsync(
            "/api/v1/tipos-cambio",
            new CrearTipoCambioRequest(540.50m, DateTimeOffset.UtcNow, Activo: false),
            Json);

        creacion.EnsureSuccessStatusCode();
        TipoCambioDto nuevo = (await creacion.Content.ReadFromJsonAsync<TipoCambioDto>(Json))!;

        HttpResponseMessage activacion =
            await Cliente.PatchAsync($"/api/v1/tipos-cambio/{nuevo.Id}/activar", content: null);

        activacion.StatusCode.Should().Be(HttpStatusCode.OK);

        TipoCambioDto activoFinal =
            (await Cliente.GetFromJsonAsync<TipoCambioDto>("/api/v1/tipos-cambio/activo", Json))!;

        activoFinal.Id.Should().Be(nuevo.Id);
        activoFinal.Id.Should().NotBe(anterior.Id);

        // Restaurar el estado inicial para no afectar a las demas pruebas de la coleccion.
        await Cliente.PatchAsync($"/api/v1/tipos-cambio/{anterior.Id}/activar", content: null);
    }

    /// <summary>
    /// El tipo de cambio activo no puede eliminarse: dejaria al sistema sin poder convertir.
    /// </summary>
    [Fact]
    public async Task Delete_TipoCambioActivo_DevuelveUnprocessableEntity()
    {
        TipoCambioDto activo =
            (await Cliente.GetFromJsonAsync<TipoCambioDto>("/api/v1/tipos-cambio/activo", Json))!;

        HttpResponseMessage respuesta =
            await Cliente.DeleteAsync($"/api/v1/tipos-cambio/{activo.Id}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>
    /// Los montos deben viajar con su equivalente en dolares calculado con el tipo de cambio
    /// activo, sin que el valor en colones se altere (seccion 8.8).
    /// </summary>
    [Fact]
    public async Task Get_Licitacion_IncluyeElEquivalenteEnDolaresYLaFechaDelTipoDeCambio()
    {
        TipoCambioDto tipoCambio =
            (await Cliente.GetFromJsonAsync<TipoCambioDto>("/api/v1/tipos-cambio/activo", Json))!;

        HttpResponseMessage creacion = await Cliente.PostAsJsonAsync(
            "/api/v1/licitaciones",
            new CrearLicitacionRequest(
                "LIC-MONEDA-001",
                "Titulo",
                1_000_000m,
                DateTimeOffset.UtcNow.AddDays(10)),
            Json);

        creacion.EnsureSuccessStatusCode();
        LicitacionDetalleDto detalle =
            (await creacion.Content.ReadFromJsonAsync<LicitacionDetalleDto>(Json))!;

        decimal esperadoUsd = decimal.Round(
            1_000_000m / tipoCambio.CrcPorUsd,
            2,
            MidpointRounding.AwayFromZero);

        detalle.PresupuestoEstimado.Crc.Should().Be(1_000_000m);
        detalle.PresupuestoEstimado.Usd.Should().Be(esperadoUsd);
        detalle.TipoCambioAplicado!.CrcPorUsd.Should().Be(tipoCambio.CrcPorUsd);
    }

    [Fact]
    public async Task Get_Swagger_DevuelveElDocumentoOpenApi()
    {
        HttpResponseMessage respuesta = await Cliente.GetAsync("/swagger/v1/swagger.json");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        string documento = await respuesta.Content.ReadAsStringAsync();

        documento.Should().Contain("/api/v1/licitaciones");
        documento.Should().Contain("/api/v1/proveedores");
        documento.Should().Contain("/api/v1/ofertas");
        documento.Should().Contain("/api/v1/niveles-aprobacion");
        documento.Should().Contain("/api/v1/tipos-cambio");
    }

    [Fact]
    public async Task Get_Health_DevuelveOk()
    {
        HttpResponseMessage respuesta = await Cliente.GetAsync("/health");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
