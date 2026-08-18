using FluentValidation;
using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Abstracciones;
using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;

namespace Licitaciones.Application.Servicios;

/// <inheritdoc cref="ITipoCambioServicio"/>
/// <remarks>
/// La invariante "solo un tipo de cambio activo" se sostiene con una transaccion explicita:
/// desactivar los actuales y activar el nuevo tienen que ocurrir juntos o no ocurrir. Sin la
/// transaccion, un fallo entre ambas operaciones dejaria el sistema con cero o con dos activos,
/// y la conversion a dolares quedaria indefinida.
/// </remarks>
public sealed class TipoCambioServicio : ITipoCambioServicio
{
    private readonly ITipoCambioRepositorio _tiposCambio;
    private readonly IUnidadTrabajo _unidadTrabajo;
    private readonly IRelojSistema _reloj;
    private readonly IValidator<CrearTipoCambioRequest> _validadorCrear;
    private readonly IValidator<ActualizarTipoCambioRequest> _validadorActualizar;

    /// <summary>Inicializa el servicio con sus dependencias.</summary>
    /// <param name="tiposCambio">Repositorio de tipos de cambio.</param>
    /// <param name="unidadTrabajo">Unidad de trabajo que confirma los cambios.</param>
    /// <param name="reloj">Reloj del sistema inyectado.</param>
    /// <param name="validadorCrear">Validador de la peticion de creacion.</param>
    /// <param name="validadorActualizar">Validador de la peticion de modificacion.</param>
    public TipoCambioServicio(
        ITipoCambioRepositorio tiposCambio,
        IUnidadTrabajo unidadTrabajo,
        IRelojSistema reloj,
        IValidator<CrearTipoCambioRequest> validadorCrear,
        IValidator<ActualizarTipoCambioRequest> validadorActualizar)
    {
        _tiposCambio = tiposCambio;
        _unidadTrabajo = unidadTrabajo;
        _reloj = reloj;
        _validadorCrear = validadorCrear;
        _validadorActualizar = validadorActualizar;
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<TipoCambioDto>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default)
    {
        var pagina = await _tiposCambio.ListarAsync(parametros, cancelacion);

        return new PaginaResultado<TipoCambioDto>(
            pagina.Elementos.Select(Mapear).ToArray(),
            pagina.Pagina,
            pagina.TamanoPagina,
            pagina.TotalElementos);
    }

    /// <inheritdoc />
    public async Task<TipoCambioDto> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
        => Mapear(await ObtenerOFallarAsync(id, cancelacion));

    /// <inheritdoc />
    public async Task<TipoCambioDto?> ObtenerActivoAsync(CancellationToken cancelacion = default)
    {
        TipoCambio? activo = await _tiposCambio.ObtenerActivoAsync(cancelacion);

        return activo is null ? null : Mapear(activo);
    }

    /// <inheritdoc />
    public async Task<TipoCambioDto> CrearAsync(
        CrearTipoCambioRequest peticion,
        CancellationToken cancelacion = default)
    {
        await _validadorCrear.AsegurarValidoAsync(peticion, cancelacion);

        return await _unidadTrabajo.EnTransaccionAsync(async ct =>
        {
            TipoCambio tipoCambio = TipoCambio.Crear(
                peticion.CrcPorUsd,
                peticion.FechaVigencia,
                peticion.Activo,
                _reloj.AhoraUtc);

            if (peticion.Activo)
            {
                await DesactivarActivosAsync(idExcluido: null, ct);
            }

            _tiposCambio.Agregar(tipoCambio);
            await _unidadTrabajo.GuardarCambiosAsync(ct);

            return Mapear(tipoCambio);
        }, cancelacion);
    }

    /// <inheritdoc />
    public async Task<TipoCambioDto> ActualizarAsync(
        Guid id,
        ActualizarTipoCambioRequest peticion,
        CancellationToken cancelacion = default)
    {
        await _validadorActualizar.AsegurarValidoAsync(peticion, cancelacion);

        TipoCambio tipoCambio = await ObtenerOFallarAsync(id, cancelacion);

        tipoCambio.Actualizar(peticion.CrcPorUsd, peticion.FechaVigencia, _reloj.AhoraUtc);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        return Mapear(tipoCambio);
    }

    /// <inheritdoc />
    public async Task<TipoCambioDto> ActivarAsync(Guid id, CancellationToken cancelacion = default)
    {
        return await _unidadTrabajo.EnTransaccionAsync(async ct =>
        {
            TipoCambio tipoCambio = await ObtenerOFallarAsync(id, ct);

            await DesactivarActivosAsync(idExcluido: id, ct);
            tipoCambio.Activar(_reloj.AhoraUtc);

            await _unidadTrabajo.GuardarCambiosAsync(ct);

            return Mapear(tipoCambio);
        }, cancelacion);
    }

    /// <inheritdoc />
    /// <remarks>
    /// No se permite eliminar el tipo de cambio activo: dejaria al sistema sin poder convertir
    /// montos. Primero debe activarse otro registro.
    /// </remarks>
    public async Task EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        TipoCambio tipoCambio = await ObtenerOFallarAsync(id, cancelacion);

        if (tipoCambio.Activo)
        {
            throw new ReglaNegocioVioladaException(
                "No se puede eliminar el tipo de cambio activo. Active otro registro antes de eliminarlo.",
                CodigosError.SinTipoCambioActivo);
        }

        _tiposCambio.Eliminar(tipoCambio);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
    }

    private async Task DesactivarActivosAsync(Guid? idExcluido, CancellationToken cancelacion)
    {
        var activos = await _tiposCambio.ListarActivosAsync(cancelacion);

        foreach (TipoCambio activo in activos.Where(t => t.Id != idExcluido))
        {
            activo.Desactivar(_reloj.AhoraUtc);
        }
    }

    private async Task<TipoCambio> ObtenerOFallarAsync(Guid id, CancellationToken cancelacion)
    {
        return await _tiposCambio.ObtenerPorIdAsync(id, cancelacion)
            ?? throw new RecursoNoEncontradoException("Tipo de cambio", id);
    }

    private static TipoCambioDto Mapear(TipoCambio tipoCambio) => new(
        tipoCambio.Id,
        tipoCambio.CrcPorUsd,
        tipoCambio.FechaVigencia,
        tipoCambio.Activo,
        tipoCambio.CreatedAt,
        tipoCambio.UpdatedAt);
}
