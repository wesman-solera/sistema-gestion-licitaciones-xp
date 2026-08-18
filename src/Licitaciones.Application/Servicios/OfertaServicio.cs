using FluentValidation;
using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Abstracciones;
using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.Application.Servicios;

/// <inheritdoc cref="IOfertaServicio"/>
/// <remarks>
/// La comprobacion de oferta duplicada se hace aqui, contra la base de datos, porque la entidad
/// no puede conocer las demas ofertas sin depender de la persistencia. Es la primera de las dos
/// defensas: la segunda es el indice unico compuesto <c>(licitacion_id, proveedor_id)</c>, que
/// cubre la carrera entre dos peticiones simultaneas que pasen ambas por esta comprobacion.
/// </remarks>
public sealed class OfertaServicio : IOfertaServicio
{
    private readonly IOfertaRepositorio _ofertas;
    private readonly ILicitacionRepositorio _licitaciones;
    private readonly IProveedorRepositorio _proveedores;
    private readonly IUnidadTrabajo _unidadTrabajo;
    private readonly IRelojSistema _reloj;
    private readonly IContextoMoneda _moneda;
    private readonly IValidator<CrearOfertaRequest> _validadorCrear;
    private readonly IValidator<ActualizarOfertaRequest> _validadorActualizar;

    /// <summary>Inicializa el servicio con sus dependencias.</summary>
    /// <param name="ofertas">Repositorio de ofertas.</param>
    /// <param name="licitaciones">Repositorio de licitaciones.</param>
    /// <param name="proveedores">Repositorio de proveedores.</param>
    /// <param name="unidadTrabajo">Unidad de trabajo que confirma los cambios.</param>
    /// <param name="reloj">Reloj del sistema inyectado.</param>
    /// <param name="moneda">Contexto de conversion monetaria de la peticion.</param>
    /// <param name="validadorCrear">Validador de la peticion de registro.</param>
    /// <param name="validadorActualizar">Validador de la peticion de modificacion.</param>
    public OfertaServicio(
        IOfertaRepositorio ofertas,
        ILicitacionRepositorio licitaciones,
        IProveedorRepositorio proveedores,
        IUnidadTrabajo unidadTrabajo,
        IRelojSistema reloj,
        IContextoMoneda moneda,
        IValidator<CrearOfertaRequest> validadorCrear,
        IValidator<ActualizarOfertaRequest> validadorActualizar)
    {
        _ofertas = ofertas;
        _licitaciones = licitaciones;
        _proveedores = proveedores;
        _unidadTrabajo = unidadTrabajo;
        _reloj = reloj;
        _moneda = moneda;
        _validadorCrear = validadorCrear;
        _validadorActualizar = validadorActualizar;
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<OfertaDto>> ListarAsync(
        ParametrosConsulta parametros,
        Guid? licitacionId = null,
        Guid? proveedorId = null,
        CancellationToken cancelacion = default)
    {
        await _moneda.CargarAsync(cancelacion);

        var pagina = await _ofertas.ListarAsync(parametros, licitacionId, proveedorId, cancelacion);

        // La marca de mejor oferta solo tiene sentido dentro de una licitacion concreta: en un
        // listado mezclado se omite en lugar de calcular algo enganoso.
        Guid? mejorId = licitacionId is Guid unico
            ? await ObtenerMejorOfertaIdAsync(unico, cancelacion)
            : null;

        var elementos = pagina.Elementos
            .Select(o => Mapear(o, o.Id == mejorId))
            .ToArray();

        return new PaginaResultado<OfertaDto>(
            elementos,
            pagina.Pagina,
            pagina.TamanoPagina,
            pagina.TotalElementos);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OfertaDto>> ListarPorLicitacionAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default)
    {
        await _moneda.CargarAsync(cancelacion);

        var ofertas = await _ofertas.ListarPorLicitacionAsync(licitacionId, cancelacion);
        Oferta? mejor = EvaluadorOfertas.DeterminarMejorOferta(ofertas);

        return ofertas
            .OrderBy(o => o.MontoOfertadoCrc)
            .ThenBy(o => o.FechaRegistro)
            .Select(o => Mapear(o, o.Id == mejor?.Id))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<OfertaDto> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        await _moneda.CargarAsync(cancelacion);

        Oferta oferta = await ObtenerOFallarAsync(id, cancelacion);
        Guid? mejorId = await ObtenerMejorOfertaIdAsync(oferta.LicitacionId, cancelacion);

        return Mapear(oferta, oferta.Id == mejorId);
    }

    /// <inheritdoc />
    public async Task<OfertaDto> RegistrarAsync(
        CrearOfertaRequest peticion,
        CancellationToken cancelacion = default)
    {
        await _validadorCrear.AsegurarValidoAsync(peticion, cancelacion);
        await _moneda.CargarAsync(cancelacion);

        Licitacion licitacion =
            await _licitaciones.ObtenerPorIdAsync(peticion.LicitacionId, false, cancelacion)
            ?? throw new RecursoNoEncontradoException("Licitacion", peticion.LicitacionId);

        Proveedor proveedor =
            await _proveedores.ObtenerPorIdAsync(peticion.ProveedorId, false, cancelacion)
            ?? throw new RecursoNoEncontradoException("Proveedor", peticion.ProveedorId);

        await AsegurarSinOfertaPreviaAsync(
            licitacion.Id,
            proveedor.Id,
            idExcluido: null,
            cancelacion);

        // La entidad valida estado, vencimiento y presupuesto en un solo lugar.
        Oferta oferta = Oferta.Registrar(
            licitacion,
            proveedor.Id,
            peticion.MontoOfertadoCrc,
            _reloj.AhoraUtc);

        _ofertas.Agregar(oferta);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        Guid? mejorId = await ObtenerMejorOfertaIdAsync(licitacion.Id, cancelacion);

        return new OfertaDto(
            oferta.Id,
            oferta.LicitacionId,
            licitacion.Codigo,
            oferta.ProveedorId,
            proveedor.Nombre,
            _moneda.Monto(oferta.MontoOfertadoCrc),
            oferta.FechaRegistro,
            oferta.UpdatedAt,
            oferta.Id == mejorId);
    }

    /// <inheritdoc />
    public async Task<OfertaDto> ActualizarAsync(
        Guid id,
        ActualizarOfertaRequest peticion,
        CancellationToken cancelacion = default)
    {
        await _validadorActualizar.AsegurarValidoAsync(peticion, cancelacion);
        await _moneda.CargarAsync(cancelacion);

        Oferta oferta = await ObtenerOFallarAsync(id, cancelacion);

        Licitacion licitacion =
            await _licitaciones.ObtenerPorIdAsync(oferta.LicitacionId, true, cancelacion)
            ?? throw new RecursoNoEncontradoException("Licitacion", oferta.LicitacionId);

        oferta.CambiarMonto(licitacion, peticion.MontoOfertadoCrc, _reloj.AhoraUtc);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        Guid? mejorId = await ObtenerMejorOfertaIdAsync(oferta.LicitacionId, cancelacion);

        return Mapear(oferta, oferta.Id == mejorId);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Seccion 8.2: una oferta vencida o de una licitacion cerrada no puede eliminarse. La
    /// comprobacion la hace la entidad, no este metodo.
    /// </remarks>
    public async Task EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        Oferta oferta = await ObtenerOFallarAsync(id, cancelacion);

        Licitacion licitacion =
            await _licitaciones.ObtenerPorIdAsync(oferta.LicitacionId, true, cancelacion)
            ?? throw new RecursoNoEncontradoException("Licitacion", oferta.LicitacionId);

        oferta.AsegurarMutable(licitacion, _reloj.AhoraUtc);

        _ofertas.Eliminar(oferta);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
    }

    private async Task<Guid?> ObtenerMejorOfertaIdAsync(Guid licitacionId, CancellationToken cancelacion)
    {
        var ofertas = await _ofertas.ListarPorLicitacionAsync(licitacionId, cancelacion);

        return EvaluadorOfertas.DeterminarMejorOferta(ofertas)?.Id;
    }

    private async Task<Oferta> ObtenerOFallarAsync(Guid id, CancellationToken cancelacion)
    {
        return await _ofertas.ObtenerPorIdAsync(id, cancelacion)
            ?? throw new RecursoNoEncontradoException("Oferta", id);
    }

    private async Task AsegurarSinOfertaPreviaAsync(
        Guid licitacionId,
        Guid proveedorId,
        Guid? idExcluido,
        CancellationToken cancelacion)
    {
        bool existe = await _ofertas.ExisteOfertaDeProveedorAsync(
            licitacionId,
            proveedorId,
            idExcluido,
            cancelacion);

        if (existe)
        {
            throw new ConflictoUnicidadException(
                nameof(CrearOfertaRequest.ProveedorId),
                "El proveedor ya registro una oferta para esta licitacion.",
                CodigosError.OfertaDuplicada);
        }
    }

    private OfertaDto Mapear(Oferta oferta, bool esMejor) => new(
        oferta.Id,
        oferta.LicitacionId,
        oferta.Licitacion?.Codigo ?? string.Empty,
        oferta.ProveedorId,
        oferta.Proveedor?.Nombre ?? string.Empty,
        _moneda.Monto(oferta.MontoOfertadoCrc),
        oferta.FechaRegistro,
        oferta.UpdatedAt,
        esMejor);
}
