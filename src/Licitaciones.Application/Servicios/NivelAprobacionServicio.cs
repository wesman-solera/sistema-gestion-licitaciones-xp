using FluentValidation;
using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Abstracciones;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.Application.Servicios;

/// <inheritdoc cref="INivelAprobacionServicio"/>
/// <remarks>
/// Las invariantes del conjunto (rangos que no se traslapan, un unico rango abierto) no pueden
/// validarse mirando una sola fila. Por eso cada operacion de escritura arma la lista completa
/// que quedaria vigente y se la pasa a <see cref="SelectorNivelAprobacion.AsegurarConjuntoValido"/>
/// antes de confirmar. Es mas trabajo que validar la fila sola, pero es la unica forma de que la
/// tabla no pueda quedar en un estado contradictorio.
/// </remarks>
public sealed class NivelAprobacionServicio : INivelAprobacionServicio
{
    private readonly INivelAprobacionRepositorio _niveles;
    private readonly IUnidadTrabajo _unidadTrabajo;
    private readonly IRelojSistema _reloj;
    private readonly IContextoMoneda _moneda;
    private readonly IValidator<CrearNivelAprobacionRequest> _validadorCrear;
    private readonly IValidator<ActualizarNivelAprobacionRequest> _validadorActualizar;

    /// <summary>Inicializa el servicio con sus dependencias.</summary>
    /// <param name="niveles">Repositorio de niveles de aprobacion.</param>
    /// <param name="unidadTrabajo">Unidad de trabajo que confirma los cambios.</param>
    /// <param name="reloj">Reloj del sistema inyectado.</param>
    /// <param name="moneda">Contexto de conversion monetaria de la peticion.</param>
    /// <param name="validadorCrear">Validador de la peticion de creacion.</param>
    /// <param name="validadorActualizar">Validador de la peticion de modificacion.</param>
    public NivelAprobacionServicio(
        INivelAprobacionRepositorio niveles,
        IUnidadTrabajo unidadTrabajo,
        IRelojSistema reloj,
        IContextoMoneda moneda,
        IValidator<CrearNivelAprobacionRequest> validadorCrear,
        IValidator<ActualizarNivelAprobacionRequest> validadorActualizar)
    {
        _niveles = niveles;
        _unidadTrabajo = unidadTrabajo;
        _reloj = reloj;
        _moneda = moneda;
        _validadorCrear = validadorCrear;
        _validadorActualizar = validadorActualizar;
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<NivelAprobacionDto>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default)
    {
        await _moneda.CargarAsync(cancelacion);

        var pagina = await _niveles.ListarAsync(parametros, cancelacion);

        return new PaginaResultado<NivelAprobacionDto>(
            pagina.Elementos.Select(Mapear).ToArray(),
            pagina.Pagina,
            pagina.TamanoPagina,
            pagina.TotalElementos);
    }

    /// <inheritdoc />
    public async Task<NivelAprobacionDto> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        await _moneda.CargarAsync(cancelacion);

        return Mapear(await ObtenerOFallarAsync(id, cancelacion));
    }

    /// <inheritdoc />
    public async Task<NivelAprobacionDto> CrearAsync(
        CrearNivelAprobacionRequest peticion,
        CancellationToken cancelacion = default)
    {
        await _validadorCrear.AsegurarValidoAsync(peticion, cancelacion);
        await _moneda.CargarAsync(cancelacion);

        NivelAprobacion nivel = NivelAprobacion.Crear(
            peticion.MontoMinimoCrc,
            peticion.MontoMaximoCrc,
            peticion.Aprobador,
            _reloj.AhoraUtc);

        var existentes = await _niveles.ListarTodosAsync(cancelacion);
        SelectorNivelAprobacion.AsegurarConjuntoValido([.. existentes, nivel]);

        _niveles.Agregar(nivel);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        return Mapear(nivel);
    }

    /// <inheritdoc />
    public async Task<NivelAprobacionDto> ActualizarAsync(
        Guid id,
        ActualizarNivelAprobacionRequest peticion,
        CancellationToken cancelacion = default)
    {
        await _validadorActualizar.AsegurarValidoAsync(peticion, cancelacion);
        await _moneda.CargarAsync(cancelacion);

        NivelAprobacion nivel = await ObtenerOFallarAsync(id, cancelacion);

        nivel.Actualizar(
            peticion.MontoMinimoCrc,
            peticion.MontoMaximoCrc,
            peticion.Aprobador,
            _reloj.AhoraUtc);

        // El conjunto a validar es el actual con este rango ya modificado en memoria.
        var existentes = await _niveles.ListarTodosAsync(cancelacion);
        SelectorNivelAprobacion.AsegurarConjuntoValido(
            [.. existentes.Where(n => n.Id != id), nivel]);

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        return Mapear(nivel);
    }

    /// <inheritdoc />
    public async Task EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        NivelAprobacion nivel = await ObtenerOFallarAsync(id, cancelacion);

        _niveles.Eliminar(nivel);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<ConsultaAprobadorDto> ConsultarAprobadorAsync(
        decimal montoCrc,
        CancellationToken cancelacion = default)
    {
        var niveles = await _niveles.ListarTodosAsync(cancelacion);
        NivelAprobacion? nivel = SelectorNivelAprobacion.Seleccionar(montoCrc, niveles);

        return new ConsultaAprobadorDto(montoCrc, nivel?.Aprobador, nivel?.Id);
    }

    private async Task<NivelAprobacion> ObtenerOFallarAsync(Guid id, CancellationToken cancelacion)
    {
        return await _niveles.ObtenerPorIdAsync(id, cancelacion)
            ?? throw new RecursoNoEncontradoException("Nivel de aprobacion", id);
    }

    private NivelAprobacionDto Mapear(NivelAprobacion nivel) => new(
        nivel.Id,
        _moneda.Monto(nivel.MontoMinimoCrc),
        nivel.MontoMaximoCrc is decimal maximo ? _moneda.Monto(maximo) : null,
        nivel.Aprobador,
        nivel.EsRangoAbierto,
        nivel.CreatedAt,
        nivel.UpdatedAt);
}
