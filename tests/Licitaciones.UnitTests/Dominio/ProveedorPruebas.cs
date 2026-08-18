using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Servicios;
using Licitaciones.UnitTests.Comun;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>Reglas de normalizacion y de caracteres del proveedor (secciones 8.3 y 8.4).</summary>
public sealed class ProveedorPruebas
{
    private readonly RelojFijo _reloj = new();

    /// <summary>
    /// Los tres ejemplos son los que el enunciado declara equivalentes en la seccion 8.3.
    /// </summary>
    [Theory]
    [InlineData("Empresa Central")]
    [InlineData(" empresa central")]
    [InlineData("EMPRESA  CENTRAL")]
    [InlineData("  EmPrEsA   CeNtRaL  ")]
    public void Crear_NormalizaLosNombresEquivalentesAlMismoValor(string nombre)
    {
        Proveedor proveedor = Constructores.CrearProveedor(_reloj, nombre);

        proveedor.NombreNormalizado.Should().Be("EMPRESA CENTRAL");
    }

    [Fact]
    public void Crear_ConservaElNombreVisibleLimpioDeEspaciosSobrantes()
    {
        Proveedor proveedor = Constructores.CrearProveedor(_reloj, "  Distribuidora   del   Norte  ");

        proveedor.Nombre.Should().Be("Distribuidora del Norte");
    }

    [Theory]
    [InlineData("Servicios Tecnicos S.A.")]
    [InlineData("Grupo 2000, Sociedad Anonima")]
    [InlineData("Consorcio (Region Norte)")]
    [InlineData("Proveedor 123")]
    public void Crear_AdmiteLosCaracteresPermitidos(string nombre)
    {
        Action accion = () => Constructores.CrearProveedor(_reloj, nombre);

        accion.Should().NotThrow();
    }

    [Theory]
    [InlineData("Empresa @ Central")]
    [InlineData("Proveedor #1")]
    [InlineData("Servicios & Mas")]
    [InlineData("Grupo <script>")]
    [InlineData("Empresa/Sucursal")]
    public void Crear_RechazaLosCaracteresNoPermitidos(string nombre)
    {
        Action accion = () => Constructores.CrearProveedor(_reloj, nombre);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.CaracteresProveedorNoPermitidos);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinNombre_Falla(string nombre)
    {
        Action accion = () => Constructores.CrearProveedor(_reloj, nombre);

        accion.Should()
            .Throw<ReglaNegocioVioladaException>()
            .Which.CodigoError.Should().Be(CodigosError.ValidacionFallida);
    }

    [Fact]
    public void CambiarNombre_ActualizaTambienLaFormaNormalizada()
    {
        Proveedor proveedor = Constructores.CrearProveedor(_reloj, "Empresa Central");

        _reloj.Avanzar(TimeSpan.FromHours(1));
        proveedor.CambiarNombre("  distribuidora  sur  ", _reloj.AhoraUtc);

        proveedor.Nombre.Should().Be("distribuidora sur");
        proveedor.NombreNormalizado.Should().Be("DISTRIBUIDORA SUR");
        proveedor.UpdatedAt.Should().Be(_reloj.AhoraUtc);
    }

    [Fact]
    public void CambiarNombre_SobreUnProveedorEliminado_Falla()
    {
        Proveedor proveedor = Constructores.CrearProveedor(_reloj);
        proveedor.EliminarLogicamente(_reloj.AhoraUtc);

        Action accion = () => proveedor.CambiarNombre("Otro nombre", _reloj.AhoraUtc);

        accion.Should().Throw<ReglaNegocioVioladaException>();
    }

    [Fact]
    public void Restaurar_ReactivaUnProveedorEliminado()
    {
        Proveedor proveedor = Constructores.CrearProveedor(_reloj);
        proveedor.EliminarLogicamente(_reloj.AhoraUtc);

        proveedor.Restaurar(_reloj.AhoraUtc);

        proveedor.EstaEliminado.Should().BeFalse();
        proveedor.DeletedAt.Should().BeNull();
    }

    /// <summary>
    /// La normalizacion Unicode hace que dos representaciones del mismo caracter acentuado
    /// colapsen en la misma cadena, de modo que el indice unico las trate como iguales.
    /// </summary>
    [Fact]
    public void NormalizarNombre_UnificaLasRepresentacionesUnicodeDeUnAcento()
    {
        // Se escriben con escapes y no con el caracter directo para que la prueba siga siendo
        // significativa aunque el editor o el sistema de control de versiones normalice el
        // archivo: precompuesto usa U+00F1, descompuesto usa n seguida de la tilde combinante.
        const string precompuesto = "Nu\u00F1ez";
        const string descompuesto = "Nun\u0303ez";

        precompuesto.Should().NotBe(descompuesto, "las dos cadenas deben partir siendo distintas");

        NormalizadorTexto.NormalizarNombre(precompuesto)
            .Should().Be(NormalizadorTexto.NormalizarNombre(descompuesto));
    }
}
