using FluentValidation;
using Licitaciones.Application.Dtos;

namespace Licitaciones.Application.Validadores;

/// <summary>Valida el formato de los datos de registro de una oferta.</summary>
/// <remarks>
/// Solo cubre formato. Las reglas que dependen del estado de la licitacion (publicada, no
/// vencida, monto dentro del presupuesto, sin duplicado) viven en el dominio y en el servicio,
/// porque necesitan datos que un validador de entrada no tiene.
/// </remarks>
public sealed class CrearOfertaRequestValidador : AbstractValidator<CrearOfertaRequest>
{
    /// <summary>Configura las reglas de validacion.</summary>
    public CrearOfertaRequestValidador()
    {
        RuleFor(x => x.LicitacionId)
            .NotEmpty().WithMessage("Debe indicarse la licitacion.");

        RuleFor(x => x.ProveedorId)
            .NotEmpty().WithMessage("Debe indicarse el proveedor.");

        RuleFor(x => x.MontoOfertadoCrc)
            .GreaterThan(0m).WithMessage("El monto ofertado debe ser mayor que cero.")
            .LessThanOrEqualTo(CrearLicitacionRequestValidador.MontoMaximo)
                .WithMessage("El monto excede el valor maximo admitido.")
            .Must(CrearLicitacionRequestValidador.TieneMaximoDosDecimales)
                .WithMessage("El monto admite como maximo dos decimales.");
    }
}

/// <summary>Valida el formato de los datos de registro de una oferta con licitacion en la ruta.</summary>
public sealed class RegistrarOfertaEnLicitacionRequestValidador
    : AbstractValidator<RegistrarOfertaEnLicitacionRequest>
{
    /// <summary>Configura las reglas de validacion.</summary>
    public RegistrarOfertaEnLicitacionRequestValidador()
    {
        RuleFor(x => x.ProveedorId)
            .NotEmpty().WithMessage("Debe indicarse el proveedor.");

        RuleFor(x => x.MontoOfertadoCrc)
            .GreaterThan(0m).WithMessage("El monto ofertado debe ser mayor que cero.")
            .LessThanOrEqualTo(CrearLicitacionRequestValidador.MontoMaximo)
                .WithMessage("El monto excede el valor maximo admitido.")
            .Must(CrearLicitacionRequestValidador.TieneMaximoDosDecimales)
                .WithMessage("El monto admite como maximo dos decimales.");
    }
}

/// <summary>Valida el formato de los datos de modificacion de una oferta.</summary>
public sealed class ActualizarOfertaRequestValidador : AbstractValidator<ActualizarOfertaRequest>
{
    /// <summary>Configura las reglas de validacion.</summary>
    public ActualizarOfertaRequestValidador()
    {
        RuleFor(x => x.MontoOfertadoCrc)
            .GreaterThan(0m).WithMessage("El monto ofertado debe ser mayor que cero.")
            .LessThanOrEqualTo(CrearLicitacionRequestValidador.MontoMaximo)
                .WithMessage("El monto excede el valor maximo admitido.")
            .Must(CrearLicitacionRequestValidador.TieneMaximoDosDecimales)
                .WithMessage("El monto admite como maximo dos decimales.");
    }
}
