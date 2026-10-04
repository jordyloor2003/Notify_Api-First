# Notify API Platform — Documentación Integral (Fases 1 a 4)

Plataforma empresarial **API-First** orientada a la orquestación, gestión y despacho desacoplado de notificaciones transaccionales multicanal (**Email, SMS y Push**). 

Desarrollada en **C# / .NET 8 LTS** bajo principios de **Clean Architecture**, patrones **GoF**, resiliencia perimetral (**Rate Limiting Token Bucket, Circuit Breaker, Idempotencia**), seguridad **JWT/RBAC**, estandarización de errores **RFC 7807**, persistencia NoSQL de alto rendimiento con **MongoDB Atlas** (con índices TTL e índices compuestos) y contenedorización con **Docker** en **AWS**.

---

## 📚 Índice de Entregables por Fase Académica

| Fase | Entregable Oficial | Descripción Técnica |
| :---: | :--- | :--- |
| **Fase 1 (RA1)** | [Fase 1: Justificación de Negocio](fase_1_vision_y_modelo_de_negocio.md) | Visión de producto (Geoffrey Moore), Canvas de Propuesta de Valor, Cadena de Valor de APIs, Modelo de Créditos Ponderados (SMS vs Email) y ROI B2B. |
| **Fase 2 (RA2)** | [Fase 2: Arquitectura y Patrones](fase_2_arquitectura_y_patrones.md) | Modelo C4 integral (Contexto, Contenedores, Componentes, Código GoF modular y Despliegue en AWS), patrones Strategy, Factory, Adapter, State, Circuit Breaker y Clean Architecture. |
| **Fase 3 (RA3)** | [Fase 3: Diseño del Modelo y OpenAPI](fase_3_diseno_modelo_y_especificacion.md)<br>*(Ver también [openapi.yaml](openapi.yaml))* | Especificación formal del contrato OpenAPI 3.0 (Swagger) en YAML, DTOs de recursos, respuestas RFC 7807 (Problem Details), Idempotency-Key y cURL. |
| **Fase 4 (RA4)** | [Fase 4: Desarrollo, Seguridad y Pruebas](fase_4_desarrollo_seguridad_despliegue.md) | Código productivo en .NET 8, seguridad JWT con RBAC, persistencia en MongoDB Atlas con fallback InMemory, suite de 20 pruebas unitarias (100% superadas), Dockerfile/Docker Compose y reporte analítico k6 (200 y 500 VUs). |

---

## 🚀 Inicio Rápido en Entorno Local

### 1. Ejecutar Pruebas Unitarias
Para validar los patrones de diseño GoF, transiciones de estado, idempotencia y reconstituciones de entidades:
```bash
dotnet test
```
*Resultado:* **20 pruebas superadas con éxito (100%)**, cobertura superior al 90%.

---

### 2. Configurar Base de Datos (MongoDB Atlas o Local)

Crea un archivo `.env` a partir de `.env.example`:
```bash
cp .env.example .env
```

* **Opción A (MongoDB Atlas en la Nube):** Pega tu cadena de conexión en `.env`:
  ```env
  MONGODB_URI=mongodb+srv://<usuario>:<password>@cluster0.abcde.mongodb.net/notifydb?retryWrites=true&w=majority
  ```
* **Opción B (MongoDB Local con Docker Compose):** Deja la variable vacía o no definida; Docker Compose levantará un contenedor con MongoDB 7 automáticamente.

---

### 3. Levantar el Ecosistema Completo con Docker Compose
Para iniciar la API, Worker, MongoDB, Redis 7 y RabbitMQ:
```bash
docker compose up -d
```
* **Swagger UI:** `http://localhost:8080`
* **RabbitMQ Dashboard:** `http://localhost:15672` *(usuario: guest / clave: guest)*
* **MongoDB:** Puerto `27017` *(o en la nube con MongoDB Atlas)*
* **Redis:** Puerto `6379`

---

### 4. Ejecutar Pruebas de Carga con k6

#### Escenario 1: Carga Sostenida (200 VUs durante 8 minutos)
```bash
k6 run load-tests/load-test-k6.js
```
*KPIs obtenidos:* Throughput promedio de **176.08 RPS**, **p95 de 36.50 ms** (\(\le 500\text{ ms}\)), **tasa de error 0.00%** (\(< 1\%\)).

#### Escenario 2: Pico Extremo (500 VUs instantáneo)
```bash
k6 run -e SCENARIO=spike load-tests/load-test-k6.js
```
*KPIs obtenidos:* Throughput pico de **445.60 RPS**, activación de **Rate Limiting (HTTP 429)** en el 11.4% de peticiones para proteger la base de datos, 0 caídas del backend y recuperación elástica en **< 3 segundos**.

---

## 🛠️ Estructura del Repositorio

```text
Notify_Api-First/
├── src/
│   ├── NotifyApi.Domain/          # Entidades, Value Objects, GoF State Pattern y Reconstitute DDD
│   ├── NotifyApi.Application/     # Casos de uso, DTOs, GoF Factory Method y Strategy Pattern
│   ├── NotifyApi.Infrastructure/  # MongoDB Atlas Driver, MongoDbContext, Adapters (SES, Twilio, FCM) y Messaging
│   ├── NotifyApi.WebApi/          # API REST, Controllers, JWT Auth, Middleware RFC 7807 y Rate Limiting
│   └── NotifyApi.Worker/          # BackgroundService, consumidor de cola desacoplado
├── tests/
│   └── NotifyApi.UnitTests/       # xUnit, FluentAssertions (20 tests unitarios)
├── docker/
│   ├── Dockerfile                 # Compilación multi-stage sobre Alpine (<120MB, non-root)
│   └── init-db.sql                # Script referencial SQL
├── load-tests/
│   └── load-test-k6.js            # Script oficial k6 para 200 y 500 VUs
├── docker-compose.yml             # Orquestador del stack completo con MongoDB Atlas/Local
├── NotifyApi.sln                  # Solución oficial .NET 8
├── openapi.yaml                   # Contrato formal OpenAPI 3.0 en YAML puro
└── README.md
```
