using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.Domain.Entidades;

/// <summary>
/// Empresa o persona habilitada para presentar ofertas economicas.
/// </summary>
/// <remarks>
/// La unicidad del nombre se resuelve con la columna <see cref="NombreNormalizado"/>, que
/// se mantiene siempre sincronizada con <see cref="Nombre"/> y respalda un indice unico
/// en PostgreSQL. El estado interno solo se modifica mediante los metodos publicos de la
/// entidad; los <c>set</c> son privados para impedir que una capa superior deje la entidad
/// en un estado invalido.
/// </remarks>
public sealed class Proveedor
{
    private readonly List<Oferta> _ofertas = [];

    /// <summary>Identificador generado por el sistema. No es editable por el usuario.</summary>
    public Guid Id { get; private set; }

    /// <summary>Nombre tal como se muestra al usuario, ya limpiado de espacios sobrantes.</summary>
    public string Nombre { get; private set; } = string.Empty;

    /// <summary>Forma normalizada del nombre usada por el indice unico.</summary>
    /// <remarks>Se calcula con <see cref="NormalizadorTexto.NormalizarNombre"/>.</remarks>
    public string NombreNormalizado { get; private set; } = string.Empty;

    /// <summary>Instante de creacion del registro, en UTC.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Instante de la ultima modificacion del registro, en UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Instante de borrado logico, o <c>null</c> si el proveedor esta activo.</summary>
    /// <remarks>El borrado logico permite conservar la trazabilidad de las ofertas historicas.</remarks>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Token de concurrencia optimista mapeado a la columna de sistema <c>xmin</c>.</summary>
    public uint Version { get; private set; }

    /// <summary>Ofertas presentadas por el proveedor.</summary>
    public IReadOnlyCollection<Oferta> Ofertas => _ofertas.AsReadOnly();

    /// <summary>Indica si el proveedor fue eliminado logicamente.</summary>
    public bool EstaEliminado => DeletedAt is not null;

    /// <summary>Constructor requerido por Entity Framework Core.</summary>
    private Proveedor()
    {
    }

    /// <summary>
    /// Crea un proveedor validando el nombre contra las reglas de las secciones 8.3 y 8.4.
    /// </summary>
    /// <param name="nombre">Nombre propuesto por el usuario.</param>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <returns>Un proveedor valido y activo.</returns>
    /// <exception cref="ReglaNegocioVioladaException">Si el nombre esta vacio o usa caracteres no permitidos.</exception>
    public static Proveedor Crear(string nombre, DateTimeOffset ahoraUtc)
    {
        string limpio = ValidarNombre(nombre);

        return new Proveedor
        {
            Id = Guid.CreateVersion7(),
            Nombre = limpio,
            NombreNormalizado = NormalizadorTexto.NormalizarNombre(limpio),
            CreatedAt = ahoraUtc,
            UpdatedAt = ahoraUtc
        };
    }

    /// <summary>Cambia el nombre del proveedor aplicando las mismas validaciones que la creacion.</summary>
    /// <param name="nuevoNombre">Nombre propuesto.</param>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <exception cref="ReglaNegocioVioladaException">Si el nombre no es valido o el proveedor esta eliminado.</exception>
    public void CambiarNombre(string nuevoNombre, DateTimeOffset ahoraUtc)
    {
        AsegurarActivo();

        string limpio = ValidarNombre(nuevoNombre);
        Nombre = limpio;
        NombreNormalizado = NormalizadorTexto.NormalizarNombre(limpio);
        UpdatedAt = ahoraUtc;
    }

    /// <summary>
    /// Marca el proveedor como eliminado logicamente conservando sus ofertas historicas.
    /// </summary>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <remarks>
    /// El enunciado (seccion 8.9) prohibe la eliminacion fisica de un proveedor con ofertas
    /// relacionadas. La capa de aplicacion decide entre borrado fisico y logico segun exista
    /// o no relacion; esta entidad solo expone la variante segura.
    /// </remarks>
    public void EliminarLogicamente(DateTimeOffset ahoraUtc)
    {
        if (EstaEliminado)
        {
            return;
        }

        DeletedAt = ahoraUtc;
        UpdatedAt = ahoraUtc;
    }

    /// <summary>Revierte un borrado logico previo.</summary>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    public void Restaurar(DateTimeOffset ahoraUtc)
    {
        DeletedAt = null;
        UpdatedAt = ahoraUtc;
    }

    private void AsegurarActivo()
    {
        if (EstaEliminado)
        {
            throw new ReglaNegocioVioladaException(
                "No se puede modificar un proveedor eliminado.",
                CodigosError.RecursoNoEncontrado);
        }
    }

    private static string ValidarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ReglaNegocioVioladaException(
                "El nombre del proveedor es obligatorio.",
                CodigosError.ValidacionFallida);
        }

        string limpio = NormalizadorTexto.LimpiarParaMostrar(nombre);

        if (!NormalizadorTexto.NombreProveedorTieneCaracteresValidos(limpio))
        {
            throw new ReglaNegocioVioladaException(
                "El nombre del proveedor solo admite letras, numeros, espacios, punto, coma y parentesis.",
                CodigosError.CaracteresProveedorNoPermitidos);
        }

        return limpio;
    }
}
