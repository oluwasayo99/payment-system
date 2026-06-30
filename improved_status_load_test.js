import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5003';

export const options = {
    vus: 100,
    duration: '2m',
    thresholds: {
        http_req_duration: ['p(95)<200'],
        http_req_failed: ['rate<0.01'],
    }
};

export default function () {
    let res = http.get(`${BASE_URL}/api/inventory/products/fast`);

    check(res, {
        'status is 200': (r) => r.status === 200,
        'under 200ms': (r) => r.timings.duration < 200,
        'under 500ms': (r) => r.timings.duration < 500,
    });

    sleep(0.5); // simulate user think time
}
