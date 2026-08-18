using FluentValidation;
using Licitaciones.Application.Dtos;

namespace Licitaciones.Application.Validadores;

/// <summary>Valida el formato de los datos de creacion de un tipo de cambio.</summary>
public sealed class CrearTipoCambioRequestValidador : AbstractValidator<CrearTipoCambioRequest>
{
    /// <summary>Configura las reglas de validacion.</summary>
    public CrearTipoCambioRequestValidador()
    {
        RuleFor(x => x.CrcPorUsd)
            .GreaterThan(0m).WithMessage("El tipo de cambio debe ser mayor que cero.")
            .LessThanOrEqualTo(CrearLicitacionRequestValidador.MontoMaximo)
                .WithMessage("El tipo de cambio excede el valor maximo admitido.")
            .Must(CrearLicitacionRequestValidador.TieneMaximoDosDecimales)
                .WithMessage("El tipo de cambio admite como maximo dos decimales.");

        RuleFor(x => x.FechaVigencia)
            .NotEmpty().WithMessage("La fecha de vigencia es obligatoria.");
    }
}

/// <summary>Valida el formato de los datos de modificacion de un tipo de cambio.</summary>
public sealed class ActualizarTipoCambioRequestValidador
    : AbstractValidator<ActualizarTipoCambioRequest>
{
    /// <summary>Configura las reglas de validacion.</summary>
    public ActualizarTipoCambioRequestValidador()
    {
        RuleFor(x => x.CrcPorUsd)
            .GreaterThan(0m).WithMessage("El tipo de cambio debe ser mayor que cero.")
            .LessThanOrEqualTo(CrearLicitacionRequestValidador.MontoMaximo)
                .WithMessage("El tipo de cambio excede el valor maximo admitido.")
            .Must(CrearLicitacionRequestValidador.TieneMaximoDosDecimales)
                .WithMessage("El tipo de cambio admite como maximo dos decimales.");

        RuleFor(x => x.FechaVigencia)
            .NotEmpty().WithMessage("La fecha de vigencia es obligatoria.");
    }
}
