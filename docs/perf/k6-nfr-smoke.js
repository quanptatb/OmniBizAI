import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const VUS = Number(__ENV.VUS || 20);
const DURATION = __ENV.DURATION || '1m';
const AUTH_COOKIE = __ENV.AUTH_COOKIE || '';
const ROUTES = (__ENV.ROUTES || '/,/Home/Privacy')
  .split(',')
  .map((route) => route.trim())
  .filter(Boolean);

export const options = {
  scenarios: {
    mvc_page_smoke: {
      executor: 'constant-vus',
      vus: VUS,
      duration: DURATION,
    },
  },
  thresholds: {
    'http_req_duration{scenario:mvc_page_smoke}': ['p(95)<3000'],
    'http_req_failed{scenario:mvc_page_smoke}': ['rate<0.01'],
    checks: ['rate>0.99'],
  },
};

function requestParams(route) {
  const headers = {
    Accept: 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
  };

  if (AUTH_COOKIE) {
    headers.Cookie = AUTH_COOKIE;
  }

  return {
    headers,
    tags: {
      route,
      name: `mvc:${route}`,
    },
  };
}

export default function mvcPageSmoke() {
  const responses = http.batch(
    ROUTES.map((route) => ['GET', `${BASE_URL}${route}`, null, requestParams(route)])
  );

  responses.forEach((res) => {
    check(res, {
      'MVC route returns a usable response': (r) => [200, 302].includes(r.status),
      'MVC route duration under 3s': (r) => r.timings.duration < 3000,
      'MVC route is not a missing page': (r) => r.status !== 404,
      'MVC route is not a server error': (r) => r.status < 500,
    });
  });

  sleep(1);
}
