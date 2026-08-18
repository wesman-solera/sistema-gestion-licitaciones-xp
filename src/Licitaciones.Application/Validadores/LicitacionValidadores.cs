using FluentValidation;
using Licitaciones.Application.Dtos;

namespace Licitaciones.Application.Validadores;

/// <summary>Valida el formato de los datos de creacion de una licitacion.</summary>
public sealed class CrearLicitacionRequestValidador : AbstractValidator<CrearLicitacionRequest>
{
    /// <summary>Largo maximo admitido para el codigo.</summary>
    public const int LargoMaximoCodigo = 50;

    /// <summary>Largo maximo admitido para el titulo.</summary>
    public const int LargoMaximoTitulo = 300;

    /// <summary>Valor maximo admitido para un monto, acorde con <c>numeric(18,2)</c>.</summary>
    /// <remarks>
    /// La columna admite 18 digitos con 2 decimales, es decir hasta 9999999999999999,99.
    /// Se acota aqui para devolver un 400 legible en lugar de dejar que PostgreSQL falle.
    /// </remarks>
    public const decimal MontoMaximo = 9_999_999_999_999_999.99m;

    /// <summary>Configura las reglas de validacion.</summary>
    public CrearLicitacionRequestValidador()
    {
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El codigo de la licitacion es obligatorio.")
            .MaximumLength(LargoMaximoCodigo)
                .WithMessage($"El codigo no puede superar {LargoMaximoCodigo} caracteres.");

        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("El titulo de la licitacion es obligatorio.")
            .MaximumLength(LargoMaximoTitulo)
                .WithMessage($"El titulo no puede superar {LargoMaximoTitulo} caracteres.");

        RuleFor(x => x.PresupuestoEstimadoCrc)
            .GreaterThan(0m).WithMessage("El presupuesto estimado debe ser mayor que cero.")
            .LessThanOrEqualTo(MontoMaximo).WithMessage("El presupuesto excede el valor maximo admitido.")
            .Must(TieneMaximoDosDecimales).WithMessage("El presupuesto admite como maximo dos decimales.");

        RuleFor(x => x.FechaCierre)
            .NotEmpty().WithMessage("La fecha y hora de cierre es obligatoria.");
    }

    /// <summary>Comprueba que un monto no tenga mas de dos decimales.</summary>
    /// <param name="valor">Monto a evaluar.</param>
    /// <returns><c>true</c> si el monto cabe en <c>numeric(18,2)</c> sin perder precision.</returns>
    /// <remarks>
    /// La escala de un <c>decimal</c> se obtiene del cuarto entero de su representacion binaria.
    /// Se valida en la aplicacion para no depender del truncamiento silencioso de la base de datos.
    /// </remarks>
    internal static bool TieneMaximoDosDecimales(decimal valor)
        => (decimal.GetBits(valor)[3] >> 16 & 0xFF) <= 2;
}

/// <summary>Valida el formato de los datos de modificacion de una licitacion.</summary>
public sealed class ActualizarLicitacionRequestValidador : AbstractValidator<ActualizarLicitacionRequest>
{
    /// <summary>Configura las reglas de validacion.</summary>
    public ActualizarLicitacionRequestValidador()
    {
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El codigo de la licitacion es obligatorio.")
            .MaximumLength(CrearLicitacionRequestValidador.LargoMaximoCodigo)
                .WithMessage($"El codigo no puede superar {CrearLicitacionRequestValidador.LargoMaximoCodigo} caracteres.");

        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("El titulo de la licitacion es obligatorio.")
            .MaximumLength(CrearLicitacionRequestValidador.LargoMaximoTitulo)
                .WithMessage($"El titulo no puede superar {CrearLicitacionRequestValidador.LargoMaximoTitulo} caracteres.");

        RuleFor(x => x.PresupuestoEstimadoCrc)
            .GreaterThan(0m).WithMessage("El presupuesto estimado debe ser mayor que cero.")
            .LessThanOrEqualTo(CrearLicitacionRequestValidador.MontoMaximo)
                .WithMessage("El presupuesto excede el valor maximo admitido.")
            .Must(CrearLicitacionRequestValidador.TieneMaximoDosDecimales)
                .WithMessage("El presupuesto admite como maximo dos decimales.");

        RuleFor(x => x.FechaCierre)
            .NotEmpty().WithMessage("La fecha y hora de cierre es obligatoria.");
    }
}

/// <summary>Valida que el estado destino solicitado sea un valor conocido.</summary>
public sealed class CambiarEstadoRequestValidador : AbstractValidator<CambiarEstadoRequest>
{
    /// <summary>Configura las reglas de validacion.</summary>
    public CambiarEstadoRequestValidador()
    {
        RuleFor(x => x.Estado)
            .IsInEnum().WithMessage("El estado solicitado no es valido.");
    }
}
