# Módulo: Niveles de aprobación

## Propósito

Determina quién debe autorizar una adjudicación según el monto de la mejor oferta. El enunciado es
explícito en la forma: *«El aprobador debe obtenerse desde una tabla parametrizable y no mediante
una cadena fija de condiciones if/else»*.

## Responsabilidades

- Mantener la tabla de rangos de monto y sus responsables
- Determinar el aprobador aplicable a un monto
- Impedir que los rangos se traslapen o que exista más de un rango abierto

## Lo que no hace

- No conoce licitaciones ni ofertas: solo trabaja con montos

---

## Dependencias

| Depende de | Para qué |
|---|---|
| Tipo de cambio | Mostrar los límites de cada rango también en dólares |
| Persistencia | Almacenar y recuperar los rangos |

---

## Tabla inicial (sección 8.7)

| Monto mínimo CRC | Monto máximo CRC | Aprobador |
|---|---|---|
| 0,01 | 999 999,99 | Encargado de área |
| 1 000 000,00 | 9 999 999,99 | Gerencia |
| 10 000 000,00 | Sin límite | Junta Directiva |

Se carga como dato semilla en la migración inicial, con identificadores fijos para que la carga sea
idempotente entre entornos y las pruebas puedan referenciarlos.

---

## Por qué una tabla y no condiciones

La diferencia no es estética. Con condiciones fijas en el código:

```csharp
// Lo que el enunciado prohíbe, y con razón
if (monto < 1_000_000) return "Encargado de area";
if (monto < 10_000_000) return "Gerencia";
return "Junta Directiva";
```

Cambiar un umbral exige modificar el código, recompilar, probar y desplegar. Con la tabla, cambiar
un umbral es editar una fila desde la interfaz.

La implementación real no contiene **ningún umbral literal**:

```csharp
public static NivelAprobacion? Seleccionar(decimal montoCrc, IEnumerable<NivelAprobacion> niveles)
{
    return niveles
        .OrderBy(n => n.MontoMinimoCrc)
        .FirstOrDefault(n => n.Cubre(montoCrc));
}
```

El orden por monto mínimo hace que el resultado no dependa del orden en que la base de datos
devuelva las filas.

---

## Reglas de negocio

### Rangos inclusivos en ambos extremos

```csharp
public bool Cubre(decimal montoCrc)
    => montoCrc >= MontoMinimoCrc && (MontoMaximoCrc is null || montoCrc <= MontoMaximoCrc);
```

Un monto de exactamente 999 999,99 corresponde al «Encargado de área»; uno de exactamente
1 000 000,00 corresponde a «Gerencia».

### Rango abierto

Un rango con `MontoMaximoCrc` nulo cubre desde su mínimo hacia arriba sin límite. **Solo puede
existir uno**: dos rangos abiertos se traslaparían necesariamente.

### No traslape

Dos rangos no pueden compartir ningún monto. La comprobación trata el rango abierto como si su
máximo fuera infinito:

```csharp
decimal maximoPropio = MontoMaximoCrc ?? decimal.MaxValue;
decimal maximoOtro = otro.MontoMaximoCrc ?? decimal.MaxValue;

return MontoMinimoCrc <= maximoOtro && otro.MontoMinimoCrc <= maximoPropio;
```

**Esta regla no puede expresarse con una restricción `CHECK` de fila**, porque requiere ver todas las
filas. Se valida en la capa de aplicación sobre el conjunto completo que quedaría vigente:

```csharp
var existentes = await _niveles.ListarTodosAsync(cancelacion);
SelectorNivelAprobacion.AsegurarConjuntoValido([.. existentes.Where(n => n.Id != id), nivel]);
```

Nótese que la validación se hace con el conjunto **resultante**, no con el actual: se excluye el
rango que se está modificando y se incluye su versión nueva.

### Coherencia interna del rango

`monto_maximo_crc IS NULL OR monto_maximo_crc >= monto_minimo_crc`, garantizada por una restricción
`CHECK` en la base de datos además de la validación de aplicación.

---

## Monto no cubierto

Si ningún rango cubre el monto consultado, el sistema **informa la ausencia sin interrumpir la
consulta**.

Es una decisión deliberada. Interrumpir dejaría la pantalla de detalle de la licitación inutilizable
por un dato de configuración que el administrador puede corregir en un minuto. En su lugar, el
aprobador se devuelve como nulo y la interfaz muestra un aviso con enlace a la pantalla de
administración.

Existe además `SeleccionarObligatorio`, que sí falla con `APR-004`, para los contextos donde la
ausencia de aprobador debe detener la operación.

---

## Errores

| Código | Estado | Situación |
|---|---|---|
| `APR-001` | 422 | Los rangos se traslapan |
| `APR-002` | 422 | Ya existe un rango abierto |
| `APR-003` | 422 | El monto máximo es menor que el mínimo |
| `APR-004` | 422 | Ningún rango cubre el monto |
| `GEN-001` | 422 | Monto mínimo o máximo cero o negativo |
| `GEN-002` | 404 | Rango inexistente |

---

## Pruebas

| Prueba | Verifica |
|---|---|
| `SelectorNivelAprobacionPruebas.Seleccionar_DevuelveElAprobador*` | Ocho montos en los bordes de los tres rangos |
| `SelectorNivelAprobacionPruebas.Cubre_EsInclusivoEnAmbosExtremos` | Inclusividad de los límites |
| `SelectorNivelAprobacionPruebas.Seleccionar_NoDependeDelOrden*` | Determinismo |
| `SelectorNivelAprobacionPruebas.AsegurarConjuntoValido_ConRangosTraslapados_Falla` | Traslape |
| `SelectorNivelAprobacionPruebas.AsegurarConjuntoValido_ConDosRangosAbiertos_Falla` | Rango abierto único |
| `EsquemaYSemillaPruebas.Semilla_CargaLosTresNiveles*` | Datos semilla exactos |
| `LicitacionServicioPruebas.ObtenerMejorOfertaAsync_SinRangoAplicable_*` | Ausencia informada, no error |

La prueba de bordes recorre ocho montos: 0,01 · 500 000 · 999 999,99 · 1 000 000,00 · 5 000 000 ·
9 999 999,99 · 10 000 000,00 · 500 000 000. Son exactamente los puntos donde aparecería un error de
comparación por uno.

---

## Archivos

| Archivo | Contenido |
|---|---|
| `Domain/Entidades/NivelAprobacion.cs` | Entidad, `Cubre` y `SeTraslapaCon` |
| `Domain/Servicios/SelectorNivelAprobacion.cs` | Selección y validación del conjunto |
| `Application/Servicios/NivelAprobacionServicio.cs` | Coordinación de los casos de uso |
| `Infrastructure/Repositorios/NivelAprobacionRepositorio.cs` | Acceso a datos |
| `Api/Controladores/NivelesAprobacionApiController.cs` | Endpoints REST |
| `Web/Controladores/NivelesAprobacionController.cs` | Pantallas |
