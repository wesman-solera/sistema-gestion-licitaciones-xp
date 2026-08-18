# Módulo: Tipo de cambio

## Propósito

Administra el valor de conversión entre colones y dólares. Su papel es exclusivamente de
presentación: **ninguna regla de negocio del sistema depende de él**.

## Responsabilidades

- Mantener el histórico de tipos de cambio
- Garantizar que exista como máximo uno activo
- Convertir montos de colones a dólares para mostrarlos

## Lo que no hace

- **No consulta ningún servicio externo.** El enunciado exige que la solución funcione sin Internet
- No persiste ningún valor en dólares
- No participa en ninguna validación de negocio

---

## El colón es la fuente de verdad (sección 8.8)

Esta es la regla que gobierna todo el módulo:

| Aspecto | Comportamiento |
|---|---|
| Almacenamiento | Solo en colones, en columnas `numeric(18,2)` |
| Conversión | Calculada en el momento de responder |
| Persistencia del valor en dólares | **Nunca** |
| Efecto de cambiar el tipo de cambio | Ninguno sobre los datos almacenados |
| Reglas de negocio que lo consultan | Ninguna |

Cambiar el tipo de cambio no altera un solo registro de licitaciones, ofertas ni niveles de
aprobación. Solo cambia el número que se muestra junto al monto en colones.

### Consecuencia práctica

Si el sistema no tiene tipo de cambio activo, **todo sigue funcionando**. Se pueden crear
licitaciones, registrar ofertas, evaluar la mejor y determinar el aprobador. Lo único que ocurre es
que el componente en dólares de cada monto viaja como nulo y la interfaz lo indica.

Esa decisión está probada:
`LicitacionServicioPruebas.ObtenerDetalleAsync_SinTipoDeCambioActivo_DevuelveMontoEnColonesSinDolares`.

---

## Fórmula

```
Monto USD = Monto CRC / Tipo de cambio CRC por USD
```

Se calcula enteramente con `decimal` y se redondea a dos decimales alejándose del cero.

**Por qué alejándose del cero y no con redondeo bancario.** El redondeo bancario, que aproxima al
par más cercano, distribuye el error uniformemente en un conjunto grande de operaciones. Aquí cada
monto se muestra por separado, y ocultar medio céntimo hacia abajo en la mitad de los casos no
aporta nada. La regla habitual en presentación monetaria es alejarse del cero.

---

## Un solo tipo de cambio activo

La invariante se sostiene en dos niveles.

### Nivel de base de datos: índice único parcial

```sql
CREATE UNIQUE INDEX ux_tipos_cambio_unico_activo
    ON tipos_cambio (activo)
    WHERE activo;
```

PostgreSQL solo aplica el índice a las filas donde `activo` es verdadero. Puede haber tantos
registros históricos inactivos como se quiera, pero **jamás dos activos a la vez**.

### Nivel de aplicación: activación transaccional

Aquí apareció un defecto real durante el desarrollo. La primera implementación activaba el nuevo
registro y desactivaba el anterior en la misma llamada a `SaveChanges`. PostgreSQL evaluaba la
restricción con las dos filas activas y rechazaba la operación.

La solución fue ordenar los pasos dentro de la transacción:

```csharp
await _unidadTrabajo.EnTransaccionAsync(async ct =>
{
    await DesactivarActivosAsync(idExcluido: id, ct);
    await _unidadTrabajo.GuardarCambiosAsync(ct);   // ← confirma la desactivación primero

    tipoCambio.Activar(_reloj.AhoraUtc);
    await _unidadTrabajo.GuardarCambiosAsync(ct);

    return Mapear(tipoCambio);
}, cancelacion);
```

La transacción garantiza que ambos pasos ocurran o que no ocurra ninguno: nunca puede quedar el
sistema con cero tipos de cambio activos.

### No se puede eliminar el activo

Eliminar el tipo de cambio activo dejaría al sistema sin poder convertir. Se rechaza con `TCB-001`;
primero hay que activar otro.

---

## Contexto de moneda por petición

Sin cuidado, cada monto convertido dispararía su propia consulta del tipo de cambio activo. Un
listado de 20 licitaciones haría 20 consultas idénticas a la misma fila.

`ContextoMoneda` se registra con ciclo de vida por petición y carga el valor **una sola vez**:

```csharp
public async Task CargarAsync(CancellationToken cancelacion = default)
{
    if (_cargado) { return; }

    _tipoCambio = await _tipoCambioRepositorio.ObtenerActivoAsync(cancelacion);
    _cargado = true;
}
```

La bandera `_cargado` es necesaria además del valor: sin ella, la ausencia de tipo de cambio activo
provocaría una consulta nueva en cada llamada, porque el campo seguiría siendo nulo.

La prueba `LicitacionServicioPruebas.ObtenerDetalleAsync_ConsultaElTipoDeCambioUnaSolaVez` verifica
este comportamiento con un sustituto que cuenta las invocaciones.

---

## Fecha de vigencia

El enunciado exige mostrar la fecha del tipo de cambio utilizado. Viaja junto a los montos
convertidos en `TipoCambioAplicadoDto`, de modo que el cliente no tiene que hacer una consulta
aparte para poder mostrarla.

---

## Errores

| Código | Estado | Situación |
|---|---|---|
| `TCB-001` | 422 | No hay tipo de cambio activo, o se intentó eliminar el activo |
| `TCB-002` | 422 | El valor no es mayor que cero |
| `GEN-002` | 404 | Tipo de cambio inexistente |

---

## Pruebas

| Prueba | Verifica |
|---|---|
| `ConversorMonedaPruebas.ConvertirAUsd_AplicaLaFormula*` | Fórmula del enunciado |
| `ConversorMonedaPruebas.ConvertirAUsd_RedondeaADosDecimales` | Redondeo |
| `ConversorMonedaPruebas.ConvertirAUsd_RedondeaAlejandoseDelCero` | Modo de redondeo |
| `ConversorMonedaPruebas.ConvertirAUsd_NoModificaElMontoOriginal` | El colón no se altera |
| `RestriccionesPruebas.IndiceUnicoParcial_RechazaUnSegundo*` | Índice único parcial |
| `RestriccionesPruebas.IndiceUnicoParcial_AdmiteVariosInactivos` | El histórico no se limita |
| `ConcurrenciaYTransaccionPruebas.Transaccion_ActivarUnTipoDeCambio*` | Activación transaccional |
| `LicitacionServicioPruebas.ObtenerDetalleAsync_ConsultaElTipoDeCambioUnaSolaVez` | Sin consultas repetidas |
| `InterfazPruebas.AlternarMoneda_*` | Alternancia desde el navegador |

---

## Archivos

| Archivo | Contenido |
|---|---|
| `Domain/Entidades/TipoCambio.cs` | Entidad, activación y desactivación |
| `Domain/Servicios/ConversorMoneda.cs` | Fórmula de conversión |
| `Application/Servicios/ContextoMoneda.cs` | Carga única por petición |
| `Application/Servicios/TipoCambioServicio.cs` | Coordinación y transacción |
| `Infrastructure/Repositorios/TipoCambioRepositorio.cs` | Acceso a datos |
| `Api/Controladores/TiposCambioController.cs` | Endpoints REST |
| `Web/Controladores/TiposCambioController.cs` | Pantallas |
