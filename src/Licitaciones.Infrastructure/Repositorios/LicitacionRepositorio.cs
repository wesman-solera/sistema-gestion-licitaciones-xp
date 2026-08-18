using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enums;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Repositorios;

/// <inheritdoc cref="ILicitacionRepositorio"/>
public sealed class LicitacionRepositorio : ILicitacionRepositorio
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>Inicializa el repositorio con el contexto de datos.</summary>
    /// <param name="contexto">Contexto de Entity Framework Core.</param>
    public LicitacionRepositorio(LicitacionesDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<Licitacion?> ObtenerPorIdAsync(
        Guid id,
        bool incluirEliminadas = false,
        CancellationToken cancelacion = default)
    {
        IQueryable<Licitacion> consulta = _contexto.Licitaciones;

        if (!incluirEliminadas)
        {
            consulta = consulta.Where(l => l.DeletedAt == null);
        }

        return await consulta.FirstOrDefaultAsync(l => l.Id == id, cancelacion);
    }

    /// <inheritdoc />
    public async Task<Licitacion?> ObtenerConOfertasAsync(
        Guid id,
        CancellationToken cancelacion = default)
    {
        return await _contexto.Licitaciones
            .Include(l => l.Ofertas)
                .ThenInclude(o => o.Proveedor)
            // Consulta dividida: sin esto, el JOIN duplicaria las columnas de la licitacion por
            // cada oferta y el volumen transferido creceria de forma innecesaria.
            .AsSplitQuery()
            .Where(l => l.DeletedAt == null)
            .FirstOrDefaultAsync(l => l.Id == id, cancelacion);
    }

    /// <inheritdoc />
    public async Task<bool> ExisteCodigoAsync(
        string codigoNormalizado,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default)
    {
        // Se comparan las formas normalizadas, no los valores visibles: es lo que respalda el
        // indice unico y lo que hace equivalentes "ABC-1" y " abc-1 ".
        return await _contexto.Licitaciones
            .AnyAsync(
                l => l.CodigoNormalizado == codigoNormalizado
                     && (idExcluido == null || l.Id != idExcluido),
                cancelacion);
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<Licitacion>> ListarAsync(
        ParametrosConsulta parametros,
        EstadoLicitacion? estado = null,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        IQueryable<Licitacion> consulta = _contexto.Licitaciones
            .Include(l => l.Ofertas)
            .AsSplitQuery();

        if (!parametros.IncluirEliminados)
        {
            consulta = consulta.Where(l => l.DeletedAt == null);
        }

        if (estado is EstadoLicitacion filtroEstado)
        {
            consulta = consulta.Where(l => l.Estado == filtroEstado);
        }

        if (!string.IsNullOrWhiteSpace(parametros.Buscar))
        {
            string patron = $"%{parametros.Buscar.Trim()}%";

            // ILIKE de PostgreSQL: busqueda sin distinguir mayusculas sin traer filas a memoria.
            consulta = consulta.Where(l =>
                EF.Functions.ILike(l.Codigo, patron) || EF.Functions.ILike(l.Titulo, patron));
        }

        consulta = Ordenar(consulta, parametros);

        int total = await consulta.CountAsync(cancelacion);

        var elementos = await consulta
            .Skip(parametros.Omitir)
            .Take(parametros.TamanoPagina)
            .ToListAsync(cancelacion);

        return new PaginaResultado<Licitacion>(
            elementos,
            parametros.Pagina,
            parametros.TamanoPagina,
            total);
    }

    /// <inheritdoc />
    public void Agregar(Licitacion licitacion) => _contexto.Licitaciones.Add(licitacion);

    /// <inheritdoc />
    public void Eliminar(Licitacion licitacion) => _contexto.Licitaciones.Remove(licitacion);

    /// <summary>Aplica el ordenamiento solicitado sobre la consulta.</summary>
    /// <param name="consulta">Consulta en construccion.</param>
    /// <param name="parametros">Parametros que indican campo y direccion.</param>
    /// <returns>La consulta ordenada.</returns>
    /// <remarks>
    /// Solo se admiten campos de una lista cerrada. Aceptar un nombre de columna arbitrario
    /// del cliente abriria la puerta a construir expresiones no previstas.
    /// </remarks>
    private static IQueryable<Licitacion> Ordenar(
        IQueryable<Licitacion> consulta,
        ParametrosConsulta parametros)
    {
        bool desc = parametros.Descendente;

        return (parametros.OrdenarPor?.ToLowerInvariant()) switch
        {
            "codigo" => desc
                ? consulta.OrderByDescending(l => l.Codigo)
                : consulta.OrderBy(l => l.Codigo),
            "titulo" => desc
                ? consulta.OrderByDescending(l => l.Titulo)
                : consulta.OrderBy(l => l.Titulo),
            "estado" => desc
                ? consulta.OrderByDescending(l => l.Estado)
                : consulta.OrderBy(l => l.Estado),
            "presupuesto" => desc
                ? consulta.OrderByDescending(l => l.PresupuestoEstimadoCrc)
                : consulta.OrderBy(l => l.PresupuestoEstimadoCrc),
            "fechacierre" => desc
                ? consulta.OrderByDescending(l => l.FechaCierre)
                : consulta.OrderBy(l => l.FechaCierre),
            _ => desc
                ? consulta.OrderBy(l => l.CreatedAt)
                : consulta.OrderByDescending(l => l.CreatedAt)
        };
    }
}
