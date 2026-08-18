using FluentAssertions;
using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enums;
using Licitaciones.Domain.Excepciones;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>Reglas de la entidad Licitacion (secciones 8.1, 8.2, 8.3 y 8.5).</summary>
public sealed class LicitacionPruebas
{
    private readonly RelojFijo _reloj = new();

    [Fact]
    public void Crear_ConDatosValidos_NaceEnBorrador()
    {
        Licitacion licitacion = Constructores.Licitacion(_reloj);

        licitacion.Estado.Should().Be(EstadoLicitacion.Borrador);
        licitacion.Id.Should().NotBe(Guid.Empty);
        licitacion.CreatedAt.Should().Be(_reloj.AhoraUtc);
        licitacion.EstaEliminada.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-500_000)]
    public void Crear_ConPresupuestoNoPositivo_Falla(decimal presupuesto)
    {
        Action accion = () => Constructores.Licitacion(_reloj, presupuestoCrc: presupuesto);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.MontoNoPositivo);
    }

    [Fact]
    public void Crear_ConFechaDeCierrePasada_Falla()
    {
        Action accion = () => Constructores.Licitacion(_reloj, horasHastaCierre: -1);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.FechaCierreNoFutura);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinCodigo_Falla(string codigo)
    {
        Action accion = () => Constructores.Licitacion(_reloj, codigo: codigo);

        accion.Should().Throw<ReglaNegocioVioladaException>();
    }

    /// <summary>
    /// El codigo normalizado es lo que respalda el indice unico: dos escrituras distintas del
    /// mismo codigo deben producir exactamente la misma forma normalizada (seccion 8.3).
    /// </summary>
    [Theory]
    [InlineData("LIC-2026-001", "LIC-2026-001")]
    [InlineData("  lic-2026-001  ", "LIC-2026-001")]
    [InlineData("Lic-2026-001", "LIC-2026-001")]
    public void Crear_NormalizaElCodigo(string entrada, string normalizadoEsperado)
    {
        Licitacion licitacion = Constructores.Licitacion(_reloj, codigo: entrada);

        licitacion.CodigoNormalizado.Should().Be(normalizadoEsperado);
        licitacion.Codigo.Should().Be(entrada.Trim());
    }

    [Fact]
    public void Publicar_DesdeBorradorConDatosValidos_CambiaAPublicada()
    {
        Licitacion licitacion = Constructores.Licitacion(_reloj);

        licitacion.Publicar(_reloj.AhoraUtc);

        licitacion.Estado.Should().Be(EstadoLicitacion.Publicada);
        licitacion.UpdatedAt.Should().Be(_reloj.AhoraUtc);
    }

    [Fact]
    public void Publicar_ConFechaDeCierreYaVencida_Falla()
    {
        Licitacion licitacion = Constructores.Licitacion(_reloj, horasHastaCierre: 2);

        _reloj.Avanzar(TimeSpan.FromHours(3));

        Action accion = () => licitacion.Publicar(_reloj.AhoraUtc);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.FechaCierreNoFutura);
    }

    [Fact]
    public void Publicar_UnaLicitacionYaPublicada_Falla()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj);

        Action accion = () => licitacion.Publicar(_reloj.AhoraUtc);

        accion.Should().Throw<TransicionEstadoInvalidaException>();
    }

    [Fact]
    public void Cerrar_DesdeBorrador_EsUnaCancelacionPermitida()
    {
        Licitacion licitacion = Constructores.Licitacion(_reloj);

        licitacion.Cerrar(_reloj.AhoraUtc);

        licitacion.Estado.Should().Be(EstadoLicitacion.Cerrada);
    }

    [Fact]
    public void CambiarEstado_HaciaBorrador_SiempreFalla()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj);

        Action accion = () => licitacion.CambiarEstado(EstadoLicitacion.Borrador, _reloj.AhoraUtc);

        accion.Should().Throw<TransicionEstadoInvalidaException>();
    }

    /// <summary>
    /// Aclaracion de la seccion 8.1: pasada la fecha de cierre la licitacion esta cerrada
    /// funcionalmente aunque la columna de estado todavia diga Publicada.
    /// </summary>
    [Fact]
    public void EstaCerradaFuncionalmente_TrasElVencimiento_EsVerdaderoAunqueElEstadoDigaPublicada()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj, horasHastaCierre: 1);

        licitacion.EstaCerradaFuncionalmente(_reloj.AhoraUtc).Should().BeFalse();

        _reloj.Avanzar(TimeSpan.FromHours(2));

        licitacion.Estado.Should().Be(EstadoLicitacion.Publicada);
        licitacion.EstaCerradaFuncionalmente(_reloj.AhoraUtc).Should().BeTrue();
        licitacion.PuedeRecibirOfertas(_reloj.AhoraUtc).Should().BeFalse();
    }

    /// <summary>
    /// El instante exacto del cierre ya cuenta como vencido: la seccion 8.2 dice "iguales o
    /// posteriores a la fecha de cierre".
    /// </summary>
    [Fact]
    public void EstaCerradaFuncionalmente_EnElInstanteExactoDelCierre_EsVerdadero()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj, horasHastaCierre: 5);

        licitacion.EstaCerradaFuncionalmente(licitacion.FechaCierre).Should().BeTrue();
    }

    [Fact]
    public void ActualizarDatos_ReduciendoElPresupuestoBajoUnaOfertaExistente_Falla()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj, presupuestoCrc: 1_000_000m);

        Action accion = () => licitacion.ActualizarDatos(
            licitacion.Titulo,
            700_000m,
            licitacion.FechaCierre,
            mayorOfertaRegistradaCrc: 800_000m,
            _reloj.AhoraUtc);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.PresupuestoMenorQueOfertaExistente);
    }

    [Fact]
    public void ActualizarDatos_ReduciendoElPresupuestoHastaLaOfertaMasAlta_EsValido()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj, presupuestoCrc: 1_000_000m);

        licitacion.ActualizarDatos(
            licitacion.Titulo,
            800_000m,
            licitacion.FechaCierre,
            mayorOfertaRegistradaCrc: 800_000m,
            _reloj.AhoraUtc);

        licitacion.PresupuestoEstimadoCrc.Should().Be(800_000m);
    }

    [Fact]
    public void ActualizarDatos_SobreUnaLicitacionCerrada_Falla()
    {
        Licitacion licitacion = Constructores.Licitacion(_reloj);
        licitacion.Cerrar(_reloj.AhoraUtc);

        Action accion = () => licitacion.ActualizarDatos(
            "Otro titulo",
            500_000m,
            _reloj.AhoraUtc.AddDays(5),
            null,
            _reloj.AhoraUtc);

        accion.Should().Throw<ReglaNegocioVioladaException>();
    }

    [Fact]
    public void CambiarCodigo_EnEstadoPublicada_Falla()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj);

        Action accion = () => licitacion.CambiarCodigo("LIC-2026-999", _reloj.AhoraUtc);

        accion.Should().Throw<ReglaNegocioVioladaException>();
    }

    [Fact]
    public void EliminarLogicamente_MarcaLaFechaYEsIdempotente()
    {
        Licitacion licitacion = Constructores.Licitacion(_reloj);

        licitacion.EliminarLogicamente(_reloj.AhoraUtc);
        DateTimeOffset? primeraMarca = licitacion.DeletedAt;

        _reloj.Avanzar(TimeSpan.FromHours(1));
        licitacion.EliminarLogicamente(_reloj.AhoraUtc);

        licitacion.EstaEliminada.Should().BeTrue();
        licitacion.DeletedAt.Should().Be(primeraMarca);
    }
}
