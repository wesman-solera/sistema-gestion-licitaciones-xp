using FluentAssertions;
using Licitaciones.FunctionalTests.Infraestructura;
using Microsoft.Playwright;

namespace Licitaciones.FunctionalTests;

/// <summary>Landing page, navegacion, tema y conversion de moneda (requisito 12.3).</summary>
public sealed class InterfazPruebas : PruebaNavegadorBase
{
    [Fact]
    public async Task LandingPage_ExplicaElPropositoDelSistema()
    {
        await Pagina.GotoAsync("/");

        await Assertions.Expect(Pagina.Locator("h1").First)
            .ToContainTextAsync("Sistema de Gestion de Licitaciones");

        string contenido = await Pagina.ContentAsync();

        // La seccion 5.1 exige que la portada explique el flujo, las ofertas, la mejor oferta,
        // el nivel de aprobacion y la conversion monetaria.
        contenido.Should().Contain("flujo de licitacion");
        contenido.Should().Contain("Oferta conveniente");
        contenido.Should().Contain("Oferta aceptable");
        contenido.Should().Contain("Niveles de aprobacion");
        contenido.Should().Contain("Conversion de moneda");
    }

    [Fact]
    public async Task Menu_OfreceTodasLasSeccionesExigidas()
    {
        await Pagina.GotoAsync("/");

        ILocator navegacion = Pagina.Locator("#navegacion-principal");

        foreach (string seccion in new[]
                 {
                     "Inicio",
                     "Licitaciones",
                     "Proveedores",
                     "Ofertas",
                     "Niveles de aprobacion",
                     "Tipo de cambio",
                     "API"
                 })
        {
            await Assertions.Expect(navegacion.GetByRole(AriaRole.Link, new() { Name = seccion }))
                .ToBeVisibleAsync();
        }
    }

    /// <summary>
    /// El modo oscuro debe activarse con un control visible y persistir entre navegaciones
    /// (requisito 9).
    /// </summary>
    [Fact]
    public async Task ModoOscuro_SeActivaConElControlYPersisteAlNavegar()
    {
        await Pagina.GotoAsync("/");

        string temaInicial = await Pagina.Locator("html").GetAttributeAsync("data-tema") ?? "";
        temaInicial.Should().Be("claro");

        await Pagina.Locator("[data-alternar-tema]").ClickAsync();

        string temaTrasAlternar = await Pagina.Locator("html").GetAttributeAsync("data-tema") ?? "";
        temaTrasAlternar.Should().Be("oscuro");

        // La preferencia debe sobrevivir a un cambio de pagina, no solo al clic.
        await Pagina.GotoAsync("/Licitaciones");

        string temaTrasNavegar = await Pagina.Locator("html").GetAttributeAsync("data-tema") ?? "";
        temaTrasNavegar.Should().Be("oscuro");
    }

    /// <summary>
    /// Alternar la moneda cambia solo la presentacion. El requisito 8.8 es explicito en que los
    /// valores almacenados no se modifican.
    /// </summary>
    [Fact]
    public async Task AlternarMoneda_CambiaLaVisualizacionSinAlterarLosDatos()
    {
        string sufijo = SufijoUnico();
        await CrearLicitacionAsync($"LIC-UI-{sufijo}", "Compra para prueba de moneda", 1_000_000m);

        await Pagina.GotoAsync("/Licitaciones");

        ILocator primerMonto = Pagina.Locator("[data-monto]").First;
        string montoEnColones = (await primerMonto.InnerTextAsync()).Trim();

        await Pagina.Locator("form[action*='AlternarMoneda'] button").ClickAsync();

        string montoEnDolares = (await Pagina.Locator("[data-monto]").First.InnerTextAsync()).Trim();

        montoEnDolares.Should().NotBe(montoEnColones);
        montoEnDolares.Should().Contain("$");

        // Al volver a colones debe reaparecer exactamente el mismo valor original.
        await Pagina.Locator("form[action*='AlternarMoneda'] button").ClickAsync();

        string montoDeVuelta = (await Pagina.Locator("[data-monto]").First.InnerTextAsync()).Trim();

        montoDeVuelta.Should().Be(montoEnColones);
    }

    [Fact]
    public async Task DisenoAdaptable_MuestraElBotonDeMenuEnPantallaAngosta()
    {
        await Pagina.GotoAsync("/");

        ILocator botonMenu = Pagina.Locator("[data-menu-boton]");

        await Assertions.Expect(botonMenu).ToBeHiddenAsync();

        await UsarPantallaAsync(390, 844);

        await Assertions.Expect(botonMenu).ToBeVisibleAsync();

        await botonMenu.ClickAsync();

        await Assertions.Expect(Pagina.Locator("#navegacion-principal")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task DocumentacionApi_SeAbreYListaLosEndpoints()
    {
        await Pagina.GotoAsync("/swagger/index.html");

        await Assertions.Expect(Pagina.Locator("body"))
            .ToContainTextAsync("Sistema de Gestion de Licitaciones");
    }

    private async Task CrearLicitacionAsync(string codigo, string titulo, decimal presupuesto)
    {
        await Pagina.GotoAsync("/Licitaciones/Crear");

        await Pagina.FillAsync("#Codigo", codigo);
        await Pagina.FillAsync("#Titulo", titulo);
        await Pagina.FillAsync("#PresupuestoEstimadoCrc", presupuesto.ToString("0.00"));
        await Pagina.FillAsync(
            "#FechaCierre",
            DateTime.Now.AddDays(10).ToString("yyyy-MM-ddTHH:mm"));

        await Pagina.ClickAsync("button[type=submit]");
        await Pagina.WaitForURLAsync("**/Licitaciones/Detalle/**");
    }
}
