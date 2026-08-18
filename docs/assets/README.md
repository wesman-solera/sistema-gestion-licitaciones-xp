# Recursos gráficos de la documentación

Los diagramas principales del proyecto están escritos en **Mermaid**, directamente dentro de los
documentos Markdown que los usan:

| Documento | Diagramas |
|---|---|
| [`../arquitectura-general.md`](../arquitectura-general.md) | Capas y dependencias, flujo de una petición, manejo de errores |
| [`../modelo-datos.md`](../modelo-datos.md) | Diagrama entidad-relación |
| [`../integracion-modulos.md`](../integracion-modulos.md) | Mapa de módulos y tres flujos de extremo a extremo |

**Por qué Mermaid y no imágenes.** Un diagrama en Mermaid vive junto al texto que explica, se
versiona como código y se puede corregir en la misma revisión que cambia la implementación. Una
imagen exportada se desactualiza en cuanto el diseño evoluciona, y nadie nota la diferencia hasta
que alguien la mira con atención meses después.

Esta carpeta contiene los recursos que no pueden expresarse en Mermaid.

| Archivo | Contenido |
|---|---|
| `ciclo-estados.svg` | Ciclo de vida de una licitación, con las transiciones prohibidas marcadas |
