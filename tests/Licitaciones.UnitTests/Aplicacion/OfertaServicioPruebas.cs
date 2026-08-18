using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Licitaciones.Application.Validadores;
using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;
using Licitaciones.UnitTests.Comun;
using NSubstitute;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>Coordinacion del modulo de ofertas.</summary>
public sealed class OfertaServicioPruebas
{
    private readonly RelojFijo _reloj = new();
    private readonly IOfertaRepositorio _ofertas = Substitute.For<IOfertaRepositorio>();
    private readonly ILicitacionRepositorio _licitaciones = Substitute.For<ILicitacionRepositorio>();
    private readonly IProveedorRepositorio _proveedores = Substitute.For<IProveedorRepositorio>();
    private readonly ITipoCambioRepositorio _tiposCambio = Substitute.For<ITipoCambioRepositorio>();
    private readonly IUnidadTrabajo _unidadTrabajo = Substitute.For<IUnidadTrabajo>();

    private OfertaServicio CrearServicio() => new(
        _ofertas,
        _licitaciones,
        _proveedores,
        _unidadTrabajo,
        _reloj,
        new ContextoMoneda(_tiposCambio),
        new CrearOfertaRequestValidador(),
        new ActualizarOfertaRequestValidador());

    private (Licitacion Licitacion, Proveedor Proveedor) PrepararEscenario(
        decimal presupuesto = 1_000_000m,
        int horasHastaCierre = 48)
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(
            _reloj,
            presupuestoCrc: presupuesto,
            horasHastaCierre: horasHastaCierre);

        Proveedor proveedor = Constructores.CrearProveedor(_reloj);

        _licitaciones.ObtenerPorIdAsync(licitacion.Id, false, Arg.Any<CancellationToken>())
            .Returns(licitacion);
        _licitaciones.ObtenerPorIdAsync(licitacion.Id, true, Arg.Any<CancellationToken>())
            .Returns(licitacion);
        _proveedores.ObtenerPorIdAsync(proveedor.Id, false, Arg.Any<CancellationToken>())
            .Returns(proveedor);
        _ofertas.ListarPorLicitacionAsync(licitacion.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Oferta>());

        return (licitacion, proveedor);
    }

    [Fact]
    public async Task RegistrarAsync_ConDatosValidos_PersisteLaOferta()
    {
        var (licitacion, proveedor) = PrepararEscenario();

        _ofertas.ExisteOfertaDeProveedorAsync(
                licitacion.Id, proveedor.Id, null, Arg.Any<CancellationToken>())
            .Returns(false);

        OfertaServicio servicio = CrearServicio();

        OfertaDto resultado = await servicio.RegistrarAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 750_000m));

        resultado.Monto.Crc.Should().Be(750_000m);
        resultado.NombreProveedor.Should().Be(proveedor.Nombre);
        _ofertas.Received(1).Agregar(Arg.Any<Oferta>());
    }

    /// <summary>
    /// Un proveedor no puede registrar mas de una oferta para la misma licitacion (seccion 8.3).
    /// Esta es la primera de las dos defensas; la segunda es el indice unico compuesto.
    /// </summary>
    [Fact]
    public async Task RegistrarAsync_ConOfertaDuplicadaDelMismoProveedor_Falla()
    {
        var (licitacion, proveedor) = PrepararEscenario();

        _ofertas.ExisteOfertaDeProveedorAsync(
                licitacion.Id, proveedor.Id, null, Arg.Any<CancellationToken>())
            .Returns(true);

        OfertaServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.RegistrarAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 750_000m));

        var excepcion = await accion.Should().ThrowAsync<ConflictoUnicidadException>();
        excepcion.Which.CodigoError.Should().Be(CodigosError.OfertaDuplicada);

        _ofertas.DidNotReceive().Agregar(Arg.Any<Oferta>());
    }

    [Fact]
    public async Task RegistrarAsync_SuperandoElPresupuesto_Falla()
    {
        var (licitacion, proveedor) = PrepararEscenario(presupuesto: 1_000_000m);

        _ofertas.ExisteOfertaDeProveedorAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        OfertaServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.RegistrarAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 1_500_000m));

        var excepcion = await accion.Should().ThrowAsync<ReglaNegocioVioladaException>();
        excepcion.Which.CodigoError.Should().Be(CodigosError.OfertaSuperaPresupuesto);
    }

    [Fact]
    public async Task RegistrarAsync_TrasElVencimientoDeLaLicitacion_Falla()
    {
        var (licitacion, proveedor) = PrepararEscenario(horasHastaCierre: 2);

        _ofertas.ExisteOfertaDeProveedorAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _reloj.Avanzar(TimeSpan.FromHours(3));

        OfertaServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.RegistrarAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 500_000m));

        var excepcion = await accion.Should().ThrowAsync<ReglaNegocioVioladaException>();
        excepcion.Which.CodigoError.Should().Be(CodigosError.LicitacionVencida);
    }

    [Fact]
    public async Task RegistrarAsync_ConLicitacionInexistente_Falla()
    {
        _licitaciones.ObtenerPorIdAsync(Arg.Any<Guid>(), false, Arg.Any<CancellationToken>())
            .Returns((Licitacion?)null);

        OfertaServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.RegistrarAsync(
            new CrearOfertaRequest(Guid.NewGuid(), Guid.NewGuid(), 500_000m));

        await accion.Should().ThrowAsync<RecursoNoEncontradoException>();
    }

    [Fact]
    public async Task RegistrarAsync_ConProveedorInexistente_Falla()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj);

        _licitaciones.ObtenerPorIdAsync(licitacion.Id, false, Arg.Any<CancellationToken>())
            .Returns(licitacion);
        _proveedores.ObtenerPorIdAsync(Arg.Any<Guid>(), false, Arg.Any<CancellationToken>())
            .Returns((Proveedor?)null);

        OfertaServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.RegistrarAsync(
            new CrearOfertaRequest(licitacion.Id, Guid.NewGuid(), 500_000m));

        await accion.Should().ThrowAsync<RecursoNoEncontradoException>();
    }

    /// <summary>
    /// Seccion 8.9: una oferta de licitacion cerrada es evidencia y no puede eliminarse.
    /// </summary>
    [Fact]
    public async Task EliminarAsync_ConLicitacionCerrada_Falla()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj);
        Oferta oferta = Constructores.CrearOferta(licitacion, _reloj, 500_000m);
        licitacion.Cerrar(_reloj.AhoraUtc);

        _ofertas.ObtenerPorIdAsync(oferta.Id, Arg.Any<CancellationToken>()).Returns(oferta);
        _licitaciones.ObtenerPorIdAsync(licitacion.Id, true, Arg.Any<CancellationToken>())
            .Returns(licitacion);

        OfertaServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.EliminarAsync(oferta.Id);

        var excepcion = await accion.Should().ThrowAsync<ReglaNegocioVioladaException>();
        excepcion.Which.CodigoError.Should().Be(CodigosError.OfertaInmutable);

        _ofertas.DidNotReceive().Eliminar(Arg.Any<Oferta>());
    }

    [Fact]
    public async Task EliminarAsync_ConLicitacionVigente_EliminaLaOferta()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj);
        Oferta oferta = Constructores.CrearOferta(licitacion, _reloj, 500_000m);

        _ofertas.ObtenerPorIdAsync(oferta.Id, Arg.Any<CancellationToken>()).Returns(oferta);
        _licitaciones.ObtenerPorIdAsync(licitacion.Id, true, Arg.Any<CancellationToken>())
            .Returns(licitacion);

        OfertaServicio servicio = CrearServicio();

        await servicio.EliminarAsync(oferta.Id);

        _ofertas.Received(1).Eliminar(oferta);
        await _unidadTrabajo.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// El listado por licitacion debe marcar cual es la oferta ganadora, para que la interfaz no
    /// tenga que recalcularlo por su cuenta.
    /// </summary>
    [Fact]
    public async Task ListarPorLicitacionAsync_MarcaLaMejorOferta()
    {
        Licitacion licitacion = Constructores.CrearLicitacionPublicada(_reloj);

        Oferta cara = Constructores.CrearOferta(licitacion, _reloj, 900_000m);
        Oferta barata = Constructores.CrearOferta(licitacion, _reloj, 600_000m);

        _ofertas.ListarPorLicitacionAsync(licitacion.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { cara, barata });

        OfertaServicio servicio = CrearServicio();

        IReadOnlyList<OfertaDto> resultado = await servicio.ListarPorLicitacionAsync(licitacion.Id);

        resultado.Should().HaveCount(2);
        resultado[0].Monto.Crc.Should().Be(600_000m, "el listado se ordena por monto ascendente");
        resultado.Single(o => o.EsMejorOferta).Id.Should().Be(barata.Id);
    }
}
