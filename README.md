# mtls-auth-dotnet

Projeto de exemplo com dois aplicativos .NET que se comunicam via mTLS:

- Payment.Api (cliente) chama o Webhook usando certificado de cliente
- Webhook (servidor) exige certificado de cliente de confiança (CA)

## Estrutura

- `Solution/Payment.Api` (API cliente)
- `Solution/Webhook` (API servidor)
- `Solution/*/Certificates` (certificados usados no runtime e no build)

## Portas e URLs (Docker)

- Payment.Api: `https://localhost:8080`
- Webhook: `https://localhost:8081`
- A Payment.Api chama o Webhook por `https://webhook:8081/` (nome do serviço em rede Docker)

## Variáveis de ambiente usadas

Payment.Api:

- `Webhook__BaseUrl` (ex: `https://webhook:8081/`)
- `Webhook__ClientCertificate__Path` (ex: `/app/Certificates/payment-api-client.pfx`)
- `Webhook__ClientCertificate__Password`
- `ASPNETCORE_URLS` (ex: `https://+:8080`)
- `ASPNETCORE_Kestrel__Certificates__Default__Path` (ex: `/app/Certificates/payment-api-server.pfx`)
- `ASPNETCORE_Kestrel__Certificates__Default__Password`

Webhook:

- `Webhook__ClientCertificate__AuthorityPath` (ex: `/app/Certificates/webhook-ca.cer`)
- `ASPNETCORE_URLS` (ex: `https://+:8081`)
- `ASPNETCORE_Kestrel__Certificates__Default__Path` (ex: `/app/Certificates/webhook-server.pfx`)
- `ASPNETCORE_Kestrel__Certificates__Default__Password`

## Como criar chaves e certificados (bash + openssl)

Requisitos:

- `openssl` instalado
- Bash (Linux/macOS/WSL)

Os comandos abaixo geram:

- CA raiz (para validar clientes no Webhook)
- Certificado do servidor Webhook
- Certificado do servidor Payment.Api
- Certificado de cliente da Payment.Api

Onde cada arquivo vai ficar:

- CA raiz: `Solution/Webhook/Certificates`
- Certificado de servidor do Webhook: `Solution/Webhook/Certificates`
- Certificado de servidor do Payment.Api: `Solution/Payment.Api/Certificates`
- Certificado de cliente do Payment.Api: `Solution/Payment.Api/Certificates`

```bash
set -euo pipefail

ROOT_DIR="$(pwd)"
PAY_CERTS="Solution/Payment.Api/Certificates"
WEB_CERTS="Solution/Webhook/Certificates"

mkdir -p "$PAY_CERTS" "$WEB_CERTS"

CA_KEY="$WEB_CERTS/webhook-ca.key"
CA_CERT="$WEB_CERTS/webhook-ca.cer"

# 1) CA raiz
openssl req -x509 -new -nodes -sha256 -days 3650 \
  -subj "/CN=mtls-webhook-ca" \
  -keyout "$CA_KEY" -out "$CA_CERT"

# 2) Webhook server (serverAuth + SAN)
WEBHOOK_KEY="$WEB_CERTS/webhook-server.key"
WEBHOOK_CSR="$WEB_CERTS/webhook-server.csr"
WEBHOOK_CRT="$WEB_CERTS/webhook-server.crt"
WEBHOOK_PFX="$WEB_CERTS/webhook-server.pfx"

cat > "$WEB_CERTS/webhook-server.ext" <<'EOF'
basicConstraints=CA:FALSE
keyUsage = digitalSignature, keyEncipherment
extendedKeyUsage = serverAuth
subjectAltName = @alt_names
[alt_names]
DNS.1 = webhook
DNS.2 = localhost
IP.1 = 127.0.0.1
EOF

openssl req -new -newkey rsa:2048 -nodes \
  -subj "/CN=webhook" \
  -keyout "$WEBHOOK_KEY" -out "$WEBHOOK_CSR"

openssl x509 -req -sha256 -days 825 \
  -in "$WEBHOOK_CSR" -CA "$CA_CERT" -CAkey "$CA_KEY" -CAcreateserial \
  -out "$WEBHOOK_CRT" -extfile "$WEB_CERTS/webhook-server.ext"

openssl pkcs12 -export \
  -out "$WEBHOOK_PFX" -inkey "$WEBHOOK_KEY" -in "$WEBHOOK_CRT" \
  -passout pass:change_me

# 3) Payment.Api server (serverAuth + SAN)
PAY_SERVER_KEY="$PAY_CERTS/payment-api-server.key"
PAY_SERVER_CSR="$PAY_CERTS/payment-api-server.csr"
PAY_SERVER_CRT="$PAY_CERTS/payment-api-server.crt"
PAY_SERVER_PFX="$PAY_CERTS/payment-api-server.pfx"

cat > "$PAY_CERTS/payment-api-server.ext" <<'EOF'
basicConstraints=CA:FALSE
keyUsage = digitalSignature, keyEncipherment
extendedKeyUsage = serverAuth
subjectAltName = @alt_names
[alt_names]
DNS.1 = payment-api
DNS.2 = localhost
IP.1 = 127.0.0.1
EOF

openssl req -new -newkey rsa:2048 -nodes \
  -subj "/CN=payment-api" \
  -keyout "$PAY_SERVER_KEY" -out "$PAY_SERVER_CSR"

openssl x509 -req -sha256 -days 825 \
  -in "$PAY_SERVER_CSR" -CA "$CA_CERT" -CAkey "$CA_KEY" -CAcreateserial \
  -out "$PAY_SERVER_CRT" -extfile "$PAY_CERTS/payment-api-server.ext"

openssl pkcs12 -export \
  -out "$PAY_SERVER_PFX" -inkey "$PAY_SERVER_KEY" -in "$PAY_SERVER_CRT" \
  -passout pass:change_me

# 4) Payment.Api client cert (clientAuth)
PAY_CLIENT_KEY="$PAY_CERTS/payment-api-client.key"
PAY_CLIENT_CSR="$PAY_CERTS/payment-api-client.csr"
PAY_CLIENT_CRT="$PAY_CERTS/payment-api-client.crt"
PAY_CLIENT_PFX="$PAY_CERTS/payment-api-client.pfx"

cat > "$PAY_CERTS/payment-api-client.ext" <<'EOF'
basicConstraints=CA:FALSE
keyUsage = digitalSignature, keyEncipherment
extendedKeyUsage = clientAuth
EOF

openssl req -new -newkey rsa:2048 -nodes \
  -subj "/CN=payment-api-client" \
  -keyout "$PAY_CLIENT_KEY" -out "$PAY_CLIENT_CSR"

openssl x509 -req -sha256 -days 825 \
  -in "$PAY_CLIENT_CSR" -CA "$CA_CERT" -CAkey "$CA_KEY" -CAcreateserial \
  -out "$PAY_CLIENT_CRT" -extfile "$PAY_CERTS/payment-api-client.ext"

openssl pkcs12 -export \
  -out "$PAY_CLIENT_PFX" -inkey "$PAY_CLIENT_KEY" -in "$PAY_CLIENT_CRT" \
  -passout pass:change_me

echo "OK - certificados gerados"
```

Arquivos esperados pelo código e pelo Dockerfile:

- `Solution/Webhook/Certificates/webhook-ca.cer`
- `Solution/Webhook/Certificates/webhook-server.pfx`
- `Solution/Payment.Api/Certificates/payment-api-server.pfx`
- `Solution/Payment.Api/Certificates/payment-api-client.pfx`

## Validação simples em bash (openssl + curl)

Verifique se o cliente foi assinado pela CA:

```bash
openssl verify -CAfile Solution/Webhook/Certificates/webhook-ca.cer \
  Solution/Payment.Api/Certificates/payment-api-client.crt
```

Verifique detalhes do certificado do cliente:

```bash
openssl x509 -in Solution/Payment.Api/Certificates/payment-api-client.crt -text -noout
```

Teste o mTLS no Webhook local:

```bash
# extrai PEM do PFX para uso com curl
openssl pkcs12 -in Solution/Payment.Api/Certificates/payment-api-client.pfx \
  -clcerts -nokeys -out /tmp/payment-api-client.crt \
  -passin pass:change_me

openssl pkcs12 -in Solution/Payment.Api/Certificates/payment-api-client.pfx \
  -nocerts -nodes -out /tmp/payment-api-client.key \
  -passin pass:change_me

curl -vk https://localhost:8081/webhook \
  --cacert Solution/Webhook/Certificates/webhook-ca.cer \
  --cert /tmp/payment-api-client.crt \
  --key /tmp/payment-api-client.key
```

## Build e execução com Docker

```bash
docker build -t payment-api -f Solution/Payment.Api/Dockerfile .
docker build -t webhook -f Solution/Webhook/Dockerfile .

docker network create mtls-net || true

docker run -d --name webhook --network mtls-net -p 8081:8081 webhook
docker run -d --name payment-api --network mtls-net -p 8080:8080 payment-api
```

## Dicas

- Troque `change_me` por senha segura no runtime (env var).
- Se quiser montar certificados por volume, mantenha os nomes iguais.
- No Docker, a Payment.Api chama o Webhook por `https://webhook:8081/`.
