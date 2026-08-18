using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Enums;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.Domain.Entidades;

/// <summary>
/// Proceso de compra publicado para recibir ofertas economicas de los proveedores.
/// </summary>
/// <remarks>
/// Concentra las reglas de las secciones 8.1, 8.2, 8.3 y 8.5 del enunciado. La entidad es la
/// unica responsable de aceptar o rechazar un cambio de estado: la capa de aplicacion coordina
/// la persistencia pero no reimplementa el ciclo de vida.
/// <para>
/// Distincion importante entre <see cref="Estado"/> y <see cref="EstaCerradaFuncionalmente"/>:
/// el enunciado indica que una licitacion cuya fecha de cierre ya paso se considera cerrada
/// aunque la columna todavia diga <c>Publicada</c> porque ningun proceso la actualizo. Toda
/// decision sobre aceptacion de ofertas debe consultar el metodo, nunca la columna sola.
/// </para>
/// </remarks>
public sealed class Licitacion
{
    private readonly List<Oferta> _ofertas = [];

    /// <summary>Identificador generado por el sistema. No es editable por el usuario.</summary>
    public Guid Id { get; private set; }

    /// <summary>Codigo visible de la licitacion, ya recortado de espacios laterales.</summary>
    public string Codigo { get; private set; } = string.Empty;

    /// <summary>Forma normalizada del codigo usada por el indice unico.</summary>
    public string CodigoNormalizado { get; private set; } = string.Empty;

    /// <summary>Titulo descriptivo de la licitacion.</summary>
    public string Titulo { get; private set; } = string.Empty;

    /// <summary>Estado persistido del ciclo de vida.</summary>
    public EstadoLicitacion Estado { get; private set; }

    /// <summary>Fecha y hora limite para recibir ofertas, almacenada en UTC.</summary>
    public DateTimeOffset FechaCierre { get; private set; }

    /// <summary>Presupuesto estimado en colones costarricenses.</summary>
    /// <remarks>Se persiste como <c>numeric(18,2)</c>; nunca como punto flotante (seccion 7).</remarks>
    public decimal PresupuestoEstimadoCrc { get; private set; }

    /// <summary>Instante de creacion del registro, en UTC.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Instante de la ultima modificacion del registro, en UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Instante de borrado logico, o <c>null</c> si la licitacion esta activa.</summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Token de concurrencia optimista mapeado a la columna de sistema <c>xmin</c>.</summary>
    public uint Version { get; private set; }

    /// <summary>Ofertas registradas para esta licitacion.</summary>
    public IReadOnlyCollection<Oferta> Ofertas => _ofertas.AsReadOnly();

    /// <summary>Indica si la licitacion fue eliminada logicamente.</summary>
    public bool EstaEliminada => DeletedAt is not null;

    /// <summary>Constructor requerido por Entity Framework Core.</summary>
    private Licitacion()
    {
    }

    /// <summary>
    /// Crea una licitacion en estado <see cref="EstadoLicitacion.Borrador"/>.
    /// </summary>
    /// <param name="codigo">Codigo propuesto por el usuario.</param>
    /// <param name="titulo">Titulo descriptivo.</param>
    /// <param name="presupuestoEstimadoCrc">Presupuesto en colones, estrictamente mayor que cero.</param>
    /// <param name="fechaCierre">Fecha y hora de cierre, que debe ser futura.</param>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <returns>Una licitacion valida en estado Borrador.</returns>
    /// <exception cref="ReglaNegocioVioladaException">Si algun dato incumple las reglas de negocio.</exception>
    public static Licitacion Crear(
        string codigo,
        string titulo,
        decimal presupuestoEstimadoCrc,
        DateTimeOffset fechaCierre,
        DateTimeOffset ahoraUtc)
    {
        string codigoLimpio = ValidarCodigo(codigo);
        string tituloLimpio = ValidarTitulo(titulo);
        ValidarPresupuesto(presupuestoEstimadoCrc);
        ValidarFechaCierreFutura(fechaCierre, ahoraUtc);

        return new Licitacion
        {
            Id = Guid.CreateVersion7(),
            Codigo = codigoLimpio,
            CodigoNormalizado = NormalizadorTexto.NormalizarCodigo(codigoLimpio),
            Titulo = tituloLimpio,
            Estado = EstadoLicitacion.Borrador,
            PresupuestoEstimadoCrc = presupuestoEstimadoCrc,
            FechaCierre = fechaCierre.ToUniversalTime(),
            CreatedAt = ahoraUtc,
            UpdatedAt = ahoraUtc
        };
    }

    /// <summary>
    /// Indica si la licitacion esta cerrada, ya sea por estado explicito o por vencimiento.
    /// </summary>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <returns><c>true</c> cuando no puede seguir recibiendo actividad.</returns>
    /// <remarks>
    /// Implementa la aclaracion de la seccion 8.1: el vencimiento de la fecha cierra la
    /// licitacion funcionalmente aunque la columna de estado todavia no se haya actualizado.
    /// </remarks>
    public bool EstaCerradaFuncionalmente(DateTimeOffset ahoraUtc)
        => Estado == EstadoLicitacion.Cerrada || ahoraUtc >= FechaCierre;

    /// <summary>
    /// Indica si la licitacion puede recibir una oferta nueva en este momento.
    /// </summary>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <returns><c>true</c> si esta publicada, vigente y no eliminada.</returns>
    public bool PuedeRecibirOfertas(DateTimeOffset ahoraUtc)
        => !EstaEliminada
           && Estado == EstadoLicitacion.Publicada
           && !EstaCerradaFuncionalmente(ahoraUtc);

    /// <summary>
    /// Publica la licitacion. Solo es valido desde <see cref="EstadoLicitacion.Borrador"/>.
    /// </summary>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <exception cref="TransicionEstadoInvalidaException">Si el estado de origen no es Borrador.</exception>
    /// <exception cref="ReglaNegocioVioladaException">Si los datos estan incompletos o la fecha ya vencio.</exception>
    public void Publicar(DateTimeOffset ahoraUtc)
    {
        AsegurarActiva();
        PoliticaTransicionEstado.AsegurarTransicionPermitida(Estado, EstadoLicitacion.Publicada);

        // Condicion de la seccion 8.1: datos completos, presupuesto valido y fecha de cierre futura.
        ValidarTitulo(Titulo);
        ValidarPresupuesto(PresupuestoEstimadoCrc);
        ValidarFechaCierreFutura(FechaCierre, ahoraUtc);

        Estado = EstadoLicitacion.Publicada;
        UpdatedAt = ahoraUtc;
    }

    /// <summary>
    /// Cierra la licitacion. Es valido desde Borrador (cancelacion) y desde Publicada.
    /// </summary>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <exception cref="TransicionEstadoInvalidaException">Si la licitacion ya estaba cerrada.</exception>
    public void Cerrar(DateTimeOffset ahoraUtc)
    {
        AsegurarActiva();
        PoliticaTransicionEstado.AsegurarTransicionPermitida(Estado, EstadoLicitacion.Cerrada);

        Estado = EstadoLicitacion.Cerrada;
        UpdatedAt = ahoraUtc;
    }

    /// <summary>
    /// Aplica una transicion de estado generica solicitada desde la API o la interfaz web.
    /// </summary>
    /// <param name="destino">Estado solicitado.</param>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <exception cref="TransicionEstadoInvalidaException">Si la transicion no esta permitida.</exception>
    public void CambiarEstado(EstadoLicitacion destino, DateTimeOffset ahoraUtc)
    {
        switch (destino)
        {
            case EstadoLicitacion.Publicada:
                Publicar(ahoraUtc);
                break;

            case EstadoLicitacion.Cerrada:
                Cerrar(ahoraUtc);
                break;

            default:
                // Cualquier destino hacia Borrador esta prohibido por la seccion 8.1.
                PoliticaTransicionEstado.AsegurarTransicionPermitida(Estado, destino);
                break;
        }
    }

    /// <summary>
    /// Modifica los datos editables de la licitacion.
    /// </summary>
    /// <param name="titulo">Nuevo titulo.</param>
    /// <param name="presupuestoEstimadoCrc">Nuevo presupuesto en colones.</param>
    /// <param name="fechaCierre">Nueva fecha de cierre.</param>
    /// <param name="mayorOfertaRegistradaCrc">
    /// Monto de la oferta mas alta ya registrada, o <c>null</c> si no hay ofertas.
    /// La capa de aplicacion lo consulta al repositorio y lo inyecta aqui para que el dominio
    /// no dependa de la persistencia.
    /// </param>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <exception cref="ReglaNegocioVioladaException">Si algun dato incumple las reglas de negocio.</exception>
    public void ActualizarDatos(
        string titulo,
        decimal presupuestoEstimadoCrc,
        DateTimeOffset fechaCierre,
        decimal? mayorOfertaRegistradaCrc,
        DateTimeOffset ahoraUtc)
    {
        AsegurarActiva();

        if (Estado == EstadoLicitacion.Cerrada)
        {
            throw new ReglaNegocioVioladaException(
                "Una licitacion cerrada no puede editarse.",
                CodigosError.TransicionEstadoInvalida);
        }

        string tituloLimpio = ValidarTitulo(titulo);
        ValidarPresupuesto(presupuestoEstimadoCrc);

        // Seccion 8.5: el presupuesto no puede quedar por debajo de una oferta ya registrada.
        if (mayorOfertaRegistradaCrc is decimal mayorOferta && presupuestoEstimadoCrc < mayorOferta)
        {
            throw new ReglaNegocioVioladaException(
                $"El presupuesto no puede ser menor que la oferta mas alta ya registrada " +
                $"({NormalizadorTexto.FormatearColones(mayorOferta)}).",
                CodigosError.PresupuestoMenorQueOfertaExistente);
        }

        // La fecha solo se exige futura mientras la licitacion pueda seguir recibiendo ofertas.
        if (Estado == EstadoLicitacion.Borrador || fechaCierre != FechaCierre)
        {
            ValidarFechaCierreFutura(fechaCierre, ahoraUtc);
        }

        Titulo = tituloLimpio;
        PresupuestoEstimadoCrc = presupuestoEstimadoCrc;
        FechaCierre = fechaCierre.ToUniversalTime();
        UpdatedAt = ahoraUtc;
    }

    /// <summary>Cambia el codigo de la licitacion. Solo se admite en estado Borrador.</summary>
    /// <param name="nuevoCodigo">Codigo propuesto.</param>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <exception cref="ReglaNegocioVioladaException">Si la licitacion ya no esta en Borrador.</exception>
    public void CambiarCodigo(string nuevoCodigo, DateTimeOffset ahoraUtc)
    {
        AsegurarActiva();

        if (Estado != EstadoLicitacion.Borrador)
        {
            throw new ReglaNegocioVioladaException(
                "El codigo solo puede modificarse mientras la licitacion esta en Borrador.",
                CodigosError.TransicionEstadoInvalida);
        }

        string limpio = ValidarCodigo(nuevoCodigo);
        Codigo = limpio;
        CodigoNormalizado = NormalizadorTexto.NormalizarCodigo(limpio);
        UpdatedAt = ahoraUtc;
    }

    /// <summary>Marca la licitacion como eliminada logicamente.</summary>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    public void EliminarLogicamente(DateTimeOffset ahoraUtc)
    {
        if (EstaEliminada)
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

    private void AsegurarActiva()
    {
        if (EstaEliminada)
        {
            throw new ReglaNegocioVioladaException(
                "No se puede operar sobre una licitacion eliminada.",
                CodigosError.RecursoNoEncontrado);
        }
    }

    private static string ValidarCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ReglaNegocioVioladaException(
                "El codigo de la licitacion es obligatorio.",
                CodigosError.ValidacionFallida);
        }

        return codigo.Trim();
    }

    private static string ValidarTitulo(string titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
        {
            throw new ReglaNegocioVioladaException(
                "El titulo de la licitacion es obligatorio.",
                CodigosError.ValidacionFallida);
        }

        return NormalizadorTexto.LimpiarParaMostrar(titulo);
    }

    private static void ValidarPresupuesto(decimal presupuesto)
    {
        if (presupuesto <= 0m)
        {
            throw new ReglaNegocioVioladaException(
                "El presupuesto estimado debe ser mayor que cero.",
                CodigosError.MontoNoPositivo);
        }
    }

    private static void ValidarFechaCierreFutura(DateTimeOffset fechaCierre, DateTimeOffset ahoraUtc)
    {
        if (fechaCierre <= ahoraUtc)
        {
            throw new ReglaNegocioVioladaException(
                "La fecha y hora de cierre debe ser posterior al momento actual.",
                CodigosError.FechaCierreNoFutura);
        }
    }
}
