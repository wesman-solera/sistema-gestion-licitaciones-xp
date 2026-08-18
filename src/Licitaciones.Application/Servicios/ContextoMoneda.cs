using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.Application.Servicios;

/// <inheritdoc cref="IContextoMoneda"/>
public sealed class ContextoMoneda : IContextoMoneda
{
    private readonly ITipoCambioRepositorio _tipoCambioRepositorio;
    private TipoCambio? _tipoCambio;
    private bool _cargado;

    /// <summary>Inicializa el contexto con el repositorio de tipos de cambio.</summary>
    /// <param name="tipoCambioRepositorio">Repositorio de tipos de cambio.</param>
    public ContextoMoneda(ITipoCambioRepositorio tipoCambioRepositorio)
    {
        _tipoCambioRepositorio = tipoCambioRepositorio;
    }

    /// <inheritdoc />
    public TipoCambioAplicadoDto? TipoCambioAplicado => _tipoCambio is null
        ? null
        : new TipoCambioAplicadoDto(_tipoCambio.CrcPorUsd, _tipoCambio.FechaVigencia);

    /// <inheritdoc />
    public async Task CargarAsync(CancellationToken cancelacion = default)
    {
        if (_cargado)
        {
            return;
        }

        _tipoCambio = await _tipoCambioRepositorio.ObtenerActivoAsync(cancelacion);
        _cargado = true;
    }

    /// <inheritdoc />
    public MontoDto Monto(decimal montoCrc)
    {
        if (_tipoCambio is null)
        {
            return new MontoDto(montoCrc, null);
        }

        return new MontoDto(montoCrc, ConversorMoneda.ConvertirAUsd(montoCrc, _tipoCambio.CrcPorUsd));
    }
}
