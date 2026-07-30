# Aplicación de Inventario — Plan de Implementación

**Stack:** ASP.NET Core + Blazor Server + Entity Framework Core + MariaDB + QuestPDF (mismo stack de RodcastInvoiceApp, pero **aplicación totalmente separada**: base de datos propia, contenedor propio, dominio de Cloudflare propio)
**Deployment:** Docker + Cloudflare Tunnel
**Moneda:** Colones (₡)

Referencia del modelo de datos completo: `diseno-app-inventario.md`

Cliente y diseño del PDF quedan pendientes para después — no bloquean el resto del desarrollo.

---

## Fase 0 — Setup del proyecto

- [x] Crear solución ASP.NET Core + Blazor Server nueva e independiente (proyecto, repo y base de datos propios — no comparte nada con RodcastInvoiceApp)
- [x] Configurar EF Core + conexión a su propia base MariaDB
- [x] Configurar autenticación y esquema de usuarios/roles propio de esta app
- [x] Configurar su propio contenedor Docker y su propio dominio en Cloudflare Tunnel

---

## Fase 1 — Modelo de datos y migraciones

- [x] `Usuarios` (id, nombre, correo, password_hash, rol, activo)
- [x] `Proyectos` (id, nombre, direccion, ingeniero_a_cargo [texto libre], fecha_creacion, estado)
- [x] `TiposEquipo` (id, nombre, descripcion)
- [x] `HistorialPrecioTipoEquipo` (id, tipo_equipo_id, precio, vigente_desde, vigente_hasta)
- [x] `Activos` (id, placa, tipo_equipo_id, estado, proyecto_actual_id, fecha_registro)
- [x] `Movimientos` (id, activo_id, proyecto_id, tipo_movimiento, estado_anterior, estado_nuevo, fecha_movimiento, usuario_id, observacion)
- [x] `AsignacionActivoProyecto` (id, activo_id, proyecto_id, fecha_ingreso, fecha_salida, fecha_ultimo_cobro)
- [x] `Proformas` (id, numero, proyecto_id, fecha_generacion, periodo_desde, periodo_hasta, total, usuario_genero_id, enviada_por_correo, estado)
- [x] `ProformaDetalle` (id, proforma_id, activo_id, tipo_equipo_nombre, precio_mensual_usado, dias_cobrados, subtotal)
- [x] Migración inicial + datos semilla (roles, un usuario admin)

> Nota: `precio_mensual` se renombró a `precio_por_dia` en el código y la BD (ver Fase 5) — el modelo de negocio real es precio por día × días, sin prorrateo mensual.

---

## Fase 2 — Catálogo y gestión de equipo

- [x] CRUD de `TiposEquipo` (con historial de precios: al editar precio, cerrar el registro anterior y crear uno nuevo, nunca sobreescribir)
- [x] CRUD de `Activos` (alta con placa única, tipo, estado inicial `Disponible`)
- [x] Vista de inventario: lista de activos con filtro por estado, tipo de equipo, proyecto actual
- [x] Regla de validación: no permitir asignar un activo si su estado no es `Disponible`

---

## Fase 3 — Proyectos

- [x] CRUD de `Proyectos` (nombre, dirección, ingeniero a cargo, estado)
- [x] Vista de detalle de proyecto: equipo actualmente asignado, historial de movimientos, historial de proformas, total gastado a la fecha

---

## Fase 4 — Movimientos (asignación / retorno / cambio de estado)

- [x] Acción "Asignar a proyecto": selecciona activo(s) disponible(s) + proyecto → crea `Movimiento`, actualiza `Activo.estado = Asignado`, crea/abre registro en `AsignacionActivoProyecto`
- [x] Acción "Retornar a bodega": cierra `AsignacionActivoProyecto.fecha_salida`, actualiza `Activo.estado = Disponible`, crea `Movimiento`
- [x] Acción "Cambiar estado" (ej. a `En Reparación` o `Dañado`): registra `Movimiento` con observación, actualiza `Activo.estado`
- [x] Vista de historial de movimientos con filtros (por activo, por proyecto, por usuario, por rango de fechas, por tipo de movimiento)
- [x] Restringir estas acciones al rol `Admin` (Consultor solo lectura)

> Nota de implementación: login/logout mínimo (cookie auth) se construyó en esta fase — era requisito previo pa' saber qué usuario hace cada movimiento y poder aplicar el rol.

---

## Fase 5 — Motor de facturación (proformas)

- [x] Job/función de cálculo: para un proyecto, identificar todas las `AsignacionActivoProyecto` con `fecha_ultimo_cobro < fecha_corte`
- [x] Calcular días a cobrar por activo (desde `fecha_ultimo_cobro`/`fecha_ingreso` hasta `fecha_corte`/`fecha_salida`)
- [x] Lógica de tarifa: `precio_por_dia × días_cobrados` (confirmado con el cliente: no hay prorrateo mensual, el precio registrado ya es la tarifa diaria)
- [x] Lógica de split por cambio de precio a mitad de periodo (dos líneas si `HistorialPrecioTipoEquipo` cambió dentro del rango)
- [x] Generar `Proforma` + `ProformaDetalle`, número consecutivo `AAAAMM###` (reinicia a 001 cada mes, global)
- [x] Actualizar `fecha_ultimo_cobro` de cada asignación incluida al confirmar la proforma
- [x] Acción "Anular proforma" (marca `estado = Anulada`, número queda quemado, no se reutiliza ni se borra)
- [x] Botón "Generar Proforma" en la vista de proyecto, con vista previa del detalle antes de confirmar
- [x] Restringir generación/anulación al rol `Admin`

---

## Fase 6 — PDF y envío por correo

- [x] Generar PDF de la proforma con QuestPDF (diseño básico funcional: encabezado, datos de proyecto, tabla de líneas, total — diseño final/logo pendiente de definir)
- [ ] Integrar envío por correo — **pendiente**: falta que el cliente confirme el mecanismo (SMTP propio, SendGrid, u otro; no se pudo replicar "mismo mecanismo que RodcastInvoiceApp" porque no hay acceso a ese código)
- [ ] Marcar `enviada_por_correo = true` al enviar — depende del punto anterior
- [x] Historial de proformas por proyecto con opción de descargar PDF (reenviar por correo queda pendiente junto con el punto anterior)

---

## Fase 7 — Dashboard

- [x] Total gastado por proyecto a la fecha (suma de proformas no anuladas)
- [x] % de inventario disponible / asignado / en reparación / dañado
- [x] Filtros globales (proyecto, tipo de equipo, estado, rango de fechas)

---

## Fase 8 — Roles y permisos

- [x] Rol `Admin`: acceso completo
- [x] Rol `Consultor`: solo lectura — inventario/disponibilidad, historial de movimientos, costos, historial de proformas. Sin acceso a mover equipo, generar/anular proformas, ni editar catálogo
- [x] Toda la app requiere login (antes se podía ver en modo lectura sin cuenta; se cerró ese acceso anónimo)

---

## Pendiente para después (no bloquea el desarrollo)

- [ ] Definir si `Proyectos` va a llevar datos de cliente
- [ ] Diseño visual/formato del PDF de la proforma (logo, formato exacto)
- [ ] Confirmar si el ingeniero a cargo y otros datos requieren campos adicionales una vez se defina lo del cliente
- [ ] **Mecanismo de envío de correo** (SMTP/SendGrid/otro) — bloquea el resto de la Fase 6
