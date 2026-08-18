using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
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

/// <summary>Coordinacion del modulo de tipos de cambio.</summary>
/// <remarks>
/// La unidad de trabajo se sustituye por una implementacion que ejecuta la operacion tal cual, sin
/// transaccion real. Aqui se verifica el orden de las operaciones y las reglas; que la transaccion
/// sea efectiva se comprueba en las pruebas de integracion contra PostgreSQL.
/// </remarks>
public sealed class TipoCambioServicioPruebas
{
    private readonly RelojFijo _reloj = new();
    private readonly ITipoCambioRepositorio _tiposCambio = Substitute.For<ITipoCambioRepositorio>();
    private readonly IUnidadTrabajo _unidadTrabajo = new UnidadTrabajoDirecta();

    private TipoCambioServicio CrearServicio() => new(
        _tiposCambio,
        _unidadTrabajo,
        _reloj,
        new CrearTipoCambioRequestValidador(),
        new ActualizarTipoCambioRequestValidador());

    /// <summary>
    /// Unidad de trabajo de prueba que ejecuta la operacion sin abrir transaccion.
    /// </summary>
    private sealed class UnidadTrabajoDirecta : IUnidadTrabajo
    {
        public Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default)
            => Task.FromResult(0);

        public Task<T> EnTransaccionAsync<T>(
            Func<CancellationToken, Task<T>> operacion,
            CancellationToken cancelacion = default)
            => operacion(cancelacion);
    }

    [Fact]
    public async Task CrearAsync_ConValorValido_LoPersiste()
    {
        _tiposCambio.ListarActivosAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TipoCambio>());

        TipoCambioServicio servicio = CrearServicio();

        TipoCambioDto resultado = await servicio.CrearAsync(
            new CrearTipoCambioRequest(505.00m, _reloj.AhoraUtc, Activo: true));

        resultado.CrcPorUsd.Should().Be(505.00m);
        resultado.Activo.Should().BeTrue();
        _tiposCambio.Received(1).Agregar(Arg.Any<TipoCambio>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CrearAsync_ConValorNoPositivo_FallaEnLaValidacionDeEntrada(decimal valor)
    {
        TipoCambioServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.CrearAsync(
            new CrearTipoCambioRequest(valor, _reloj.AhoraUtc, Activo: false));

        await accion.Should().ThrowAsync<ValidacionException>();
    }

    [Fact]
    public async Task CrearAsync_ConMasDeDosDecimales_FallaEnLaValidacionDeEntrada()
    {
        TipoCambioServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.CrearAsync(
            new CrearTipoCambioRequest(505.123m, _reloj.AhoraUtc, Activo: false));

        await accion.Should().ThrowAsync<ValidacionException>();
    }

    /// <summary>
    /// Al crear un tipo de cambio activo, el que estuviera vigente debe quedar desactivado. De lo
    /// contrario el indice unico parcial de PostgreSQL rechazaria la insercion.
    /// </summary>
    [Fact]
    public async Task CrearAsync_ComoActivo_DesactivaElAnterior()
    {
        TipoCambio anterior = TipoCambio.Crear(500m, _reloj.AhoraUtc, activo: true, _reloj.AhoraUtc);

        _tiposCambio.ListarActivosAsync(Arg.Any<CancellationToken>()).Returns(new[] { anterior });

        TipoCambioServicio servicio = CrearServicio();

        await servicio.CrearAsync(new CrearTipoCambioRequest(520m, _reloj.AhoraUtc, Activo: true));

        anterior.Activo.Should().BeFalse();
    }

    [Fact]
    public async Task CrearAsync_ComoInactivo_NoTocaElVigente()
    {
        TipoCambio anterior = TipoCambio.Crear(500m, _reloj.AhoraUtc, activo: true, _reloj.AhoraUtc);

        _tiposCambio.ListarActivosAsync(Arg.Any<CancellationToken>()).Returns(new[] { anterior });

        TipoCambioServicio servicio = CrearServicio();

        await servicio.CrearAsync(new CrearTipoCambioRequest(520m, _reloj.AhoraUtc, Activo: false));

        anterior.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task ActivarAsync_ActivaElNuevoYDesactivaElAnterior()
    {
        TipoCambio anterior = TipoCambio.Crear(500m, _reloj.AhoraUtc, activo: true, _reloj.AhoraUtc);
        TipoCambio nuevo = TipoCambio.Crear(520m, _reloj.AhoraUtc, activo: false, _reloj.AhoraUtc);

        _tiposCambio.ObtenerPorIdAsync(nuevo.Id, Arg.Any<CancellationToken>()).Returns(nuevo);
        _tiposCambio.ListarActivosAsync(Arg.Any<CancellationToken>()).Returns(new[] { anterior });

        TipoCambioServicio servicio = CrearServicio();

        TipoCambioDto resultado = await servicio.ActivarAsync(nuevo.Id);

        resultado.Activo.Should().BeTrue();
        nuevo.Activo.Should().BeTrue();
        anterior.Activo.Should().BeFalse();
    }

    [Fact]
    public async Task EliminarAsync_ConElTipoDeCambioActivo_Falla()
    {
        TipoCambio activo = TipoCambio.Crear(505m, _reloj.AhoraUtc, activo: true, _reloj.AhoraUtc);

        _tiposCambio.ObtenerPorIdAsync(activo.Id, Arg.Any<CancellationToken>()).Returns(activo);

        TipoCambioServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.EliminarAsync(activo.Id);

        var excepcion = await accion.Should().ThrowAsync<ReglaNegocioVioladaException>();
        excepcion.Which.CodigoError.Should().Be(CodigosError.SinTipoCambioActivo);

        _tiposCambio.DidNotReceive().Eliminar(Arg.Any<TipoCambio>());
    }

    [Fact]
    public async Task EliminarAsync_ConUnTipoDeCambioInactivo_LoElimina()
    {
        TipoCambio inactivo = TipoCambio.Crear(495m, _reloj.AhoraUtc, activo: false, _reloj.AhoraUtc);

        _tiposCambio.ObtenerPorIdAsync(inactivo.Id, Arg.Any<CancellationToken>()).Returns(inactivo);

        TipoCambioServicio servicio = CrearServicio();

        await servicio.EliminarAsync(inactivo.Id);

        _tiposCambio.Received(1).Eliminar(inactivo);
    }

    [Fact]
    public async Task ObtenerActivoAsync_SinNingunoConfigurado_DevuelveNulo()
    {
        _tiposCambio.ObtenerActivoAsync(Arg.Any<CancellationToken>()).Returns((TipoCambio?)null);

        TipoCambioServicio servicio = CrearServicio();

        (await servicio.ObtenerActivoAsync()).Should().BeNull();
    }

    [Fact]
    public async Task ObtenerAsync_ConIdentificadorInexistente_Falla()
    {
        _tiposCambio.ObtenerPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TipoCambio?)null);

        TipoCambioServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.ObtenerAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<RecursoNoEncontradoException>();
    }

    [Fact]
    public async Task ActualizarAsync_CambiaElValorYLaVigencia()
    {
        TipoCambio tipoCambio = TipoCambio.Crear(505m, _reloj.AhoraUtc, activo: true, _reloj.AhoraUtc);

        _tiposCambio.ObtenerPorIdAsync(tipoCambio.Id, Arg.Any<CancellationToken>())
            .Returns(tipoCambio);

        TipoCambioServicio servicio = CrearServicio();

        DateTimeOffset nuevaVigencia = _reloj.AhoraUtc.AddDays(1);

        TipoCambioDto resultado = await servicio.ActualizarAsync(
            tipoCambio.Id,
            new ActualizarTipoCambioRequest(512.50m, nuevaVigencia));

        resultado.CrcPorUsd.Should().Be(512.50m);
        resultado.FechaVigencia.Should().Be(nuevaVigencia);
    }

    [Fact]
    public async Task ListarAsync_DevuelveLaPaginaMapeada()
    {
        TipoCambio uno = TipoCambio.Crear(505m, _reloj.AhoraUtc, activo: true, _reloj.AhoraUtc);
        TipoCambio dos = TipoCambio.Crear(500m, _reloj.AhoraUtc.AddDays(-1), activo: false, _reloj.AhoraUtc);

        _tiposCambio.ListarAsync(Arg.Any<ParametrosConsulta>(), Arg.Any<CancellationToken>())
            .Returns(new PaginaResultado<TipoCambio>(new[] { uno, dos }, 1, 20, 2));

        TipoCambioServicio servicio = CrearServicio();

        var pagina = await servicio.ListarAsync(new ParametrosConsulta());

        pagina.Elementos.Should().HaveCount(2);
        pagina.Elementos[0].Activo.Should().BeTrue();
        pagina.TotalElementos.Should().Be(2);
    }
}
