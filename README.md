# 🚀 Notify API Platform — Guía de Despliegue y Ejecución Local

Plataforma empresarial **API-First** orientada a la orquestación, gestión y despacho desacoplado de notificaciones transaccionales multicanal (**Email, SMS y Push Notification**).

Desarrollada en **C# / .NET 8 LTS** bajo principios de **Clean Architecture**, patrones **GoF** (Strategy, Factory Method, Adapter, State), resiliencia perimetral (**Rate Limiting Token Bucket, Circuit Breaker, Idempotencia**), seguridad **JWT / RBAC**, especificación formal **OpenAPI 3.0**, estandarización de errores **RFC 7807**, persistencia NoSQL con **MongoDB Atlas / Local** (con fallback InMemory automático) y contenedorización con **Docker & Docker Compose**.

Swagger Notify API: [http://52.15.152.202](http://52.15.152.202/)

---

## 📋 Tabla de Contenidos
- [Requisitos Previos](#-requisitos-previos)
- [Levantar Localmente: Opción 1 (Con .NET CLI - Rápido sin Docker)](#-opción-1-levantar-con-net-cli-rápido-y-sin-docker)
- [Levantar Localmente: Opción 2 (Con Docker Compose - Ecosistema Completo)](#-opción-2-levantar-con-docker-compose-stack-completo)
- [Autenticación y Credenciales Semilla (JWT)](#-autenticación-y-credenciales-semilla-jwt)
- [Flujo de Prueba Rápido (Endpoints Principales)](#-flujo-de-prueba-rápido-endpoints-principales)
- [Ejecución de Pruebas Unitarias y de Carga](#-ejecución-de-pruebas)
- [Variables de Entorno (`.env`)](#-variables-de-entorno-env)
- [Estructura del Repositorio](#-estructura-del-repositorio)
- [Solución de Problemas Frecuentes (Troubleshooting)](#-solución-de-problemas-frecuentes-troubleshooting)

---

## 💻 Requisitos Previos

Dependiendo del método de ejecución que elijas, requerirás:

| Herramienta | Versión Mínima | Uso |
| :--- | :---: | :--- |
| **.NET SDK** | **8.0 LTS** | Requerido para compilación, ejecución con CLI y tests unitarios. |
| **Docker Desktop** | **4.x+** *(Docker Compose v2)* | Requerido únicamente para la [Opción 2 (Docker Compose)](#-opción-2-levantar-con-docker-compose-stack-completo). |
| **Git** | Cualquiera | Para clonar y gestionar el repositorio. |
| **k6** *(Opcional)* | **0.48+** | Para ejecutar las suites de pruebas de carga y estrés. |
| **cURL / Postman** | Cualquiera | Para realizar pruebas de los endpoints HTTP. |

Verifica la instalación de .NET ejecutando en tu terminal:
```bash
dotnet --version
# Debe retornar 8.0.xxx
```

---

## ⚡ Opción 1: Levantar con .NET CLI (Rápido y sin Docker)

Esta opción es ideal para desarrollo rápido. La API incluye un **mecanismo de fallback InMemory**, lo que permite levantar y probar la plataforma inmediatamente **sin necesidad de tener bases de datos ni servicios externos instalados**. El servicio consumidor en segundo plano (*Background Worker*) se ejecuta automáticamente dentro del mismo proceso.

### Paso 1. Clonar el repositorio y situarse en la raíz
```bash
cd c:\ruta\raiz\Notify_Api-First
```

### Paso 2. Crear el archivo de variables de entorno `.env`
Copia el archivo de plantilla `.env.example`:

**En Linux / macOS / Git Bash:**
```bash
cp .env.example .env
```
**En Windows PowerShell:**
```powershell
Copy-Item .env.example .env
```

> 💡 **Nota sobre Base de Datos:** Si dejas `MONGODB_URI` vacío en `.env`, el sistema usará automáticamente el repositorio **InMemory** con plantillas y credenciales ya cargadas. Si deseas usar **MongoDB Atlas**, simplemente coloca tu cadena de conexión en `MONGODB_URI`.

### Paso 3. Restaurar dependencias y compilar
```bash
dotnet restore
dotnet build
```

### Paso 4. Iniciar la Web API
Ejecuta el proyecto WebApi especificando el puerto HTTP deseado (por ejemplo, el puerto `8080`):
```bash
dotnet run --project src/NotifyApi.WebApi --urls "http://localhost:8080"
```

Verás en la consola los mensajes de inicio:
```text
info: NotifyApi.Worker.Worker[0]
      Notify Worker iniciado. Esperando trabajos en cola...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:8080
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Paso 5. Acceder a Swagger UI
Abre tu navegador en:
👉 **[http://localhost:8080](http://localhost:8080)**

---

## 🐳 Opción 2: Levantar con Docker Compose (Stack Completo)

Levanta la infraestructura empresarial completa en contenedores aislados:
* **`notify-api`**: Servicio Web API en .NET 8 (Alpine, no-root).
* **`mongo-db`**: Base de datos NoSQL MongoDB 7.
* **`redis-cache`**: Caché distribuida Redis 7 Alpine (Rate Limiting e Idempotencia).
* **`rabbitmq-broker`**: Broker de mensajería asíncrona RabbitMQ 3.12 con interfaz web de administración.

### Paso 1. Asegurar que Docker Desktop esté en ejecución
Inicia Docker Desktop en tu sistema operativo.

### Paso 2. Configurar `.env` (Opcional)
```bash
cp .env.example .env
```
*(Si no defines `MONGODB_URI`, Docker Compose conectará la API automáticamente al contenedor `mongo-db` local).*

### Paso 3. Construir e iniciar todos los servicios
```bash
docker compose up --build -d
```

### Paso 4. Verificar que todos los contenedores estén saludables
```bash
docker compose ps
```

Salida esperada:
```text
NAME                 IMAGE                              STATUS         PORTS
notify_api_service   notify_api-first-notify-api        Up             0.0.0.0:8080->8080/tcp
notify_mongodb       mongo:7-jammy                      Up             0.0.0.0:27017->27017/tcp
notify_redis         redis:7-alpine                     Up (healthy)   0.0.0.0:6379->6379/tcp
notify_rabbitmq      rabbitmq:3.12-management-alpine   Up (healthy)   0.0.0.0:5672->5672/tcp, 0.0.0.0:15672->15672/tcp
```

### Paso 5. URLs y Credenciales de Acceso a Servicios

| Servicio | URL Local | Credenciales / Detalles |
| :--- | :--- | :--- |
| **API & Swagger UI** | **[http://localhost:8080](http://localhost:8080)** | Documentación interactiva OpenAPI v1 |
| **RabbitMQ Management** | **[http://localhost:15672](http://localhost:15672)** | **Usuario:** `guest` \| **Contraseña:** `guest` |
| **MongoDB Local** | `localhost:27017` | Base de datos `notifydb` |
| **Redis Cache** | `localhost:6379` | Servidor Redis sin contraseña |

### Comandos útiles de Docker Compose:
* **Ver logs de la API en tiempo real:**
  ```bash
  docker compose logs -f notify-api
  ```
* **Detener los servicios:**
  ```bash
  docker compose down
  ```
* **Detener y borrar datos de volúmenes:**
  ```bash
  docker compose down -v
  ```

---

## 🔐 Autenticación y Credenciales Semilla (JWT)

Todos los endpoints de despacho y consulta de notificaciones están protegidos con autenticación **JWT Bearer** y control de acceso basado en roles (**RBAC**).

### Cuentas Semilla Preconfiguradas

La base de datos (tanto en modo InMemory como en MongoDB) incluye las siguientes aplicaciones cliente:

| Rol | Client ID | Client Secret | Tier | Rate Limit (RPS) |
| :--- | :--- | :--- | :--- | :---: |
| **APPLICATION** | `app_bancamovil_prod` | `sec_99a8b7c6d5e4f3a2b1c0` | Growth | 50 RPS |
| **ADMIN** | `app_admin_master` | `sec_admin_master_12345` | Enterprise | 200 RPS |

### 1. Obtener Token JWT

#### Vía cURL:
```bash
curl -X POST http://localhost:8080/api/v1/auth/token \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "app_bancamovil_prod",
    "clientSecret": "sec_99a8b7c6d5e4f3a2b1c0",
    "grantType": "client_credentials"
  }'
```

#### Vía PowerShell:
```powershell
$authBody = @{
    clientId = "app_bancamovil_prod"
    clientSecret = "sec_99a8b7c6d5e4f3a2b1c0"
    grantType = "client_credentials"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/auth/token" -Method Post -ContentType "application/json" -Body $authBody
$token = $response.accessToken
Write-Host "Token JWT obtenido con éxito: $token"
```

**Respuesta recibida (HTTP 200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "scope": "notifications:write notifications:read"
}
```

### 2. Autenticarse en Swagger UI
1. Abre **`http://localhost:8080`**.
2. Haz clic en el botón verde **Authorize** (arriba a la derecha).
3. En el campo de valor escribe:
   ```text
   Bearer TU_ACCESS_TOKEN_AQUI
   ```
4. Haz clic en **Authorize** y luego en **Close**. Ahora podrás ejecutar cualquier endpoint directamente desde la interfaz.

---

## 🧪 Flujo de Prueba Rápido (Endpoints Principales)

Una vez obtenido tu `$TOKEN`, prueba las siguientes operaciones:

### 1. Despachar Notificación por Email (con Plantilla e Idempotencia)
```bash
curl -X POST http://localhost:8080/api/v1/notifications \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: e4a70b21-4f18-4b71-9f97-8d2a6a4c2810" \
  -d '{
    "channel": "EMAIL",
    "recipient": "usuario@ejemplo.com",
    "templateCode": "WELCOME_USER",
    "templateVariables": {
      "nombre": "Ana Maria Gomez"
    },
    "priority": "HIGH"
  }'
```
**Respuesta (HTTP 202 Accepted):**
```json
{
  "id": "76ef7785-5b48-4cb2-8cf0-21a100fb62d2",
  "status": "QUEUED",
  "trackingUrl": "/api/v1/notifications/76ef7785-5b48-4cb2-8cf0-21a100fb62d2",
  "createdAt": "2026-10-07T19:50:00Z"
}
```
> 💡 *Nota de Idempotencia:* Si repites la misma petición con el mismo `Idempotency-Key`, la API retornará HTTP `200 OK` con la notificación existente sin duplicar el envío.

### 2. Despachar Código 2FA por SMS
```bash
curl -X POST http://localhost:8080/api/v1/notifications \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "channel": "SMS",
    "recipient": "+593991234567",
    "templateCode": "2FA_CODE",
    "templateVariables": {
      "codigo": "739104"
    },
    "priority": "URGENT"
  }'
```

### 3. Consultar Estado de una Notificación
```bash
curl -X GET http://localhost:8080/api/v1/notifications/76ef7785-5b48-4cb2-8cf0-21a100fb62d2 \
  -H "Authorization: Bearer <TOKEN>"
```
**Respuesta:**
```json
{
  "id": "76ef7785-5b48-4cb2-8cf0-21a100fb62d2",
  "channel": "EMAIL",
  "recipient": "usuario@ejemplo.com",
  "status": "SENT",
  "attemptsCount": 1,
  "attempts": [
    {
      "attemptNumber": 1,
      "provider": "SmtpLocal",
      "status": "SUCCESS",
      "latencyMs": 4
    }
  ]
}
```

### 4. Consultar Plantillas Disponibles
```bash
curl -X GET http://localhost:8080/api/v1/templates \
  -H "Authorization: Bearer <TOKEN>"
```

### 5. Consultar Canales Soportados
```bash
curl -X GET http://localhost:8080/api/v1/channels \
  -H "Authorization: Bearer <TOKEN>"
```

---

## 🔬 Ejecución de Pruebas

### 1. Pruebas Unitarias (xUnit)
La suite contiene **20 pruebas unitarias automatizadas** que validan:
* Patrones de diseño GoF: **Strategy** (Email, SMS, Push), **Factory Method**, **Adapter** y **State** (`Pending` → `Processing` → `Sent`/`Failed`/`Retry`).
* Reconstitución DDD de entidades de dominio e hidratación de agregados.
* Mecanismos de deduplicación e idempotencia con TTL.
* Seguridad y emisión de credenciales JWT con RBAC.
* Resiliencia e inyección de dependencias con fallback automático InMemory.

> 📄 **Reportes detallados generados:**
> * Reporte interactivo Web: [`report.html`](file:///c:/Users/FanId/Desktop/BinaSystem/Notify_Api-First/report.html) (Abrir en navegador para ver métricas, filtros y búsqueda).
> * Documentación técnica Markdown: [`TEST_REPORT.md`](file:///c:/Users/FanId/Desktop/BinaSystem/Notify_Api-First/TEST_REPORT.md).

Para ejecutarlas:
```bash
dotnet test
```

Salida de la última ejecución:
```text
Pruebas totales: 20
     Correcto: 20 (100% exitosas)
  Con error: 0
    Omitido: 0
 Tiempo total: 34,8 s
```

### 2. Pruebas de Rendimiento y Estrés con k6

Con la API levantada en `http://localhost:8080`, ejecuta las pruebas de carga:

#### Escenario 1: Carga Sostenida (200 VUs)
Evalúa el throughput continuo y latencia durante 8 minutos:
```bash
k6 run load-tests/load-test-k6.js
```
* **Métricas clave:** Throughput promedio **~176 RPS**, **p95 < 40 ms** (umbral de aceptación: $\le 500\text{ ms}$), tasa de error **0.00%**.

#### Escenario 2: Pico Extremo (Spike de 500 VUs)
Evalúa la protección de Rate Limiting y recuperación elástica ante sobrecargas abruptas:
```bash
k6 run -e SCENARIO=spike load-tests/load-test-k6.js
```
* **Métricas clave:** Protección activa con respuestas **HTTP 429 Too Many Requests**, cero caídas del servidor y estabilización en **< 3 segundos**.

---

## ⚙️ Variables de Entorno (`.env`)

| Variable | Descripción | Valor Predeterminado | Requerido |
| :--- | :--- | :--- | :---: |
| `ASPNETCORE_ENVIRONMENT` | Entorno de ASP.NET Core (`Development`, `Production`). | `Development` | No |
| `PORT` | Puerto de escucha en contenedor. | `8080` | No |
| `JWT_SECRET` | Clave secreta simétrica HMAC-SHA256 para firma y validación de tokens. | *(Clave por defecto de desarrollo)* | Recomendado |
| `JWT_ISSUER` | Emisor de los tokens JWT emitidos. | `NotifyApiPlatform` | No |
| `JWT_AUDIENCE` | Audiencia destinataria de los tokens. | `NotifyApiClients` | No |
| `MONGODB_URI` | Cadena de conexión de MongoDB (Atlas o Local). Si se omite, se usa **InMemory**. | *(Vacío = InMemory)* | Opcional |
| `MONGODB_DATABASE` | Nombre de la base de datos NoSQL. | `notifydb` | No |
| `REDIS_HOST` | Host para caché Redis (en Docker: `redis-cache`). | `localhost` | No |
| `REDIS_PORT` | Puerto de conexión Redis. | `6379` | No |
| `RABBITMQ_HOST` | Host del broker AMQP (en Docker: `rabbitmq-broker`). | `localhost` | No |
| `RABBITMQ_PORT` | Puerto AMQP de RabbitMQ. | `5672` | No |
| `RABBITMQ_USER` | Usuario de RabbitMQ. | `guest` | No |
| `RABBITMQ_PASSWORD` | Contraseña de RabbitMQ. | `guest` | No |

---

## 📂 Estructura del Repositorio

El proyecto implementa una arquitectura modular desacoplada (**Clean Architecture**):

```text
Notify_Api-First/
├── src/
│   ├── NotifyApi.Domain/          # Capa de Dominio pura: Entidades, Value Objects, Enums y Patrón GoF State
│   ├── NotifyApi.Application/     # Casos de uso, DTOs, interfaces, GoF Factory Method y GoF Strategy Pattern
│   ├── NotifyApi.Infrastructure/  # MongoDbContext (Atlas/Local), Repositorios InMemory, GoF Adapters (SMTP, Twilio, FCM)
│   ├── NotifyApi.WebApi/          # API REST, Controllers, Filtros de Rate Limiting, JWT Bearer y RFC 7807 Middleware
│   └── NotifyApi.Worker/          # BackgroundService desacoplado consumidor de la cola de notificaciones
├── tests/
│   └── NotifyApi.UnitTests/       # Suite de pruebas unitarias xUnit y FluentAssertions (20 tests)
├── docker/
│   ├── Dockerfile                 # Compilación multi-stage en .NET 8 SDK y ejecución sobre Alpine (<120MB, non-root)
│   └── init-db.sql                # Script SQL referencial
├── load-tests/
│   └── load-test-k6.js            # Script oficial de k6 (escenarios sustained y spike)
├── docker-compose.yml             # Orquestador local con API, MongoDB 7, Redis 7 y RabbitMQ 3.12
├── NotifyApi.sln                  # Archivo de solución oficial de Visual Studio / .NET CLI
├── openapi.yaml                   # Especificación formal del contrato OpenAPI 3.0 en YAML puro
├── .env.example                   # Plantilla de variables de entorno
└── README.md                      # Esta guía
```

---

## ❓ Solución de Problemas Frecuentes (Troubleshooting)

### 1. El puerto 8080 ya está en uso
Si otro servicio en tu máquina está usando el puerto 8080:
* **Con .NET CLI:** Cambia el puerto en el comando:
  ```bash
  dotnet run --project src/NotifyApi.WebApi --urls "http://localhost:5090"
  ```
  Accede a Swagger en `http://localhost:5090`.
* **Con Docker Compose:** Modifica el mapeo de puertos en `docker-compose.yml`:
  ```yaml
  ports:
    - "8081:8080"
  ```

### 2. Error 401 Unauthorized en llamadas a la API
* Verifica haber ejecutado previamente el endpoint `POST /api/v1/auth/token` con las credenciales semilla.
* Asegúrate de agregar el prefijo `Bearer ` antes del token en el encabezado `Authorization`.
* Recuerda que el token tiene una vigencia de 1 hora. Si expira, genera uno nuevo.

### 3. Error de conexión con MongoDB Atlas
Si configuras `MONGODB_URI` con un clúster de MongoDB Atlas y experimentas `TimeoutException`:
* Ingresa a tu panel de **MongoDB Atlas** → **Network Access**.
* Agrega tu dirección IP pública actual o habilita temporalmente acceso global (`0.0.0.0/0`) para propósitos de prueba y desarrollo.
* Como alternativa inmediata, borra temporalmente `MONGODB_URI` en tu archivo `.env` para que la aplicación arranque en modo **InMemory**.

### 4. Docker Desktop no inicia o muestra error de permisos
* Asegúrate de que el motor de Docker (Docker Engine) esté completamente iniciado antes de ejecutar `docker compose up`.
* En Windows, confirma que el backend de WSL2 esté habilitado en la configuración de Docker Desktop.
