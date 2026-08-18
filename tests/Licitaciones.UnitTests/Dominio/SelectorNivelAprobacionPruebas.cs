using FluentAssertions;
using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Servicios;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>Seleccion del aprobador desde la tabla parametrizable (seccion 8.7).</summary>
public sealed class SelectorNivelAprobacionPruebas
{
    private readonly RelojFijo _reloj = new();

    /// <summary>
    /// Los montos elegidos recorren los bordes de cada rango del enunciado: el minimo absoluto,
    /// el maximo de cada tramo y el primer valor del siguiente. Es donde aparecen los errores
    /// de comparacion por uno.
    /// </summary>
    [Theory]
    [InlineData(0.01, "Encargado de area")]
    [InlineData(500_000, "Encargado de area")]
    [InlineData(999_999.99, "Encargado de area")]
    [InlineData(1_000_000.00, "Gerencia")]
    [InlineData(5_000_000, "Gerencia")]
    [InlineData(9_999_999.99, "Gerencia")]
    [InlineData(10_000_000.00, "Junta Directiva")]
    [InlineData(500_000_000, "Junta Directiva")]
    public void Seleccionar_DevuelveElAprobadorDelRangoQueCubreElMonto(
        decimal monto,
        string aprobadorEsperado)
    {
        var niveles = Constructores.NivelesDelEnunciado(_reloj);

        NivelAprobacion? nivel = SelectorNivelAprobacion.Seleccionar(monto, niveles);

        nivel.Should().NotBeNull();
        nivel!.Aprobador.Should().Be(aprobadorEsperado);
    }

    [Fact]
    public void Seleccionar_ConTablaVacia_DevuelveNulo()
    {
        SelectorNivelAprobacion.Seleccionar(500_000m, []).Should().BeNull();
    }

    /// <summary>
    /// El rango mas bajo empieza en 0,01. Un monto menor no esta cubierto por ninguna fila y el
    /// selector debe admitirlo en lugar de inventar un aprobador.
    /// </summary>
    [Fact]
    public void Seleccionar_ConMontoPorDebajoDelPrimerRango_DevuelveNulo()
    {
        var niveles = Constructores.NivelesDelEnunciado(_reloj);

        SelectorNivelAprobacion.Seleccionar(0.005m, niveles).Should().BeNull();
    }

    [Fact]
    public void SeleccionarObligatorio_SinRangoAplicable_Falla()
    {
        var niveles = Constructores.NivelesDelEnunciado(_reloj);

        Action accion = () => SelectorNivelAprobacion.SeleccionarObligatorio(0.001m, niveles);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.SinNivelAprobacionAplicable);
    }

    /// <summary>
    /// El resultado no debe depender del orden en que la base de datos devuelva las filas.
    /// </summary>
    [Fact]
    public void Seleccionar_NoDependeDelOrdenDeLaColeccion()
    {
        var niveles = Constructores.NivelesDelEnunciado(_reloj).Reverse().ToList();

        SelectorNivelAprobacion.Seleccionar(500_000m, niveles)!
            .Aprobador.Should().Be("Encargado de area");
    }

    [Fact]
    public void AsegurarConjuntoValido_ConLosRangosDelEnunciado_NoLanza()
    {
        var niveles = Constructores.NivelesDelEnunciado(_reloj);

        Action accion = () => SelectorNivelAprobacion.AsegurarConjuntoValido([.. niveles]);

        accion.Should().NotThrow();
    }

    [Fact]
    public void AsegurarConjuntoValido_ConRangosTraslapados_Falla()
    {
        List<NivelAprobacion> niveles =
        [
            NivelAprobacion.Crear(1m, 1_000_000m, "Encargado de area", _reloj.AhoraUtc),
            NivelAprobacion.Crear(500_000m, 2_000_000m, "Gerencia", _reloj.AhoraUtc)
        ];

        Action accion = () => SelectorNivelAprobacion.AsegurarConjuntoValido(niveles);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.RangosAprobacionTraslapados);
    }

    [Fact]
    public void AsegurarConjuntoValido_ConDosRangosAbiertos_Falla()
    {
        List<NivelAprobacion> niveles =
        [
            NivelAprobacion.Crear(1m, null, "Gerencia", _reloj.AhoraUtc),
            NivelAprobacion.Crear(10_000_000m, null, "Junta Directiva", _reloj.AhoraUtc)
        ];

        Action accion = () => SelectorNivelAprobacion.AsegurarConjuntoValido(niveles);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.RangoAbiertoDuplicado);
    }

    /// <summary>
    /// Un rango abierto se traslapa con cualquier rango cerrado que empiece por encima de su
    /// minimo, porque su limite superior es infinito.
    /// </summary>
    [Fact]
    public void AsegurarConjuntoValido_RangoAbiertoQueEngullePorArriba_Falla()
    {
        List<NivelAprobacion> niveles =
        [
            NivelAprobacion.Crear(1m, null, "Junta Directiva", _reloj.AhoraUtc),
            NivelAprobacion.Crear(5_000_000m, 9_000_000m, "Gerencia", _reloj.AhoraUtc)
        ];

        Action accion = () => SelectorNivelAprobacion.AsegurarConjuntoValido(niveles);

        accion.Should().Throw<ReglaNegocioVioladaException>();
    }

    [Fact]
    public void Crear_ConMaximoMenorQueElMinimo_Falla()
    {
        Action accion = () => NivelAprobacion.Crear(1_000_000m, 500_000m, "Gerencia", _reloj.AhoraUtc);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.RangoAprobacionInvalido);
    }

    [Fact]
    public void Cubre_EsInclusivoEnAmbosExtremos()
    {
        NivelAprobacion nivel = NivelAprobacion.Crear(100m, 200m, "Encargado", _reloj.AhoraUtc);

        nivel.Cubre(100m).Should().BeTrue();
        nivel.Cubre(200m).Should().BeTrue();
        nivel.Cubre(99.99m).Should().BeFalse();
        nivel.Cubre(200.01m).Should().BeFalse();
    }
}
