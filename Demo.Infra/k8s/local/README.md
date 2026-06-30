# K8s Local Fintech Deployment

## TLS Certificate Setup

For the ingress to work with TLS, you need to create a TLS secret in Kubernetes with your certificate and key.

### Generate Self-Signed Certificate (Recommended for Local Development)

Run these commands in the `../certs/` directory:

**Windows (PowerShell):**
```powershell
# Generate private key and self-signed certificate
openssl req -x509 -nodes -days 365 -newkey rsa:2048 `
  -keyout tls.key -out tls.crt `
  -subj "/CN=payments.local" `
  -addext "subjectAltName=DNS:payments.local,DNS:*.payments.local"

# Create the TLS secret in Kubernetes
kubectl create secret tls payments-local-tls --cert=tls.crt --key=tls.key
```

**Linux/Mac:**
```bash
# Generate private key and self-signed certificate
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout tls.key -out tls.crt \
  -subj "/CN=payments.local" \
  -addext "subjectAltName=DNS:payments.local,DNS:*.payments.local"

# Create the TLS secret in Kubernetes
kubectl create secret tls payments-local-tls --cert=tls.crt --key=tls.key
```

### Use Existing Certificate Files

If you already have valid `tls.crt` and `tls.key` files in `../certs/`:

```bash
kubectl create secret tls payments-local-tls \
  --cert=../certs/tls.crt \
  --key=../certs/tls.key
```

## Deployment Order

Apply the manifests in this order:

```bash
# 1. Infrastructure services (databases, message queue, cache)
kubectl apply -f postgres.yml
kubectl apply -f redis.yml
kubectl apply -f rabitmq.yml

# 2. Application services
kubectl apply -f payment.yml
kubectl apply -f ledger.yml
kubectl apply -f settlement.yml

# 3. TLS Secret (after generating valid certs - see TLS Certificate Setup above)
kubectl create secret tls payments-local-tls --cert=../certs/tls.crt --key=../certs/tls.key

# 4. Ingress (must be last, depends on secret)
kubectl apply -f ingress.yml
```

## Accessing the Application

After deployment, add the following to your hosts file:

**Windows:** `C:\Windows\System32\drivers\etc\hosts`
**Linux/Mac:** `/etc/hosts`

```
127.0.0.1  payments.local
```

Then access the application at:
- HTTP: http://payments.local (will redirect to HTTPS due to SSL redirect annotation)
- HTTPS: https://payments.local

## Ingress Configuration

The ingress is configured to:
- Route all traffic to the `payment-service`
- Terminate TLS at the ingress controller
- Force SSL redirect (HTTP → HTTPS)
- Support the hostname `payments.local`

## Services Overview

| Service | Type | Port | Description |
|---------|------|------|-------------|
| payment-service | ClusterIP | 80 | Main payment API |
| ledger-service | ClusterIP | 80 | Ledger service |
| settlement-service | ClusterIP | 80 | Settlement service |
| postgres-service | ClusterIP | 5432 | PostgreSQL database |
| redis-service | ClusterIP | 6379 | Redis cache |
| rabbitmq-service | ClusterIP | 5672/15672 | RabbitMQ (AMQP & Management UI) |
