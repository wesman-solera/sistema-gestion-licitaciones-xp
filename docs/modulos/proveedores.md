# Módulo: Proveedores

## Propósito

Mantiene el catálogo de empresas y personas habilitadas para presentar ofertas. Su regla central es
la unicidad del nombre, que resulta menos trivial de lo que parece.

## Responsabilidades

- Registrar proveedores con nombre único normalizado
- Restringir los caracteres admitidos en el nombre
- Consultar, editar y eliminar, decidiendo entre borrado físico y lógico

## Lo que no hace

- No conoce las ofertas más allá de su existencia, que solo consulta para decidir el tipo de borrado

---

## Dependencias

| Depende de | Para qué |
|---|---|
| Ofertas | Saber si el proveedor tiene ofertas asociadas |
| Persistencia | Almacenar y recuperar proveedores |

---

## Entradas y salidas

| Contrato | Campos |
|---|---|
| `CrearProveedorRequest` | `Nombre` |
| `ActualizarProveedorRequest` | `Nombre` |
| `ProveedorDto` | `Id`, `Nombre`, `NombreNormalizado`, `CantidadOfertas`, `Eliminado`, marcas de tiempo |

---

## Reglas de negocio

### Normalización del nombre (sección 8.3)

El nombre es único **después de**:

1. Eliminar espacios laterales
2. Reducir los espacios repetidos a uno solo
3. Normalizar Unicode
4. Comparar sin distinguir mayúsculas y minúsculas

Los tres ejemplos del enunciado se consideran el mismo proveedor:

| Entrada | Forma normalizada |
|---|---|
| `Empresa Central` | `EMPRESA CENTRAL` |
| `  empresa central` | `EMPRESA CENTRAL` |
| `EMPRESA  CENTRAL` | `EMPRESA CENTRAL` |

**El paso 3 es el que suele olvidarse.** Un carácter acentuado puede representarse de dos formas
distintas en Unicode: la letra precompuesta, o la letra base seguida de un signo diacrítico
combinante. Las dos se ven idénticas en pantalla pero son cadenas diferentes. Sin normalización, dos
proveedores llamados «Núñez» con representaciones distintas pasarían el índice único como si fueran
empresas diferentes.

Se aplica la forma de composición canónica y se compara con la cultura invariante, de modo que el
resultado no dependa de la configuración regional del servidor.

### Dos columnas, no una

Se guardan `nombre` con lo que escribió el usuario y `nombre_normalizado` para comparar. El primero
es lo que se muestra; el segundo es lo que indexa el índice único.

**Alternativa descartada:** un índice único sobre una expresión como `UPPER(TRIM(nombre))`. Es
posible en PostgreSQL, pero la lógica de normalización quedaría duplicada en la base y en el código,
con el riesgo de que ambas versiones se separen con el tiempo.

### Caracteres permitidos (sección 8.4)

Se admiten letras, números, espacios, punto, coma y paréntesis:

```
^[\p{L}\p{N} .,()]+$
```

`\p{L}` y `\p{N}` son categorías Unicode, no rangos ASCII: admiten letras acentuadas y de cualquier
alfabeto.

| Válido | Inválido |
|---|---|
| `Servicios Tecnicos S.A.` | `Empresa @ Central` |
| `Grupo 2000, Sociedad Anonima` | `Proveedor #1` |
| `Consorcio (Region Norte)` | `Servicios & Mas` |
| `Distribuidora Núñez` | `Empresa/Sucursal` |

### Eliminación (sección 8.9)

| Situación | Comportamiento |
|---|---|
| Sin ofertas asociadas | Borrado físico |
| Con ofertas asociadas | Borrado lógico: se marca `DeletedAt` |

Un proveedor eliminado lógicamente desaparece de los listados y de las listas de selección, pero sus
ofertas históricas se conservan intactas.

La decisión la toma el servicio, que es quien puede consultar la existencia de relaciones. Como
última defensa, la clave foránea de `ofertas` usa `ON DELETE RESTRICT`.

---

## Validación en tres capas

| Capa | Qué aporta |
|---|---|
| Interfaz | Expresión regular en el atributo del formulario, respuesta inmediata |
| Servidor | `Proveedor.Crear` valida caracteres; el servicio consulta la unicidad |
| PostgreSQL | Índice único `ux_proveedores_nombre_normalizado` |

La tercera capa cubre lo que las dos primeras no pueden: dos peticiones simultáneas que superen
ambas la comprobación de aplicación. En ese caso, el manejador global traduce la violación de índice
al mismo código `PRO-001` que habría devuelto la comprobación previa, de modo que el cliente ve la
misma respuesta gane quien gane la carrera.

---

## Errores

| Código | Estado | Situación |
|---|---|---|
| `PRO-001` | 409 | Nombre duplicado tras normalizar |
| `PRO-002` | 400 | Caracteres no permitidos |
| `GEN-002` | 404 | Proveedor inexistente |
| `GEN-004` | 400 | Nombre vacío o demasiado largo |

---

## Rendimiento

El listado muestra la cantidad de ofertas de cada proveedor. La implementación ingenua cargaría la
colección completa de ofertas de cada uno para contarlas: un listado de 20 proveedores con 50
ofertas cada uno traería 1000 filas para mostrar 20 números.

Se resuelve con `ContarOfertasAsync`, que hace **una sola agregación** con `GROUP BY` para toda la
página.

---

## Pruebas

| Prueba | Verifica |
|---|---|
| `ProveedorPruebas.Crear_NormalizaLosNombresEquivalentes*` | Los tres ejemplos del enunciado |
| `ProveedorPruebas.NormalizarNombre_UnificaLasRepresentacionesUnicode*` | Composición canónica |
| `ProveedorPruebas.Crear_RechazaLosCaracteresNoPermitidos` | Expresión regular de la sección 8.4 |
| `ProveedorServicioPruebas.EliminarAsync_*` | Borrado físico frente a lógico |
| `RestriccionesPruebas.IndiceUnico_RechazaDosProveedores*` | Índice único en PostgreSQL |
| `ProveedoresYTiposCambioEndpointsPruebas.Post_Proveedor_*` | Contrato HTTP |
| `FlujoCompletoPruebas.Proveedor_ConNombreEquivalente_*` | Mensaje junto al campo en la interfaz |

---

## Archivos

| Archivo | Contenido |
|---|---|
| `Domain/Entidades/Proveedor.cs` | Entidad y sus reglas |
| `Domain/Servicios/NormalizadorTexto.cs` | Normalización y validación de caracteres |
| `Application/Servicios/ProveedorServicio.cs` | Coordinación de los casos de uso |
| `Infrastructure/Repositorios/ProveedorRepositorio.cs` | Acceso a datos y conteo agregado |
| `Api/Controladores/ProveedoresController.cs` | Endpoints REST |
| `Web/Controladores/ProveedoresController.cs` | Pantallas |
