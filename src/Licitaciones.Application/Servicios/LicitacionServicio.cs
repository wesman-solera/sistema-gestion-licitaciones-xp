using FluentValidation;
using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Abstracciones;
using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enums;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.ObjetosValor;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.Application.Servicios;

/// <inheritdoc cref="ILicitacionServicio"/>
/// <remarks>
/// Este servicio es el punto donde convergen casi todas las reglas del enunciado, por lo que
/// vale aclarar que hace y que no hace. Coordina: valida formato, consulta lo que el dominio
/// necesita saber pero no puede averiguar por si mismo (si el codigo ya existe, cual es la
/// oferta mas alta, que niveles de aprobacion hay parametrizados) y confirma la transaccion.
/// No decide: el ciclo de estados, la mejor oferta, la clasificacion del ahorro y la seleccion
/// del aprobador viven en el dominio y aqui solo se invocan.
/// </remarks>
public sealed class LicitacionServicio : ILicitacionServicio
{
    private readonly ILicitacionRepositorio _licitaciones;
    private readonly IOfertaRepositorio _ofertas;
    private readonly INivelAprobacionRepositorio _nivelesAprobacion;
    private readonly IUnidadTrabajo _unidadTrabajo;
    private readonly IRelojSistema _reloj;
    private readonly IContextoMoneda _moneda;
    private readonly IValidator<CrearLicitacionRequest> _validadorCrear;
    private readonly IValidator<ActualizarLicitacionRequest> _validadorActualizar;
    private readonly IValidator<CambiarEstadoRequest> _validadorEstado;

    /// <summary>Inicializa el servicio con sus dependencias.</summary>
    /// <param name="licitaciones">Repositorio de licitaciones.</param>
    /// <param name="ofertas">Repositorio de ofertas.</param>
    /// <param name="nivelesAprobacion">Repositorio de niveles de aprobacion.</param>
    /// <param name="unidadTrabajo">Unidad de trabajo que confirma los cambios.</param>
    /// <param name="reloj">Reloj del sistema inyectado.</param>
    /// <param name="moneda">Contexto de conversion monetaria de la peticion.</param>
    /// <param name="validadorCrear">Validador de la peticion de creacion.</param>
    /// <param name="validadorActualizar">Validador de la peticion de modificacion.</param>
    /// <param name="validadorEstado">Validador de la peticion de cambio de estado.</param>
    public LicitacionServicio(
        ILicitacionRepositorio licitaciones,
        IOfertaRepositorio ofertas,
        INivelAprobacionRepositorio nivelesAprobacion,
        IUnidadTrabajo unidadTrabajo,
        IRelojSistema reloj,
        IContextoMoneda moneda,
        IValidator<CrearLicitacionRequest> validadorCrear,
        IValidator<ActualizarLicitacionRequest> validadorActualizar,
        IValidator<CambiarEstadoRequest> validadorEstado)
    {
        _licitaciones = licitaciones;
        _ofertas = ofertas;
        _nivelesAprobacion = nivelesAprobacion;
        _unidadTrabajo = unidadTrabajo;
        _reloj = reloj;
        _moneda = moneda;
        _validadorCrear = validadorCrear;
        _validadorActualizar = validadorActualizar;
        _validadorEstado = validadorEstado;
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<LicitacionResumenDto>> ListarAsync(
        ParametrosConsulta parametros,
        EstadoLicitacion? estado = null,
        CancellationToken cancelacion = default)
    {
        await _moneda.CargarAsync(cancelacion);

        var pagina = await _licitaciones.ListarAsync(parametros, estado, cancelacion);
        DateTimeOffset ahora = _reloj.AhoraUtc;

        var resumenes = pagina.Elementos
            .Select(l => new LicitacionResumenDto(
                l.Id,
                l.Codigo,
                l.Titulo,
                l.Estado,
                EstadoEfectivo(l, ahora),
                l.FechaCierre,
                _moneda.Monto(l.PresupuestoEstimadoCrc),
                l.Ofertas.Count,
                l.EstaEliminada,
                l.CreatedAt,
                l.UpdatedAt))
            .ToArray();

        return new PaginaResultado<LicitacionResumenDto>(
            resumenes,
            pagina.Pagina,
            pagina.TamanoPagina,
            pagina.TotalElementos);
    }

    /// <inheritdoc />
    public async Task<LicitacionDetalleDto> ObtenerDetalleAsync(
        Guid id,
        CancellationToken cancelacion = default)
    {
        await _moneda.CargarAsync(cancelacion);

        Licitacion licitacion = await ObtenerConOfertasOFallarAsync(id, cancelacion);

        return await MapearDetalleAsync(licitacion, cancelacion);
    }

    /// <inheritdoc />
    public async Task<LicitacionDetalleDto> CrearAsync(
        CrearLicitacionRequest peticion,
        CancellationToken cancelacion = default)
    {
        await _validadorCrear.AsegurarValidoAsync(peticion, cancelacion);
        await _moneda.CargarAsync(cancelacion);

        string codigoNormalizado = NormalizadorTexto.NormalizarCodigo(peticion.Codigo);
        await AsegurarCodigoDisponibleAsync(codigoNormalizado, idExcluido: null, cancelacion);

        Licitacion licitacion = Licitacion.Crear(
            peticion.Codigo,
            peticion.Titulo,
            peticion.PresupuestoEstimadoCrc,
            peticion.FechaCierre,
            _reloj.AhoraUtc);

        _licitaciones.Agregar(licitacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        return await MapearDetalleAsync(licitacion, cancelacion);
    }

    /// <inheritdoc />
    public async Task<LicitacionDetalleDto> ActualizarAsync(
        Guid id,
        ActualizarLicitacionRequest peticion,
        CancellationToken cancelacion = default)
    {
        await _validadorActualizar.AsegurarValidoAsync(peticion, cancelacion);
        await _moneda.CargarAsync(cancelacion);

        Licitacion licitacion = await ObtenerConOfertasOFallarAsync(id, cancelacion);

        // La regla "el presupuesto no puede bajar de una oferta existente" la aplica el dominio,
        // pero el dato de cual es la oferta mas alta solo lo conoce la base de datos.
        decimal? mayorOferta = await _ofertas.ObtenerMayorMontoAsync(id, cancelacion);

        licitacion.ActualizarDatos(
            peticion.Titulo,
            peticion.PresupuestoEstimadoCrc,
            peticion.FechaCierre,
            mayorOferta,
            _reloj.AhoraUtc);

        string codigoNormalizado = NormalizadorTexto.NormalizarCodigo(peticion.Codigo);

        if (!string.Equals(codigoNormalizado, licitacion.CodigoNormalizado, StringComparison.Ordinal))
        {
            await AsegurarCodigoDisponibleAsync(codigoNormalizado, idExcluido: id, cancelacion);
            licitacion.CambiarCodigo(peticion.Codigo, _reloj.AhoraUtc);
        }

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        return await MapearDetalleAsync(licitacion, cancelacion);
    }

    /// <inheritdoc />
    public async Task<LicitacionDetalleDto> CambiarEstadoAsync(
        Guid id,
        CambiarEstadoRequest peticion,
        CancellationToken cancelacion = default)
    {
        await _validadorEstado.AsegurarValidoAsync(peticion, cancelacion);
        await _moneda.CargarAsync(cancelacion);

        Licitacion licitacion = await ObtenerConOfertasOFallarAsync(id, cancelacion);

        licitacion.CambiarEstado(peticion.Estado, _reloj.AhoraUtc);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        return await MapearDetalleAsync(licitacion, cancelacion);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Seccion 8.9: una licitacion con ofertas no se borra fisicamente porque arrastraria las
    /// ofertas asociadas, que deben conservarse como evidencia.
    /// </remarks>
    public async Task<bool> EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        Licitacion licitacion = await ObtenerOFallarAsync(id, cancelacion);

        bool tieneOfertas = await _ofertas.LicitacionTieneOfertasAsync(id, cancelacion);

        if (tieneOfertas)
        {
            licitacion.EliminarLogicamente(_reloj.AhoraUtc);
        }
        else
        {
            _licitaciones.Eliminar(licitacion);
        }

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        return tieneOfertas;
    }

    /// <inheritdoc />
    public async Task<EvaluacionLicitacionDto> ObtenerMejorOfertaAsync(
        Guid id,
        CancellationToken cancelacion = default)
    {
        await _moneda.CargarAsync(cancelacion);

        Licitacion licitacion = await ObtenerConOfertasOFallarAsync(id, cancelacion);

        return await EvaluarAsync(licitacion, cancelacion);
    }

    /// <summary>
    /// Calcula el estado que debe mostrarse al usuario considerando el vencimiento.
    /// </summary>
    /// <param name="licitacion">Licitacion evaluada.</param>
    /// <param name="ahora">Instante actual, en UTC.</param>
    /// <returns>El estado efectivo.</returns>
    /// <remarks>
    /// El estado persistido puede decir <c>Publicada</c> aunque la fecha de cierre ya haya
    /// pasado, porque ningun proceso lo actualizo. La seccion 8.1 aclara que en ese caso la
    /// licitacion esta cerrada funcionalmente, y eso es lo que debe verse en pantalla.
    /// </remarks>
    private static EstadoLicitacion EstadoEfectivo(Licitacion licitacion, DateTimeOffset ahora)
        => licitacion.EstaCerradaFuncionalmente(ahora)
            ? EstadoLicitacion.Cerrada
            : licitacion.Estado;

    private async Task<EvaluacionLicitacionDto> EvaluarAsync(
        Licitacion licitacion,
        CancellationToken cancelacion)
    {
        IReadOnlyList<Oferta> ofertas = licitacion.Ofertas.Count > 0
            ? [.. licitacion.Ofertas]
            : await _ofertas.ListarPorLicitacionAsync(licitacion.Id, cancelacion);

        ResultadoEvaluacionOfertas resultado =
            EvaluadorOfertas.Evaluar(ofertas, licitacion.PresupuestoEstimadoCrc);

        string? aprobador = null;
        Guid? nivelId = null;

        if (resultado.MejorOferta is Oferta mejor)
        {
            var niveles = await _nivelesAprobacion.ListarTodosAsync(cancelacion);
            NivelAprobacion? nivel =
                SelectorNivelAprobacion.Seleccionar(mejor.MontoOfertadoCrc, niveles);

            // Si la tabla no cubre el monto no se interrumpe la consulta: se informa la ausencia.
            // Interrumpir dejaria la pantalla de detalle inutilizable por un dato de configuracion.
            aprobador = nivel?.Aprobador;
            nivelId = nivel?.Id;
        }

        return new EvaluacionLicitacionDto(
            resultado.MejorOferta is null ? null : MapearOferta(resultado.MejorOferta, licitacion, true),
            resultado.PorcentajeAhorro,
            resultado.Clasificacion,
            resultado.EtiquetaClasificacion,
            resultado.CantidadOfertas,
            aprobador,
            nivelId);
    }

    private async Task<LicitacionDetalleDto> MapearDetalleAsync(
        Licitacion licitacion,
        CancellationToken cancelacion)
    {
        EvaluacionLicitacionDto evaluacion = await EvaluarAsync(licitacion, cancelacion);

        IReadOnlyList<Oferta> ofertas = licitacion.Ofertas.Count > 0
            ? [.. licitacion.Ofertas]
            : await _ofertas.ListarPorLicitacionAsync(licitacion.Id, cancelacion);

        Guid? mejorOfertaId = evaluacion.MejorOferta?.Id;

        var ofertasDto = ofertas
            .OrderBy(o => o.MontoOfertadoCrc)
            .ThenBy(o => o.FechaRegistro)
            .Select(o => MapearOferta(o, licitacion, o.Id == mejorOfertaId))
            .ToArray();

        return new LicitacionDetalleDto(
            licitacion.Id,
            licitacion.Codigo,
            licitacion.Titulo,
            licitacion.Estado,
            EstadoEfectivo(licitacion, _reloj.AhoraUtc),
            licitacion.FechaCierre,
            _moneda.Monto(licitacion.PresupuestoEstimadoCrc),
            licitacion.EstaEliminada,
            [.. PoliticaTransicionEstado.DestinosDisponibles(licitacion.Estado)],
            evaluacion,
            ofertasDto,
            _moneda.TipoCambioAplicado,
            licitacion.CreatedAt,
            licitacion.UpdatedAt);
    }

    private OfertaDto MapearOferta(Oferta oferta, Licitacion licitacion, bool esMejor) => new(
        oferta.Id,
        oferta.LicitacionId,
        licitacion.Codigo,
        oferta.ProveedorId,
        oferta.Proveedor?.Nombre ?? string.Empty,
        _moneda.Monto(oferta.MontoOfertadoCrc),
        oferta.FechaRegistro,
        oferta.UpdatedAt,
        esMejor);

    private async Task<Licitacion> ObtenerOFallarAsync(Guid id, CancellationToken cancelacion)
    {
        return await _licitaciones.ObtenerPorIdAsync(id, incluirEliminadas: false, cancelacion)
            ?? throw new RecursoNoEncontradoException("Licitacion", id);
    }

    private async Task<Licitacion> ObtenerConOfertasOFallarAsync(Guid id, CancellationToken cancelacion)
    {
        return await _licitaciones.ObtenerConOfertasAsync(id, cancelacion)
            ?? throw new RecursoNoEncontradoException("Licitacion", id);
    }

    private async Task AsegurarCodigoDisponibleAsync(
        string codigoNormalizado,
        Guid? idExcluido,
        CancellationToken cancelacion)
    {
        if (await _licitaciones.ExisteCodigoAsync(codigoNormalizado, idExcluido, cancelacion))
        {
            throw new ConflictoUnicidadException(
                nameof(CrearLicitacionRequest.Codigo),
                "Ya existe una licitacion registrada con ese codigo.",
                CodigosError.CodigoLicitacionDuplicado);
        }
    }
}
