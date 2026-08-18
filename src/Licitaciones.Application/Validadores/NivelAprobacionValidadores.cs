using FluentValidation;
using Licitaciones.Application.Dtos;

namespace Licitaciones.Application.Validadores;

/// <summary>Valida el formato de los datos de creacion de un rango de aprobacion.</summary>
public sealed class CrearNivelAprobacionRequestValidador
    : AbstractValidator<CrearNivelAprobacionRequest>
{
    /// <summary>Largo maximo admitido para el nombre del aprobador.</summary>
    public const int LargoMaximoAprobador = 150;

    /// <summary>Configura las reglas de validacion.</summary>
    public CrearNivelAprobacionRequestValidador()
    {
        RuleFor(x => x.MontoMinimoCrc)
            .GreaterThan(0m).WithMessage("El monto minimo debe ser mayor que cero.")
            .Must(CrearLicitacionRequestValidador.TieneMaximoDosDecimales)
                .WithMessage("El monto minimo admite como maximo dos decimales.");

        RuleFor(x => x.MontoMaximoCrc)
            .GreaterThan(0m).WithMessage("El monto maximo debe ser mayor que cero.")
            .Must(v => v is null || CrearLicitacionRequestValidador.TieneMaximoDosDecimales(v.Value))
                .WithMessage("El monto maximo admite como maximo dos decimales.")
            .When(x => x.MontoMaximoCrc.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MontoMaximoCrc.HasValue || x.MontoMaximoCrc.Value >= x.MontoMinimoCrc)
            .WithName("MontoMaximoCrc")
            .WithMessage("El monto maximo no puede ser menor que el monto minimo.");

        RuleFor(x => x.Aprobador)
            .NotEmpty().WithMessage("El aprobador es obligatorio.")
            .MaximumLength(LargoMaximoAprobador)
                .WithMessage($"El aprobador no puede superar {LargoMaximoAprobador} caracteres.");
    }
}

/// <summary>Valida el formato de los datos de modificacion de un rango de aprobacion.</summary>
public sealed class ActualizarNivelAprobacionRequestValidador
    : AbstractValidator<ActualizarNivelAprobacionRequest>
{
    /// <summary>Configura las reglas de validacion.</summary>
    public ActualizarNivelAprobacionRequestValidador()
    {
        RuleFor(x => x.MontoMinimoCrc)
            .GreaterThan(0m).WithMessage("El monto minimo debe ser mayor que cero.")
            .Must(CrearLicitacionRequestValidador.TieneMaximoDosDecimales)
                .WithMessage("El monto minimo admite como maximo dos decimales.");

        RuleFor(x => x.MontoMaximoCrc)
            .GreaterThan(0m).WithMessage("El monto maximo debe ser mayor que cero.")
            .Must(v => v is null || CrearLicitacionRequestValidador.TieneMaximoDosDecimales(v.Value))
                .WithMessage("El monto maximo admite como maximo dos decimales.")
            .When(x => x.MontoMaximoCrc.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MontoMaximoCrc.HasValue || x.MontoMaximoCrc.Value >= x.MontoMinimoCrc)
            .WithName("MontoMaximoCrc")
            .WithMessage("El monto maximo no puede ser menor que el monto minimo.");

        RuleFor(x => x.Aprobador)
            .NotEmpty().WithMessage("El aprobador es obligatorio.")
            .MaximumLength(CrearNivelAprobacionRequestValidador.LargoMaximoAprobador)
                .WithMessage($"El aprobador no puede superar {CrearNivelAprobacionRequestValidador.LargoMaximoAprobador} caracteres.");
    }
}
