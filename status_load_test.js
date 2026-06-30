import http from 'k6/http';
import { check } from 'k6';

export const options = {
    vus: 100,
    duration: '2m',
    thresholds: {
        http_req_duration: ['p(95)<200'],
        http_req_failed: ['rate<0.01'],
    }
}


export default function ()
{
    let res = http.get('http://localhost:5003/api/inventory/products/fast');
    check(res, {
        'status is 200': (r) => r.status === 200,
        'under 200ms': (r) => r.timings.duration < 200,
        'under 500ms': (r) => r.timings.duration < 500,
    })
}