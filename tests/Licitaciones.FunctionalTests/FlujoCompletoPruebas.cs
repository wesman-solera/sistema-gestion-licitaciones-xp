using Licitaciones.FunctionalTests.Infraestructura;
using Microsoft.Playwright;

namespace Licitaciones.FunctionalTests;

/// <summary>
/// Recorre desde el navegador el flujo funcional minimo de la seccion 5.3 del enunciado.
/// </summary>
/// <remarks>
/// Es la prueba que demuestra que el sistema funciona de punta a punta: registra un proveedor,
/// crea y publica una licitacion, registra una oferta valida, comprueba que se rechacen la
/// duplicada y la que supera el presupuesto, y verifica la mejor oferta con su clasificacion y
/// su nivel de aprobacion.
/// </remarks>
public sealed class FlujoCompletoPruebas : PruebaNavegadorBase
{
    [Fact]
    public async Task FlujoMinimo_DesdeElRegistroDelProveedorHastaLaMejorOferta()
    {
        string sufijo = SufijoUnico();
        string codigoLicitacion = $"LIC-E2E-{sufijo}";
        string proveedorUno = $"Consorcio Alfa {sufijo}";
        string proveedorDos = $"Consorcio Beta {sufijo}";

        // 1. Registrar dos proveedores con nombres validos y unicos.
        await RegistrarProveedorAsync(proveedorUno);
        await RegistrarProveedorAsync(proveedorDos);

        // 2. Crear la licitacion con codigo unico y fecha de cierre elegida en el calendario.
        await CrearLicitacionAsync(codigoLicitacion, "Compra de equipo de laboratorio", 10_000_000m);

        await Assertions.Expect(Pagina.Locator("[data-estado]")).ToContainTextAsync("Borrador");

        // 3. Publicar mediante una transicion permitida.
        await Pagina.Locator("[data-transicion='Publicada']").ClickAsync();
        await Assertions.Expect(Pagina.Locator("[data-estado]")).ToContainTextAsync("Publicada");

        string urlDetalle = Pagina.Url;

        // 4. Registrar una oferta valida.
        await RegistrarOfertaAsync(codigoLicitacion, proveedorUno, 9_000_000m);
        await Assertions.Expect(Pagina.Locator("[data-mensaje='exito']"))
            .ToContainTextAsync("La oferta se registro correctamente");

        // 5. La segunda oferta del mismo proveedor debe rechazarse.
        await RegistrarOfertaAsync(codigoLicitacion, proveedorUno, 8_000_000m, esperarExito: false);
        (await Pagina.ContentAsync()).Should().Contain("ya registro una oferta para esta licitacion");

        // 6. Una oferta superior al presupuesto debe rechazarse.
        await RegistrarOfertaAsync(codigoLicitacion, proveedorDos, 12_000_000m, esperarExito: false);
        (await Pagina.ContentAsync()).Should().Contain("no puede superar el presupuesto");

        // 7. Una oferta mas baja y valida del segundo proveedor.
        await RegistrarOfertaAsync(codigoLicitacion, proveedorDos, 7_500_000m);

        // 8. Consultar la mejor oferta, su clasificacion y el nivel de aprobacion.
        await Pagina.GotoAsync(urlDetalle);

        await Assertions.Expect(Pagina.Locator("[data-clasificacion]"))
            .ToContainTextAsync("Oferta conveniente");

        // ((10 000 000 - 7 500 000) / 10 000 000) x 100 = 25,00 %
        await Assertions.Expect(Pagina.Locator("[data-ahorro]")).ToContainTextAsync("25,00");

        // 7 500 000 cae en el rango de 1 000 000 a 9 999 999,99 de la tabla semilla.
        await Assertions.Expect(Pagina.Locator("[data-aprobador]")).ToContainTextAsync("Gerencia");

        await Assertions.Expect(Pagina.Locator("[data-mejor-oferta]")).ToContainTextAsync("7");
    }

    /// <summary>
    /// El nombre duplicado debe rechazarse aunque se escriba con otra combinacion de espacios y
    /// mayusculas (seccion 8.3).
    /// </summary>
    [Fact]
    public async Task Proveedor_ConNombreEquivalente_MuestraElErrorJuntoAlCampo()
    {
        string nombre = $"Empresa Central {SufijoUnico()}";

        await RegistrarProveedorAsync(nombre);

        await Pagina.GotoAsync("/Proveedores/Crear");
        await Pagina.FillAsync("#Nombre", $"   {nombre.ToUpperInvariant()}   ");
        await Pagina.ClickAsync("button[type=submit]");

        // El mensaje debe aparecer junto al campo, no en una pagina de error generica.
        await Assertions.Expect(Pagina.Locator(".campo__error, .field-validation-error").First)
            .ToContainTextAsync("Ya existe un proveedor");
    }

    [Fact]
    public async Task Proveedor_ConCaracteresNoPermitidos_MuestraElErrorDeValidacion()
    {
        await Pagina.GotoAsync("/Proveedores/Crear");

        await Pagina.FillAsync("#Nombre", "Empresa @ Central");
        await Pagina.ClickAsync("button[type=submit]");

        (await Pagina.ContentAsync()).Should().Contain("solo admite letras, numeros, espacios");
    }

    [Fact]
    public async Task Licitacion_ConCodigoDuplicado_MuestraElErrorJuntoAlCampo()
    {
        string codigo = $"LIC-DUP-{SufijoUnico()}";

        await CrearLicitacionAsync(codigo, "Primera licitacion", 1_000_000m);

        await Pagina.GotoAsync("/Licitaciones/Crear");
        await Pagina.FillAsync("#Codigo", $"  {codigo.ToLowerInvariant()}  ");
        await Pagina.FillAsync("#Titulo", "Segunda licitacion");
        await Pagina.FillAsync("#PresupuestoEstimadoCrc", "500000.00");
        await Pagina.FillAsync("#FechaCierre", DateTime.Now.AddDays(10).ToString("yyyy-MM-ddTHH:mm"));
        await Pagina.ClickAsync("button[type=submit]");

        (await Pagina.ContentAsync()).Should().Contain("Ya existe una licitacion registrada con ese codigo");
    }

    [Fact]
    public async Task Licitacion_ConPresupuestoCero_MuestraElErrorDeValidacion()
    {
        await Pagina.GotoAsync("/Licitaciones/Crear");

        await Pagina.FillAsync("#Codigo", $"LIC-CERO-{SufijoUnico()}");
        await Pagina.FillAsync("#Titulo", "Licitacion con presupuesto invalido");
        await Pagina.FillAsync("#PresupuestoEstimadoCrc", "0");
        await Pagina.FillAsync("#FechaCierre", DateTime.Now.AddDays(10).ToString("yyyy-MM-ddTHH:mm"));
        await Pagina.ClickAsync("button[type=submit]");

        (await Pagina.ContentAsync()).Should().Contain("mayor que cero");
    }

    /// <summary>
    /// Una licitacion sin ofertas debe reportar el texto exacto que exige la seccion 8.6.
    /// </summary>
    [Fact]
    public async Task Licitacion_SinOfertas_MuestraSinOfertasValidas()
    {
        await CrearLicitacionAsync($"LIC-VACIA-{SufijoUnico()}", "Licitacion sin ofertas", 2_000_000m);

        await Assertions.Expect(Pagina.Locator("[data-clasificacion]"))
            .ToContainTextAsync("Sin ofertas validas");
    }

    /// <summary>
    /// La eliminacion debe pedir confirmacion en una pantalla propia (seccion 8.9).
    /// </summary>
    [Fact]
    public async Task Eliminar_PideConfirmacionAntesDeBorrar()
    {
        string codigo = $"LIC-DEL-{SufijoUnico()}";
        await CrearLicitacionAsync(codigo, "Licitacion a eliminar", 1_000_000m);

        await Pagina.GotoAsync("/Licitaciones");
        await Pagina.Locator($"tr[data-licitacion='{codigo}'] a:has-text('Eliminar')").ClickAsync();

        await Assertions.Expect(Pagina.Locator("h1")).ToContainTextAsync("Eliminar licitacion");
        await Assertions.Expect(Pagina.GetByRole(AriaRole.Button, new() { Name = "Si, eliminar" }))
            .ToBeVisibleAsync();

        await Pagina.GetByRole(AriaRole.Button, new() { Name = "Si, eliminar" }).ClickAsync();

        await Assertions.Expect(Pagina.Locator("[data-mensaje='exito']")).ToBeVisibleAsync();
        (await Pagina.ContentAsync()).Should().NotContain(codigo);
    }

    [Fact]
    public async Task NivelesAprobacion_MuestraLosTresRangosDeLaSemilla()
    {
        await Pagina.GotoAsync("/NivelesAprobacion");

        string contenido = await Pagina.ContentAsync();

        contenido.Should().Contain("Encargado de area");
        contenido.Should().Contain("Gerencia");
        contenido.Should().Contain("Junta Directiva");
        contenido.Should().Contain("Sin limite");
    }

    private async Task RegistrarProveedorAsync(string nombre)
    {
        await Pagina.GotoAsync("/Proveedores/Crear");
        await Pagina.FillAsync("#Nombre", nombre);
        await Pagina.ClickAsync("button[type=submit]");

        await Assertions.Expect(Pagina.Locator("[data-mensaje='exito']")).ToBeVisibleAsync();
    }

    private async Task CrearLicitacionAsync(string codigo, string titulo, decimal presupuesto)
    {
        await Pagina.GotoAsync("/Licitaciones/Crear");

        await Pagina.FillAsync("#Codigo", codigo);
        await Pagina.FillAsync("#Titulo", titulo);
        await Pagina.FillAsync("#PresupuestoEstimadoCrc", presupuesto.ToString("0.00"));
        await Pagina.FillAsync("#FechaCierre", DateTime.Now.AddDays(10).ToString("yyyy-MM-ddTHH:mm"));

        await Pagina.ClickAsync("button[type=submit]");
        await Pagina.WaitForURLAsync("**/Licitaciones/Detalle/**");
    }

    private async Task RegistrarOfertaAsync(
        string codigoLicitacion,
        string nombreProveedor,
        decimal monto,
        bool esperarExito = true)
    {
        await Pagina.GotoAsync("/Ofertas/Crear");

        await Pagina.SelectOptionAsync(
            "#LicitacionId",
            new SelectOptionValue { Label = await ObtenerEtiquetaLicitacionAsync(codigoLicitacion) });

        await Pagina.SelectOptionAsync(
            "#ProveedorId",
            new SelectOptionValue { Label = nombreProveedor });

        await Pagina.FillAsync("#MontoOfertadoCrc", monto.ToString("0.00"));
        await Pagina.ClickAsync("button[type=submit]");

        if (esperarExito)
        {
            await Assertions.Expect(Pagina.Locator("[data-mensaje='exito']")).ToBeVisibleAsync();
        }
    }

    /// <summary>
    /// Obtiene el texto completo de la opcion del desplegable que corresponde a una licitacion.
    /// </summary>
    /// <param name="codigoLicitacion">Codigo de la licitacion buscada.</param>
    /// <returns>La etiqueta exacta de la opcion.</returns>
    /// <remarks>
    /// La opcion incluye el codigo, el titulo y el presupuesto formateado, por lo que no puede
    /// construirse a mano desde la prueba sin duplicar el formato de la vista.
    /// </remarks>
    private async Task<string> ObtenerEtiquetaLicitacionAsync(string codigoLicitacion)
    {
        IReadOnlyList<string> etiquetas = await Pagina
            .Locator("#LicitacionId option")
            .AllInnerTextsAsync();

        return etiquetas.First(e => e.Contains(codigoLicitacion, StringComparison.Ordinal)).Trim();
    }
}
