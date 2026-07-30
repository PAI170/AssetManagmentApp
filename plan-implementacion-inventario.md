# Aplicación de Inventario — Plan de Implementación

**Stack:** ASP.NET Core + Blazor Server + Entity Framework Core + MariaDB + QuestPDF (mismo stack de RodcastInvoiceApp, pero **aplicación totalmente separada**: base de datos propia, contenedor propio, dominio de Cloudflare propio)
**Deployment:** Docker + Cloudflare Tunnel
**Moneda:** Colones (₡)

Referencia del modelo de datos completo: `diseno-app-inventario.md`

Cliente y diseño del PDF quedan pendientes para después — no bloquean el resto del desarrollo.

---

## Fase 0 — Setup del proyecto

- [ ] Crear solución ASP.NET Core + Blazor Server nueva e independiente (proyecto, repo y base de datos propios — no comparte nada con RodcastInvoiceApp)
- [ ] Configurar EF Core + conexión a su propia base MariaDB
- [ ] Configurar autenticación y esquema de usuarios/roles propio de esta app
- [ ] Configurar su propio contenedor Docker y su propio dominio en Cloudflare Tunnel

---

## Fase 1 — Modelo de datos y migraciones

- [ ] `Usuarios` (id, nombre, correo, password_hash, rol, activo)
- [ ] `Proyectos` (id, nombre, direccion, ingeniero_a_cargo [texto libre], fecha_creacion, estado)
- [ ] `TiposEquipo` (id, nombre, descripcion)
- [ ] `HistorialPrecioTipoEquipo` (id, tipo_equipo_id, precio, vigente_desde, vigente_hasta)
- [ ] `Activos` (id, placa, tipo_equipo_id, estado, proyecto_actual_id, fecha_registro)
- [ ] `Movimientos` (id, activo_id, proyecto_id, tipo_movimiento, estado_anterior, estado_nuevo, fecha_movimiento, usuario_id, observacion)
- [ ] `AsignacionActivoProyecto` (id, activo_id, proyecto_id, fecha_ingreso, fecha_salida, fecha_ultimo_cobro)
- [ ] `Proformas` (id, numero, proyecto_id, fecha_generacion, periodo_desde, periodo_hasta, total, usuario_genero_id, enviada_por_correo, estado)
- [ ] `ProformaDetalle` (id, proforma_id, activo_id, tipo_equipo_nombre, precio_mensual_usado, dias_cobrados, subtotal)
- [ ] Migración inicial + datos semilla (roles, un usuario admin)

---

## Fase 2 — Catálogo y gestión de equipo

- [ ] CRUD de `TiposEquipo` (con historial de precios: al editar precio, cerrar el registro anterior y crear uno nuevo, nunca sobreescribir)
- [ ] CRUD de `Activos` (alta con placa única, tipo, estado inicial `Disponible`)
- [ ] Vista de inventario: lista de activos con filtro por estado, tipo de equipo, proyecto actual
- [ ] Regla de validación: no permitir asignar un activo si su estado no es `Disponible`

---

## Fase 3 — Proyectos

- [ ] CRUD de `Proyectos` (nombre, dirección, ingeniero a cargo, estado)
- [ ] Vista de detalle de proyecto: equipo actualmente asignado, historial de movimientos, historial de proformas, total gastado a la fecha

---

## Fase 4 — Movimientos (asignación / retorno / cambio de estado)

- [ ] Acción "Asignar a proyecto": selecciona activo(s) disponible(s) + proyecto → crea `Movimiento`, actualiza `Activo.estado = Asignado`, crea/abre registro en `AsignacionActivoProyecto`
- [ ] Acción "Retornar a bodega": cierra `AsignacionActivoProyecto.fecha_salida`, actualiza `Activo.estado = Disponible`, crea `Movimiento`
- [ ] Acción "Cambiar estado" (ej. a `En Reparación` o `Dañado`): registra `Movimiento` con observación, actualiza `Activo.estado`
- [ ] Vista de historial de movimientos con filtros (por activo, por proyecto, por usuario, por rango de fechas, por tipo de movimiento)
- [ ] Restringir estas acciones al rol `Admin` (Consultor solo lectura)

---

## Fase 5 — Motor de facturación (proformas)

- [ ] Job/función de cálculo: para un proyecto, identificar todas las `AsignacionActivoProyecto` con `fecha_ultimo_cobro < fecha_corte`
- [ ] Calcular días a cobrar por activo (desde `fecha_ultimo_cobro`/`fecha_ingreso` hasta `fecha_corte`/`fecha_salida`)
- [ ] Lógica de tarifa diaria: `precio_mensual / días_reales_del_mes_correspondiente`
- [ ] Lógica de split por cambio de precio a mitad de periodo (dos líneas si `HistorialPrecioTipoEquipo` cambió dentro del rango)
- [ ] Generar `Proforma` + `ProformaDetalle`, número consecutivo `AAAAMM###` (reinicia a 001 cada mes, global)
- [ ] Actualizar `fecha_ultimo_cobro` de cada asignación incluida al confirmar la proforma
- [ ] Acción "Anular proforma" (marca `estado = Anulada`, número queda quemado, no se reutiliza ni se borra)
- [ ] Botón "Generar Proforma" en la vista de proyecto, con vista previa del detalle antes de confirmar
- [ ] Restringir generación/anulación al rol `Admin`

---

## Fase 6 — PDF y envío por correo

- [ ] Generar PDF de la proforma con QuestPDF (diseño exacto: pendiente)
- [ ] Integrar envío por correo (mismo mecanismo que RodcastInvoiceApp)
- [ ] Marcar `enviada_por_correo = true` al enviar
- [ ] Historial de proformas por proyecto con opción de reenviar/descargar PDF

---

## Fase 7 — Dashboard

- [ ] Total gastado por proyecto a la fecha (suma de proformas no anuladas)
- [ ] % de inventario disponible / asignado / en reparación / dañado
- [ ] Filtros globales (proyecto, tipo de equipo, estado, rango de fechas)

---

## Fase 8 — Roles y permisos

- [ ] Rol `Admin`: acceso completo
- [ ] Rol `Consultor`: solo lectura — inventario/disponibilidad, historial de movimientos, costos, historial de proformas. Sin acceso a mover equipo, generar/anular proformas, ni editar catálogo

---

## Pendiente para después (no bloquea el desarrollo)

- [ ] Definir si `Proyectos` va a llevar datos de cliente
- [ ] Diseño visual/formato del PDF de la proforma
- [ ] Confirmar si el ingeniero a cargo y otros datos requieren campos adicionales una vez se defina lo del cliente
