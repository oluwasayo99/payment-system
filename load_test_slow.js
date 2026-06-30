import http from 'k6/http';

export const options = {
    stages: [
        { duration: '30s', target: 100 },
        { duration: '1m', target: 100 }
    ]
}

export default function ()
{
    http.get('http://localhost:5003/api/inventory/products/slow')
}