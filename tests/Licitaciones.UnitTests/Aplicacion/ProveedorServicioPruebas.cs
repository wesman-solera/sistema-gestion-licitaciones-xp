using FluentAssertions;
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

/// <summary>
/// Coordinacion del modulo de proveedores.
/// </summary>
/// <remarks>
/// Se sustituyen los repositorios pero se usan los validadores reales. Sustituir tambien los
/// validadores dejaria sin probar la conexion entre el servicio y sus reglas de entrada, que es
/// justo lo que estas pruebas deben cubrir.
/// </remarks>
public sealed class ProveedorServicioPruebas
{
    private readonly RelojFijo _reloj = new();
    private readonly IProveedorRepositorio _proveedores = Substitute.For<IProveedorRepositorio>();
    private readonly IOfertaRepositorio _ofertas = Substitute.For<IOfertaRepositorio>();
    private readonly IUnidadTrabajo _unidadTrabajo = Substitute.For<IUnidadTrabajo>();

    private ProveedorServicio CrearServicio() => new(
        _proveedores,
        _ofertas,
        _unidadTrabajo,
        _reloj,
        new CrearProveedorRequestValidador(),
        new ActualizarProveedorRequestValidador());

    [Fact]
    public async Task CrearAsync_ConNombreDisponible_PersisteYDevuelveElProveedor()
    {
        _proveedores.ExisteNombreAsync("EMPRESA CENTRAL", null, Arg.Any<CancellationToken>())
            .Returns(false);
        _proveedores.ContarOfertasAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());

        ProveedorServicio servicio = CrearServicio();

        ProveedorDto resultado = await servicio.CrearAsync(new CrearProveedorRequest("Empresa Central"));

        resultado.Nombre.Should().Be("Empresa Central");
        resultado.NombreNormalizado.Should().Be("EMPRESA CENTRAL");

        _proveedores.Received(1).Agregar(Arg.Any<Proveedor>());
        await _unidadTrabajo.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// La comprobacion de unicidad usa la forma normalizada, de modo que un nombre escrito con
    /// otra combinacion de espacios y mayusculas se detecta como duplicado (seccion 8.3).
    /// </summary>
    [Fact]
    public async Task CrearAsync_ConNombreEquivalenteYaRegistrado_Falla()
    {
        _proveedores.ExisteNombreAsync("EMPRESA CENTRAL", null, Arg.Any<CancellationToken>())
            .Returns(true);

        ProveedorServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.CrearAsync(new CrearProveedorRequest("  empresa   central  "));

        var excepcion = await accion.Should().ThrowAsync<ConflictoUnicidadException>();
        excepcion.Which.CodigoError.Should().Be(CodigosError.NombreProveedorDuplicado);

        _proveedores.DidNotReceive().Agregar(Arg.Any<Proveedor>());
        await _unidadTrabajo.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CrearAsync_ConCaracteresNoPermitidos_FallaEnLaValidacionDeEntrada()
    {
        ProveedorServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.CrearAsync(new CrearProveedorRequest("Empresa @ Central"));

        await accion.Should().ThrowAsync<ValidacionException>();

        // La peticion ni siquiera debe llegar a consultar la base de datos.
        await _proveedores.DidNotReceive().ExisteNombreAsync(
            Arg.Any<string>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ObtenerAsync_CuandoNoExiste_Falla()
    {
        _proveedores.ObtenerPorIdAsync(Arg.Any<Guid>(), false, Arg.Any<CancellationToken>())
            .Returns((Proveedor?)null);

        ProveedorServicio servicio = CrearServicio();

        Func<Task> accion = () => servicio.ObtenerAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<RecursoNoEncontradoException>();
    }

    /// <summary>
    /// Seccion 8.9: un proveedor con ofertas se borra logicamente para no destruir la evidencia.
    /// </summary>
    [Fact]
    public async Task EliminarAsync_ConOfertasAsociadas_AplicaBorradoLogico()
    {
        Proveedor proveedor = Constructores.Proveedor(_reloj);

        _proveedores.ObtenerPorIdAsync(proveedor.Id, false, Arg.Any<CancellationToken>())
            .Returns(proveedor);
        _ofertas.ProveedorTieneOfertasAsync(proveedor.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        ProveedorServicio servicio = CrearServicio();

        bool borradoLogico = await servicio.EliminarAsync(proveedor.Id);

        borradoLogico.Should().BeTrue();
        proveedor.EstaEliminado.Should().BeTrue();
        _proveedores.DidNotReceive().Eliminar(Arg.Any<Proveedor>());
    }

    [Fact]
    public async Task EliminarAsync_SinOfertasAsociadas_AplicaBorradoFisico()
    {
        Proveedor proveedor = Constructores.Proveedor(_reloj);

        _proveedores.ObtenerPorIdAsync(proveedor.Id, false, Arg.Any<CancellationToken>())
            .Returns(proveedor);
        _ofertas.ProveedorTieneOfertasAsync(proveedor.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        ProveedorServicio servicio = CrearServicio();

        bool borradoLogico = await servicio.EliminarAsync(proveedor.Id);

        borradoLogico.Should().BeFalse();
        proveedor.EstaEliminado.Should().BeFalse();
        _proveedores.Received(1).Eliminar(proveedor);
    }

    [Fact]
    public async Task ActualizarAsync_ExcluyeAlPropioProveedorDeLaComprobacionDeUnicidad()
    {
        Proveedor proveedor = Constructores.Proveedor(_reloj, "Empresa Central");

        _proveedores.ObtenerPorIdAsync(proveedor.Id, false, Arg.Any<CancellationToken>())
            .Returns(proveedor);
        _proveedores.ExisteNombreAsync("EMPRESA CENTRAL", proveedor.Id, Arg.Any<CancellationToken>())
            .Returns(false);
        _proveedores.ContarOfertasAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, int>());

        ProveedorServicio servicio = CrearServicio();

        await servicio.ActualizarAsync(proveedor.Id, new ActualizarProveedorRequest("Empresa Central"));

        // Guardar el mismo nombre no debe interpretarse como un duplicado de si mismo.
        await _proveedores.Received(1).ExisteNombreAsync(
            "EMPRESA CENTRAL",
            proveedor.Id,
            Arg.Any<CancellationToken>());
    }
}
