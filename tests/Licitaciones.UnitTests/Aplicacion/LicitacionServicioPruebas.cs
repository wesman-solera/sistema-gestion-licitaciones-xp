using FluentAssertions;
using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Excepciones;
using Licitaciones.Application.Servicios;
using Licitaciones.Application.Validadores;
using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enums;
using Licitaciones.Domain.Excepciones;
using Licitaciones.UnitTests.Comun;
using NSubstitute;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>Coordinacion del modulo de licitaciones.</summary>
public sealed class LicitacionServicioPruebas
{
    private readonly RelojFijo _reloj = new();
    private readonly ILicitacionRepositorio _licitaciones = Substitute.For<ILicitacionRepositorio>();
    private readonly IOfertaRepositorio _ofertas = Substitute.For<IOfertaRepositorio>();
    private readonly INivelAprobacionRepositorio _niveles = Substitute.For<INivelAprobacionRepositorio>();
    private readonly ITipoCambioRepositorio _tiposCambio = Substitute.For<ITipoCambioRepositorio>();
    private readonly IUnidadTrabajo _unidadTrabajo = Substitute.For<IUnidadTrabajo>();

    private LicitacionServicio CrearServicio() => new(
        _licitaciones,
        _ofertas,
        _niveles,
        _unidadTrabajo,
        _reloj,
        new ContextoMoneda(_tiposCambio),
        new CrearLicitacionRequestValidador(),
        new ActualizarLicitacionRequestValidador(),
        new CambiarEstadoRequestValidador());

    private CrearLicitacionRequest PeticionValida(string codigo = "LIC-2026-001") => new(
        codigo,
        "Compra de equipo de computo",
        1_000_000m,
        _reloj.AhoraUtc.AddDays(7));

    [Fact]
    public async Task CrearAsync_ConCodigoDisponible_CreaEnBorrador()
    {
        _licitaciones.ExisteCodigoAsync("LIC-2026-001", null, Arg.Any<CancellationToken>())
            .Returns(false);
        _ofertas.ListarPorLicitacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _niveles.ListarTodosAsync(Arg.Any<CancellationToken>()).Returns([]);

        LicitacionServicio servicio = CrearServicio();

        LicitacionDetalleDto resultado = await servicio.CrearAsync(PeticionValida());

        resultado.Estado.Should().Be(EstadoLicitacion.Borrador);
        resultado.Codigo.Should().Be("LIC-2026-001");
        _licitaciones.Received(1).Agregar(Arg.Any<Licitacion>());
    }

    /// <summary>
    /// El codigo se compara normalizado, de modo que " lic-2026-001 " colisiona con
    /// "LIC-2026-001" aunque las cadenas visibles sean distintas (seccion 8.3).
    /// </summary>
    [Fact]
    public async Task CrearAsync_ConCodigoEquivalenteYaRegistrado_Falla()
    {
        _licitaciones.ExisteCodigoAsync("LIC-2026-001", null, Arg.Any<CancellationToken>())
            .Returns(true);

        LicitacionServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.CrearAsync(PeticionValida("  lic-2026-001  "));

        var excepcion = await accion.Should().ThrowAsync<ConflictoUnicidadException>();
        excepcion.Which.CodigoError.Should().Be(CodigosError.CodigoLicitacionDuplicado);
    }

    [Fact]
    public async Task CrearAsync_ConPresupuestoNoPositivo_FallaEnLaValidacionDeEntrada()
    {
        LicitacionServicio servicio = CrearServicio();

        var peticion = new CrearLicitacionRequest(
            "LIC-2026-002",
            "Titulo",
            0m,
            _reloj.AhoraUtc.AddDays(7));

        await servicio.Invoking(s => s.CrearAsync(peticion))
            .Should().ThrowAsync<ValidacionException>();
    }

    /// <summary>
    /// Un monto con tres decimales no cabe en numeric(18,2). Se rechaza en la aplicacion en
    /// lugar de dejar que la base de datos lo trunque en silencio.
    /// </summary>
    [Fact]
    public async Task CrearAsync_ConMasDeDosDecimales_FallaEnLaValidacionDeEntrada()
    {
        LicitacionServicio servicio = CrearServicio();

        var peticion = new CrearLicitacionRequest(
            "LIC-2026-003",
            "Titulo",
            1_000.999m,
            _reloj.AhoraUtc.AddDays(7));

        await servicio.Invoking(s => s.CrearAsync(peticion))
            .Should().ThrowAsync<ValidacionException>();
    }

    [Fact]
    public async Task ActualizarAsync_ConsultaLaOfertaMasAltaAntesDeCambiarElPresupuesto()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj);

        _licitaciones.ObtenerConOfertasAsync(licitacion.Id, Arg.Any<CancellationToken>())
            .Returns(licitacion);
        _ofertas.ObtenerMayorMontoAsync(licitacion.Id, Arg.Any<CancellationToken>())
            .Returns(900_000m);
        _ofertas.ListarPorLicitacionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _niveles.ListarTodosAsync(Arg.Any<CancellationToken>()).Returns([]);

        LicitacionServicio servicio = CrearServicio();

        var peticion = new ActualizarLicitacionRequest(
            licitacion.Codigo,
            licitacion.Titulo,
            800_000m,
            licitacion.FechaCierre);

        Func<Task> accion = () => servicio.ActualizarAsync(licitacion.Id, peticion);

        var excepcion = await accion.Should().ThrowAsync<ReglaNegocioVioladaException>();
        excepcion.Which.CodigoError.Should().Be(CodigosError.PresupuestoMenorQueOfertaExistente);
    }

    [Fact]
    public async Task CambiarEstadoAsync_DePublicadaABorrador_Falla()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj);

        _licitaciones.ObtenerConOfertasAsync(licitacion.Id, Arg.Any<CancellationToken>())
            .Returns(licitacion);

        LicitacionServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.CambiarEstadoAsync(
            licitacion.Id,
            new CambiarEstadoRequest(EstadoLicitacion.Borrador));

        await accion.Should().ThrowAsync<TransicionEstadoInvalidaException>();
    }

    /// <summary>
    /// El detalle debe reportar el aprobador que corresponde al monto de la mejor oferta,
    /// tomandolo de la tabla parametrizable y no de una condicion fija (seccion 8.7).
    /// </summary>
    [Fact]
    public async Task ObtenerMejorOfertaAsync_DevuelveElAprobadorDeLaTablaParametrizable()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(
            _reloj,
            presupuestoCrc: 20_000_000m);

        Oferta ganadora = Constructores.Oferta(licitacion, _reloj, 12_000_000m);
        Oferta perdedora = Constructores.Oferta(licitacion, _reloj, 15_000_000m);

        _licitaciones.ObtenerConOfertasAsync(licitacion.Id, Arg.Any<CancellationToken>())
            .Returns(licitacion);
        _ofertas.ListarPorLicitacionAsync(licitacion.Id, Arg.Any<CancellationToken>())
            .Returns([ganadora, perdedora]);
        _niveles.ListarTodosAsync(Arg.Any<CancellationToken>())
            .Returns([.. Constructores.NivelesDelEnunciado(_reloj)]);

        LicitacionServicio servicio = CrearServicio();

        EvaluacionLicitacionDto evaluacion = await servicio.ObtenerMejorOfertaAsync(licitacion.Id);

        evaluacion.MejorOferta!.Id.Should().Be(ganadora.Id);
        evaluacion.Aprobador.Should().Be("Junta Directiva");
        evaluacion.Clasificacion.Should().Be(ClasificacionAhorro.OfertaConveniente);
        evaluacion.PorcentajeAhorro.Should().Be(40.00m);
    }

    /// <summary>
    /// Si la tabla de aprobacion no cubre el monto, la consulta debe seguir respondiendo con el
    /// aprobador en nulo. Interrumpir dejaria la pantalla de detalle inutilizable por un dato de
    /// configuracion que el usuario puede corregir.
    /// </summary>
    [Fact]
    public async Task ObtenerMejorOfertaAsync_SinRangoAplicable_DevuelveAprobadorNulo()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj);
        Oferta oferta = Constructores.Oferta(licitacion, _reloj, 500_000m);

        _licitaciones.ObtenerConOfertasAsync(licitacion.Id, Arg.Any<CancellationToken>())
            .Returns(licitacion);
        _ofertas.ListarPorLicitacionAsync(licitacion.Id, Arg.Any<CancellationToken>())
            .Returns([oferta]);
        _niveles.ListarTodosAsync(Arg.Any<CancellationToken>()).Returns([]);

        LicitacionServicio servicio = CrearServicio();

        EvaluacionLicitacionDto evaluacion = await servicio.ObtenerMejorOfertaAsync(licitacion.Id);

        evaluacion.MejorOferta.Should().NotBeNull();
        evaluacion.Aprobador.Should().BeNull();
        evaluacion.NivelAprobacionId.Should().BeNull();
    }

    /// <summary>
    /// Sin tipo de cambio activo la lectura no debe fallar: solo se omite el equivalente en
    /// dolares. El colon es la fuente de verdad y siempre esta disponible (seccion 8.8).
    /// </summary>
    [Fact]
    public async Task ObtenerDetalleAsync_SinTipoDeCambioActivo_DevuelveMontoEnColonesSinDolares()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj);

        _licitaciones.ObtenerConOfertasAsync(licitacion.Id, Arg.Any<CancellationToken>())
            .Returns(licitacion);
        _ofertas.ListarPorLicitacionAsync(licitacion.Id, Arg.Any<CancellationToken>()).Returns([]);
        _niveles.ListarTodosAsync(Arg.Any<CancellationToken>()).Returns([]);
        _tiposCambio.ObtenerActivoAsync(Arg.Any<CancellationToken>()).Returns((TipoCambio?)null);

        LicitacionServicio servicio = CrearServicio();

        LicitacionDetalleDto detalle = await servicio.ObtenerDetalleAsync(licitacion.Id);

        detalle.PresupuestoEstimado.Crc.Should().Be(licitacion.PresupuestoEstimadoCrc);
        detalle.PresupuestoEstimado.Usd.Should().BeNull();
        detalle.TipoCambioAplicado.Should().BeNull();
    }

    [Fact]
    public async Task ObtenerDetalleAsync_ConTipoDeCambioActivo_IncluyeElEquivalenteYSuFecha()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj, presupuestoCrc: 1_010_000m);
        TipoCambio tipoCambio = TipoCambio.Crear(505m, _reloj.AhoraUtc, true, _reloj.AhoraUtc);

        _licitaciones.ObtenerConOfertasAsync(licitacion.Id, Arg.Any<CancellationToken>())
            .Returns(licitacion);
        _ofertas.ListarPorLicitacionAsync(licitacion.Id, Arg.Any<CancellationToken>()).Returns([]);
        _niveles.ListarTodosAsync(Arg.Any<CancellationToken>()).Returns([]);
        _tiposCambio.ObtenerActivoAsync(Arg.Any<CancellationToken>()).Returns(tipoCambio);

        LicitacionServicio servicio = CrearServicio();

        LicitacionDetalleDto detalle = await servicio.ObtenerDetalleAsync(licitacion.Id);

        detalle.PresupuestoEstimado.Usd.Should().Be(2_000.00m);
        detalle.TipoCambioAplicado!.CrcPorUsd.Should().Be(505m);
    }

    /// <summary>
    /// El contexto de moneda debe leer el tipo de cambio una sola vez por peticion, no una vez
    /// por monto convertido.
    /// </summary>
    [Fact]
    public async Task ObtenerDetalleAsync_ConsultaElTipoDeCambioUnaSolaVez()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj);
        Oferta primera = Constructores.Oferta(licitacion, _reloj, 500_000m);
        Oferta segunda = Constructores.Oferta(licitacion, _reloj, 600_000m);

        _licitaciones.ObtenerConOfertasAsync(licitacion.Id, Arg.Any<CancellationToken>())
            .Returns(licitacion);
        _ofertas.ListarPorLicitacionAsync(licitacion.Id, Arg.Any<CancellationToken>())
            .Returns([primera, segunda]);
        _niveles.ListarTodosAsync(Arg.Any<CancellationToken>()).Returns([]);
        _tiposCambio.ObtenerActivoAsync(Arg.Any<CancellationToken>())
            .Returns(TipoCambio.Crear(505m, _reloj.AhoraUtc, true, _reloj.AhoraUtc));

        LicitacionServicio servicio = CrearServicio();

        await servicio.ObtenerDetalleAsync(licitacion.Id);

        await _tiposCambio.Received(1).ObtenerActivoAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EliminarAsync_ConOfertasAsociadas_AplicaBorradoLogico()
    {
        Licitacion licitacion = Constructores.LicitacionPublicada(_reloj);

        _licitaciones.ObtenerPorIdAsync(licitacion.Id, false, Arg.Any<CancellationToken>())
            .Returns(licitacion);
        _ofertas.LicitacionTieneOfertasAsync(licitacion.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        LicitacionServicio servicio = CrearServicio();

        bool borradoLogico = await servicio.EliminarAsync(licitacion.Id);

        borradoLogico.Should().BeTrue();
        licitacion.EstaEliminada.Should().BeTrue();
        _licitaciones.DidNotReceive().Eliminar(Arg.Any<Licitacion>());
    }
}
