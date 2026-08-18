using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Repositorios;

/// <inheritdoc cref="IProveedorRepositorio"/>
public sealed class ProveedorRepositorio : IProveedorRepositorio
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>Inicializa el repositorio con el contexto de datos.</summary>
    /// <param name="contexto">Contexto de Entity Framework Core.</param>
    public ProveedorRepositorio(LicitacionesDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<Proveedor?> ObtenerPorIdAsync(
        Guid id,
        bool incluirEliminados = false,
        CancellationToken cancelacion = default)
    {
        IQueryable<Proveedor> consulta = _contexto.Proveedores;

        if (!incluirEliminados)
        {
            consulta = consulta.Where(p => p.DeletedAt == null);
        }

        return await consulta.FirstOrDefaultAsync(p => p.Id == id, cancelacion);
    }

    /// <inheritdoc />
    public async Task<bool> ExisteNombreAsync(
        string nombreNormalizado,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default)
    {
        return await _contexto.Proveedores
            .AnyAsync(
                p => p.NombreNormalizado == nombreNormalizado
                     && (idExcluido == null || p.Id != idExcluido),
                cancelacion);
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<Proveedor>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        IQueryable<Proveedor> consulta = _contexto.Proveedores;

        if (!parametros.IncluirEliminados)
        {
            consulta = consulta.Where(p => p.DeletedAt == null);
        }

        if (!string.IsNullOrWhiteSpace(parametros.Buscar))
        {
            string patron = $"%{parametros.Buscar.Trim()}%";
            consulta = consulta.Where(p => EF.Functions.ILike(p.Nombre, patron));
        }

        bool desc = parametros.Descendente;

        consulta = (parametros.OrdenarPor?.ToLowerInvariant()) switch
        {
            "nombre" => desc
                ? consulta.OrderByDescending(p => p.Nombre)
                : consulta.OrderBy(p => p.Nombre),
            "creacion" => desc
                ? consulta.OrderByDescending(p => p.CreatedAt)
                : consulta.OrderBy(p => p.CreatedAt),
            _ => consulta.OrderBy(p => p.Nombre)
        };

        int total = await consulta.CountAsync(cancelacion);

        var elementos = await consulta
            .Skip(parametros.Omitir)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new PaginaResultado<Proveedor>(
            elementos,
            parametros.Pagina,
            parametros.TamanoPagina,
            total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Proveedor>> ListarActivosAsync(
        CancellationToken cancelacion = default)
    {
        return await _contexto.Proveedores
            .Where(p => p.DeletedAt == null)
            .OrderBy(p => p.Nombre)
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> ContarOfertasAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        Guid[] listaIds = ids as Guid[] ?? ids.ToArray();

        if (listaIds.Length == 0)
        {
            return new Dictionary<Guid, int>();
        }

        // Una sola agregacion GROUP BY en lugar de una consulta por proveedor.
        var conteos = await _contexto.Ofertas
            .Where(o => listaIds.Contains(o.ProveedorId))
            .GroupBy(o => o.ProveedorId)
            .Select(g => new { ProveedorId = g.Key, Cantidad = g.Count() })
            .ToListAsync(cancelacion);

        return conteos.ToDictionary(c => c.ProveedorId, c => c.Cantidad);
    }

    /// <inheritdoc />
    public void Agregar(Proveedor proveedor) => _contexto.Proveedores.Add(proveedor);

    /// <inheritdoc />
    public void Eliminar(Proveedor proveedor) => _contexto.Proveedores.Remove(proveedor);
}
