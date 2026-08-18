namespace Licitaciones.Application.Dtos;

/// <summary>Datos necesarios para crear un proveedor.</summary>
/// <param name="Nombre">Nombre de la empresa o persona oferente.</param>
public sealed record CrearProveedorRequest(string Nombre);

/// <summary>Datos necesarios para modificar un proveedor.</summary>
/// <param name="Nombre">Nuevo nombre del proveedor.</param>
public sealed record ActualizarProveedorRequest(string Nombre);

/// <summary>Proyeccion de lectura de un proveedor.</summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="Nombre">Nombre visible.</param>
/// <param name="NombreNormalizado">Forma normalizada usada para la unicidad.</param>
/// <param name="CantidadOfertas">Cantidad de ofertas presentadas por el proveedor.</param>
/// <param name="Eliminado">Indica si el proveedor fue eliminado logicamente.</param>
/// <param name="CreatedAt">Instante de creacion, en UTC.</param>
/// <param name="UpdatedAt">Instante de la ultima modificacion, en UTC.</param>
/// <remarks>
/// Es un DTO y no la entidad: el enunciado (seccion 10) prohibe exponer directamente las
/// entidades de Entity Framework Core a traves de la API.
/// </remarks>
public sealed record ProveedorDto(
    Guid Id,
    string Nombre,
    string NombreNormalizado,
    int CantidadOfertas,
    bool Eliminado,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
