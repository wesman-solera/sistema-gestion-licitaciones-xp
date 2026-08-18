using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Repositorios;

/// <inheritdoc cref="ITipoCambioRepositorio"/>
public sealed class TipoCambioRepositorio : ITipoCambioRepositorio
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>Inicializa el repositorio con el contexto de datos.</summary>
    /// <param name="contexto">Contexto de Entity Framework Core.</param>
    public TipoCambioRepositorio(LicitacionesDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<TipoCambio?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default)
    {
        return await _contexto.TiposCambio.FirstOrDefaultAsync(t => t.Id == id, cancelacion);
    }

    /// <inheritdoc />
    public async Task<TipoCambio?> ObtenerActivoAsync(CancellationToken cancelacion = default)
    {
        // El indice unico parcial garantiza que a lo sumo exista una fila activa, por lo que
        // FirstOrDefault no puede ocultar un segundo resultado inesperado.
        return await _contexto.TiposCambio
            .Where(t => t.Activo)
            .OrderByDescending(t => t.FechaVigencia)
            .FirstOrDefaultAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TipoCambio>> ListarActivosAsync(
        CancellationToken cancelacion = default)
    {
        return await _contexto.TiposCambio.Where(t => t.Activo).ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<TipoCambio>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        IQueryable<TipoCambio> consulta = _contexto.TiposCambio;

        consulta = (parametros.OrdenarPor?.ToLowerInvariant()) switch
        {
            "valor" => parametros.Descendente
                ? consulta.OrderByDescending(t => t.CrcPorUsd)
                : consulta.OrderBy(t => t.CrcPorUsd),
            _ => parametros.Descendente
                ? consulta.OrderBy(t => t.FechaVigencia)
                : consulta.OrderByDescending(t => t.FechaVigencia)
        };

        int total = await consulta.CountAsync(cancelacion);

        var elementos = await consulta
            .Skip(parametros.Omitir)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new PaginaResultado<TipoCambio>(
            elementos,
            parametros.Pagina,
            parametros.TamanoPagina,
            total);
    }

    /// <inheritdoc />
    public void Agregar(TipoCambio tipoCambio) => _contexto.TiposCambio.Add(tipoCambio);

    /// <inheritdoc />
    public void Eliminar(TipoCambio tipoCambio) => _contexto.TiposCambio.Remove(tipoCambio);
}
