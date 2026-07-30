# Aplicación de Inventario — Rodcast Solutions
## Modelo de Datos v1

---

## 1. Usuarios
| Campo | Tipo | Notas |
|---|---|---|
| id | int (PK) | |
| nombre | string | |
| correo | string | único |
| password_hash | string | |
| rol | enum | `Admin`, `Consultor` |
| activo | bool | para desactivar sin borrar |

**Permisos:**
- **Admin:** todo (mover equipos, generar proformas, gestionar catálogo, usuarios).
- **Consultor:** solo lectura — historial de movimientos, costos, inventario (disponibilidad), historial de proformas. No puede mover equipo ni generar proformas.

---

## 2. Proyectos
| Campo | Tipo | Notas |
|---|---|---|
| id | int (PK) | |
| nombre | string | |
| direccion | string | |
| ingeniero_a_cargo | string | texto libre, no está ligado a un usuario del sistema |
| fecha_creacion | date | |
| estado | enum | `Activo`, `Cerrado` |
| cliente_info | — | **pendiente**, se agrega cuando se defina |

---

## 3. TiposEquipo (catálogo)
| Campo | Tipo | Notas |
|---|---|---|
| id | int (PK) | |
| nombre | string | ej. "Vibradora", "Vibradora Eléctrica", "Andamio", "Mezcladora de Cemento" |
| precio_mensual | decimal | en colones — **lista pendiente** |
| descripcion | string | opcional, para diferenciar tipos similares |

Cada tipo es una entrada independiente aunque haga lo mismo que otro (vibradora ≠ vibradora eléctrica), porque cada uno tiene su propio precio.

### 3.1 HistorialPrecioTipoEquipo
| Campo | Tipo | Notas |
|---|---|---|
| id | int (PK) | |
| tipo_equipo_id | int (FK → TiposEquipo) | |
| precio | decimal | |
| vigente_desde | date | |
| vigente_hasta | date, nullable | null = precio actual |

Cuando se actualiza el precio de un `TipoEquipo`, no se sobreescribe — se cierra el registro anterior (`vigente_hasta`) y se crea uno nuevo. Esto permite dividir el cobro en dos tarifas si el cambio de precio cae a mitad de un periodo facturado.

---

## 4. Activos (equipo físico individual)
| Campo | Tipo | Notas |
|---|---|---|
| id | int (PK) | |
| placa | string | único, código físico de identificación |
| tipo_equipo_id | int (FK → TiposEquipo) | |
| estado | enum | `Disponible`, `Asignado`, `En Reparación`, `Dañado` |
| proyecto_actual_id | int (FK → Proyectos, nullable) | null si está en bodega |
| fecha_registro | date | |

**Regla:** si `estado` es `En Reparación` o `Dañado`, el activo NO puede asignarse a un proyecto (no está disponible). Solo se puede asignar si `estado = Disponible`.

---

## 5. Movimientos (historial unificado + log)
| Campo | Tipo | Notas |
|---|---|---|
| id | int (PK) | |
| activo_id | int (FK → Activos) | |
| proyecto_id | int (FK → Proyectos, nullable) | null si es retorno a bodega |
| tipo_movimiento | enum | `Asignación`, `Retorno a bodega`, `Cambio de estado` |
| estado_anterior | enum | |
| estado_nuevo | enum | |
| fecha_movimiento | datetime | |
| usuario_id | int (FK → Usuarios) | quién hizo el movimiento |
| observacion | string | notas, daños, pérdidas, etc. |

Cada vez que un activo entra/sale de un proyecto o cambia de estado, se crea un registro aquí. Esta tabla es a la vez el historial de movimientos y el log del sistema.

---

## 6. AsignacionActivoProyecto (control de facturación)
Tabla auxiliar para saber qué días ya se cobraron y cuáles no, evitando doble cobro cuando un equipo pasa varios meses en el mismo proyecto.

| Campo | Tipo | Notas |
|---|---|---|
| id | int (PK) | |
| activo_id | int (FK → Activos) | |
| proyecto_id | int (FK → Proyectos) | |
| fecha_ingreso | date | |
| fecha_salida | date, nullable | null si sigue en el proyecto |
| fecha_ultimo_cobro | date | avanza cada vez que se genera una proforma que incluye este activo |

---

## 7. Proformas
| Campo | Tipo | Notas |
|---|---|---|
| id | int (PK) | |
| numero | string | formato `AAAAMM###` ej. `202608001` — consecutivo reinicia a 001 cada mes, global (no por proyecto) |
| proyecto_id | int (FK → Proyectos) | |
| fecha_generacion | date | |
| periodo_desde | date | |
| periodo_hasta | date | normalmente el 24 del mes |
| total | decimal | colones |
| usuario_genero_id | int (FK → Usuarios) | |
| enviada_por_correo | bool | |
| estado | enum | `Generada`, `Anulada` — si se anula, el número queda quemado (no se reutiliza) |

---

## 8. ProformaDetalle (líneas de la proforma)
| Campo | Tipo | Notas |
|---|---|---|
| id | int (PK) | |
| proforma_id | int (FK → Proformas) | |
| activo_id | int (FK → Activos) | |
| tipo_equipo_nombre | string | **copia/snapshot**, no referencia viva |
| precio_mensual_usado | decimal | **snapshot** del precio al momento de facturar |
| dias_cobrados | int | días no cobrados aún, hasta la fecha de corte |
| subtotal | decimal | `(precio_mensual_usado / dias_reales_del_mes) * dias_cobrados` — el divisor son los días reales del mes en curso (28-31), no un fijo 30 |

---

## Lógica del botón "Generar Proforma"

1. Se elige un proyecto.
2. El sistema busca en `AsignacionActivoProyecto` todos los activos con `fecha_ultimo_cobro < fecha_corte (24)`.
3. Para cada uno, calcula los días entre `fecha_ultimo_cobro` (o `fecha_ingreso` si es la primera vez) y `fecha_corte` (o `fecha_salida` si el equipo ya salió antes del corte).
4. Si dentro de ese rango de días hubo un cambio de precio (ver `HistorialPrecioTipoEquipo`), se generan **dos líneas** en `ProformaDetalle`: una con el precio anterior por los días antes del cambio, y otra con el precio nuevo por los días restantes.
5. La facturación no depende del estado físico real del equipo — un activo sigue asignado y se sigue cobrando hasta que alguien registre formalmente un cambio de estado (ej. a `Dañado`), aunque el daño haya ocurrido antes y no se haya reportado a tiempo.
6. Suma el total, crea el registro en `Proformas` con el siguiente número consecutivo del mes.
7. Actualiza `fecha_ultimo_cobro` de cada asignación incluida.
8. Genera el PDF y opcionalmente lo envía por correo.

---

## Dashboard (propuesto)

- Total gastado por proyecto a la fecha (suma de proformas, excluyendo anuladas).
- % de inventario disponible vs asignado vs en reparación/dañado.
- Filtros por: proyecto, tipo de equipo, estado, rango de fechas.

---

## Preguntas abiertas

1. **Datos del cliente en Proyectos** — pendiente, se define después.
2. **Diseño del PDF de la proforma** — pendiente, se define después.
