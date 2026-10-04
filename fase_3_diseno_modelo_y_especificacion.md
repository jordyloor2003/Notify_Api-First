# Fase 3: Diseño del Modelo y Especificación

---

# 1. Portada y Control del Documento

## Información General
* **Nombre de la API / Producto:** Notify API
* **Organización:** Consultoría de Software
* **Fase:** Fase 3 — Diseño del Modelo y Especificación (Resultado de Aprendizaje 3 - RA3)
* **Versión:** 1.0.0
* **Fecha de Emisión:** 2026-10-02
* **Estado:** Aprobado para Implementación
* **Autores:**
  * Christian Naranjo — Ingeniero de Software
  * Jordy Loor — Ingeniero de Software
  * Diego Mesias — Ingeniero de Software

## Historial de Revisiones
| Versión | Fecha | Autor | Resumen del Cambio |
| :--- | :---: | :--- | :--- |
| **v0.1** | 2026-09-28 | C. Naranjo | Estructura base de recursos REST, esquema preliminar de DTOs y convenciones de nombrado. |
| **v0.2** | 2026-09-30 | J. Loor | Diseño formal del estándar RFC 7807 (Problem Details), esquemas de paginación e Idempotencia. |
| **v1.0** | 2026-10-02 | D. Mesias | Contrato formal OpenAPI 3.0 (Swagger) definitivo en YAML, esquemas de seguridad JWT y ejemplos de consumo cURL. |

---

# 2. Introducción y Alcance del Modelo Orientado a APIs

El propósito de esta fase es definir el **modelo de datos orientado a recursos (Resource-Oriented Design)** y la **especificación formal del contrato de la API** para **Notify API**, cumpliendo con el Resultado de Aprendizaje 3 (RA3).

Siguiendo el principio **API-First**, el contrato público de la API actúa como el acuerdo formal vinculante entre los sistemas empresariales consumidores (ERP, CRM, plataformas de e-commerce, banca) y el equipo de ingeniería backend. Se priorizan la claridad semántica, la interoperabilidad estricta, la validación exhaustiva de tipos, el desacoplamiento mediante procesamiento asíncrono y la consistencia en el manejo de errores.

---

# 3. Principios de Diseño Orientado a APIs (Resource-Oriented API Design)

## 3.1. Estructura de URI y Versionamiento
* **Versionamiento en URI:** Se adopta versionamiento semántico mayor directamente en la ruta:
  ```text
  https://api.notify.consultoria.com/api/v1/{resource}
  ```
* **Convención de Recursos en Plural:** Los identificadores de recursos utilizan sustantivos en minúsculas y en plural (`/notifications`, `/templates`, `/channels`, `/applications`).
* **Sub-recursos Jerárquicos y Procesos:** Para consultar estados o activar comandos específicos se emplean sub-rutas semánticas:
  * `/notifications/{id}/status` — Consulta rápida y liviana del estado transaccional.
  * `/notifications/{id}/retry` — Comando de reintento manual sobre notificaciones fallidas.

## 3.2. Uso Semántico de Métodos HTTP
| Método HTTP | Propósito en Notify API | Semántica | Idempotente por Definición |
| :--- | :--- | :---: | :---: |
| **POST** | Creación de recursos (`/templates`), inicio de procesamiento (`/notifications`) o emisión de tokens (`/auth/token`). | No | No (se protege con `Idempotency-Key`) |
| **GET** | Consulta de recursos individuales o listados paginados. | Sí (Safe) | Sí |
| **PUT** | Actualización o sustitución completa de una plantilla (`/templates/{id}`). | No | Sí |
| **DELETE** | Cancelación o deshabilitación lógica de recursos. | No | Sí |

## 3.3. Códigos de Respuesta HTTP y Flujo Asíncrono
Para satisfacer el SLO de latencia (\(p95 \le 500\text{ ms}\)) definido en la Fase 1:
* **`202 Accepted`:** Retornado de inmediato al invocar `POST /api/v1/notifications`. Confirma que el payload fue validado, persistido en estado `PENDING` y publicado en la cola de mensajería (RabbitMQ/SQS). Incluye la cabecera `Location` apuntando a `/api/v1/notifications/{id}/status`.
* **`200 OK`:** Retornado en consultas de estado, listados o cuando una petición `POST` es reconocida como duplicada válida mediante su `Idempotency-Key`.
* **`201 Created`:** Retornado tras la creación sincrónica de una plantilla o registro de aplicación.
* **`400 Bad Request`:** Errores de validación de sintaxis o esquema JSON.
* **`401 Unauthorized`:** Token JWT ausente, expirado o con firma inválida.
* **`403 Forbidden`:** El token JWT es válido pero no posee el rol requerido (RBAC).
* **`404 Not Found`:** El recurso solicitado no existe.
* **`409 Conflict`:** Conflicto de estado (ej. intentar reintentar una notificación que ya está en estado `SENT`).
* **`429 Too Many Requests`:** Se superó la cuota perimetral de peticiones por segundo (*Rate Limit*). Incluye cabecera obligatoria `Retry-After: <segundos>`.
* **`500 Internal Server Error`:** Fallo técnico interno no controlado.

---

## 3.4. Estandarización de Errores bajo RFC 7807 / RFC 9457 (Problem Details)
Toda respuesta de error (códigos 4xx y 5xx) retorna obligatoriamente el tipo MIME `application/problem+json` con la siguiente estructura uniforme:

```json
{
  "type": "https://notify.consultoria.com/errors/validation-failed",
  "title": "Error de Validación de Parámetros",
  "status": 400,
  "detail": "El destinatario ingresado no corresponde a un formato E.164 válido para el canal SMS.",
  "instance": "/api/v1/notifications",
  "invalidParams": [
    {
      "name": "recipient",
      "reason": "El número '+593-XYZ' debe seguir el formato internacional E.164 (+[código][número])."
    }
  ],
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

---

## 3.5. Idempotencia y Trazabilidad en Cabeceras HTTP
* **Cabecera `Idempotency-Key` (Requerida en `POST /notifications`):**
  * Tipo: `string (UUIDv4)`.
  * Propósito: Evitar cobros o envíos duplicados ante desconexiones de red del cliente.
  * Ciclo de Vida: El sistema almacena la clave en caché (Redis) durante 24 horas. Si se recibe una petición con la misma clave, responde con el resultado previo (`HTTP 200 OK`) sin encolar un nuevo trabajo.
* **Cabecera `X-Correlation-Id`:**
  * Tipo: `string (UUIDv4)`.
  * Propósito: Trazabilidad distribuida extremo a extremo (Gateway \(\to\) API \(\to\) Queue \(\to\) Worker \(\to\) Proveedor). Si el cliente no la envía, el API Gateway la genera automáticamente.

---

## 3.6. Estándar de Paginación y Filtrado
Para endpoints de colecciones (`/notifications`, `/templates`, `/audit/logs`):
* **Parámetros Query:** `page` (entero positivo, default: 1), `pageSize` (entero positivo, default: 20, máximo: 100).
* **Parámetros de Filtro:** `channel` (EMAIL, SMS, PUSH), `status` (PENDING, PROCESSING, SENT, FAILED, RETRY, CANCELLED), `from` (ISO 8601), `to` (ISO 8601).
* **Estructura Envolvente de Respuesta:**
```json
{
  "items": [ ... ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1540,
    "totalPages": 77,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

---

# 4. Diccionario de Datos y Especificación de Recursos (DTOs)

## 4.1. Recurso: Notificación (`Notification`)

### 4.1.1. DTO de Creación: `CreateNotificationRequest`
| Campo | Tipo | Requerido | Restricciones / Validación | Descripción |
| :--- | :--- | :---: | :--- | :--- |
| `recipient` | `string` | **Sí** | Email válido o formato telefónico E.164 o token push. | Destinatario final de la comunicación. |
| `channel` | `string` | **Sí** | Enum: `EMAIL`, `SMS`, `PUSH`. | Canal de telecomunicación solicitado. |
| `templateCode`| `string` | Opcional | Alfanumérico con guiones, máx. 50 caracteres. | Código identificador de plantilla registrada. |
| `subject` | `string` | Condicional | Máx. 250 caracteres. Requerido si canal = `EMAIL` y no se usa plantilla. | Asunto del mensaje. |
| `body` | `string` | Condicional | Máx. 160 caracteres para `SMS`, máx. 500 KB para `EMAIL`. | Contenido textual o HTML del mensaje. |
| `templateVariables`| `object` | Opcional | Objeto clave-valor JSON. | Variables dinámicas para renderizar en la plantilla. |
| `metadata` | `object` | Opcional | Objeto clave-valor (máx. 10 campos). | Metadatos de negocio del cliente (ej. `orderId`, `userId`). |
| `priority` | `string` | No | Enum: `LOW`, `NORMAL`, `HIGH`. Default: `NORMAL`. | Nivel de urgencia para consumo de la cola. |

### 4.1.2. DTO de Aceptación Inmediata: `NotificationAcceptedResponse`
Retornado con código HTTP `202 Accepted`:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "PENDING",
  "channel": "EMAIL",
  "recipient": "usuario@empresa.com",
  "createdAt": "2026-10-02T21:40:00Z",
  "trackingUrl": "/api/v1/notifications/3fa85f64-5717-4562-b3fc-2c963f66afa6/status"
}
```

### 4.1.3. DTO Detallado: `NotificationDetailResponse`
Retornado con código HTTP `200 OK` en `GET /api/v1/notifications/{id}`:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "applicationId": "c8a4b679-b1d5-4422-b91c-148c3b4a2e11",
  "channel": "EMAIL",
  "recipient": "usuario@empresa.com",
  "status": "SENT",
  "subject": "Confirmación de Transferencia Bancaria",
  "retryCount": 0,
  "idempotencyKey": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "metadata": {
    "transactionId": "TX-998822"
  },
  "createdAt": "2026-10-02T21:40:00Z",
  "sentAt": "2026-10-02T21:40:02Z",
  "attempts": [
    {
      "attemptNumber": 1,
      "provider": "AWS_SES",
      "status": "SUCCESS",
      "httpStatusCode": 200,
      "latencyMs": 145,
      "attemptedAt": "2026-10-02T21:40:02Z",
      "errorDetail": null
    }
  ]
}
```

---

## 4.2. Recurso: Plantilla (`Template`)

### DTO de Creación: `CreateTemplateRequest`
| Campo | Tipo | Requerido | Descripción |
| :--- | :--- | :---: | :--- |
| `code` | `string` | **Sí** | Código único de plantilla (ej. `AUTH_PASSWORD_RESET`). |
| `name` | `string` | **Sí** | Nombre legible para humanos. |
| `channel` | `string` | **Sí** | Enum: `EMAIL`, `SMS`, `PUSH`. |
| `subject` | `string` | No | Asunto base con soporte para variables `{{nombre}}`. |
| `bodyTemplate`| `string` | **Sí** | Plantilla en formato Handlebars/Mustache. |
| `requiredVariables`| `array[string]` | No | Lista de nombres de variables obligatorias. |

---

## 4.3. Recurso: Autenticación (`Auth`)

### Petición: `POST /api/v1/auth/token`
Formato: `application/x-www-form-urlencoded` o `application/json`.
```json
{
  "clientId": "app_bancamovil_prod",
  "clientSecret": "sec_99a8b7c6d5e4f3a2b1c0",
  "grantType": "client_credentials"
}
```

### Respuesta: `TokenResponse` (`HTTP 200 OK`)
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "scope": "notifications:write notifications:read"
}
```

---

# 5. Especificación Formal OpenAPI 3.0 (Swagger) en YAML

A continuación se presenta el contrato formal completo de **Notify API** bajo el estándar **OpenAPI 3.0.3**:

```yaml
openapi: 3.0.3
info:
  title: Notify API Platform
  description: |
    Plataforma empresarial API-First para la orquestación, gestión y despacho de notificaciones transaccionales multicanal (Email, SMS y Push).
    Diseñada bajo arquitectura asíncrona resiliente con soporte de JWT, Idempotencia, Rate Limiting y especificación de errores RFC 7807.
  version: 1.0.0
  contact:
    name: Soporte de Integración Notify API
    email: soporte-api@notify.consultoria.com
    url: https://notify.consultoria.com/docs
  license:
    name: Propiedad Privada - Consultoría de Software
    url: https://notify.consultoria.com/terms

servers:
  - url: https://api.notify.consultoria.com/api/v1
    description: Servidor de Producción (AWS us-east-1)
  - url: https://sandbox.notify.consultoria.com/api/v1
    description: Servidor Sandbox / Pruebas (Adaptadores simulados)

tags:
  - name: Auth
    description: Autenticación perimetral y generación de tokens de acceso JWT.
  - name: Notifications
    description: Gestión, encolado, consulta y reintento de notificaciones transaccionales.
  - name: Templates
    description: Administración de plantillas parametrizadas de mensajería.
  - name: Channels
    description: Metadatos y configuración de canales de telecomunicación soportados.
  - name: Audit
    description: Consulta de bitácoras inmutables de auditoría (Exclusivo Auditor / Admin).

paths:
  # =========================================================================
  # AUTHENTICATION
  # =========================================================================
  /auth/token:
    post:
      tags:
        - Auth
      summary: Emitir token de acceso JWT Bearer
      description: Permite a las aplicaciones clientes obtener un token JWT firmado mediante credenciales de aplicación (Client Credentials Flow).
      operationId: generateToken
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/TokenRequest'
      responses:
        '200':
          description: Token de autenticación generado exitosamente.
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/TokenResponse'
        '400':
          $ref: '#/components/responses/BadRequestProblem'
        '401':
          $ref: '#/components/responses/UnauthorizedProblem'
        '429':
          $ref: '#/components/responses/TooManyRequestsProblem'

  # =========================================================================
  # NOTIFICATIONS
  # =========================================================================
  /notifications:
    post:
      tags:
        - Notifications
      summary: Despachar nueva notificación transaccional
      description: |
        Ingesta, valida y encola asíncronamente una notificación para su posterior envío vía Email, SMS o Push.
        Retorna inmediatamente **202 Accepted**. Admite cabecera `Idempotency-Key` para prevenir dobles envíos.
      operationId: dispatchNotification
      security:
        - BearerAuth: []
      parameters:
        - $ref: '#/components/parameters/IdempotencyKeyHeader'
        - $ref: '#/components/parameters/CorrelationIdHeader'
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CreateNotificationRequest'
      responses:
        '202':
          description: Notificación aceptada y encolada para procesamiento asíncrono.
          headers:
            Location:
              description: URL de consulta directa para seguimiento del estado de la notificación.
              schema:
                type: string
                example: /api/v1/notifications/3fa85f64-5717-4562-b3fc-2c963f66afa6/status
            X-Correlation-Id:
              description: Identificador de correlación asignado a la traza.
              schema:
                type: string
                format: uuid
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/NotificationAcceptedResponse'
        '200':
          description: Petición duplicada identificada mediante Idempotency-Key. Retorna los datos originales sin duplicar el despacho.
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/NotificationDetailResponse'
        '400':
          $ref: '#/components/responses/BadRequestProblem'
        '401':
          $ref: '#/components/responses/UnauthorizedProblem'
        '429':
          $ref: '#/components/responses/TooManyRequestsProblem'
        '500':
          $ref: '#/components/responses/InternalServerErrorProblem'

    get:
      tags:
        - Notifications
      summary: Listar historial de notificaciones con paginación y filtros
      description: Retorna un conjunto paginado de notificaciones emitidas por la aplicación autenticada.
      operationId: listNotifications
      security:
        - BearerAuth: []
      parameters:
        - name: page
          in: query
          description: Número de página solicitada (base 1).
          schema:
            type: integer
            minimum: 1
            default: 1
        - name: pageSize
          in: query
          description: Cantidad de registros por página (máximo 100).
          schema:
            type: integer
            minimum: 1
            maximum: 100
            default: 20
        - name: channel
          in: query
          description: Filtrar por canal de comunicación.
          schema:
            $ref: '#/components/schemas/ChannelType'
        - name: status
          in: query
          description: Filtrar por estado del ciclo de vida.
          schema:
            $ref: '#/components/schemas/NotificationStatus'
        - name: from
          in: query
          description: Fecha/hora mínima de creación en formato ISO 8601.
          schema:
            type: string
            format: date-time
        - name: to
          in: query
          description: Fecha/hora máxima de creación en formato ISO 8601.
          schema:
            type: string
            format: date-time
      responses:
        '200':
          description: Listado paginado de notificaciones.
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/PaginatedNotificationListResponse'
        '401':
          $ref: '#/components/responses/UnauthorizedProblem'
        '403':
          $ref: '#/components/responses/ForbiddenProblem'

  /notifications/{id}:
    get:
      tags:
        - Notifications
      summary: Consultar detalle completo de una notificación
      description: Obtiene todos los metadatos de la notificación, incluyendo el historial de intentos de entrega y códigos de proveedor.
      operationId: getNotificationById
      security:
        - BearerAuth: []
      parameters:
        - name: id
          in: path
          required: true
          description: Identificador único UUID de la notificación.
          schema:
            type: string
            format: uuid
      responses:
        '200':
          description: Detalle completo de la notificación.
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/NotificationDetailResponse'
        '401':
          $ref: '#/components/responses/UnauthorizedProblem'
        '404':
          $ref: '#/components/responses/NotFoundProblem'

  /notifications/{id}/status:
    get:
      tags:
        - Notifications
      summary: Consulta liviana de estado transaccional
      description: Endpoint optimizado para polling rápido que retorna únicamente el estado actual y marca temporal de la notificación.
      operationId: getNotificationStatus
      security:
        - BearerAuth: []
      parameters:
        - name: id
          in: path
          required: true
          description: UUID de la notificación.
          schema:
            type: string
            format: uuid
      responses:
        '200':
          description: Estado actual de la notificación.
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/NotificationStatusResponse'
        '404':
          $ref: '#/components/responses/NotFoundProblem'

  /notifications/{id}/retry:
    post:
      tags:
        - Notifications
      summary: Reintentar manualmente una notificación fallida
      description: Permite solicitar el reenvió de una notificación en estado FAILED si no ha alcanzado los intentos máximos permitidos.
      operationId: retryNotification
      security:
        - BearerAuth: []
      parameters:
        - name: id
          in: path
          required: true
          description: UUID de la notificación fallida.
          schema:
            type: string
            format: uuid
      responses:
        '202':
          description: Reintento aceptado y reencolado con éxito.
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/NotificationAcceptedResponse'
        '404':
          $ref: '#/components/responses/NotFoundProblem'
        '409':
          description: Conflicto de estado (la notificación no se encuentra en estado FAILED o ya fue entregada).
          content:
            application/problem+json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'

  # =========================================================================
  # TEMPLATES
  # =========================================================================
  /templates:
    get:
      tags:
        - Templates
      summary: Listar plantillas de mensajería registradas
      operationId: listTemplates
      security:
        - BearerAuth: []
      responses:
        '200':
          description: Lista de plantillas disponibles para la aplicación.
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/TemplateResponse'
    post:
      tags:
        - Templates
      summary: Registrar una nueva plantilla de mensajería
      operationId: createTemplate
      security:
        - BearerAuth: []
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CreateTemplateRequest'
      responses:
        '201':
          description: Plantilla registrada exitosamente.
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/TemplateResponse'
        '400':
          $ref: '#/components/responses/BadRequestProblem'

  # =========================================================================
  # CHANNELS
  # =========================================================================
  /channels:
    get:
      tags:
        - Channels
      summary: Listar canales de comunicación soportados y sus límites
      operationId: listChannels
      security:
        - BearerAuth: []
      responses:
        '200':
          description: Lista de canales activos (EMAIL, SMS, PUSH) con sus restricciones operativas.
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/ChannelInfoResponse'

  # =========================================================================
  # AUDIT
  # =========================================================================
  /audit/logs:
    get:
      tags:
        - Audit
      summary: Consultar bitácora inmutable de auditoría
      description: Requiere rol AUDITOR o ADMIN. Retorna el historial de eventos críticos de seguridad y transacciones.
      operationId: getAuditLogs
      security:
        - BearerAuth: []
      parameters:
        - name: page
          in: query
          schema:
            type: integer
            default: 1
        - name: pageSize
          in: query
          schema:
            type: integer
            default: 50
      responses:
        '200':
          description: Registros de auditoría.
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/PaginatedAuditLogsResponse'
        '403':
          $ref: '#/components/responses/ForbiddenProblem'

# ===========================================================================
# COMPONENTS
# ===========================================================================
components:
  securitySchemes:
    BearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
      description: Ingrese el token JWT precedido por la palabra Bearer (ej. "Bearer eyJhbGciOi...").

  parameters:
    IdempotencyKeyHeader:
      name: Idempotency-Key
      in: header
      required: false
      description: Clave UUIDv4 única de idempotencia para prevenir el despacho duplicado en reintentos de red.
      schema:
        type: string
        format: uuid
        example: 9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d

    CorrelationIdHeader:
      name: X-Correlation-Id
      in: header
      required: false
      description: Identificador UUIDv4 para trazabilidad distribuida extremo a extremo.
      schema:
        type: string
        format: uuid
        example: 4bf92f35-77b3-4da6-a3ce-929d0e0e4736

  responses:
    BadRequestProblem:
      description: Error de sintaxis o validación de campos en la petición (RFC 7807).
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'
    UnauthorizedProblem:
      description: Autenticación requerida o token JWT inválido/expirado (RFC 7807).
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'
    ForbiddenProblem:
      description: Permisos insuficientes (RBAC) para el recurso solicitado (RFC 7807).
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'
    NotFoundProblem:
      description: El recurso solicitado no fue encontrado (RFC 7807).
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'
    TooManyRequestsProblem:
      description: Límite de frecuencia de peticiones excedido (RFC 7807). Retorna cabecera Retry-After.
      headers:
        Retry-After:
          description: Cantidad de segundos a esperar antes de reintentar la solicitud.
          schema:
            type: integer
            example: 2
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'
    InternalServerErrorProblem:
      description: Error interno no controlado en la plataforma Notify API (RFC 7807).
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'

  schemas:
    # -----------------------------------------------------------------------
    # ENUMS
    # -----------------------------------------------------------------------
    ChannelType:
      type: string
      enum: [EMAIL, SMS, PUSH]
      description: Canal de comunicación soportado.
      example: EMAIL

    NotificationStatus:
      type: string
      enum: [PENDING, PROCESSING, SENT, FAILED, RETRY, CANCELLED]
      description: Estado transaccional en el ciclo de vida de la notificación.
      example: PENDING

    PriorityLevel:
      type: string
      enum: [LOW, NORMAL, HIGH]
      default: NORMAL
      description: Nivel de prioridad para la atención en cola.
      example: NORMAL

    # -----------------------------------------------------------------------
    # AUTH SCHEMAS
    # -----------------------------------------------------------------------
    TokenRequest:
      type: object
      required:
        - clientId
        - clientSecret
      properties:
        clientId:
          type: string
          example: app_bancamovil_prod
        clientSecret:
          type: string
          format: password
          example: sec_99a8b7c6d5e4f3a2b1c0
        grantType:
          type: string
          enum: [client_credentials]
          default: client_credentials

    TokenResponse:
      type: object
      properties:
        accessToken:
          type: string
          example: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
        tokenType:
          type: string
          example: Bearer
        expiresIn:
          type: integer
          description: Tiempo de vigencia en segundos (1 hora).
          example: 3600
        scope:
          type: string
          example: notifications:write notifications:read

    # -----------------------------------------------------------------------
    # NOTIFICATION SCHEMAS
    # -----------------------------------------------------------------------
    CreateNotificationRequest:
      type: object
      required:
        - channel
        - recipient
      properties:
        channel:
          $ref: '#/components/schemas/ChannelType'
        recipient:
          type: string
          description: Correo electrónico, número en formato E.164 o token Push del dispositivo.
          example: usuario@empresa.com
        templateCode:
          type: string
          description: Código de plantilla preconfigurada. Si se define, subject y body son omitibles.
          example: WELCOME_USER
        subject:
          type: string
          maxLength: 250
          description: Requerido para canal EMAIL si no se emplea plantilla.
          example: Bienvenido a la Plataforma
        body:
          type: string
          description: Contenido del mensaje (HTML o texto plano). Requerido si no se emplea plantilla.
          example: Hola Juan, tu registro ha sido exitoso.
        templateVariables:
          type: object
          description: Variables de sustitución dinámica para la plantilla.
          additionalProperties: true
          example:
            nombre: Juan Pérez
            codigoActivacion: "982143"
        metadata:
          type: object
          description: Metadatos arbitrarios de negocio para trazabilidad.
          additionalProperties: true
          example:
            orderId: ORD-5544
            userId: USR-123
        priority:
          $ref: '#/components/schemas/PriorityLevel'

    NotificationAcceptedResponse:
      type: object
      properties:
        id:
          type: string
          format: uuid
          example: 3fa85f64-5717-4562-b3fc-2c963f66afa6
        status:
          $ref: '#/components/schemas/NotificationStatus'
        channel:
          $ref: '#/components/schemas/ChannelType'
        recipient:
          type: string
          example: usuario@empresa.com
        createdAt:
          type: string
          format: date-time
          example: '2026-10-02T21:40:00Z'
        trackingUrl:
          type: string
          example: /api/v1/notifications/3fa85f64-5717-4562-b3fc-2c963f66afa6/status

    NotificationStatusResponse:
      type: object
      properties:
        id:
          type: string
          format: uuid
          example: 3fa85f64-5717-4562-b3fc-2c963f66afa6
        status:
          $ref: '#/components/schemas/NotificationStatus'
        channel:
          $ref: '#/components/schemas/ChannelType'
        retryCount:
          type: integer
          example: 0
        createdAt:
          type: string
          format: date-time
        sentAt:
          type: string
          format: date-time
          nullable: true
        lastError:
          type: string
          nullable: true

    NotificationDetailResponse:
      type: object
      properties:
        id:
          type: string
          format: uuid
          example: 3fa85f64-5717-4562-b3fc-2c963f66afa6
        applicationId:
          type: string
          format: uuid
        channel:
          $ref: '#/components/schemas/ChannelType'
        recipient:
          type: string
          example: usuario@empresa.com
        status:
          $ref: '#/components/schemas/NotificationStatus'
        subject:
          type: string
          nullable: true
        retryCount:
          type: integer
          example: 0
        idempotencyKey:
          type: string
          nullable: true
          example: 9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d
        metadata:
          type: object
          additionalProperties: true
        createdAt:
          type: string
          format: date-time
        sentAt:
          type: string
          format: date-time
          nullable: true
        attempts:
          type: array
          items:
            $ref: '#/components/schemas/NotificationAttemptItem'

    NotificationAttemptItem:
      type: object
      properties:
        attemptNumber:
          type: integer
          example: 1
        provider:
          type: string
          example: AWS_SES
        status:
          type: string
          example: SUCCESS
        httpStatusCode:
          type: integer
          example: 200
        latencyMs:
          type: integer
          example: 145
        attemptedAt:
          type: string
          format: date-time
        errorDetail:
          type: string
          nullable: true

    PaginatedNotificationListResponse:
      type: object
      properties:
        items:
          type: array
          items:
            $ref: '#/components/schemas/NotificationDetailResponse'
        pagination:
          $ref: '#/components/schemas/PaginationMetadata'

    PaginationMetadata:
      type: object
      properties:
        page:
          type: integer
          example: 1
        pageSize:
          type: integer
          example: 20
        totalItems:
          type: integer
          example: 1540
        totalPages:
          type: integer
          example: 77
        hasNextPage:
          type: boolean
          example: true
        hasPreviousPage:
          type: boolean
          example: false

    # -----------------------------------------------------------------------
    # TEMPLATE SCHEMAS
    # -----------------------------------------------------------------------
    CreateTemplateRequest:
      type: object
      required:
        - code
        - name
        - channel
        - bodyTemplate
      properties:
        code:
          type: string
          example: WELCOME_USER
        name:
          type: string
          example: Plantilla de Bienvenida
        channel:
          $ref: '#/components/schemas/ChannelType'
        subject:
          type: string
          example: Bienvenido {{nombre}} a Nuestra Plataforma
        bodyTemplate:
          type: string
          example: <h1>Hola {{nombre}}</h1><p>Tu cuenta fue activada.</p>
        requiredVariables:
          type: array
          items:
            type: string
          example: [nombre]

    TemplateResponse:
      type: object
      properties:
        id:
          type: string
          format: uuid
        code:
          type: string
          example: WELCOME_USER
        name:
          type: string
          example: Plantilla de Bienvenida
        channel:
          $ref: '#/components/schemas/ChannelType'
        subject:
          type: string
          nullable: true
        version:
          type: integer
          example: 1
        updatedAt:
          type: string
          format: date-time

    # -----------------------------------------------------------------------
    # CHANNEL & AUDIT SCHEMAS
    # -----------------------------------------------------------------------
    ChannelInfoResponse:
      type: object
      properties:
        code:
          $ref: '#/components/schemas/ChannelType'
        name:
          type: string
          example: Correo Electrónico
        isEnabled:
          type: boolean
          example: true
        maxPayloadSizeBytes:
          type: integer
          example: 524288
        supportsHtml:
          type: boolean
          example: true

    PaginatedAuditLogsResponse:
      type: object
      properties:
        items:
          type: array
          items:
            $ref: '#/components/schemas/AuditLogItem'
        pagination:
          $ref: '#/components/schemas/PaginationMetadata'

    AuditLogItem:
      type: object
      properties:
        id:
          type: string
          format: uuid
        action:
          type: string
          example: NOTIFICATION_DISPATCHED
        entityName:
          type: string
          example: Notification
        entityId:
          type: string
          example: 3fa85f64-5717-4562-b3fc-2c963f66afa6
        performedBy:
          type: string
          example: app_bancamovil_prod
        timestamp:
          type: string
          format: date-time
        details:
          type: object
          additionalProperties: true

    # -----------------------------------------------------------------------
    # PROBLEM DETAILS (RFC 7807)
    # -----------------------------------------------------------------------
    ProblemDetails:
      type: object
      required:
        - type
        - title
        - status
      properties:
        type:
          type: string
          format: uri
          description: URI de referencia que categoriza el tipo de error ocurrido.
          example: https://notify.consultoria.com/errors/validation-failed
        title:
          type: string
          description: Resumen corto y legible del tipo de problema.
          example: Error de Validación de Parámetros
        status:
          type: integer
          description: Código de estado HTTP retornado.
          example: 400
        detail:
          type: string
          description: Explicación detallada de la causa específica del fallo.
          example: El destinatario ingresado no corresponde a un formato E.164 válido para SMS.
        instance:
          type: string
          format: uri-reference
          description: Ruta URI donde se originó el fallo.
          example: /api/v1/notifications
        invalidParams:
          type: array
          description: Desglose campo a campo de fallos de validación (exclusivo para 400 Bad Request).
          items:
            $ref: '#/components/schemas/InvalidParamItem'
        traceId:
          type: string
          description: Identificador de correlación para soporte técnico y depuración en logs.
          example: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01

    InvalidParamItem:
      type: object
      required:
        - name
        - reason
      properties:
        name:
          type: string
          example: recipient
        reason:
          type: string
          example: El número debe iniciar con prefijo internacional (+) seguido del código de país.
```

---

# 6. Guía Rápida de Consumo e Integración (TTFHW < 30 min)

Para garantizar el objetivo de experiencia de integración (*Time to First Hello World* inferior a 30 minutos), se proporcionan ejemplos directos en cURL:

### Paso 1: Autenticación y obtención de token JWT
```bash
curl -X POST https://sandbox.notify.consultoria.com/api/v1/auth/token \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "app_demo_sandbox",
    "clientSecret": "sec_demo_123456789",
    "grantType": "client_credentials"
  }'
```
*Respuesta:*
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsIn...",
  "tokenType": "Bearer",
  "expiresIn": 3600
}
```

---

### Paso 2: Envío de Notificación con Idempotencia
```bash
curl -X POST https://sandbox.notify.consultoria.com/api/v1/notifications \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsIn..." \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: 9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d" \
  -d '{
    "channel": "EMAIL",
    "recipient": "cliente@empresa.com",
    "subject": "Prueba de Integración Exitosa",
    "body": "<h1>Hola!</h1><p>Este es tu primer mensaje enviado mediante Notify API.</p>",
    "priority": "HIGH"
  }'
```
*Respuesta Inmediata (`HTTP 202 Accepted`):*
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "PENDING",
  "channel": "EMAIL",
  "recipient": "cliente@empresa.com",
  "createdAt": "2026-10-02T21:40:00Z",
  "trackingUrl": "/api/v1/notifications/3fa85f64-5717-4562-b3fc-2c963f66afa6/status"
}
```

---

### Paso 3: Consulta Rápida de Estado
```bash
curl -X GET https://sandbox.notify.consultoria.com/api/v1/notifications/3fa85f64-5717-4562-b3fc-2c963f66afa6/status \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsIn..."
```
*Respuesta (`HTTP 200 OK`):*
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "SENT",
  "channel": "EMAIL",
  "retryCount": 0,
  "createdAt": "2026-10-02T21:40:00Z",
  "sentAt": "2026-10-02T21:40:02Z",
  "lastError": null
}
```

---

# 7. Validación del Contrato y Compatibilidad con Fases 1, 2 y 4

El diseño del contrato OpenAPI formalizado en esta Fase 3 garantiza:
1. **Alineación con la Fase 1:** Cobertura de las políticas de Rate Limiting con cabeceras `Retry-After`, modelo de créditos multicanal y soporte para los 4 roles RBAC.
2. **Alineación con la Fase 2:** Implementación estricta de las operaciones asíncronas (`202 Accepted`), soporte de `Idempotency-Key` y estructura RFC 7807/9457 para excepciones capturadas por el middleware.
3. **Alineación con la Fase 4:** Este archivo OpenAPI es directamente importable en **k6** y **Apache JMeter** para automatizar la ejecución de los escenarios de **Carga Sostenida (100–200 VUs)** y **Pico Extremo (500 VUs)**, permitiendo verificar los tiempos de respuesta y percentiles críticos (\(p90, p95, p99\)).

---
