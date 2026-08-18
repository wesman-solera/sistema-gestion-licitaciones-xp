using FluentAssertions;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enums;
using Licitaciones.Domain.ObjetosValor;
using Licitaciones.Domain.Servicios;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Verifica la seleccion de la mejor oferta y la clasificacion del ahorro (seccion 8.6).
/// </summary>
public sealed class EvaluadorOfertasPruebas
{
    private readonly RelojFijo _reloj = new();

    [Fact]
    public void Evaluar_SinOfertas_DevuelveSinOfertasValidas()
    {
        ResultadoEvaluacionOfertas resultado = EvaluadorOfertas.Evaluar([], 1_000_000m);

        resultado.MejorOferta.Should().BeNull();
        resultado.PorcentajeAhorro.Should().BeNull();
        resultado.Clasificacion.Should().Be(ClasificacionAhorro.SinOfertasValidas);
        resultado.EtiquetaClasificacion.Should().Be("Sin ofertas validas");
        resultado.CantidadOfertas.Should().Be(0);
    }

    [Fact]
    public void Evaluar_ConVariasOfertas_EligeLaDeMenorMonto()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj);

        Oferta cara = Constructores.Oferta(licitacion, _reloj, 950_000m);
        Oferta barata = Constructores.Oferta(licitacion, _reloj, 700_000m);
        Oferta intermedia = Constructores.Oferta(licitacion, _reloj, 800_000m);

        ResultadoEvaluacionOfertas resultado =
            EvaluadorOfertas.Evaluar([cara, barata, intermedia], licitacion.PresupuestoEstimadoCrc);

        resultado.MejorOferta.Should().BeSameAs(barata);
        resultado.CantidadOfertas.Should().Be(3);
    }

    /// <summary>
    /// En empate de monto gana la oferta registrada primero (seccion 8.6).
    /// </summary>
    [Fact]
    public void Evaluar_ConEmpateDeMonto_EligeLaRegistradaPrimero()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj);

        Oferta primera = Constructores.Oferta(licitacion, _reloj, 800_000m);

        _reloj.Avanzar(TimeSpan.FromMinutes(30));
        Oferta segunda = Constructores.Oferta(licitacion, _reloj, 800_000m);

        // Se pasan en orden inverso para comprobar que el desempate no depende del orden de
        // la coleccion recibida, sino de la fecha de registro.
        ResultadoEvaluacionOfertas resultado =
            EvaluadorOfertas.Evaluar([segunda, primera], licitacion.PresupuestoEstimadoCrc);

        resultado.MejorOferta.Should().BeSameAs(primera);
        resultado.MejorOferta!.FechaRegistro.Should().BeBefore(segunda.FechaRegistro);
    }

    [Theory]
    // Ahorro de exactamente 10 %: el umbral es inclusivo.
    [InlineData(1_000_000, 900_000, 10.00, ClasificacionAhorro.OfertaConveniente)]
    // Ahorro muy por encima del umbral.
    [InlineData(1_000_000, 500_000, 50.00, ClasificacionAhorro.OfertaConveniente)]
    // Ahorro mayor que cero pero menor que 10 %.
    [InlineData(1_000_000, 950_000, 5.00, ClasificacionAhorro.OfertaAceptable)]
    // Justo por debajo del umbral: no debe ascender a conveniente.
    [InlineData(1_000_000, 900_001, 9.99, ClasificacionAhorro.OfertaAceptable)]
    // Oferta igual al presupuesto: valida, sin ahorro.
    [InlineData(1_000_000, 1_000_000, 0.00, ClasificacionAhorro.OfertaValidaSinAhorro)]
    public void Evaluar_ClasificaSegunElAhorro(
        decimal presupuesto,
        decimal montoOferta,
        decimal ahorroEsperado,
        ClasificacionAhorro clasificacionEsperada)
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj, presupuestoCrc: presupuesto);
        Oferta oferta = Constructores.Oferta(licitacion, _reloj, montoOferta);

        ResultadoEvaluacionOfertas resultado = EvaluadorOfertas.Evaluar([oferta], presupuesto);

        resultado.PorcentajeAhorro.Should().Be(ahorroEsperado);
        resultado.Clasificacion.Should().Be(clasificacionEsperada);
    }

    [Fact]
    public void Evaluar_EtiquetasCoincidenConElTextoDelEnunciado()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj);

        EvaluadorOfertas
            .Evaluar([Constructores.Oferta(licitacion, _reloj, 500_000m)], 1_000_000m)
            .EtiquetaClasificacion.Should().Be("Oferta conveniente");

        EvaluadorOfertas
            .Evaluar([Constructores.Oferta(licitacion, _reloj, 990_000m)], 1_000_000m)
            .EtiquetaClasificacion.Should().Be("Oferta aceptable");

        EvaluadorOfertas
            .Evaluar([Constructores.Oferta(licitacion, _reloj, 1_000_000m)], 1_000_000m)
            .EtiquetaClasificacion.Should().Be("Oferta valida sin ahorro");
    }

    [Fact]
    public void CalcularPorcentajeAhorro_AplicaLaFormulaDelEnunciado()
    {
        // ((1 000 000 - 850 000) / 1 000 000) x 100 = 15
        decimal ahorro = EvaluadorOfertas.CalcularPorcentajeAhorro(1_000_000m, 850_000m);

        ahorro.Should().Be(15m);
    }

    [Fact]
    public void CalcularPorcentajeAhorro_ConPresupuestoNoPositivo_Falla()
    {
        Action accion = () => EvaluadorOfertas.CalcularPorcentajeAhorro(0m, 100m);

        accion.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// El calculo debe hacerse con decimal: un tipo de punto flotante introduciria un error de
    /// representacion que aqui se detectaria como una diferencia en el ultimo decimal.
    /// </summary>
    [Fact]
    public void CalcularPorcentajeAhorro_MantieneLaPrecisionDecimal()
    {
        decimal ahorro = EvaluadorOfertas.CalcularPorcentajeAhorro(3m, 1m);

        // 2/3 x 100 = 66,666... El valor exacto en decimal conserva muchos mas digitos que
        // los que un double podria representar sin error.
        ahorro.Should().BeApproximately(66.6666666666666666666666666m, 0.0000000000000000000000001m);
    }
}
