# Visión y alcance

## Propósito

Una organización que compra bienes y servicios necesita un proceso trazable para pedir ofertas al
mercado y elegir entre ellas. Sin un sistema, ese proceso vive en hojas de cálculo y correos: los
montos se comparan a mano, no queda claro quién debe autorizar una adjudicación, y una oferta
recibida después del cierre puede colarse sin que nadie lo note.

Este sistema resuelve ese problema. Registra el proceso completo, aplica las reglas de forma
uniforme y deja evidencia de cada paso.

## Alcance funcional

### Dentro del alcance

| Capacidad | Descripción |
|---|---|
| Gestión de proveedores | Alta, consulta, edición y eliminación, con nombre único normalizado |
| Gestión de licitaciones | Alta, consulta, edición, cambio de estado y eliminación, con código único |
| Ciclo de vida controlado | Borrador → Publicada → Cerrada, con transiciones restringidas |
| Registro de ofertas | Una oferta por proveedor y licitación, validada contra presupuesto y vencimiento |
| Evaluación automática | Mejor oferta, porcentaje de ahorro y clasificación cualitativa |
| Niveles de aprobación | Tabla parametrizable de rangos de monto y responsables |
| Conversión monetaria | Visualización en dólares calculada con un tipo de cambio administrable |
| Interfaz web | Landing page, CRUD completo, modo claro y oscuro, diseño adaptable |
| API REST | Operaciones equivalentes, versionadas y documentadas con OpenAPI |
| Despliegue | Docker Compose para desarrollo, Kubernetes para el entorno de destino |

### Fuera del alcance

Estas exclusiones son deliberadas. El principio de **diseño simple** de XP pide implementar la
solución más sencilla que satisfaga las historias vigentes, sin complejidad especulativa.

| Fuera del alcance | Razón |
|---|---|
| Autenticación y autorización de usuarios | Ninguna historia del enunciado lo requiere |
| Notificaciones por correo | No forma parte del alcance funcional definido |
| Consulta automática del tipo de cambio | El enunciado exige que el sistema funcione sin Internet |
| Firma digital de ofertas | No solicitado |
| Adjuntos y documentos por licitación | No solicitado |
| Multimoneda más allá de CRC y USD | El enunciado define exactamente dos monedas |
| Auditoría con historial de cambios por campo | Solo se exigen `CreatedAt`, `UpdatedAt` y `DeletedAt` |

Si alguna de estas capacidades se necesitara después, entraría como una historia nueva en una
iteración posterior. Anticiparlas ahora agregaría código sin cliente que lo pida.

## Actores

| Actor | Descripción |
|---|---|
| Encargado de compras | Registra proveedores y licitaciones, publica, registra ofertas y consulta resultados |
| Administrador de parámetros | Mantiene la tabla de niveles de aprobación y el tipo de cambio |
| Consumidor de la API | Sistema externo que integra las mismas operaciones por HTTP |

El sistema no distingue estos roles técnicamente, porque no hay historia de autenticación. La
distinción es funcional y sirve para entender quién usa cada pantalla.

## Reglas de negocio principales

Se enumeran aquí en resumen; el detalle y su implementación están en los documentos de cada módulo.

1. **Unicidad normalizada.** El código de licitación y el nombre de proveedor son únicos
   ignorando espacios sobrantes y diferencias de mayúsculas.
2. **Ciclo de estados restringido.** Una licitación publicada no vuelve a Borrador; una cerrada no
   se reabre sin autorización expresa.
3. **Vencimiento efectivo.** Alcanzada la fecha de cierre, la licitación queda cerrada
   funcionalmente aunque su columna de estado no se haya actualizado.
4. **Oferta única por proveedor.** Un proveedor presenta como máximo una oferta por licitación.
5. **Oferta acotada por el presupuesto.** Una oferta igual al presupuesto es válida; una superior
   se rechaza.
6. **Presupuesto no reducible.** El presupuesto no puede quedar por debajo de una oferta ya
   registrada.
7. **Mejor oferta.** Es la de menor monto en colones; en empate gana la registrada primero.
8. **Clasificación del ahorro.** 10 % o más es conveniente; entre 0 % y 10 % es aceptable; 0 % es
   válida sin ahorro; sin ofertas se reporta como tal.
9. **Aprobador parametrizable.** Se obtiene de una tabla de rangos, no de condiciones fijas.
10. **Colón como fuente de verdad.** El valor en dólares es siempre calculado y nunca persistido.

## Glosario

| Término | Significado |
|---|---|
| **Licitación** | Proceso de compra publicado para recibir ofertas |
| **Oferta** | Propuesta económica de un proveedor para una licitación |
| **Mejor oferta** | Oferta válida de menor monto en colones |
| **Ahorro** | Diferencia porcentual entre el presupuesto y la mejor oferta |
| **Nivel de aprobación** | Rango de montos con el cargo responsable de autorizarlo |
| **Rango abierto** | Rango de aprobación sin monto máximo; solo puede existir uno |
| **Cerrada funcionalmente** | Licitación cuya fecha de cierre ya pasó, con independencia de su estado persistido |
| **Forma normalizada** | Versión de un texto usada para comparar unicidad |
| **CRC** | Colón costarricense, moneda oficial del sistema |
| **USD** | Dólar estadounidense, moneda de visualización alternativa |

## Criterios de éxito

El proyecto se considera terminado cuando:

- El flujo funcional mínimo de la sección 5.3 del enunciado se completa desde el navegador y desde
  la API REST.
- Las tres suites de pruebas pasan y la cobertura alcanza los umbrales exigidos.
- `docker compose up --build` levanta la solución sin pasos manuales.
- Los manifiestos de Kubernetes despliegan la aplicación y la base con persistencia comprobada.
- La integración continua termina satisfactoriamente en la entrega etiquetada.
- Cada elemento evaluado se puede rastrear hasta una historia, una prueba, commits y documentación.
