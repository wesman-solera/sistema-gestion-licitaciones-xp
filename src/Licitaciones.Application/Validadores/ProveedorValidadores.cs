using FluentValidation;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.Application.Validadores;

/// <summary>Valida el formato de los datos de creacion de un proveedor.</summary>
/// <remarks>
/// Esta validacion cubre formato y longitud. La unicidad se comprueba en el servicio de
/// aplicacion (que necesita la base de datos) y, como ultima defensa, en el indice unico de
/// PostgreSQL: son las tres capas que exige la seccion 8.3.
/// </remarks>
public sealed class CrearProveedorRequestValidador : AbstractValidator<CrearProveedorRequest>
{
    /// <summary>Largo maximo admitido para el nombre del proveedor.</summary>
    public const int LargoMaximoNombre = 200;

    /// <summary>Configura las reglas de validacion.</summary>
    public CrearProveedorRequestValidador()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del proveedor es obligatorio.")
            .MaximumLength(LargoMaximoNombre)
                .WithMessage($"El nombre no puede superar {LargoMaximoNombre} caracteres.")
            .Must(nombre => NormalizadorTexto.NombreProveedorTieneCaracteresValidos(
                NormalizadorTexto.LimpiarParaMostrar(nombre ?? string.Empty)))
                .WithMessage("El nombre solo admite letras, numeros, espacios, punto, coma y parentesis.");
    }
}

/// <summary>Valida el formato de los datos de modificacion de un proveedor.</summary>
public sealed class ActualizarProveedorRequestValidador : AbstractValidator<ActualizarProveedorRequest>
{
    /// <summary>Configura las reglas de validacion.</summary>
    public ActualizarProveedorRequestValidador()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del proveedor es obligatorio.")
            .MaximumLength(CrearProveedorRequestValidador.LargoMaximoNombre)
                .WithMessage($"El nombre no puede superar {CrearProveedorRequestValidador.LargoMaximoNombre} caracteres.")
            .Must(nombre => NormalizadorTexto.NombreProveedorTieneCaracteresValidos(
                NormalizadorTexto.LimpiarParaMostrar(nombre ?? string.Empty)))
                .WithMessage("El nombre solo admite letras, numeros, espacios, punto, coma y parentesis.");
    }
}
