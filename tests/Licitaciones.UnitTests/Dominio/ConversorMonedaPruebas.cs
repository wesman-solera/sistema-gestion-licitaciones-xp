using FluentAssertions;
using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Servicios;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>Conversion de colones a dolares (seccion 8.8).</summary>
public sealed class ConversorMonedaPruebas
{
    private readonly RelojFijo _reloj = new();

    [Theory]
    [InlineData(505_000, 505, 1000.00)]
    [InlineData(1_000_000, 500, 2000.00)]
    [InlineData(0.01, 505, 0.00)]
    [InlineData(1_010, 505, 2.00)]
    public void ConvertirAUsd_AplicaLaFormulaDelEnunciado(
        decimal montoCrc,
        decimal crcPorUsd,
        decimal esperado)
    {
        // Monto USD = Monto CRC / Tipo de cambio CRC por USD
        ConversorMoneda.ConvertirAUsd(montoCrc, crcPorUsd).Should().Be(esperado);
    }

    [Fact]
    public void ConvertirAUsd_RedondeaADosDecimales()
    {
        // 1000 / 3 = 333,333... que debe presentarse como 333,33
        ConversorMoneda.ConvertirAUsd(1_000m, 3m).Should().Be(333.33m);
    }

    [Fact]
    public void ConvertirAUsd_RedondeaAlejandoseDelCero()
    {
        // 5,005 se redondea a 5,01 y no a 5,00: el redondeo bancario ocultaria medio centimo
        // en la mitad de los casos.
        ConversorMoneda.ConvertirAUsd(5.005m, 1m).Should().Be(5.01m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConvertirAUsd_ConTipoDeCambioNoPositivo_Falla(decimal crcPorUsd)
    {
        Action accion = () => ConversorMoneda.ConvertirAUsd(1_000m, crcPorUsd);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.TipoCambioInvalido);
    }

    [Fact]
    public void ConvertirAUsd_SinTipoDeCambioActivo_Falla()
    {
        Action accion = () => ConversorMoneda.ConvertirAUsd(1_000m, (TipoCambio?)null);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.SinTipoCambioActivo);
    }

    [Fact]
    public void ConvertirAUsd_ConEntidadDeTipoDeCambio_UsaSuValor()
    {
        TipoCambio tipoCambio = TipoCambio.Crear(505m, _reloj.AhoraUtc, true, _reloj.AhoraUtc);

        ConversorMoneda.ConvertirAUsd(1_010m, tipoCambio).Should().Be(2.00m);
    }

    /// <summary>
    /// La conversion es una representacion calculada: no debe alterar el valor en colones ni
    /// quedar almacenada en ninguna parte (seccion 8.8).
    /// </summary>
    [Fact]
    public void ConvertirAUsd_NoModificaElMontoOriginal()
    {
        decimal montoCrc = 1_000_000m;

        ConversorMoneda.ConvertirAUsd(montoCrc, 505m);

        montoCrc.Should().Be(1_000_000m);
    }

    [Fact]
    public void TipoCambio_Crear_ConValorNoPositivo_Falla()
    {
        Action accion = () => TipoCambio.Crear(0m, _reloj.AhoraUtc, true, _reloj.AhoraUtc);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.TipoCambioInvalido);
    }

    [Fact]
    public void TipoCambio_ActivarYDesactivar_SonIdempotentes()
    {
        TipoCambio tipoCambio = TipoCambio.Crear(505m, _reloj.AhoraUtc, false, _reloj.AhoraUtc);
        DateTimeOffset creacion = tipoCambio.UpdatedAt;

        _reloj.Avanzar(TimeSpan.FromHours(1));

        tipoCambio.Desactivar(_reloj.AhoraUtc);
        tipoCambio.UpdatedAt.Should().Be(creacion, "ya estaba inactivo");

        tipoCambio.Activar(_reloj.AhoraUtc);
        tipoCambio.Activo.Should().BeTrue();
        tipoCambio.UpdatedAt.Should().Be(_reloj.AhoraUtc);
    }
}
