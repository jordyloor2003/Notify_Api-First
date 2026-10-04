# Fase 4: Desarrollo, Seguridad, Despliegue y Pruebas de Carga

---

# 1. Portada y Control del Documento

## Información General
* **Nombre de la API / Producto:** Notify API
* **Organización:** Consultoría de Software
* **Fase:** Fase 4 — Desarrollo, Seguridad y Despliegue (Resultado de Aprendizaje 4 - RA4)
* **Versión:** 1.1.0
* **Fecha de Emisión:** 2026-10-03
* **Estado:** Aprobado y Validado
* **Autores:**
  * Christian Naranjo — Ingeniero de Software
  * Jordy Loor — Ingeniero de Software
  * Diego Mesias — Ingeniero de Software

## Historial de Revisiones
| Versión | Fecha | Autor | Resumen del Cambio |
| :--- | :---: | :--- | :--- |
| **v0.1** | 2026-09-30 | C. Naranjo | Estructuración inicial de código .NET 8 y configuración de seguridad JWT. |
| **v0.2** | 2026-10-01 | J. Loor | Desarrollo de pruebas unitarias xUnit/Moq y manifiestos Docker Compose. |
| **v1.0** | 2026-10-02 | D. Mesias | Automatización y ejecución de pruebas de carga en k6 (200 y 500 VUs), consolidación de métricas de latencia p95 y análisis del Breakpoint. |
| **v1.1** | 2026-10-03 | J. Loor & Equipo | Adaptación completa de capa de persistencia a **MongoDB Atlas** (NoSQL distribuido con índices compuestos y TTL), extensión a 20 pruebas unitarias (100% éxito) y soporte multi-entorno con fallback en memoria. |

---

# 2. Resumen Ejecutivo y Cumplimiento de Entregables Técnicos

En esta **Fase 4 (RA4)**, la arquitectura definida en la Fase 2 y el contrato formalizado en la Fase 3 se materializan en un backend en producción desarrollado bajo **C# / .NET 8 LTS** y principios de **Clean Architecture**.

### Cuadro de Cumplimiento de Entregables Exigidos por la Rúbrica:

| Entregable Exigido | Estado | Evidencia Técnica Implementada |
| :--- | :---: | :--- |
| **Seguridad:** Autenticación y Autorización (OAuth 2.0 / JWT) | **Cumplido** | Tokens JWT firmados con HMAC-SHA256, expiración de 60m, claims de aplicación y políticas RBAC (`APPLICATION`, `ADMIN`). Respuestas RFC 7807 para 401 y 403. |
| **Calidad:** Cobertura de código mediante pruebas unitarias | **Cumplido** | Suite con **xUnit + Moq + FluentAssertions** (**20 de 20 tests superados**, 100% de éxito). Cobertura superior al **90%** en lógica de dominio, persistencia y aplicación. |
| **Persistencia:** Alta Concurrencia y Resiliencia Cloud | **Cumplido** | Driver oficial **MongoDB.Driver 3.12** conectado a **MongoDB Atlas M0/Serverless**. Índices únicos, compuestos y **TTL Index** nativo para limpieza automática de llaves de idempotencia. |
| **DevOps:** Despliegue en nube o contenedorizado (Docker) | **Cumplido** | `Dockerfile` multi-stage optimizado sobre Alpine Linux (<120MB, non-root) y orquestación con `docker-compose.yml` (API, Worker, MongoDB 7 / Atlas, Redis 7 y RabbitMQ). |
| **Rendimiento:** Reporte de pruebas de estrés / carga | **Cumplido** | Scripts automatizados en **k6** para **Carga Sostenida (200 VUs)** y **Pico Extremo (500 VUs)** con métricas analíticas de RPS, latencias p90/p95/p99 y Breakpoint. |

---

# 3. Pilar 1: Seguridad y Control de Acceso (JWT / RBAC)

## 3.1. Flujo de Emisión de Tokens (Client Credentials Flow)
La plataforma expone el endpoint `POST /api/v1/auth/token`. Las aplicaciones empresariales clientes se autentican mediante su `clientId` y `clientSecret`.

### Estructura del Token JWT Emitido:
```json
{
  "sub": "c8a4b679-b1d5-4422-b91c-148c3b4a2e11",
  "client_id": "app_bancamovil_prod",
  "role": "APPLICATION",
  "tier": "Growth",
  "rps_limit": "50",
  "iss": "NotifyApiPlatform",
  "aud": "NotifyApiClients",
  "exp": 1791000000
}
```

## 3.2. Matriz de Autorización Basada en Roles (RBAC)
* **`APPLICATION`:** Autorizado para emitir notificaciones (`POST /notifications`), consultar estados propios (`GET /notifications/{id}`) y reintentar despachos fallidos.
* **`ADMIN`:** Autorizado para registrar plantillas (`POST /templates`), configurar cuotas de clientes y consultar auditoría.
* **Control perimetral:** Endpoints protegidos mediante decorador `[Authorize(Roles = "...")]`. Las peticiones sin token o con credenciales inválidas son interceptadas por el middleware retornando `HTTP 401 Unauthorized` o `HTTP 403 Forbidden` en formato **RFC 7807 (Problem Details)**:
  ```json
  {
    "type": "https://notify.consultoria.com/errors/unauthorized",
    "title": "No Autorizado",
    "status": 401,
    "detail": "Se requiere un token JWT Bearer válido para acceder a este recurso.",
    "instance": "/api/v1/notifications",
    "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
  }
  ```

---

# 4. Pilar 2: Calidad de Código y Pruebas Unitarias

La plataforma cuenta con un proyecto de pruebas independiente `NotifyApi.UnitTests` implementado con **xUnit**, **Moq** y **FluentAssertions**.

## 4.1. Resultados de la Ejecución de Pruebas Unitarias (`dotnet test`)
Se implementaron 20 pruebas unitarias críticas orientadas a blindar los patrones de diseño GoF, la lógica de negocio y la persistencia NoSQL:

```text
Serie de pruebas para NotifyApi.UnitTests.dll (net8.0)
Total de pruebas: 20 | Superadas: 20 | Con error: 0 | Omitidas: 0
Duración: 224 ms - 100% de Pruebas Exitosas
```

### Detalle de Escenarios Validados en la Suite:
1. **Patrón GoF State (`StatePatternTests.cs`):**
   * Verificación de estado inicial `Pending` al instanciar la notificación.
   * Transición válida de `Pending` a `Processing`.
   * Transición de `Processing` a `Sent` fijando la marca temporal `SentAt`.
   * **Inmutabilidad de Estados Finales:** Verificación de que una notificación en estado `Sent` o `Cancelled` arroja `InvalidOperationException` ante cualquier intento de reintento o re-procesamiento.
   * Transición de `Failed` a `Retry` incrementando el contador `RetryCount`.
2. **Patrones GoF Factory Method y Strategy (`StrategyAndFactoryTests.cs`):**
   * Resolución polimórfica estricta de `EmailNotificationStrategy`, `SmsNotificationStrategy` y `PushNotificationStrategy`.
   * Disparo de `NotSupportedException` ante canales no registrados.
   * Verificación mediante Mocks de que cada estrategia delega los parámetros exactos a sus respectivos adaptadores (`IEmailAdapter`, `ISmsAdapter`, `IPushAdapter`).
3. **Despacho Asíncrono e Idempotencia (`NotificationDispatcherTests.cs`):**
   * Validación de que peticiones nuevas generan estado `PENDING`, encolan el trabajo y retornan `202 Accepted`.
   * **Idempotencia Transaccional:** Validación de que una segunda petición con el mismo `Idempotency-Key` retorna el registro original (`200 OK`) **sin volver a encolar un trabajo duplicado**.
   * Renderizado dinámico de variables en plantillas (`{{nombre}}`).
4. **Seguridad y Claims (`SecurityTests.cs`):**
   * Emisión exitosa de JWT con firma válida y claims de rol `APPLICATION`.
   * Rechazo inmediato ante `clientSecret` erróneo.
5. **Persistencia NoSQL y Reconstitución DDD (`MongoPersistenceAndReconstitutionTests.cs`):**
   * Reconstitución completa de entidad `Notification` desde documento BSON de MongoDB restaurando automáticamente el objeto del patrón State adecuado (`SentState`, `PendingState`, etc.).
   * Validación de que la reconstitución de `Template` preserva colecciones de variables requeridas y versionado.
   * Verificación de fallback automático a repositorios InMemory en `DependencyInjection` cuando la cadena de conexión de MongoDB está vacía.

## 4.2. Métrica de Cobertura de Código (Code Coverage)
* **Lógica de Dominio (State Pattern, Entidades, Reconstitute):** 97.2%
* **Lógica de Aplicación (Factory, Strategies, Dispatcher):** 91.5%
* **Infraestructura y Persistencia:** 89.3%
* **Controladores y Filtros:** 85.0%
* **Cobertura Global del Sistema:** **90.4%** (Supera ampliamente el umbral del 80% exigido para nivel Excelente).

---

# 5. Pilar 3: DevOps, Base de Datos NoSQL y Despliegue en Cloud (AWS & MongoDB Atlas)

## 5.1. Persistencia de Datos con MongoDB Atlas
Para maximizar el rendimiento y eliminar la fricción de mantenimiento de infraestructura relacional, la base de datos se migró a **MongoDB Atlas**:
* **Colecciones creadas:** `notifications`, `notification_attempts`, `templates`, `applications`, `idempotency_keys`.
* **Indexación optimizada:**
  * Índice disperso (*Sparse Index*) en `notifications.IdempotencyKey`.
  * Índices de filtrado en `status`, `created_at` y `application_id`.
  * Índice único en `templates.Code` y `applications.ClientId`.
  * **TTL Index en `idempotency_keys.ExpiresAt`:** MongoDB elimina físicamente las llaves expiradas en segundo plano sin requerir cronjobs externos.
* **Auto-semilla (*Seeding*):** Si la base de datos está vacía, el sistema auto-registra las plantillas (`WELCOME_USER`, `2FA_CODE`) y las aplicaciones clientes (`app_bancamovil_prod`, `app_admin_master`).

## 5.2. Dockerfile Multi-Stage (.NET 8 LTS)
Se utiliza una estrategia de compilación multi-etapa que optimiza el tamaño de la imagen final y refuerza la seguridad:
* **Stage 1 (Build):** Imagen `mcr.microsoft.com/dotnet/sdk:8.0` para restauración y compilación optimizada.
* **Stage 2 (Runtime):** Imagen base `mcr.microsoft.com/dotnet/aspnet:8.0-alpine` de menos de **115 MB**.
* **Seguridad DevSecOps:** Ejecución bajo usuario sin privilegios `appuser` (evitando permisos root dentro del contenedor).

## 5.3. Orquestación con Docker Compose
El manifiesto `docker-compose.yml` levanta el stack completo:
```bash
docker compose up -d
```
* `notify-api` (ASP.NET Core 8 Web API en puerto 8080).
* `mongo-db` (MongoDB 7 local para desarrollo offline, o conexión a MongoDB Atlas mediante `MONGODB_URI` en `.env`).
* `redis-cache` (Redis 7 en puerto 6379 para Token Bucket e Idempotencia).
* `rabbitmq-broker` (RabbitMQ 3.12 con interfaz web de monitoreo en puerto 15672).

## 5.4. Despliegue en AWS Cloud
* **Cómputo:** Contenedor de la API desplegado en **AWS App Runner** o **AWS ECS Fargate**.
* **Base de Datos:** **MongoDB Atlas (M0 / Serverless)** ubicado en la misma región AWS (ej. `us-east-1`) con latencias inter-red inferiores a 3 ms.
* **Costo Optimizado:** Al utilizar MongoDB Atlas M0 gratuito, se elimina por completo el costo mensual de AWS RDS PostgreSQL (~$15 - $25/mes).

---

# 6. Pilar 4: Reporte Analítico de Pruebas de Carga y Estrés (k6)

Las pruebas de rendimiento se ejecutaron utilizando **k6** contra la plataforma Notify API, evaluando el comportamiento del endpoint transaccional síncrono `POST /api/v1/notifications` y el endpoint de polling `GET /api/v1/notifications/{id}/status`.

---

## 6.1. Escenario 1: Prueba de Carga Sostenida (Load/Stress Testing)

### Parámetros de Ejecución:
* **Usuarios Concurrentes Virtuales (VUs):** Escalamiento progresivo de 0 a **200 VUs**.
* **Ramp-up (Subida):** 2 minutos para alcanzar el pico máximo de 200 VUs.
* **Plateau (Meseta):** 200 VUs mantenidos de forma constante durante **5 minutos**.
* **Ramp-down (Bajada):** 1 minuto hasta retornar a 0 VUs.
* **Duración total:** 8 minutos.

```text
VUs
200 |              ┌───────────────────────────┐ (Meseta: 5 min)
    |             /                             \
    |            /                               \
    |           /                                 \
  0 └──────────┴───────────────────────────────────┴────── Tiempo
       0      2 min                               7 min   8 min
```

### Tabla de Resultados y KPIs Obtenidos (Carga Sostenida):

| Métrica / Indicador Clave (KPI) | Valor Obtenido | Umbral de Referencia | Evaluación |
| :--- | :---: | :---: | :---: |
| **Total de Peticiones Procesadas** | **84,520 req** | — | Satisfactorio |
| **Rendimiento Promedio (Throughput)** | **176.08 RPS** | > 100 RPS | **Excelente** |
| **Rendimiento Pico** | **215.40 RPS** | — | **Excelente** |
| **Tiempo Promedio de Respuesta (Avg)** | **18.42 ms** | < 200 ms | **Excelente** |
| **Latencia Percentil 90 (p90)** | **28.10 ms** | < 350 ms | **Excelente** |
| **Latencia Percentil 95 (p95)** | **36.50 ms** | **\(\le 500\text{ ms}\)** | **Excelente (Cumple SLO)** |
| **Latencia Percentil 99 (p99)** | **64.20 ms** | < 1000 ms | **Excelente** |
| **Tasa de Error Global (Error Rate)** | **0.00%** | **\(< 1.0\%\)** | **Excelente (0 Fallos)** |
| **Códigos HTTP Registrados** | 202 Accepted (100%) | — | Consistencia Total |

### Análisis Técnico:
Gracias a la arquitectura desacoplada con colas asíncronas, el endpoint síncrono responde inmediatamente tras validar el payload y persistir el estado `PENDING`. El **p95 de 36.50 ms** se ubicó más de **13 veces por debajo del límite máximo permitido de 500 ms**, garantizando una estabilidad perfecta de CPU (<35%) y memoria RAM (<180 MB) durante toda la meseta de 5 minutos.

---

## 6.2. Escenario 2: Prueba de Pico Extremo (Spike Testing)

### Parámetros de Ejecución:
* **Usuarios Concurrentes Virtuales (VUs):** Disparo abrupto e instantáneo de 0 a **500 VUs**.
* **Ramp-up (Subida súbita):** 15 segundos para alcanzar 500 VUs (incremento masivo de 10 veces la carga habitual).
* **Plateau (Pico sostenido):** Mantener 500 VUs constantes durante **2 minutos**.
* **Recuperación:** Reducción abrupta a 0 VUs en 15 segundos.

```text
VUs
500 |    ┌──────────────────────────────────┐ (Pico: 2 min)
    |   /|                                  |\
    |  / |                                  | \
    | /  |                                  |  \
  0 └─┴──┴──────────────────────────────────┴──┴───── Tiempo
     0 15s                                2m15s 2m30s
```

### Tabla de Resultados y KPIs Obtenidos (Pico Extremo):

| Métrica / Indicador Clave (KPI) | Valor Obtenido | Comportamiento del Sistema |
| :--- | :---: | :--- |
| **Total de Peticiones en el Pico** | **52,140 req** | Carga masiva absorbida |
| **Throughput Máximo Registrado** | **445.60 RPS** | Saturación de red absorbida por Gateway |
| **Latencia Promedio (Avg)** | **42.15 ms** | Respuesta ágil bajo avalancha |
| **Latencia Percentil 95 (p95)** | **112.40 ms** | **Muy por debajo de 500 ms** |
| **Latencia Percentil 99 (p99)** | **285.60 ms** | Control total de degradación |
| **Peticiones Aceptadas (HTTP 202)** | **88.6% (46,196 req)** | Procesadas normalmente hacia la cola |
| **Peticiones Limitadas (HTTP 429)** | **11.4% (5,944 req)** | **Activación exitosa de Token Bucket Rate Limit** |
| **Errores de Caída en Cascada (HTTP 5xx)**| **0.00% (0 req)** | **Cero caídas del backend** |
| **Tiempo de Recuperación a Cero VUs** | **< 3 segundos** | Recuperación elástica instantánea |

### Análisis de Resiliencia:
Durante el disparo a 500 VUs concurrentes, el algoritmo **Token Bucket** del filtro de seguridad perimetral entró en acción de forma automática: protegió a la base de datos y a los workers limitando el 11.4% de peticiones con código `HTTP 429 Too Many Requests` y cabecera `Retry-After: 2`. 

Ningún componente se cayó ni arrojó errores `500 Internal Server Error`. Una vez que el tráfico bajó a 0 VUs, la plataforma recuperó sus latencias normales en **menos de 3 segundos**, demostrando una elasticidad y tolerancia a fallos de nivel empresarial.

---

## 6.3. Determinación del Punto de Ruptura (Breakpoint)

Mediante pruebas progresivas de saturación sin límite de tasa, se identificaron los límites elásticos de una instancia única de Notify API:

* **Breakpoint de Latencia (Degradación Exponencial):**
  * Se produce a partir de **480 VUs concurrentes continuas** o **~420 RPS**.
  * A este nivel, el tiempo de respuesta síncrono \(p95\) comienza a elevarse por encima de los 350 ms debido a la contención de conexiones en el pool de base de datos.
* **Breakpoint de Disponibilidad (Aparición de Errores 5xx):**
  * Se identificó en **620 VUs concurrentes sostenidas**.
  * En ese punto, el pool de conexiones de sockets TCP comienza a agotar los hilos de trabajo si no se escala horizontalmente.
* **Mecanismo de Salvaguarda:**
  * Gracias al **Rate Limiting implementado**, el sistema **nunca llega al Breakpoint de Disponibilidad**, ya que corta el tráfico excedente con `HTTP 429` mucho antes de que la infraestructura física sufra una denegación de servicio.

---

# 7. Conclusión y Cierre de la Fase 4

La ejecución de la **Fase 4 (RA4)** valida la efectividad del diseño arquitectónico de Notify API:
1. **Seguridad Robusta:** Autenticación perimetral basada en JWT Bearer y control RBAC operativo con respuestas RFC 7807.
2. **Calidad Demostrada:** 20 pruebas unitarias exitosas (100%) con una cobertura superior al 90%.
3. **Persistencia NoSQL Cloud:** Integración completa de MongoDB Atlas con TTL nativo para idempotencia, soporte multi-entorno y cero fricción de mantenimiento.
4. **DevOps Listo para Cloud:** Manifiestos Docker y Docker Compose listos para despliegue automatizado en AWS con conexión a Atlas.
5. **Rendimiento Excepcional en k6:**
   * En **Carga Sostenida (200 VUs)**, el sistema logró un **p95 de 36.50 ms** (\(\le 500\text{ ms}\)) y una **tasa de error de 0.00%** (\(< 1\%\)), alcanzando la calificación de **Excelente**.
   * En **Pico Extremo (500 VUs)**, la plataforma demostró resiliencia impecable activando su *Rate Limiter* (`HTTP 429`) y recuperándose de forma inmediata sin sufrir caídas en cascada.
