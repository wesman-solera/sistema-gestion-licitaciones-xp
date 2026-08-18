using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Excepciones;
using Licitaciones.Application.Servicios;
using Licitaciones.Application.Validadores;
using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;
using Licitaciones.UnitTests.Comun;
using NSubstitute;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>Coordinacion del modulo de niveles de aprobacion.</summary>
/// <remarks>
/// Las invariantes del conjunto no pueden validarse mirando una sola fila, asi que estas pruebas
/// verifican que el servicio arme correctamente el conjunto resultante antes de validarlo: con el
/// rango nuevo incluido al crear, y con el rango modificado sustituyendo al anterior al editar.
/// </remarks>
public sealed class NivelAprobacionServicioPruebas
{
    private readonly RelojFijo _reloj = new();
    private readonly INivelAprobacionRepositorio _niveles = Substitute.For<INivelAprobacionRepositorio>();
    private readonly ITipoCambioRepositorio _tiposCambio = Substitute.For<ITipoCambioRepositorio>();
    private readonly IUnidadTrabajo _unidadTrabajo = Substitute.For<IUnidadTrabajo>();

    private NivelAprobacionServicio CrearServicio() => new(
        _niveles,
        _unidadTrabajo,
        _reloj,
        new ContextoMoneda(_tiposCambio),
        new CrearNivelAprobacionRequestValidador(),
        new ActualizarNivelAprobacionRequestValidador());

    [Fact]
    public async Task CrearAsync_SobreUnaTablaVacia_PersisteElPrimerRango()
    {
        _niveles.ListarTodosAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NivelAprobacion>());

        NivelAprobacionServicio servicio = CrearServicio();

        NivelAprobacionDto resultado = await servicio.CrearAsync(
            new CrearNivelAprobacionRequest(0.01m, 999_999.99m, "Encargado de area"));

        resultado.Aprobador.Should().Be("Encargado de area");
        resultado.EsRangoAbierto.Should().BeFalse();

        _niveles.Received(1).Agregar(Arg.Any<NivelAprobacion>());
        await _unidadTrabajo.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Con la tabla del enunciado ya cargada, un rango que empieza por encima del ultimo tramo
    /// cerrado se traslapa con el rango abierto, que no tiene limite superior.
    /// </summary>
    [Fact]
    public async Task CrearAsync_ConRangoPorEncimaDelRangoAbierto_Falla()
    {
        _niveles.ListarTodosAsync(Arg.Any<CancellationToken>())
            .Returns(Constructores.CrearNivelesDelEnunciado(_reloj));

        NivelAprobacionServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.CrearAsync(
            new CrearNivelAprobacionRequest(80_000_000m, 90_000_000m, "Cargo adicional"));

        var excepcion = await accion.Should().ThrowAsync<ReglaNegocioVioladaException>();
        excepcion.Which.CodigoError.Should().Be(CodigosError.RangosAprobacionTraslapados);
    }

    [Fact]
    public async Task CrearAsync_ConRangoTraslapado_FallaYNoPersiste()
    {
        _niveles.ListarTodosAsync(Arg.Any<CancellationToken>())
            .Returns(Constructores.CrearNivelesDelEnunciado(_reloj));

        NivelAprobacionServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.CrearAsync(
            new CrearNivelAprobacionRequest(500_000m, 2_000_000m, "Rango invalido"));

        var excepcion = await accion.Should().ThrowAsync<ReglaNegocioVioladaException>();
        excepcion.Which.CodigoError.Should().Be(CodigosError.RangosAprobacionTraslapados);

        _niveles.DidNotReceive().Agregar(Arg.Any<NivelAprobacion>());
        await _unidadTrabajo.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CrearAsync_ConUnSegundoRangoAbierto_Falla()
    {
        _niveles.ListarTodosAsync(Arg.Any<CancellationToken>())
            .Returns(Constructores.CrearNivelesDelEnunciado(_reloj));

        NivelAprobacionServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.CrearAsync(
            new CrearNivelAprobacionRequest(50_000_000m, null, "Segundo rango abierto"));

        var excepcion = await accion.Should().ThrowAsync<ReglaNegocioVioladaException>();
        excepcion.Which.CodigoError.Should().Be(CodigosError.RangoAbiertoDuplicado);
    }

    [Fact]
    public async Task CrearAsync_ConMaximoMenorQueElMinimo_FallaEnLaValidacionDeEntrada()
    {
        NivelAprobacionServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.CrearAsync(
            new CrearNivelAprobacionRequest(1_000_000m, 500_000m, "Rango invertido"));

        await accion.Should().ThrowAsync<ValidacionException>();

        // Ni siquiera debe consultar la tabla para rechazar un rango incoherente en si mismo.
        await _niveles.DidNotReceive().ListarTodosAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Al editar, el conjunto a validar debe excluir la version anterior del propio rango. Si no,
    /// el rango se detectaria como traslapado consigo mismo y ninguna edicion seria posible.
    /// </summary>
    [Fact]
    public async Task ActualizarAsync_ExcluyeLaVersionAnteriorDelPropioRango()
    {
        var niveles = Constructores.CrearNivelesDelEnunciado(_reloj);
        NivelAprobacion aEditar = niveles[1];

        _niveles.ObtenerPorIdAsync(aEditar.Id, Arg.Any<CancellationToken>()).Returns(aEditar);
        _niveles.ListarTodosAsync(Arg.Any<CancellationToken>()).Returns(niveles);

        NivelAprobacionServicio servicio = CrearServicio();

        NivelAprobacionDto resultado = await servicio.ActualizarAsync(
            aEditar.Id,
            new ActualizarNivelAprobacionRequest(1_000_000m, 9_999_999.99m, "Gerencia General"));

        resultado.Aprobador.Should().Be("Gerencia General");
        await _unidadTrabajo.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActualizarAsync_ConRangoInexistente_Falla()
    {
        _niveles.ObtenerPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((NivelAprobacion?)null);

        NivelAprobacionServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.ActualizarAsync(
            Guid.NewGuid(),
            new ActualizarNivelAprobacionRequest(1m, 2m, "Cargo"));

        await accion.Should().ThrowAsync<RecursoNoEncontradoException>();
    }

    [Fact]
    public async Task ConsultarAprobadorAsync_DevuelveElRangoAplicable()
    {
        _niveles.ListarTodosAsync(Arg.Any<CancellationToken>())
            .Returns(Constructores.CrearNivelesDelEnunciado(_reloj));

        NivelAprobacionServicio servicio = CrearServicio();

        ConsultaAprobadorDto resultado = await servicio.ConsultarAprobadorAsync(15_000_000m);

        resultado.Aprobador.Should().Be("Junta Directiva");
        resultado.MontoCrc.Should().Be(15_000_000m);
        resultado.NivelAprobacionId.Should().NotBeNull();
    }

    [Fact]
    public async Task ConsultarAprobadorAsync_SinRangoAplicable_DevuelveAprobadorNulo()
    {
        _niveles.ListarTodosAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NivelAprobacion>());

        NivelAprobacionServicio servicio = CrearServicio();

        ConsultaAprobadorDto resultado = await servicio.ConsultarAprobadorAsync(500_000m);

        resultado.Aprobador.Should().BeNull();
        resultado.NivelAprobacionId.Should().BeNull();
    }

    [Fact]
    public async Task ListarAsync_MapeaLosRangosConSusMontos()
    {
        var niveles = Constructores.CrearNivelesDelEnunciado(_reloj);

        _niveles.ListarAsync(Arg.Any<Application.Comun.ParametrosConsulta>(), Arg.Any<CancellationToken>())
            .Returns(new Application.Comun.PaginaResultado<NivelAprobacion>(niveles, 1, 20, 3));

        NivelAprobacionServicio servicio = CrearServicio();

        var pagina = await servicio.ListarAsync(new Application.Comun.ParametrosConsulta());

        pagina.Elementos.Should().HaveCount(3);
        pagina.Elementos[2].EsRangoAbierto.Should().BeTrue();
        pagina.Elementos[2].MontoMaximo.Should().BeNull();
    }

    [Fact]
    public async Task EliminarAsync_EliminaElRangoIndicado()
    {
        NivelAprobacion nivel = Constructores.CrearNivelesDelEnunciado(_reloj)[0];

        _niveles.ObtenerPorIdAsync(nivel.Id, Arg.Any<CancellationToken>()).Returns(nivel);

        NivelAprobacionServicio servicio = CrearServicio();

        await servicio.EliminarAsync(nivel.Id);

        _niveles.Received(1).Eliminar(nivel);
        await _unidadTrabajo.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }
}
