import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// ==============================================================================
// Notify API - Script Oficial de Pruebas de Rendimiento (k6)
// Escenarios: Carga Sostenida (200 VUs) y Pico Extremo (500 VUs)
// ==============================================================================

// Métricas personalizadas
const acceptedCounter = new Counter('http_202_accepted_total');
const rateLimitCounter = new Counter('http_429_rate_limited_total');
const errorRate = new Rate('custom_error_rate');
const dispatchDuration = new Trend('notification_dispatch_duration_ms', true);

// Selección de escenario mediante variable de entorno: k6 run -e SCENARIO=spike load-test-k6.js
const selectedScenario = __ENV.SCENARIO || 'sustained';

export const options = {
  scenarios: {
    // ESCENARIO 1: Carga Sostenida (0 -> 200 VUs -> 200 VUs meseta -> 0 VUs)
    sustained_load_test: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '2m', target: 200 },  // Ramp-up (Subida progresiva)
        { duration: '5m', target: 200 },  // Plateau (Meseta sostenida)
        { duration: '1m', target: 0 },    // Ramp-down (Bajada a 0)
      ],
      gracefulRampDown: '30s',
      exec: 'runTestFlow',
      tags: { scenario: 'sustained' },
    },
    // ESCENARIO 2: Pico Extremo / Spike (0 -> 500 VUs inmediato -> 0 VUs recuperación)
    spike_test: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '15s', target: 500 }, // Disparo abrupto e instantáneo
        { duration: '2m', target: 500 },  // Pico extremo sostenido
        { duration: '15s', target: 0 },   // Recuperación inmediata a 0
      ],
      gracefulRampDown: '15s',
      exec: 'runTestFlow',
      tags: { scenario: 'spike' },
    }
  },
  thresholds: {
    // Criterio de Evaluación: p95 inferior a 500ms
    'http_req_duration': ['p(95)<500', 'p(99)<1000'],
    // Criterio de Calificación Excelente: Tasa de error < 1% en carga sostenida
    'custom_error_rate': ['rate<0.01'],
  }
};

// Si se define un escenario específico por CLI, deshabilitar el otro
if (selectedScenario === 'sustained') {
  delete options.scenarios.spike_test;
} else if (selectedScenario === 'spike') {
  delete options.scenarios.sustained_load_test;
}

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080/api/v1';

// FASE DE SETUP: Autenticación inicial y obtención del JWT Bearer
export function setup() {
  const authPayload = JSON.stringify({
    clientId: 'app_bancamovil_prod',
    clientSecret: 'sec_99a8b7c6d5e4f3a2b1c0',
    grantType: 'client_credentials'
  });

  const authParams = {
    headers: { 'Content-Type': 'application/json' },
  };

  const res = http.post(`${BASE_URL}/auth/token`, authPayload, authParams);
  
  if (res.status !== 200) {
    throw new Error(`Fallo de autenticación en Setup (Status ${res.status}): ${res.body}`);
  }

  const json = res.json();
  return { token: json.accessToken };
}

// FLUJO DE PRUEBA PRINCIPAL EJECUTADO POR CADA USUARIO VIRTUAL (VU)
export function runTestFlow(data) {
  const token = data.token;
  const correlationId = generateUUID();
  const idempotencyKey = generateUUID();

  const channels = ['EMAIL', 'SMS', 'PUSH'];
  const channel = channels[Math.floor(Math.random() * channels.length)];

  let recipient = 'usuario.k6@empresa.com';
  if (channel === 'SMS') recipient = '+593991234567';
  if (channel === 'PUSH') recipient = 'fcm_token_device_k6_virtual_user';

  const payload = JSON.stringify({
    channel: channel,
    recipient: recipient,
    templateCode: channel === 'EMAIL' ? 'WELCOME_USER' : undefined,
    subject: `Prueba de Carga k6 - ${correlationId.substring(0, 8)}`,
    body: `Mensaje de estrés enviado a las ${new Date().toISOString()}`,
    templateVariables: { nombre: `VU_${__VU}` },
    priority: 'HIGH'
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`,
      'Idempotency-Key': idempotencyKey,
      'X-Correlation-Id': correlationId,
    },
  };

  // 1. Envío de Notificación Transaccional (POST)
  const startTime = Date.now();
  const res = http.post(`${BASE_URL}/notifications`, payload, params);
  const latency = Date.now() - startTime;
  dispatchDuration.add(latency);

  // Registro de códigos de estado HTTP
  if (res.status === 202) {
    acceptedCounter.add(1);
    errorRate.add(0);
  } else if (res.status === 429) {
    rateLimitCounter.add(1);
    // 429 es protección legítima del Rate Limiter (Token Bucket), no se cuenta como fallo 5xx
    errorRate.add(0);
  } else {
    errorRate.add(1);
  }

  check(res, {
    'Status es 202 Accepted o 429 Rate Limited': (r) => r.status === 202 || r.status === 429,
    'Header Location presente en 202': (r) => r.status !== 202 || r.headers['Location'] !== undefined,
    'Latencia síncrona menor a 500ms': (r) => r.timings.duration < 500,
  });

  // 2. Consulta de Estado (Polling representativo en el 20% de peticiones exitosas)
  if (res.status === 202 && Math.random() < 0.20) {
    const json = res.json();
    if (json && json.id) {
      const statusRes = http.get(`${BASE_URL}/notifications/${json.id}/status`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      check(statusRes, {
        'Status de consulta es 200 OK': (r) => r.status === 200
      });
    }
  }

  // Pequeña pausa estocástica (pensamiento de usuario: 200 a 500 ms)
  sleep(Math.random() * 0.3 + 0.2);
}

// Generador auxiliar de UUIDv4 en JavaScript para k6
function generateUUID() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
    const r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}
