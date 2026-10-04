# Documento de Visión de Producto y Modelo de Negocio de API

---

# 1. Portada y Control del Documento

## Información General

* **Nombre de la API / Producto:** Notify API
* **Organización:** Consultoría de Software
* **Versión:** 1.1.0
* **Fecha de Emisión:** 2026-10-02
* **Estado:** Aprobado para Fase 1 (RA1)
* **Autores:**
  * Christian Naranjo — Ingeniero
  * Jordy Loor — Ingeniero
  * Diego Mesias — Ingeniero

---

# 2. Resumen Ejecutivo

## Problema

Las aplicaciones empresariales requieren enviar constantemente comunicaciones a sus usuarios como consecuencia de eventos de negocio. Entre estos eventos se encuentran confirmaciones de operaciones, recordatorios, cambios de estado, vencimientos, recuperación de acceso, facturación y otras comunicaciones transaccionales.

Cuando cada aplicación implementa de manera independiente la integración con correo electrónico, SMS o notificaciones push, se genera duplicación de lógica, múltiples integraciones con proveedores heterogéneos, diferentes mecanismos de control y una severa dificultad para administrar los estados y errores de las comunicaciones.

Esta situación genera una arquitectura dispersa y costosa:

```text
Sistema A (CRM)  ─────► Email (SMTP / SendGrid)
Sistema B (ERP)  ─────► Email (AWS SES)
Sistema C (Web)  ─────► SMS (Twilio)
Sistema D (App)  ─────► Push (Firebase FCM)
```

Además de multiplicar el esfuerzo y coste de mantenimiento, este enfoque dificulta incorporar nuevos canales, sustituir proveedores y obtener una visión centralizada del historial de comunicaciones.

## Solución: Notify API

**Notify** es una plataforma API-First orientada a la gestión centralizada de notificaciones empresariales.

La plataforma proporciona una API REST unificada mediante la cual diferentes aplicaciones empresariales pueden solicitar notificaciones multicanal, utilizar plantillas versionadas, consultar estados en tiempo real, gestionar reintentos automáticos y auditar el historial de operaciones.

La aplicación consumidora únicamente se integra con Notify:

```text
Aplicación Empresarial (CRM / ERP / Core)
                 │
                 ▼
             Notify API
                 │
       ┌─────────┼─────────┐
       ▼         ▼         ▼
     Email      SMS      Push
```

El backend es desarrollado en ASP.NET Core y protegido mediante tokens JWT. El servicio se despliega sobre contenedores Docker en infraestructura AWS. El procesamiento de las notificaciones se realiza de forma **asíncrona desacoplada mediante colas y workers**, separando la recepción inmediata de la solicitud de la entrega efectiva al proveedor final.

## Propuesta de Monetización y Sostenibilidad

El modelo de negocio es **B2B SaaS / API-as-a-Service**, estructurado bajo un esquema de suscripción mensual escalonada combinada con un **sistema de créditos ponderados por canal**. Este mecanismo previene riesgos de margen negativo ante la disparidad de costes entre proveedores (ej. SMS vs. Email).

Se establecen tres niveles:
* **Developer / Free:** Evaluación y desarrollo inicial (Hard Cap).
* **Growth / Pro:** Empresas en expansión con volumen recurrente y sobreconsumo flexible (*Soft Quota con Overage*).
* **Enterprise:** Grandes corporaciones con SLA personalizado y soporte dedicado.

## Impacto Esperado y Retorno de Inversión

Notify reduce el tiempo de integración de canales de comunicación de semanas a **menos de una jornada laboral**, logrando un ahorro superior al **80% en costes de desarrollo y mantenimiento** para las empresas consumidoras. Para la consultora, Notify constituye un activo tecnológico reutilizable de alto valor y un generador recurrente de ingresos operativos.

---

# 3. Visión del Producto y Propuesta de Valor

## 3.1. Declaración Canónica de Visión

> **Para** organizaciones y equipos de desarrollo que necesitan incorporar comunicaciones transaccionales en sus aplicaciones empresariales,
>
> **que** requieren integrar múltiples canales de notificación sin desarrollar y mantener individualmente cada integración con proveedores,
>
> **la API Notify es una** plataforma API-First de gestión y orquestación de notificaciones empresariales,
>
> **que** centraliza el envío, seguimiento, administración y procesamiento de comunicaciones mediante una interfaz REST unificada, resiliente y asíncrona.
>
> **A diferencia de** las integraciones aisladas y acopladas desarrolladas históricamente por cada sistema empresarial,
>
> **nuestro producto** desacopla las aplicaciones consumidoras de la infraestructura de telecomunicaciones, centralizando plantillas, estados, reintentos inteligentes, seguridad y trazabilidad auditada en un único contrato estándar.

---

## 3.2. Mapa de Propuesta de Valor

### Dolores que alivia (Pain Relievers)

* **Dolor 1 — Integraciones duplicadas y deuda técnica:** Elimina el desarrollo redundante de clientes SMTP, SDKs de SMS o librerías Push en cada proyecto.
* **Dolor 2 — Dependencia y acoplamiento directo a proveedores (Vendor Lock-in):** Introduce una capa de abstracción basada en patrones de diseño (*Adapter/Strategy*) que permite cambiar de proveedor (ej. de Twilio a AWS SNS) sin alterar las aplicaciones consumidoras.
* **Dolor 3 — Falta de trazabilidad y auditoría consolidada:** Provee un repositorio único donde consultar el ciclo de vida, marcas de tiempo, intentos de entrega y respuestas de proveedores.
* **Dolor 4 — Caídas por indisponibilidad de proveedores:** El procesamiento asíncrono con colas y mecanismos de resiliencia (*Retry con Backoff y Circuit Breaker*) previene fallos en cascada en las aplicaciones de negocio.

### Ganancias que genera (Gain Creators)

* **Ganancia 1 — Integración unificada:** Un solo contrato REST/JSON atiende correo electrónico, SMS y notificaciones móviles push.
* **Ganancia 2 — Reducción drástica del Time-to-Market:** Permite a nuevos proyectos empresariales contar con notificaciones funcionales en menos de 30 minutos (TTFHW).
* **Ganancia 3 — Observabilidad centralizada:** Métricas consolidadas sobre latencia de entrega, tasa de rebotes y costes de consumo.
* **Ganancia 4 — Resiliencia y absorción de picos:** Capacidad de absorber tráfico masivo sin saturar los enlaces con los proveedores externos.

### Tareas del cliente (Customer Jobs)

1. Notificar a usuarios finales tras eventos transaccionales (registro, compras, 2FA, alertas).
2. Garantizar la entrega omnicanal según la preferencia del usuario.
3. Gestionar plantillas dinámicas sin desplegar código nuevo.
4. Auditar el historial de entregas ante reclamos operativos o regulatorios.

---

## 3.3. Objetivos Estratégicos y Métricas Clave (KPIs)

### KPIs del Producto y Experiencia de Integración (DX)

| KPI | Objetivo | Justificación Técnica |
| :--- | :---: | :--- |
| **Time to First Hello World (TTFHW)** | < 30 minutos | Tiempo desde la consulta de Swagger hasta el primer envío exitoso en sandbox. |
| **Tiempo de Integración Inicial** | < 1 jornada (8h) | Integración completa del cliente REST en la aplicación de negocio. |
| **Disponibilidad Objetivo (SLO)** | ≥ 99.0% | Respaldo mínimo exigido para servicios transaccionales. |
| **Latencia p95 (Endpoints síncronos)** | ≤ 500 ms | Recepción, validación y encolado inmediato de la notificación bajo carga. |
| **Tasa de Error Global** | < 1.0% | Porcentaje admisible de peticiones fallidas bajo carga sostenida. |

### Parámetros de Validación Técnica (Enlace con Fase 4)

| Parámetro de Carga | Escenario Sostenido (Load Testing) | Escenario de Pico (Spike Testing) |
| :--- | :---: | :---: |
| **Concurrencia (VUs)** | 100 a 200 Virtual Users | 0 a 500 Virtual Users (Abrupto) |
| **Ramp-up (Subida)** | 2 a 3 minutos | Inmediato (10 a 15 segundos) |
| **Plateau (Meseta)** | 5 a 10 minutos | 1 a 2 minutos |
| **Ramp-down (Bajada)** | 1 a 2 minutos | Recuperación inmediata a 0 VUs |

---

# 4. Segmentación de Usuarios y Arquetipos (Personas)

## 4.1. Consumidor Técnico — Developer / Integrator
* **Perfil:** Desarrollador backend o ingeniero de integración responsable de conectar el ERP, CRM o tienda web con Notify.
* **Objetivos:** Contrato OpenAPI claro, documentación interactiva en Swagger, códigos HTTP semánticos y mensajes de error descriptivos bajo estándar RFC 7807 (Problem Details).
* **Frustraciones:** Respuestas ambiguas, SDKs desactualizados y caídas de servicio no documentadas.

## 4.2. Tomador de Decisiones — Buyer / Executive (CTO / Tech Lead)
* **Perfil:** Director de tecnología o arquitecto corporativo evaluando costes de integración, cumplimiento y escalabilidad.
* **Objetivos:** Previsibilidad de costes, eliminación de silos técnicos, trazabilidad regulatoria y reducción del TCO.
* **Frustraciones:** Sobrecostes imprevistos de infraestructura y soluciones que generan dependencia hacia un solo proveedor comercial.

## 4.3. Usuario Final Indirecto
* **Perfil:** Cliente final de la empresa consumidora (ej. cuentahabiente bancario, comprador de e-commerce).
* **Objetivo:** Recepción puntual, clara e íntegra de sus códigos de verificación, recibos o alertas de seguridad.

---

# 5. Alcance Funcional y Capacidades de la API

## 5.1. Capacidades Incluidas (In-Scope)

| Módulo / Capacidad | Descripción Funcional | Exposición |
| :--- | :--- | :---: |
| **Authentication & IAM** | Registro, emisión y validación de tokens JWT por aplicación. | REST / JSON |
| **Applications Registry** | Gestión de identidades y credenciales de las aplicaciones consumidoras. | REST / JSON |
| **Notifications Dispatch** | Ingesta, validación y encolado de solicitudes de notificación. | REST / JSON |
| **Template Engine** | Creación y renderizado de plantillas con parámetros dinámicos. | REST / JSON |
| **Channel Adapters** | Orquestación hacia adaptadores de Email, SMS y Push. | Interno / Extensible |
| **Status & Tracking** | Consulta del ciclo de vida (`PENDING`, `PROCESSING`, `SENT`, `FAILED`). | REST / JSON |
| **Resilience & Retry** | Reintentos configurables con retroceso exponencial. | Worker asíncrono |
| **Audit & History** | Registro inmutable de transacciones para auditoría. | REST / JSON |

Principales recursos expuestos:
```text
POST   /api/v1/auth/token
GET    /api/v1/applications/{id}
POST   /api/v1/notifications
GET    /api/v1/notifications/{id}/status
POST   /api/v1/notifications/{id}/retry
GET    /api/v1/templates
GET    /api/v1/audit/logs
```

## 5.2. Fuera de Alcance (Out-of-Scope)

* Pasarela de cobro integrada con tarjeta de crédito dentro de la API (la facturación se gestiona en plataforma externa).
* Creación de clientes nativos móviles (iOS/Android).
* Implementación de orquestadores complejos como Kubernetes en la versión inicial (se utilizará Docker Compose / ECS en AWS para el MVP).
* Proveedores comerciales reales de SMS/Push en el entorno de pruebas inicial (se emplean adaptadores simulados para aislar el rendimiento del backend).

---

# 6. Modelo de Negocio, Monetización y Cadena de Valor

## 6.1. Clasificación del Modelo
Monetización directa mediante **API-as-a-Service / B2B SaaS**, complementada con valor indirecto como componente reutilizable en proyectos de consultoría.

---

## 6.2. Estructura de Precios y Sistema de Créditos

Dado que los costes marginales de entrega varían drásticamente entre canales (un SMS internacional puede costar hasta 200 veces más que un correo electrónico transaccional), Notify implementa una unidad de tarificación normalizada denominada **Crédito de Comunicación**:

$$\text{1 Notificación Email o Push} = 1\text{ Crédito}$$
$$\text{1 Notificación SMS} = 20\text{ Créditos}$$

Esta ponderación protege a la plataforma contra márgenes negativos y permite al cliente flexibilidad para distribuir su consumo multicanal.

| Nivel (Tier) | Precio Base Mensual | Créditos Incluidos | Rate Limit (RPS) | Política de Excedente (Overage) | Nivel de Soporte |
| :--- | :---: | :---: | :---: | :---: | :--- |
| **Developer / Free** | \$0 | 1,000 créditos/mes | 5 req/s | Bloqueo estricto (*Hard Cap*) | Documentación / Foro |
| **Growth / Pro** | \$29 | 50,000 créditos/mes | 50 req/s | \$0.0008 / crédito extra (*Soft Quota*) | Correo (SLA 24h) |
| **Enterprise** | Personalizado | > 500,000 créditos | Negociado (>200 req/s) | Descuento por volumen escalonado | 24/7 / Canal dedicado |

---

## 6.3. Políticas de Tráfico y Continuidad Operativa

Para garantizar la estabilidad del servicio y la continuidad del negocio de los clientes, se establecen dos controles independientes:

```text
                                Petición Entrante
                                        │
                                        ▼
                   ┌─────────────────────────────────────────┐
                   │  1. Rate Limiting Inmediato (Token Bucket)│
                   └────────────────────┬────────────────────┘
                                        │
                       ¿Supera el límite de req/s?
                                  /            \
                              SÍ /              \ NO
                                ▼                ▼
                      HTTP 429 Too Many   ┌───────────────────────────┐
                          Requests        │ 2. Verificación de Cuota  │
                     (Retry-After: N)     │      Mensual de Créditos  │
                                          └─────────────┬─────────────┘
                                                        │
                                         ¿Cuota mensual agotada?
                                            /               \
                                        SÍ /                 \ NO
                                          ▼                   ▼
                            ┌─────────────────────┐      Procesar y
                            │  Evaluar Nivel      │       Encolar
                            └──────────┬──────────┘
                                       │
                         ┌─────────────┴─────────────┐
                         ▼                           ▼
                 Plan Developer                Plan Growth / Enterprise
                 (Hard Cap)                    (Soft Quota)
                 HTTP 402 / 429                Procesar notificación +
                 "Quota Exceeded"              Registrar cobro de Overage
```

1. **Protección de Frecuencia (Rate Limiting - Throttling):**
   * Controla ráfagas inmediatas mediante el algoritmo **Token Bucket**.
   * Si una aplicación sobrepasa su límite de peticiones por segundo (ej. > 50 req/s en Pro), se retorna `HTTP 429 Too Many Requests` acompañado de la cabecera `Retry-After: <segundos>`.
2. **Control de Cuota Mensual y Continuidad (Quotas & Overages):**
   * **Developer Tier:** Al alcanzar los 1,000 créditos, se bloquea el procesamiento hasta el siguiente ciclo de facturación.
   * **Growth & Enterprise Tiers:** Aplican **Soft Quota**. Los eventos transaccionales críticos (2FA, alertas) **nunca se interrumpen**. Las peticiones por encima de la cuota base se encolan y entregan con normalidad, registrando el excedente para facturación al costo unitario pactado (\$0.0008 por crédito adicional).

---

## 6.4. Economía Unitaria y Retorno de Inversión (ROI)

### Estructura de Costes y Margen Bruto (Escenario Tier Pro)

Considerando una cuenta en el nivel Growth / Pro (\$29/mes) que consume sus 50,000 créditos con una distribución típica (45,000 correos y 250 SMS = 50,000 créditos):

| Componente de Coste | Base de Cálculo | Coste Unitario Estimado |
| :--- | :--- | :---: |
| **Infraestructura AWS prorrateada** | Cómputo ECS Fargate, SQS, RDS Postgres compartido | \$3.50 USD |
| **Coste Proveedor Email (AWS SES)** | 45,000 envíos transaccionales (\$0.10 por cada 1,000) | \$4.50 USD |
| **Coste Proveedor SMS (Twilio / AWS SNS)** | 250 SMS nacionales (\$0.02 por mensaje) | \$5.00 USD |
| **Coste Operativo Total por Cliente:** | — | **\$13.00 USD** |

$$\text{Margen Bruto Proyectado} = \frac{\text{Ingreso Base} - \text{Coste Operativo}}{\text{Ingreso Base}} \times 100 = \frac{\$29.00 - \$13.00}{\$29.00} \times 100 \approx \mathbf{55.17\%}$$

### Retorno de Inversión para el Cliente Empresarial (Business ROI)

Para una empresa mediana con 3 sistemas internos:
* **Desarrollo in-house:** Requiere diseñar 3 integraciones de proveedores, tablas de reintentos, colas y dashboards. Estimación: **120 horas de ingeniería inicial + 10 horas/mes de mantenimiento** (~ \$3,600 USD el primer año).
* **Integración con Notify API:** Requiere únicamente **8 horas de desarrollo** contra un contrato OpenAPI estándar, con un coste de suscripción de \$348 USD/año en plan Pro.
* **Ahorro Neto Año 1:** **> 85% de reducción de costes directos**, acelerando el lanzamiento de sus funcionalidades críticas.

---

## 6.5. Cadena de Valor de Notify API (API Value Chain)

Notify transforma insumos básicos de infraestructura en capacidades de negocio de alto nivel:

```text
[Proveedores de Infraestructura Base / Telcos]
  └── Conectividad SMS, Servidores SMTP, Gateways APNs/FCM (Insumos crudos de bajo nivel)
          │
          ▼
[Notify Platform (Empaquetador y Orquestador de Valor)]
  └── Unificación REST, plantillas dinámicas, idempotencia, colas asíncronas, reintentos y SLA
          │
          ▼
[Desarrollador / Empresa Consumidora B2B]
  └── Integración simplificada (<30 min TTFHW), eliminación de silos y reducción de costes (TCO)
          │
          ▼
[Aplicación de Negocio (ERP / CRM / Core Bancario / E-commerce)]
  └── Automatización confiable de procesos de negocio y eventos transaccionales
          │
          ▼
[Usuario Final (Cliente del Cliente)]
  └── Recepción inmediata, confiable y segura de sus comunicaciones en el canal adecuado
```

---

# 7. Experiencia del Desarrollador (Developer Experience - DX)

## 7.1. Portal de Desarrolladores y Contrato OpenAPI
Notify expone su contrato mediante **OpenAPI 3.0 (Swagger UI)** interactivo, permitiendo:
* Visualización exhaustiva de esquemas JSON con validaciones requeridas.
* Descarga del archivo `swagger.json` para generación automática de clientes en cualquier lenguaje.
* Ejecución de pruebas en vivo contra el entorno Sandbox utilizando autenticación JWT interactiva.

## 7.2. Estrategia de Entornos
* **Sandbox / Dev:** Entorno con datos sintéticos y adaptadores de comunicación simulados, ideal para pruebas de integración continua y validación de contratos sin incurrir en costes de proveedores.
* **Production:** Entorno desplegado en AWS con alta disponibilidad, aislamiento de credenciales y conexión a los adaptadores finales.

## 7.3. Herramientas de Integración
* Especificación OpenAPI 3.0 universal.
* Soporte nativo HTTP/REST para cualquier stack (C#, Python, Node.js, Go, PHP, Java).
* Respuestas estandarizadas de error basadas en el formato **RFC 7807 (Problem Details)** para facilitar el parseo automático de excepciones.

---

# 8. Seguridad, Gobernanza y Cumplimiento

## 8.1. Autenticación y Autorización
* **Autenticación:** Basada en **JSON Web Tokens (JWT)** firmados con algoritmo HMAC-SHA256 o RSA. Cada aplicación consumidora recibe un `client_id` y `client_secret` para obtener su token temporal.
* **Control de Acceso Basado en Roles (RBAC):**
  * `ADMIN`: Gestión total de la plataforma, clientes y canales.
  * `APPLICATION`: Emisión de notificaciones y consulta de sus propios estados.
  * `AUDITOR`: Acceso de solo lectura a los registros de auditoría y métricas.

## 8.2. Cifrado y Protección de Datos
* **Tránsito:** Comunicación forzosa mediante TLS 1.3 / HTTPS.
* **Reposo:** Cifrado en base de datos relacional para datos sensibles de destinatarios.
* **Minimización de Datos:** No se almacena contenido confidencial innecesario; los registros de auditoría anonimizan identificadores personales de acuerdo con las buenas prácticas de protección de datos.

---

# 9. Análisis de Riesgos y Supuestos Críticos

## 9.1. Matriz de Riesgos

| Riesgo Identificado | Categoría | Probabilidad | Impacto | Estrategia de Mitigación |
| :--- | :---: | :---: | :---: | :--- |
| **Saturación por ráfagas repentinas de tráfico** | Técnico | Alta | Alta | Rate Limiting con Token Bucket y desacoplamiento con colas de mensajería asíncronas. |
| **Márgenes negativos por uso masivo de SMS** | Negocio | Media | Crítica | Sistema de créditos ponderados (1 SMS = 20 créditos) y sobreconsumo facturado. |
| **Caída o degradación de un proveedor externo** | Técnico | Alta | Alta | Patrón Circuit Breaker y adaptadores intercambiables (*Strategy/Adapter*). |
| **Duplicación en el envío de notificaciones** | Técnico | Media | Alta | Mecanismo de Idempotencia basado en encabezado `Idempotency-Key` en cada POST. |
| **Costes imprevistos de infraestructura en nube** | Operativo | Media | Media | Presupuestos con alarmas en AWS CloudWatch y uso de contenedores con dimensionamiento fijo en MVP. |

## 9.2. Supuestos Críticos
1. Los sistemas consumidores operan sobre arquitecturas web/empresariales capaces de comunicarse vía HTTPS/JSON.
2. La entrega de notificaciones puede admitir una latencia asíncrona de pocos segundos sin impactar la lógica central del cliente.
3. El uso de adaptadores simulados en la fase de evaluación técnica permite medir con rigor la elasticidad y capacidad del backend sin sesgos introducidos por cuotas externas de terceros.

---

# 10. Apéndices

## 10.1. Glosario Técnico y de Negocio
* **API-First:** Metodología de diseño donde la interfaz y el contrato del servicio se conciben como el producto primario antes de la implementación.
* **Token Bucket:** Algoritmo de control de flujo que acumula fichas a una tasa fija y las descuenta por cada petición, permitiendo ráfagas controladas.
* **Soft Quota:** Límite de consumo flexible que permite seguir utilizando el servicio aplicando tarifas de excedente sin cortar la operación.
* **Hard Cap:** Límite estricto de consumo que bloquea nuevas peticiones una vez alcanzado el umbral pactado.
* **Idempotencia:** Propiedad de una API que garantiza que realizar la misma petición múltiples veces produce el mismo resultado sin duplicar operaciones.

## 10.2. Arquitectura de Transición hacia Fase 2
El flujo funcional base acordado para el diseño arquitectónico de la Fase 2 es:

```text
[Cliente B2B] ──(HTTPS/POST)──► [API Gateway / Rate Limiter]
                                          │
                                   [JWT Auth Guard]
                                          │
                                   [Notify Backend]
                                          │
                                ┌─────────┴─────────┐
                                ▼                   ▼
                        [Database (Audit)]    [Message Queue]
                                                    │
                                                    ▼
                                            [Notification Worker]
                                                    │
                                         ┌──────────┼──────────┐
                                         ▼          ▼          ▼
                                      [Email]     [SMS]      [Push]
```

---
