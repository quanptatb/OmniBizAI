import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const STAFF_TOKEN = __ENV.STAFF_TOKEN || '';
const DIRECTOR_TOKEN = __ENV.DIRECTOR_TOKEN || STAFF_TOKEN;
const VUS = Number(__ENV.VUS || 50);
const DURATION = __ENV.DURATION || '3m';
const RUN_AI = __ENV.RUN_AI === 'true';

export const options = {
  scenarios: {
    api_smoke: {
      executor: 'constant-vus',
      vus: VUS,
      duration: DURATION
    }
  },
  thresholds: {
    'http_req_duration{scenario:api_smoke}': ['p(95)<500'],
    'http_req_failed{scenario:api_smoke}': ['rate<0.01'],
    checks: ['rate>0.99']
  }
};

if (RUN_AI) {
  options.scenarios.ai_smoke = {
    executor: 'constant-arrival-rate',
    rate: 6,
    timeUnit: '1m',
    duration: '3m',
    preAllocatedVUs: 10,
    maxVUs: 20,
    startTime: '3m30s',
    exec: 'aiScenario'
  };

  options.thresholds['http_req_duration{name:ai_chat}'] = ['p(95)<10000'];
}

function headers(token) {
  return {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json'
  };
}

export default function apiScenario() {
  const endpoints = [
    '/api/v1/dashboard/overview',
    '/api/v1/payment-requests',
    '/api/v1/kpis',
    '/api/v1/notifications/unread-count'
  ];

  const responses = http.batch(
    endpoints.map((path) => ['GET', `${BASE_URL}${path}`, null, { headers: headers(STAFF_TOKEN) }])
  );

  responses.forEach((res) => {
    check(res, {
      'API status is 200': (r) => r.status === 200,
      'API duration under 500ms': (r) => r.timings.duration < 500
    });
  });

  sleep(1);
}

export function aiScenario() {
  const payload = JSON.stringify({
    message: 'Tom tat 3 rui ro ngan sach lon nhat trong thang nay, kem citation.',
    sessionId: null
  });

  const res = http.post(`${BASE_URL}/api/v1/ai/chat`, payload, {
    headers: headers(DIRECTOR_TOKEN),
    tags: { name: 'ai_chat' },
    timeout: '15s'
  });

  check(res, {
    'AI returns success or controlled fallback': (r) => [200, 202, 503].includes(r.status),
    'AI duration under 10s': (r) => r.timings.duration < 10000,
    'AI response has citation or fallback marker': (r) =>
      (r.body || '').includes('citation') || (r.body || '').includes('fallback')
  });

  sleep(1);
}
