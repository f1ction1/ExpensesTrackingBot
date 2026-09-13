# ExpenseBot

ExpenseBot is an ASP.NET Core Telegram bot that records shared expenses in PostgreSQL. Every Telegram chat has its own ledger, categories, members, drafts, and expenses.

## Architecture

- Vertical CQRS slices live in `ExpenseBot.Api/UseCases`; every use case has its own folder and MediatR command/handler.
- EF Core mappings and migrations live in `ExpenseBot.Api/Infrastructure/Persistence`.
- The webhook controller only validates and routes Telegram updates.
- PostgreSQL stores in-progress drafts and processed update IDs, so multiple API instances can safely handle webhook traffic.
- Npgsql connection pooling is enabled through one shared `NpgsqlDataSource`; EF contexts are also pooled.
- An expense can be entered as an amount only (`89.30`) or as an amount followed by an optional description (`89.30 lunch`).

## Local development

Store the Telegram token in .NET user secrets:

```powershell
dotnet user-secrets set "Telegram:Token" "<bot-token>" --project ExpenseBot.Api/ExpenseBot.Api.csproj
```

Start the API and PostgreSQL:

```powershell
docker compose up --build
```

Development applies pending migrations on API startup. The service is available at `http://localhost:8080`, PostgreSQL at `localhost:5436`, and Swagger at `/swagger`.

### Local HTTPS through Nginx

Generate a self-signed development certificate once:

```powershell
.\scripts\New-NginxCertificate.ps1 -CertificateHost localhost
```

The local Nginx profile publishes HTTPS on port `8443`. Start it together with the regular development override so the API can still load the Telegram token from .NET user secrets:

```powershell
docker compose `
  -f docker-compose.yml `
  -f docker-compose.override.yml `
  -f docker-compose.nginx.local.yml `
  up -d --build
```

Verify the proxy while explicitly allowing the local self-signed certificate for this smoke test:

```powershell
curl.exe -k https://localhost:8443/health/live
curl.exe -k -i https://localhost:8443/not-exposed
```

The first request must return `200`; the second must return `404`. The local profile also exposes readiness at `/health/ready`. The Telegram webhook is available only as `POST /api/telegram/webhook` with `Content-Type: application/json` and the `X-Telegram-Bot-Api-Secret-Token` header. Its default local secret is `local-dev-secret`; set `TELEGRAM_WEBHOOK_SECRET` to override it. Nginx limits the webhook to an average of 10 requests per second per source IP, with a burst of 20, and 10 concurrent connections per source IP.

Direct API and PostgreSQL host ports remain available in this local development combination for Swagger, debugging, and database tools. They are not published by the production configuration.

## Production

Copy `.env.example` to `.env` and replace every placeholder. `TELEGRAM_WEBHOOK_SECRET` should be a long random value and must be the same value passed to Telegram when registering the webhook.

Set `PUBLIC_WEBHOOK_HOST` to the EC2 Elastic IPv4 address. A domain is not required: Telegram supports an IP-address webhook when a matching self-signed certificate is uploaded while registering the webhook.

Generate the production certificate on EC2, replacing the example address with the Elastic IP:

```bash
chmod +x scripts/generate-nginx-certificate.sh
./scripts/generate-nginx-certificate.sh 98.82.100.5
```

This writes the public certificate to `secrets/nginx/tls.crt` and the private key to `secrets/nginx/tls.key`. The certificate contains the Elastic IP in its Subject Alternative Name and is valid for one year. Keep the private key secret. Before it expires, generate a replacement, restart Nginx, and upload the new public certificate with `setWebhook` again.

Production intentionally publishes only TCP `443`; TCP `80`, API port `8080`, and PostgreSQL port `5432` must remain closed in the EC2 security group.

Production uses separate PostgreSQL roles: the admin role is used only by the one-shot migration container, while the API runs with a restricted application role. The initialization script runs only when PostgreSQL creates a new empty data volume.

Validate and start the production stack:

```powershell
docker compose -f docker-compose.yml -f docker-compose.production.yml config
docker compose -f docker-compose.yml -f docker-compose.production.yml up -d --build
```

The one-shot `migrations` service applies migrations before the API starts. The API does not apply migrations itself in Production, which avoids startup races when multiple replicas are deployed.

Nginx is the only public service in the production stack. It accepts only the exact Telegram webhook route with the POST method, JSON content type, and request bodies up to 1 MB. It also applies per-IP request and connection limits. All other public routes return `404`. The API validates the matching webhook secret and remains reachable only inside the private Docker `edge` network; PostgreSQL is isolated on the separate `database` network.

Register the public HTTPS webhook using Telegram's `setWebhook` method. For a self-signed certificate, `certificate` must be uploaded as a file:

```bash
curl --fail-with-body -sS \
  -X POST "https://api.telegram.org/bot<TELEGRAM_TOKEN>/setWebhook" \
  -F "url=https://<ELASTIC_IP>/api/telegram/webhook" \
  -F "certificate=@secrets/nginx/tls.crt" \
  -F "secret_token=<TELEGRAM_WEBHOOK_SECRET>" \
  -F 'allowed_updates=["message","callback_query"]' \
  -F "max_connections=10"
```

Do not pass `tls.key` to Telegram; upload only `tls.crt`.

In a group or supergroup, promote the bot to administrator and grant **Delete messages**. Without that permission, Telegram does not allow the bot to remove members' `/add` and amount messages, so the clean-chat flow cannot be guaranteed.

Keep only HTTPS port `443` publicly reachable. Do not publish the API or PostgreSQL ports in Production. Nginx-to-API traffic uses HTTP only inside the private Docker network on the same host; public traffic is always HTTPS. Nginx rate limiting protects application capacity but does not replace upstream volumetric DDoS protection.

The included PostgreSQL container is a single-node deployment. Configure regular volume backups, or use a managed PostgreSQL service with automated backups and point `ConnectionStrings__Postgres` to it.

## Health checks

- `GET /health/live` checks whether the process is running.
- `GET /health/ready` verifies PostgreSQL connectivity and should be used for readiness checks.

These are application endpoints. The local Nginx profile exposes them for smoke testing; the production Nginx allowlist intentionally keeps them private.

## Migrations

The repository contains a local `dotnet-ef` tool under the ignored `.tools` directory. To create a migration after changing the model:

```powershell
.\.tools\dotnet-ef migrations add <MigrationName> --project ExpenseBot.Api/ExpenseBot.Api.csproj --startup-project ExpenseBot.Api/ExpenseBot.Api.csproj
```

Review every generated migration before committing it.
