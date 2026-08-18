using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>Reglas de aceptacion de ofertas (secciones 8.2 y 8.5).</summary>
public sealed class OfertaPruebas
{
    private readonly RelojFijo _reloj = new();

    [Fact]
    public void Registrar_EnLicitacionPublicadaYVigente_CreaLaOferta()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj);

        Oferta oferta = Constructores.CrearOferta(licitacion, _reloj, 750_000m);

        oferta.MontoOfertadoCrc.Should().Be(750_000m);
        oferta.LicitacionId.Should().Be(licitacion.Id);
        oferta.FechaRegistro.Should().Be(_reloj.AhoraUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100_000)]
    public void Registrar_ConMontoNoPositivo_Falla(decimal monto)
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj);

        Action accion = () => Constructores.CrearOferta(licitacion, _reloj, monto);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.MontoNoPositivo);
    }

    [Fact]
    public void Registrar_ConMontoSuperiorAlPresupuesto_Falla()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj, presupuestoCrc: 1_000_000m);

        Action accion = () => Constructores.CrearOferta(licitacion, _reloj, 1_000_000.01m);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.OfertaSuperaPresupuesto);
    }

    /// <summary>
    /// La seccion 8.5 es explicita: una oferta igual al presupuesto es valida. Solo se rechaza
    /// la que lo supera.
    /// </summary>
    [Fact]
    public void Registrar_ConMontoIgualAlPresupuesto_EsValida()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj, presupuestoCrc: 1_000_000m);

        Oferta oferta = Constructores.CrearOferta(licitacion, _reloj, 1_000_000m);

        oferta.MontoOfertadoCrc.Should().Be(1_000_000m);
    }

    [Fact]
    public void Registrar_EnLicitacionEnBorrador_Falla()
    {
        Licitacion licitacion = Constructores.CrearLicitacion(_reloj);

        Action accion = () => Constructores.CrearOferta(licitacion, _reloj, 500_000m);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.LicitacionNoPublicada);
    }

    [Fact]
    public void Registrar_EnLicitacionCerrada_Falla()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj);
        licitacion.Cerrar(_reloj.AhoraUtc);

        Action accion = () => Constructores.CrearOferta(licitacion, _reloj, 500_000m);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.LicitacionNoPublicada);
    }

    [Fact]
    public void Registrar_DespuesDeLaFechaDeCierre_Falla()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj, horasHastaCierre: 2);

        _reloj.Avanzar(TimeSpan.FromHours(3));

        Action accion = () => Constructores.CrearOferta(licitacion, _reloj, 500_000m);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.LicitacionVencida);
    }

    /// <summary>
    /// El instante exacto del cierre ya rechaza la oferta: la seccion 8.2 dice que no se acepta
    /// cuando la fecha y hora actual son "iguales o posteriores" a la de cierre.
    /// </summary>
    [Fact]
    public void Registrar_EnElInstanteExactoDelCierre_Falla()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj, horasHastaCierre: 4);

        _reloj.AhoraUtc = licitacion.FechaCierre;

        Action accion = () => Constructores.CrearOferta(licitacion, _reloj, 500_000m);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.LicitacionVencida);
    }

    /// <summary>
    /// Un segundo antes del cierre la oferta todavia se acepta. Junto con la prueba anterior,
    /// fija el limite exacto y evita un error de comparacion por uno.
    /// </summary>
    [Fact]
    public void Registrar_UnSegundoAntesDelCierre_EsValida()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj, horasHastaCierre: 4);

        _reloj.AhoraUtc = licitacion.FechaCierre.AddSeconds(-1);

        Oferta oferta = Constructores.CrearOferta(licitacion, _reloj, 500_000m);

        oferta.Should().NotBeNull();
    }

    [Fact]
    public void CambiarMonto_EnLicitacionVigente_ActualizaElValor()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj);
        Oferta oferta = Constructores.CrearOferta(licitacion, _reloj, 800_000m);

        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        oferta.CambiarMonto(licitacion, 700_000m, _reloj.AhoraUtc);

        oferta.MontoOfertadoCrc.Should().Be(700_000m);
        oferta.UpdatedAt.Should().Be(_reloj.AhoraUtc);
    }

    [Fact]
    public void CambiarMonto_SuperandoElPresupuesto_Falla()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj, presupuestoCrc: 1_000_000m);
        Oferta oferta = Constructores.CrearOferta(licitacion, _reloj, 800_000m);

        Action accion = () => oferta.CambiarMonto(licitacion, 1_500_000m, _reloj.AhoraUtc);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.OfertaSuperaPresupuesto);
    }

    /// <summary>
    /// Seccion 8.9: las ofertas de licitaciones cerradas son evidencia y no pueden alterarse.
    /// </summary>
    [Fact]
    public void CambiarMonto_TrasElVencimientoDeLaLicitacion_Falla()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj, horasHastaCierre: 2);
        Oferta oferta = Constructores.CrearOferta(licitacion, _reloj, 800_000m);

        _reloj.Avanzar(TimeSpan.FromHours(3));

        Action accion = () => oferta.CambiarMonto(licitacion, 700_000m, _reloj.AhoraUtc);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.OfertaInmutable);
    }

    [Fact]
    public void AsegurarMutable_ConLicitacionCerrada_Falla()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj);
        Oferta oferta = Constructores.CrearOferta(licitacion, _reloj, 500_000m);

        licitacion.Cerrar(_reloj.AhoraUtc);

        Action accion = () => oferta.AsegurarMutable(licitacion, _reloj.AhoraUtc);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.OfertaInmutable);
    }
}
