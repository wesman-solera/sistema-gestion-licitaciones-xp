using Licitaciones.Domain.Enums;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Recorre exhaustivamente el ciclo de estados de la seccion 8.1.
/// </summary>
/// <remarks>
/// La tabla de transiciones se prueba con todas las combinaciones posibles de origen y destino
/// en lugar de con casos sueltos. Son nueve combinaciones: enumerarlas todas garantiza que
/// ninguna quede sin cubrir cuando alguien modifique la politica.
/// </remarks>
public sealed class PoliticaTransicionEstadoPruebas
{
    [Theory]
    [InlineData(EstadoLicitacion.Borrador, EstadoLicitacion.Publicada, true)]
    [InlineData(EstadoLicitacion.Borrador, EstadoLicitacion.Cerrada, true)]
    [InlineData(EstadoLicitacion.Publicada, EstadoLicitacion.Cerrada, true)]
    [InlineData(EstadoLicitacion.Publicada, EstadoLicitacion.Borrador, false)]
    [InlineData(EstadoLicitacion.Cerrada, EstadoLicitacion.Publicada, false)]
    [InlineData(EstadoLicitacion.Cerrada, EstadoLicitacion.Borrador, false)]
    [InlineData(EstadoLicitacion.Borrador, EstadoLicitacion.Borrador, false)]
    [InlineData(EstadoLicitacion.Publicada, EstadoLicitacion.Publicada, false)]
    [InlineData(EstadoLicitacion.Cerrada, EstadoLicitacion.Cerrada, false)]
    public void EsPermitida_CubreTodasLasCombinaciones(
        EstadoLicitacion origen,
        EstadoLicitacion destino,
        bool esperado)
    {
        PoliticaTransicionEstado.EsPermitida(origen, destino).Should().Be(esperado);
    }

    [Fact]
    public void AsegurarTransicionPermitida_ConTransicionValida_NoLanza()
    {
        Action accion = () => PoliticaTransicionEstado.AsegurarTransicionPermitida(
            EstadoLicitacion.Borrador,
            EstadoLicitacion.Publicada);

        accion.Should().NotThrow();
    }

    [Fact]
    public void AsegurarTransicionPermitida_DePublicadaABorrador_Lanza()
    {
        Action accion = () => PoliticaTransicionEstado.AsegurarTransicionPermitida(
            EstadoLicitacion.Publicada,
            EstadoLicitacion.Borrador);

        accion.Should()
            .Throw<TransicionEstadoInvalidaException>()
            .Which.EstadoActual.Should().Be(EstadoLicitacion.Publicada);
    }

    [Fact]
    public void AsegurarTransicionPermitida_ReaperturaDeCerrada_ExplicaQueRequiereAutorizacion()
    {
        Action accion = () => PoliticaTransicionEstado.AsegurarTransicionPermitida(
            EstadoLicitacion.Cerrada,
            EstadoLicitacion.Publicada);

        accion.Should()
            .Throw<TransicionEstadoInvalidaException>()
            .WithMessage("*autorizacion expresa*");
    }

    [Fact]
    public void DestinosDisponibles_DesdeBorrador_OfreceAmbasSalidas()
    {
        var destinos = PoliticaTransicionEstado.DestinosDisponibles(EstadoLicitacion.Borrador);

        destinos.Should().BeEquivalentTo([EstadoLicitacion.Publicada, EstadoLicitacion.Cerrada]);
    }

    [Fact]
    public void DestinosDisponibles_DesdeCerrada_NoOfreceNinguna()
    {
        PoliticaTransicionEstado
            .DestinosDisponibles(EstadoLicitacion.Cerrada)
            .Should().BeEmpty();
    }
}
