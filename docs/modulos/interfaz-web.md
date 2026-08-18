# Módulo: Interfaz web

## Propósito

Presenta el sistema a las personas que lo usan: landing page explicativa, CRUD completo de los cinco
módulos, tema claro y oscuro, alternancia de moneda y diseño adaptable.

## Responsabilidades

- Renderizar las pantallas con ASP.NET Core MVC y Razor
- Gestionar las preferencias de tema y moneda
- Aplicar el formato cultural costarricense a montos y fechas
- Traducir los errores de negocio a mensajes junto al campo correspondiente

## Lo que no hace

- No contiene reglas de negocio ni consultas: consume los mismos servicios de aplicación que la API

---

## Sin dependencias externas de front-end

El requisito 9 pide que los recursos estén incluidos localmente y que *«la interfaz no debe quedar
inutilizable por falta de acceso a una CDN»*.

En lugar de incorporar un framework visual y copiarlo al repositorio, se escribieron **una hoja de
estilos y un archivo de guiones propios**. El resultado:

| Aspecto | Valor |
|---|---|
| Peso de la hoja de estilos | ~15 KB sin comprimir |
| Peso de los guiones | ~4 KB sin comprimir |
| Dependencias externas | **Ninguna** |
| Peticiones a terceros | **Ninguna** |

La interfaz funciona idéntica con y sin conexión a Internet.

### La página funciona sin JavaScript

Todo lo que hacen los guiones es una mejora sobre una página que ya funciona:

| Funcionalidad | Sin JavaScript | Con JavaScript |
|---|---|---|
| Formularios | Se envían al servidor | Igual, más protección contra doble envío |
| Validación | La hace el servidor | Aviso inmediato mientras se escribe |
| Alternar tema | Formulario POST | Igual |
| Alternar moneda | Formulario POST | Igual |
| Menú en móvil | Visible por defecto | Desplegable |
| Confirmar eliminación | Pantalla propia de confirmación | Igual, más un diálogo adicional |

Los alternadores de tema y moneda son formularios POST y no enlaces porque **cambian estado del
lado del servidor**. Un enlace `GET` que modifica estado es un error de diseño: los navegadores y
los rastreadores pueden precargarlo.

---

## Tema claro y oscuro

### Resuelto en el servidor

El tema se guarda en una cookie y el servidor escribe el atributo `data-tema` en el elemento
`<html>` al renderizar.

```html
<html lang="es-CR" data-tema="@temaActual">
```

**Por qué no se resuelve en el navegador.** Si el tema se aplicara desde JavaScript después de
cargar, la página se pintaría primero en claro y saltaría a oscuro: el parpadeo que se ve en muchos
sitios. Resolverlo en el servidor lo elimina por completo.

Como efecto secundario, las pruebas funcionales pueden fijar el tema con una cookie sin ejecutar
guiones.

### Implementación con variables CSS

```css
:root {
    --color-fondo: #f4f6f9;
    --color-texto: #1b2432;
    /* ... */
}

[data-tema="oscuro"] {
    --color-fondo: #12161d;
    --color-texto: #e8ecf2;
    /* ... */
}
```

Cambiar el tema es cambiar un atributo. No hay dos hojas de estilo ni clases duplicadas.

### Por qué cookie y no almacenamiento del navegador

| Aspecto | Cookie | Almacenamiento local |
|---|---|---|
| Disponible al renderizar en el servidor | **Sí** | No |
| Evita el parpadeo inicial | **Sí** | No |
| Accesible desde las pruebas funcionales | **Sí** | Requiere ejecutar guiones |

---

## Alternancia de moneda

El botón de la barra superior alterna entre CRC y USD. La preferencia se guarda en cookie y el
servidor la aplica al formatear cada monto.

**Los datos no cambian.** Las respuestas de los servicios incluyen siempre ambos valores —el oficial
en colones y el calculado en dólares— y `FormateadorMonto` elige cuál mostrar. Alternar la moneda no
dispara ninguna escritura.

La prueba `InterfazPruebas.AlternarMoneda_CambiaLaVisualizacionSinAlterarLosDatos` lo verifica de
forma explícita: alterna a dólares, comprueba que el valor cambió, alterna de vuelta y comprueba que
reaparece **exactamente** el valor original.

Si no hay tipo de cambio activo y el usuario pidió dólares, se muestra el valor en colones con la
nota «sin tipo de cambio». Es preferible mostrar el dato correcto en otra moneda que dejar la celda
vacía.

---

## Formato cultural

| Elemento | Cultura | Ejemplo |
|---|---|---|
| Montos en colones | `es-CR` | `₡1 234 567,89` |
| Montos en dólares | `en-US` | `$2,444.69` |
| Fechas | `es-CR` | `30/09/2026 17:00` |

La cultura de la aplicación se fija en `es-CR`, lo que además hace que el enlace de modelo
interprete los separadores decimales igual que como se muestran: un monto escrito en el formulario
se lee como el usuario lo escribió.

### Zona horaria

Todo se almacena y se compara en UTC. La conversión a `America/Costa_Rica` ocurre solo en
`FormateadorFecha`.

**Detalle que causó un defecto real.** El control `datetime-local` del navegador envía la fecha
**sin desplazamiento horario**. Interpretarla como UTC desplazaba el cierre seis horas: una
licitación que debía cerrar a las 17:00 quedaba registrada a las 11:00.

```csharp
public static DateTimeOffset DesdeControlCalendario(DateTime fechaLocal)
{
    DateTime sinZona = DateTime.SpecifyKind(fechaLocal, DateTimeKind.Unspecified);
    TimeSpan desplazamiento = Zona.GetUtcOffset(sinZona);

    return new DateTimeOffset(sinZona, desplazamiento).ToUniversalTime();
}
```

La resolución de la zona tolera que el sistema no traiga la base de datos de zonas horarias: cae a
UTC en lugar de impedir el arranque.

---

## Estructura de las pantallas

| Módulo | Pantallas |
|---|---|
| Inicio | Landing page explicativa |
| Licitaciones | Listado, detalle, crear, editar, eliminar |
| Proveedores | Listado, crear, editar, eliminar |
| Ofertas | Listado, crear, editar, eliminar |
| Niveles de aprobación | Listado, crear, editar, eliminar |
| Tipos de cambio | Listado, crear, editar, eliminar |

### Vistas parciales compartidas

| Parcial | Función |
|---|---|
| `_Layout` | Plantilla, navegación, alternadores |
| `_Mensajes` | Mensajes de éxito, advertencia y error entre peticiones |
| `_ResumenValidacion` | Errores no atribuibles a un campo concreto |
| `_Paginacion` | Controles de paginación reutilizables |
| `_Formulario` | Campos comunes entre crear y editar, uno por módulo |

---

## Mensajes de error junto al campo

El requisito 9 pide *«formularios con validación junto al campo correspondiente»*.

`ControladorBase.EjecutarAsync` traduce las excepciones de negocio al estado del modelo, asociando
cada mensaje a su campo:

```csharp
catch (ConflictoUnicidadException conflicto)
{
    ModelState.AddModelError(conflicto.Campo, conflicto.Message);
    return false;
}
```

Por eso `ConflictoUnicidadException` transporta el nombre del campo: sin él, el mensaje aparecería
en el resumen general en lugar de junto al control que el usuario debe corregir.

**Solo se capturan las excepciones previstas.** Cualquier otra se deja propagar para que la maneje
la página de error. Ocultarla aquí convertiría un fallo real en un mensaje de formulario engañoso.

---

## Diseño adaptable

Tres puntos de ruptura, definidos por dónde el contenido deja de caber y no por dispositivos
concretos:

| Ancho | Comportamiento |
|---|---|
| > 900 px | Rejillas de hasta cuatro columnas |
| 760–900 px | Rejillas de dos columnas |
| < 760 px | Una columna, menú desplegable, tablas con desplazamiento horizontal |

Las tablas se envuelven en un contenedor con desplazamiento horizontal en lugar de ocultar columnas:
en un listado de licitaciones, todas las columnas son información que el usuario puede necesitar.

---

## Accesibilidad

| Práctica | Implementación |
|---|---|
| Enlace para saltar al contenido | Primer elemento de la página, visible al enfocar |
| Etiquetas asociadas | Todo control tiene su `<label>` |
| Textos alternativos | Los iconos decorativos llevan `aria-hidden`; los funcionales, `aria-label` |
| Roles y regiones | `role="status"` en avisos, `role="alert"` en errores |
| Contraste | Ambos temas cumplen la relación mínima de contraste para texto |
| Navegación por teclado | Contorno de foco visible en todos los controles interactivos |
| Movimiento reducido | Se respeta la preferencia del sistema |
| Descripción de tablas | `<caption>` para lectores de pantalla |

---

## Prevención del doble envío

Un doble clic en «Registrar oferta» podría crear dos ofertas. Se atiende en dos niveles:

1. **Cliente:** los formularios marcados con `data-una-vez` bloquean visualmente el botón tras el
   primer envío. No se usa el atributo `disabled` porque impediría que el valor del botón viajara en
   el envío; se usa `aria-disabled` y se desactivan los eventos de puntero.
2. **Servidor:** el índice único compuesto de ofertas rechaza el duplicado aunque los dos envíos
   lleguen.

---

## Pruebas

| Prueba | Verifica |
|---|---|
| `InterfazPruebas.LandingPage_*` | Contenido exigido por la sección 5.1 |
| `InterfazPruebas.Menu_OfreceTodasLasSecciones*` | Las siete entradas del menú |
| `InterfazPruebas.ModoOscuro_*` | Activación y persistencia entre páginas |
| `InterfazPruebas.AlternarMoneda_*` | Conversión sin alterar datos |
| `InterfazPruebas.DisenoAdaptable_*` | Menú desplegable en pantalla angosta |
| `FlujoCompletoPruebas.*` | Flujo completo y mensajes junto al campo |

---

## Archivos

| Archivo | Contenido |
|---|---|
| `Web/Controladores/*.cs` | Controladores MVC |
| `Web/Modelos/*.cs` | Modelos de vista y de formulario |
| `Web/Servicios/PreferenciasUsuario.cs` | Tema y moneda en cookies |
| `Web/Servicios/FormateadorMonto.cs` | Formato monetario cultural |
| `Web/Servicios/FormateadorFecha.cs` | Conversión de zona horaria |
| `Web/Views/**/*.cshtml` | Vistas Razor |
| `Web/wwwroot/css/sitio.css` | Hoja de estilos propia |
| `Web/wwwroot/js/sitio.js` | Guiones propios |
