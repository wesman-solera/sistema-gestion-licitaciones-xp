# Módulo: Ofertas

## Propósito

Registra las propuestas económicas de los proveedores y aplica las cuatro condiciones que debe
cumplir una oferta para ser aceptada. Es el módulo con más rechazos posibles, y por eso el que
concentra más pruebas de casos límite.

## Responsabilidades

- Registrar ofertas válidas
- Rechazar la oferta duplicada, la que supera el presupuesto, la de una licitación no publicada y
  la vencida
- Mantener inmutables las ofertas de licitaciones cerradas

## Lo que no hace

- No decide cuál es la mejor oferta: eso lo calcula el módulo de Licitaciones
- No valida el estado de la licitación por su cuenta: se lo pregunta a la entidad `Licitacion`

---

## Dependencias

| Depende de | Para qué |
|---|---|
| Licitaciones | Estado, fecha de cierre y presupuesto para validar |
| Proveedores | Verificar que el proveedor existe |
| Tipo de cambio | Mostrar el equivalente en dólares |
| Persistencia | Almacenar y recuperar ofertas |

---

## Entradas y salidas

| Contrato | Campos |
|---|---|
| `CrearOfertaRequest` | `LicitacionId`, `ProveedorId`, `MontoOfertadoCrc` |
| `RegistrarOfertaEnLicitacionRequest` | `ProveedorId`, `MontoOfertadoCrc` |
| `ActualizarOfertaRequest` | `MontoOfertadoCrc` |
| `OfertaDto` | Datos de la oferta, códigos legibles y marca de mejor oferta |

Existen dos contratos de entrada porque hay dos endpoints. En `POST /licitaciones/{id}/ofertas` la
licitación viaja en la ruta; incluirla también en el cuerpo permitiría enviar dos valores distintos
y obligaría a decidir cuál gana.

---

## Las cuatro condiciones de aceptación

Se validan **todas en un solo lugar**, dentro de `Oferta.Registrar`. Repartirlas entre el servicio
y la entidad haría que un camino que no pasara por el servicio pudiera saltárselas.

### 1. Monto mayor que cero (sección 8.5)

Validado en interfaz, en la entidad y con la restricción `ck_ofertas_monto_positivo`.

### 2. Licitación publicada

Una licitación en Borrador o Cerrada no admite ofertas. Devuelve `OFE-004`.

### 3. Licitación no vencida (sección 8.2)

```csharp
if (ahoraUtc >= licitacion.FechaCierre)
{
    throw new ReglaNegocioVioladaException(..., CodigosError.LicitacionVencida);
}
```

El operador es `>=`, no `>`. El enunciado dice que no se acepta cuando la fecha y hora actual son
**«iguales o posteriores»** a la de cierre. El instante exacto del cierre ya está fuera.

Dos pruebas complementarias fijan ese límite y hacen imposible que un cambio futuro lo desplace sin
que la suite lo detecte:

| Prueba | Momento | Resultado esperado |
|---|---|---|
| `Registrar_EnElInstanteExactoDelCierre_Falla` | `ahora == fechaCierre` | Rechazada |
| `Registrar_UnSegundoAntesDelCierre_EsValida` | `ahora == fechaCierre - 1s` | Aceptada |

La comparación usa el reloj inyectado, no la hora del sistema. Sin eso, estas pruebas serían
intermitentes.

### 4. Monto dentro del presupuesto (sección 8.5)

```csharp
if (monto > presupuesto) { rechazar; }
```

El operador es `>`, no `>=`. El enunciado es explícito: *«Una oferta igual al presupuesto es
válida»*. Solo se rechaza la que lo **supera**.

---

## Unicidad por proveedor y licitación (sección 8.3)

Un proveedor presenta como máximo una oferta por licitación. Se protege en dos niveles:

| Nivel | Mecanismo | Qué cubre |
|---|---|---|
| Aplicación | `ExisteOfertaDeProveedorAsync` antes de registrar | El caso normal, con un mensaje claro |
| PostgreSQL | Índice único `ux_ofertas_licitacion_proveedor` | Dos peticiones simultáneas que superen ambas la comprobación |

El segundo nivel no es teórico: sin él, dos envíos casi simultáneos del mismo formulario podrían
crear dos ofertas del mismo proveedor. El manejador global traduce la violación de índice al mismo
código `OFE-001`, de modo que el cliente ve la misma respuesta en ambos casos.

---

## Inmutabilidad de las ofertas cerradas (sección 8.9)

Una oferta de licitación cerrada o vencida **no puede editarse ni eliminarse**: es evidencia del
proceso.

```csharp
public void AsegurarMutable(Licitacion licitacion, DateTimeOffset ahoraUtc)
{
    if (licitacion.EstaCerradaFuncionalmente(ahoraUtc))
    {
        throw new ReglaNegocioVioladaException(..., CodigosError.OfertaInmutable);
    }
}
```

Nótese que consulta `EstaCerradaFuncionalmente`, no la columna de estado. Una licitación vencida
cuya columna todavía diga `Publicada` protege sus ofertas igual.

Por la misma razón, **`ofertas` no tiene columna `deleted_at`**: un borrado lógico sería
precisamente una alteración de la evidencia.

---

## Desempate

Cuando dos ofertas empatan en monto, gana la registrada primero. `FechaRegistro` es el campo que lo
define, y por eso se guarda con precisión de instante y no solo de fecha.

Existe un tercer criterio: el identificador ordenable. Si dos ofertas coincidieran incluso en el
instante de registro, el resultado seguiría siendo determinista y repetible en lugar de depender del
orden en que la base de datos devuelva las filas.

---

## Errores

| Código | Estado | Situación |
|---|---|---|
| `OFE-001` | 409 | El proveedor ya ofertó en esa licitación |
| `OFE-002` | 422 | La oferta supera el presupuesto |
| `OFE-003` | 422 | La licitación ya alcanzó su fecha de cierre |
| `OFE-004` | 422 | La licitación no está publicada |
| `OFE-005` | 422 | La oferta pertenece a una licitación cerrada |
| `GEN-001` | 422 | Monto cero o negativo |
| `GEN-002` | 404 | Oferta, licitación o proveedor inexistente |

---

## Pruebas

| Prueba | Verifica |
|---|---|
| `OfertaPruebas.Registrar_ConMontoNoPositivo_Falla` | Condición 1 |
| `OfertaPruebas.Registrar_EnLicitacionEnBorrador_Falla` | Condición 2 |
| `OfertaPruebas.Registrar_EnElInstanteExactoDelCierre_Falla` | Condición 3, límite superior |
| `OfertaPruebas.Registrar_UnSegundoAntesDelCierre_EsValida` | Condición 3, límite inferior |
| `OfertaPruebas.Registrar_ConMontoIgualAlPresupuesto_EsValida` | Condición 4, borde inclusivo |
| `OfertaPruebas.CambiarMonto_TrasElVencimiento*` | Inmutabilidad |
| `OfertaServicioPruebas.RegistrarAsync_ConOfertaDuplicada*` | Unicidad en la aplicación |
| `RestriccionesPruebas.IndiceUnicoCompuesto_*` | Unicidad en PostgreSQL |
| `FlujoCompletoPruebas.FlujoMinimo_*` | Los rechazos desde el navegador |

---

## Archivos

| Archivo | Contenido |
|---|---|
| `Domain/Entidades/Oferta.cs` | Entidad y las cuatro condiciones |
| `Application/Servicios/OfertaServicio.cs` | Coordinación y unicidad |
| `Infrastructure/Repositorios/OfertaRepositorio.cs` | Acceso a datos y agregaciones |
| `Api/Controladores/OfertasController.cs` | Endpoints REST |
| `Web/Controladores/OfertasController.cs` | Pantallas |
