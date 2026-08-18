using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Repositorios;

/// <inheritdoc cref="INivelAprobacionRepositorio"/>
public sealed class NivelAprobacionRepositorio : INivelAprobacionRepositorio
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>Inicializa el repositorio con el contexto de datos.</summary>
    /// <param name="contexto">Contexto de Entity Framework Core.</param>
    public NivelAprobacionRepositorio(LicitacionesDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<NivelAprobacion?> ObtenerPorIdAsync(
        Guid id,
        CancellationToken cancelacion = default)
    {
        return await _contexto.NivelesAprobacion.FirstOrDefaultAsync(n => n.Id == id, cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NivelAprobacion>> ListarTodosAsync(
        CancellationToken cancelacion = default)
    {
        return await _contexto.NivelesAprobacion
            .OrderBy(n => n.MontoMinimoCrc)
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<NivelAprobacion>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        IQueryable<NivelAprobacion> consulta = _contexto.NivelesAprobacion;

        if (!string.IsNullOrWhiteSpace(parametros.Buscar))
        {
            string patron = $"%{parametros.Buscar.Trim()}%";
            consulta = consulta.Where(n => EF.Functions.ILike(n.Aprobador, patron));
        }

        consulta = parametros.Descendente
            ? consulta.OrderByDescending(n => n.MontoMinimoCrc)
            : consulta.OrderBy(n => n.MontoMinimoCrc);

        int total = await consulta.CountAsync(cancelacion);

        var elementos = await consulta
            .Skip(parametros.Omitir)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new PaginaResultado<NivelAprobacion>(
            elementos,
            parametros.Pagina,
            parametros.TamanoPagina,
            total);
    }

    /// <inheritdoc />
    public void Agregar(NivelAprobacion nivel) => _contexto.NivelesAprobacion.Add(nivel);

    /// <inheritdoc />
    public void Eliminar(NivelAprobacion nivel) => _contexto.NivelesAprobacion.Remove(nivel);
}
