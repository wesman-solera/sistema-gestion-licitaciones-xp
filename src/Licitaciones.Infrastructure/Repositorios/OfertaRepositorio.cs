using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Repositorios;

/// <inheritdoc cref="IOfertaRepositorio"/>
public sealed class OfertaRepositorio : IOfertaRepositorio
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>Inicializa el repositorio con el contexto de datos.</summary>
    /// <param name="contexto">Contexto de Entity Framework Core.</param>
    public OfertaRepositorio(LicitacionesDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<Oferta?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default)
    {
        return await _contexto.Ofertas
            .Include(o => o.Licitacion)
            .Include(o => o.Proveedor)
            .FirstOrDefaultAsync(o => o.Id == id, cancelacion);
    }

    /// <inheritdoc />
    public async Task<bool> ExisteOfertaDeProveedorAsync(
        Guid licitacionId,
        Guid proveedorId,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default)
    {
        return await _contexto.Ofertas
            .AnyAsync(
                o => o.LicitacionId == licitacionId
                     && o.ProveedorId == proveedorId
                     && (idExcluido == null || o.Id != idExcluido),
                cancelacion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Oferta>> ListarPorLicitacionAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default)
    {
        return await _contexto.Ofertas
            .Include(o => o.Proveedor)
            .Include(o => o.Licitacion)
            .Where(o => o.LicitacionId == licitacionId)
            .OrderBy(o => o.MontoOfertadoCrc)
            .ThenBy(o => o.FechaRegistro)
            .AsSplitQuery()
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<Oferta>> ListarAsync(
        ParametrosConsulta parametros,
        Guid? licitacionId = null,
        Guid? proveedorId = null,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        IQueryable<Oferta> consulta = _contexto.Ofertas
            .Include(o => o.Licitacion)
            .Include(o => o.Proveedor)
            .AsSplitQuery();

        if (licitacionId is Guid lic)
        {
            consulta = consulta.Where(o => o.LicitacionId == lic);
        }

        if (proveedorId is Guid prov)
        {
            consulta = consulta.Where(o => o.ProveedorId == prov);
        }

        if (!string.IsNullOrWhiteSpace(parametros.Buscar))
        {
            string patron = $"%{parametros.Buscar.Trim()}%";
            consulta = consulta.Where(o =>
                EF.Functions.ILike(o.Proveedor!.Nombre, patron)
                || EF.Functions.ILike(o.Licitacion!.Codigo, patron));
        }

        bool desc = parametros.Descendente;

        consulta = (parametros.OrdenarPor?.ToLowerInvariant()) switch
        {
            "monto" => desc
                ? consulta.OrderByDescending(o => o.MontoOfertadoCrc)
                : consulta.OrderBy(o => o.MontoOfertadoCrc),
            "proveedor" => desc
                ? consulta.OrderByDescending(o => o.Proveedor!.Nombre)
                : consulta.OrderBy(o => o.Proveedor!.Nombre),
            "licitacion" => desc
                ? consulta.OrderByDescending(o => o.Licitacion!.Codigo)
                : consulta.OrderBy(o => o.Licitacion!.Codigo),
            _ => desc
                ? consulta.OrderBy(o => o.FechaRegistro)
                : consulta.OrderByDescending(o => o.FechaRegistro)
        };

        int total = await consulta.CountAsync(cancelacion);

        var elementos = await consulta
            .Skip(parametros.Omitir)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new PaginaResultado<Oferta>(
            elementos,
            parametros.Pagina,
            parametros.TamanoPagina,
            total);
    }

    /// <inheritdoc />
    public async Task<decimal?> ObtenerMayorMontoAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default)
    {
        // MaxAsync sobre decimal? devuelve null si no hay filas, sin lanzar excepcion.
        return await _contexto.Ofertas
            .Where(o => o.LicitacionId == licitacionId)
            .MaxAsync(o => (decimal?)o.MontoOfertadoCrc, cancelacion);
    }

    /// <inheritdoc />
    public async Task<bool> LicitacionTieneOfertasAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default)
    {
        return await _contexto.Ofertas.AnyAsync(o => o.LicitacionId == licitacionId, cancelacion);
    }

    /// <inheritdoc />
    public async Task<bool> ProveedorTieneOfertasAsync(
        Guid proveedorId,
        CancellationToken cancelacion = default)
    {
        return await _contexto.Ofertas.AnyAsync(o => o.ProveedorId == proveedorId, cancelacion);
    }

    /// <inheritdoc />
    public void Agregar(Oferta oferta) => _contexto.Ofertas.Add(oferta);

    /// <inheritdoc />
    public void Eliminar(Oferta oferta) => _contexto.Ofertas.Remove(oferta);
}
