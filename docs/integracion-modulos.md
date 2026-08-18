# Integración entre módulos

Este documento explica **cómo cooperan** los módulos: qué depende de qué, cómo viaja la información
entre ellos y dónde están los límites. Cada módulo tiene además su documento propio en
[`modulos/`](modulos/).

## Mapa de dependencias

```mermaid
graph TD
    UI["Interfaz web"]
    API["API REST"]
    LIC["Licitaciones"]
    PRO["Proveedores"]
    OFE["Ofertas"]
    APR["Niveles de aprobación"]
    TCB["Tipo de cambio"]
    PER["Persistencia"]

    UI --> LIC
    UI --> PRO
    UI --> OFE
    UI --> APR
    UI --> TCB
    API --> LIC
    API --> PRO
    API --> OFE
    API --> APR
    API --> TCB

    OFE --> LIC
    OFE --> PRO
    LIC --> OFE
    LIC --> APR
    LIC --> TCB
    OFE --> TCB
    APR --> TCB

    LIC --> PER
    PRO --> PER
    OFE --> PER
    APR --> PER
    TCB --> PER

    style PER fill:#5a6577,color:#ffffff
    style TCB fill:#9a6212,color:#ffffff
```

### Naturaleza de cada dependencia

| Origen | Destino | Tipo | Qué necesita |
|---|---|---|---|
| Ofertas | Licitaciones | **Fuerte** | El estado, la fecha de cierre y el presupuesto para validar la oferta |
| Ofertas | Proveedores | **Fuerte** | La existencia del proveedor y su nombre |
| Licitaciones | Ofertas | **De lectura** | Las ofertas para evaluar la mejor y calcular el ahorro |
| Licitaciones | Niveles de aprobación | **De consulta** | El aprobador que corresponde al monto ganador |
| Todos | Tipo de cambio | **De presentación** | El valor para calcular el equivalente en dólares |

La dependencia con el tipo de cambio es la más débil de todas y merece destacarse: **su ausencia no
impide ninguna operación**. Sin tipo de cambio activo el sistema sigue funcionando por completo;
solo deja de mostrar el equivalente en dólares. Ninguna regla de negocio consulta esa conversión.

### La relación circular entre Licitaciones y Ofertas

Es aparente, no real. Se resuelve en direcciones distintas:

- **Ofertas → Licitaciones** ocurre en la **escritura**. `Oferta.Registrar` recibe la licitación
  completa y valida contra su estado, su fecha y su presupuesto.
- **Licitaciones → Ofertas** ocurre en la **lectura**. `LicitacionServicio` consulta las ofertas
  para evaluarlas, pero nunca las modifica.

Ninguna entidad modifica a la otra. La única excepción aparente —la regla que impide reducir el
presupuesto por debajo de una oferta existente— se resuelve pasando el dato a la entidad:
`Licitacion.ActualizarDatos` recibe `mayorOfertaRegistradaCrc` como parámetro, en lugar de consultar
el repositorio de ofertas.

---

## Flujos de extremo a extremo

### Flujo 1 — Del alta a la adjudicación

```mermaid
sequenceDiagram
    actor U as Encargado de compras
    participant PRO as Proveedores
    participant LIC as Licitaciones
    participant OFE as Ofertas
    participant APR as Niveles de aprobación
    participant TCB as Tipo de cambio

    U->>PRO: Registrar proveedor
    PRO->>PRO: Normalizar nombre y verificar unicidad
    PRO-->>U: Proveedor creado

    U->>LIC: Crear licitación
    LIC->>LIC: Normalizar código y verificar unicidad
    LIC-->>U: Licitación en Borrador

    U->>LIC: Publicar
    LIC->>LIC: Verificar datos completos y fecha futura
    LIC-->>U: Licitación Publicada

    U->>OFE: Registrar oferta
    OFE->>LIC: Consultar estado, fecha y presupuesto
    LIC-->>OFE: Datos de la licitación
    OFE->>PRO: Verificar que el proveedor existe
    PRO-->>OFE: Proveedor
    OFE->>OFE: Verificar que no haya oferta previa
    OFE-->>U: Oferta registrada

    U->>LIC: Consultar mejor oferta
    LIC->>OFE: Obtener las ofertas
    OFE-->>LIC: Ofertas
    LIC->>LIC: Determinar mejor oferta y calcular ahorro
    LIC->>APR: ¿Quién aprueba este monto?
    APR-->>LIC: Aprobador
    LIC->>TCB: Obtener el tipo de cambio activo
    TCB-->>LIC: Valor y fecha
    LIC-->>U: Mejor oferta, ahorro, clasificación y aprobador
```

**Puntos de integración críticos**

1. **Ofertas necesita la licitación completa, no solo su identificador.** Validar el estado, el
   vencimiento y el presupuesto requiere los datos. Por eso el servicio la carga antes de invocar
   la factoría.
2. **La evaluación no persiste nada.** `ResultadoEvaluacionOfertas` se calcula a demanda. Si se
   almacenara, quedaría desincronizado en cuanto llegara una oferta nueva.
3. **El aprobador se consulta, no se guarda en la licitación.** Cambiar la tabla de rangos se
   refleja de inmediato en todas las consultas, sin actualizar filas históricas.

---

### Flujo 2 — Cambio del tipo de cambio

```mermaid
sequenceDiagram
    actor A as Administrador
    participant TCB as Tipo de cambio
    participant TX as Transacción
    participant DB as PostgreSQL

    A->>TCB: Activar el tipo de cambio nuevo
    TCB->>TX: Abrir transacción
    TX->>DB: Desactivar los activos actuales
    TX->>DB: Confirmar la desactivación
    Note over TX,DB: El orden importa: el índice único parcial<br/>rechazaría dos filas activas simultáneas
    TX->>DB: Activar el nuevo
    TX->>DB: Confirmar la transacción
    DB-->>TCB: OK
    TCB-->>A: Tipo de cambio activo actualizado
```

Ninguna licitación ni oferta se modifica. Los montos almacenados siguen siendo exactamente los
mismos; lo único que cambia es el valor con el que se calcula la representación en dólares en las
consultas posteriores.

---

### Flujo 3 — Eliminación con integridad referencial

```mermaid
graph TD
    A["Solicitud de eliminar<br/>licitación o proveedor"] --> B{"¿Tiene ofertas<br/>asociadas?"}
    B -->|No| C["Borrado físico"]
    B -->|Sí| D["Borrado lógico:<br/>se marca DeletedAt"]
    C --> E["Fila eliminada"]
    D --> F["Fila conservada,<br/>oculta de los listados"]
    F --> G["Las ofertas se conservan<br/>como evidencia"]

    style D fill:#9a6212,color:#ffffff
    style G fill:#1a7f4b,color:#ffffff
```

La decisión se toma en el servicio de aplicación, que es quien puede consultar si existen registros
relacionados. La entidad solo expone la variante segura: `EliminarLogicamente`.

Como última defensa, las claves foráneas usan `ON DELETE RESTRICT`. Aunque alguien intentara el
borrado físico saltándose el servicio, PostgreSQL lo rechazaría.

---

## Contratos entre capas

### Interfaz web y API comparten los servicios de aplicación

Ambas fachadas consumen exactamente los mismos servicios. Esa reutilización es deliberada:
garantiza que una regla de negocio no pueda comportarse de una forma en la pantalla y de otra por
la API.

| Aspecto | Interfaz web | API REST |
|---|---|---|
| Entrada | Modelo de formulario con anotaciones de datos | DTO con validadores de FluentValidation |
| Servicio | **El mismo** | **El mismo** |
| Salida | Vista Razor | DTO serializado a JSON |
| Error | Mensaje junto al campo | ProblemDetails con código de error |

La diferencia está solo en los extremos: cómo entra el dato y cómo se presenta el resultado.

### La capa de aplicación no conoce Entity Framework Core

Depende de puertos que ella misma define. La implementación vive en `Infrastructure`. Eso permite
que las pruebas unitarias sustituyan los repositorios sin levantar una base de datos, y mantiene la
regla de dependencias apuntando hacia el dominio.

### El dominio no conoce nada externo

`Licitaciones.Domain` no referencia ningún paquete de acceso a datos ni de ASP.NET Core. Necesita
saber qué hora es, y lo resuelve con `IRelojSistema`, una abstracción propia.

---

## Puntos de acoplamiento y su justificación

| Acoplamiento | Por qué es aceptable |
|---|---|
| `Oferta.Registrar` recibe la entidad `Licitacion` completa | Necesita tres de sus datos para validar. Pasar solo el identificador obligaría a consultar desde el dominio, que es justamente lo que no debe hacer |
| `LicitacionDetalleDto` incluye las ofertas y la evaluación | La pantalla de detalle las necesita todas a la vez. Separarlas obligaría al cliente a hacer tres peticiones para mostrar una pantalla |
| `ContextoMoneda` lo consumen cuatro servicios | Es un servicio de presentación, sin reglas de negocio. Su alternativa era repetir la consulta del tipo de cambio en cada uno |
| El proyecto `Web` referencia `Api` | Es lo que permite alojar la interfaz y los endpoints en un solo proceso. La alternativa era duplicar los controladores |

---

## Trazabilidad de los requisitos

Correspondencia entre cada requisito del enunciado, el módulo que lo implementa y la prueba que lo
verifica. Es la cadena que pide el criterio de trazabilidad de la rúbrica.

| Requisito | Módulo | Prueba principal |
|---|---|---|
| 8.1 Ciclo de estados | Licitaciones | `PoliticaTransicionEstadoPruebas` |
| 8.1 Cierre funcional por vencimiento | Licitaciones | `LicitacionPruebas.EstaCerradaFuncionalmente_*` |
| 8.2 Rechazo de oferta vencida | Ofertas | `OfertaPruebas.Registrar_EnElInstanteExactoDelCierre_Falla` |
| 8.2 Fechas en UTC, presentación local | Interfaz web | `ConcurrenciaYTransaccionPruebas.Fechas_SeConservanEnUtc*` |
| 8.2 Reloj inyectable | Dominio | `RelojFijo` en todas las pruebas de vencimiento |
| 8.3 Unicidad de código | Licitaciones | `RestriccionesPruebas.IndiceUnico_RechazaDosLicitaciones*` |
| 8.3 Unicidad de proveedor | Proveedores | `ProveedorPruebas.Crear_NormalizaLosNombresEquivalentes*` |
| 8.3 Oferta única por proveedor | Ofertas | `RestriccionesPruebas.IndiceUnicoCompuesto_*` |
| 8.4 Caracteres permitidos | Proveedores | `ProveedorPruebas.Crear_RechazaLosCaracteresNoPermitidos` |
| 8.5 Montos positivos | Todos | `ck_*_positivo` y pruebas de cada entidad |
| 8.5 Oferta acotada por presupuesto | Ofertas | `OfertaPruebas.Registrar_ConMontoIgualAlPresupuesto_EsValida` |
| 8.5 Presupuesto no reducible | Licitaciones | `LicitacionPruebas.ActualizarDatos_ReduciendoElPresupuesto*` |
| 8.6 Mejor oferta y desempate | Licitaciones | `EvaluadorOfertasPruebas.Evaluar_ConEmpateDeMonto_*` |
| 8.6 Clasificación del ahorro | Licitaciones | `EvaluadorOfertasPruebas.Evaluar_ClasificaSegunElAhorro` |
| 8.7 Aprobador parametrizable | Niveles de aprobación | `SelectorNivelAprobacionPruebas.Seleccionar_*` |
| 8.7 Rangos sin traslape | Niveles de aprobación | `SelectorNivelAprobacionPruebas.AsegurarConjuntoValido_*` |
| 8.8 Conversión CRC/USD | Tipo de cambio | `ConversorMonedaPruebas.*` |
| 8.8 Un solo tipo de cambio activo | Tipo de cambio | `RestriccionesPruebas.IndiceUnicoParcial_*` |
| 8.9 Integridad al eliminar | Persistencia | `RestriccionesPruebas.ClaveForanea_*` |
| 8.9 Confirmación antes de eliminar | Interfaz web | `FlujoCompletoPruebas.Eliminar_PideConfirmacion*` |
| 9 Modo claro y oscuro | Interfaz web | `InterfazPruebas.ModoOscuro_*` |
| 9 Alternancia de moneda | Interfaz web | `InterfazPruebas.AlternarMoneda_*` |
| 9 Diseño adaptable | Interfaz web | `InterfazPruebas.DisenoAdaptable_*` |
| 10 ProblemDetails seguro | API REST | `LicitacionesEndpointsPruebas.ProblemDetails_NoExponeDetallesInternos` |
| 10 Paginación | API REST | `LicitacionesEndpointsPruebas.Get_Listado_*` |
| 11 Concurrencia optimista | Persistencia | `ConcurrenciaYTransaccionPruebas.ConcurrenciaOptimista_*` |
| 11 Transacciones | Persistencia | `ConcurrenciaYTransaccionPruebas.Transaccion_*` |
| 11 Migraciones y semilla | Persistencia | `EsquemaYSemillaPruebas.*` |
| 5.3 Flujo funcional mínimo | Todos | `FlujoCompletoPruebas.FlujoMinimo_*` |
