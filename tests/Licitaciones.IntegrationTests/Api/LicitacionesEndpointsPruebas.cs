using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Constantes;
using Licitaciones.IntegrationTests.Infraestructura;

namespace Licitaciones.IntegrationTests.Api;

/// <summary>
/// Recorre los endpoints REST contra la aplicacion real y PostgreSQL real.
/// </summary>
/// <remarks>
/// Estas pruebas comprueban el contrato completo: codigo HTTP, cuerpo de la respuesta y forma
/// del ProblemDetails. Es lo que exige la seccion 12.2 al pedir pruebas de endpoints con
/// infraestructura real.
/// </remarks>
public sealed class LicitacionesEndpointsPruebas : PruebaApiBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Inicializa la prueba.</summary>
    /// <param name="postgres">Contenedor de PostgreSQL.</param>
    public LicitacionesEndpointsPruebas(PostgresFixture postgres)
        : base(postgres)
    {
    }

    [Fact]
    public async Task Post_CrearLicitacion_DevuelveCreatedYLaUbicacion()
    {
        var peticion = new CrearLicitacionRequest(
            "LIC-API-001",
            "Compra de servidores",
            5_000_000m,
            DateTimeOffset.UtcNow.AddDays(10));

        HttpResponseMessage respuesta =
            await Cliente.PostAsJsonAsync("/api/v1/licitaciones", peticion, Json);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        respuesta.Headers.Location.Should().NotBeNull();

        LicitacionDetalleDto? creada =
            await respuesta.Content.ReadFromJsonAsync<LicitacionDetalleDto>(Json);

        creada.Should().NotBeNull();
        creada!.Codigo.Should().Be("LIC-API-001");
        creada.Estado.Should().Be(Domain.Enums.EstadoLicitacion.Borrador);
    }

    [Fact]
    public async Task Post_ConCodigoDuplicado_DevuelveConflictConCodigoDeError()
    {
        var peticion = new CrearLicitacionRequest(
            "LIC-API-002",
            "Compra de licencias",
            2_000_000m,
            DateTimeOffset.UtcNow.AddDays(10));

        await Cliente.PostAsJsonAsync("/api/v1/licitaciones", peticion, Json);

        // Misma licitacion con otra grafia del codigo: debe detectarse como duplicada.
        var duplicada = peticion with { Codigo = "  lic-api-002  " };

        HttpResponseMessage respuesta =
            await Cliente.PostAsJsonAsync("/api/v1/licitaciones", duplicada, Json);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);

        JsonDocument problema = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        problema.RootElement.GetProperty("codigoError").GetString()
            .Should().Be(CodigosError.CodigoLicitacionDuplicado);
        problema.RootElement.TryGetProperty("correlacion", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Post_ConPresupuestoNoPositivo_DevuelveBadRequest()
    {
        var peticion = new CrearLicitacionRequest(
            "LIC-API-003",
            "Titulo",
            0m,
            DateTimeOffset.UtcNow.AddDays(10));

        HttpResponseMessage respuesta =
            await Cliente.PostAsJsonAsync("/api/v1/licitaciones", peticion, Json);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ConFechaDeCierrePasada_DevuelveUnprocessableEntity()
    {
        var peticion = new CrearLicitacionRequest(
            "LIC-API-004",
            "Titulo",
            1_000_000m,
            DateTimeOffset.UtcNow.AddDays(-1));

        HttpResponseMessage respuesta =
            await Cliente.PostAsJsonAsync("/api/v1/licitaciones", peticion, Json);

        respuesta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        JsonDocument problema = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        problema.RootElement.GetProperty("codigoError").GetString()
            .Should().Be(CodigosError.FechaCierreNoFutura);
    }

    [Fact]
    public async Task Get_LicitacionInexistente_DevuelveNotFound()
    {
        HttpResponseMessage respuesta =
            await Cliente.GetAsync($"/api/v1/licitaciones/{Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_TransicionDePublicadaABorrador_DevuelveConflict()
    {
        Guid id = await CrearLicitacionAsync("LIC-API-005", 3_000_000m);
        await PublicarAsync(id);

        HttpResponseMessage respuesta = await Cliente.PatchAsJsonAsync(
            $"/api/v1/licitaciones/{id}/estado",
            new CambiarEstadoRequest(Domain.Enums.EstadoLicitacion.Borrador),
            Json);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);

        JsonDocument problema = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        problema.RootElement.GetProperty("codigoError").GetString()
            .Should().Be(CodigosError.TransicionEstadoInvalida);
    }

    /// <summary>
    /// Flujo completo del enunciado (seccion 5.3) contra la API: crear, publicar, ofertar,
    /// rechazar la duplicada y la que supera el presupuesto, y consultar la mejor oferta.
    /// </summary>
    [Fact]
    public async Task FlujoCompleto_DesdeLaCreacionHastaLaMejorOferta()
    {
        Guid licitacionId = await CrearLicitacionAsync("LIC-API-FLUJO", 10_000_000m);
        await PublicarAsync(licitacionId);

        Guid proveedorA = await CrearProveedorAsync("Consorcio Alfa");
        Guid proveedorB = await CrearProveedorAsync("Consorcio Beta");

        // Oferta valida del primer proveedor.
        HttpResponseMessage primera = await Cliente.PostAsJsonAsync(
            $"/api/v1/licitaciones/{licitacionId}/ofertas",
            new RegistrarOfertaEnLicitacionRequest(proveedorA, 9_000_000m),
            Json);
        primera.StatusCode.Should().Be(HttpStatusCode.Created);

        // El mismo proveedor no puede ofertar dos veces.
        HttpResponseMessage duplicada = await Cliente.PostAsJsonAsync(
            $"/api/v1/licitaciones/{licitacionId}/ofertas",
            new RegistrarOfertaEnLicitacionRequest(proveedorA, 8_000_000m),
            Json);
        duplicada.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Una oferta por encima del presupuesto se rechaza.
        HttpResponseMessage excesiva = await Cliente.PostAsJsonAsync(
            $"/api/v1/licitaciones/{licitacionId}/ofertas",
            new RegistrarOfertaEnLicitacionRequest(proveedorB, 11_000_000m),
            Json);
        excesiva.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        // Oferta valida y mas baja del segundo proveedor.
        HttpResponseMessage segunda = await Cliente.PostAsJsonAsync(
            $"/api/v1/licitaciones/{licitacionId}/ofertas",
            new RegistrarOfertaEnLicitacionRequest(proveedorB, 7_500_000m),
            Json);
        segunda.StatusCode.Should().Be(HttpStatusCode.Created);

        EvaluacionLicitacionDto? evaluacion = await Cliente
            .GetFromJsonAsync<EvaluacionLicitacionDto>(
                $"/api/v1/licitaciones/{licitacionId}/mejor-oferta",
                Json);

        evaluacion.Should().NotBeNull();
        evaluacion!.MejorOferta!.Monto.Crc.Should().Be(7_500_000m);
        evaluacion.MejorOferta.NombreProveedor.Should().Be("Consorcio Beta");
        // ((10 000 000 - 7 500 000) / 10 000 000) x 100 = 25 %
        evaluacion.PorcentajeAhorro.Should().Be(25.00m);
        evaluacion.EtiquetaClasificacion.Should().Be("Oferta conveniente");
        // 7 500 000 cae en el segundo rango de la tabla semilla (1 000 000 a 9 999 999,99).
        evaluacion.Aprobador.Should().Be("Gerencia");
    }

    [Fact]
    public async Task Get_Listado_DevuelveLaEstructuraDePaginacion()
    {
        await CrearLicitacionAsync("LIC-API-PAG-1", 1_000_000m);
        await CrearLicitacionAsync("LIC-API-PAG-2", 2_000_000m);

        HttpResponseMessage respuesta =
            await Cliente.GetAsync("/api/v1/licitaciones?pagina=1&tamanoPagina=1");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonDocument cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());

        cuerpo.RootElement.GetProperty("elementos").GetArrayLength().Should().Be(1);
        cuerpo.RootElement.GetProperty("totalElementos").GetInt32().Should().BeGreaterThanOrEqualTo(2);
        cuerpo.RootElement.GetProperty("tieneSiguiente").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Delete_LicitacionSinOfertas_DevuelveNoContent()
    {
        Guid id = await CrearLicitacionAsync("LIC-API-DEL", 1_000_000m);

        HttpResponseMessage respuesta = await Cliente.DeleteAsync($"/api/v1/licitaciones/{id}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage verificacion = await Cliente.GetAsync($"/api/v1/licitaciones/{id}");
        verificacion.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Ningun mensaje de error debe filtrar detalles internos del servidor (seccion 10.2).
    /// </summary>
    [Fact]
    public async Task ProblemDetails_NoExponeDetallesInternos()
    {
        HttpResponseMessage respuesta =
            await Cliente.GetAsync($"/api/v1/licitaciones/{Guid.NewGuid()}");

        string cuerpo = await respuesta.Content.ReadAsStringAsync();

        cuerpo.Should().NotContainAny(
            "Npgsql",
            "SELECT",
            "at Licitaciones.",
            "Password",
            "Host=",
            "/src/",
            "StackTrace");
    }

    private async Task<Guid> CrearLicitacionAsync(string codigo, decimal presupuesto)
    {
        HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            "/api/v1/licitaciones",
            new CrearLicitacionRequest(codigo, "Titulo de prueba", presupuesto, DateTimeOffset.UtcNow.AddDays(10)),
            Json);

        respuesta.EnsureSuccessStatusCode();

        LicitacionDetalleDto detalle =
            (await respuesta.Content.ReadFromJsonAsync<LicitacionDetalleDto>(Json))!;

        return detalle.Id;
    }

    private async Task PublicarAsync(Guid id)
    {
        HttpResponseMessage respuesta = await Cliente.PatchAsJsonAsync(
            $"/api/v1/licitaciones/{id}/estado",
            new CambiarEstadoRequest(Domain.Enums.EstadoLicitacion.Publicada),
            Json);

        respuesta.EnsureSuccessStatusCode();
    }

    private async Task<Guid> CrearProveedorAsync(string nombre)
    {
        HttpResponseMessage respuesta = await Cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new CrearProveedorRequest(nombre),
            Json);

        respuesta.EnsureSuccessStatusCode();

        ProveedorDto proveedor = (await respuesta.Content.ReadFromJsonAsync<ProveedorDto>(Json))!;

        return proveedor.Id;
    }
}
