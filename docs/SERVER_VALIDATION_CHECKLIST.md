# Чеклист проверки сервера

Use this checklist on a local machine, CI runner, or staging server that has .NET 9 SDK, Node 22, npm 10+, Docker Engine, and the Docker Compose plugin installed.

This checklist must pass before Stage 3 Telegram sales-flow work starts. It does not mark the platform production-ready.

## 1. Toolchain pre-flight

```bash
dotnet --info
docker --version
docker compose version
node --version
npm --version
```

Expected result: all commands print valid versions and exit with code `0`.

## 2. Backend validation gate

From repository root:

```bash
./scripts/check-validation-safety.sh
./scripts/validate-backend.sh
```

Expected result: restore, build, tests, EF migration listing, and EF drift check all pass.

## 3. Frontend validation gate

```bash
cd frontend
npm ci
npm run typecheck
npm run build
npm run test
npm audit --audit-level=high
cd ..
```

Expected result: install, typecheck, build, tests, and high-severity audit gate pass.

## 4. Docker build/runtime validation gate

```bash
./scripts/validate-docker.sh
```

Expected result: Compose config, image builds, backend-api/telegram-bot startup, dependency checks, health endpoints, and fatal log scan all pass.

## 5. Runtime inspection commands

If the Docker stack is left running with `KEEP_STACK=1 ./scripts/validate-docker.sh`, inspect it with:

```bash
docker compose ps
docker compose logs backend-api --tail=200
docker compose logs backend-api --tail=200
docker compose logs telegram-bot --tail=200
```

If you used the validation override directly, use:

```bash
docker compose -f docker-compose.yml -f docker-compose.validation.yml ps
docker compose -f docker-compose.yml -f docker-compose.validation.yml logs backend-api --tail=200
docker compose -f docker-compose.yml -f docker-compose.validation.yml logs backend-api --tail=200
docker compose -f docker-compose.yml -f docker-compose.validation.yml logs telegram-bot --tail=200
```

## 6. Runtime endpoint checks

```bash
curl -f http://localhost:8080/health/live
curl -f http://localhost:8080/health/ready
curl -f http://localhost:8080/metrics
curl -f http://localhost:8081/health/live
curl -f http://localhost:8081/health/ready
```

Expected result: every command exits with code `0`.

## 7. Safe validation assertions

Confirm these before recording a green gate:

- no real Telegram token was required;
- `TelegramBot__Enabled=false` during validation;
- no live payment credentials were required;
- payment provider modes were disabled by validation overrides;
- no live VPS/provisioning run was started;
- no live 3x-ui panel was contacted;
- logs do not contain Telegram tokens, payment secrets, SSH credentials, or 3x-ui passwords.

## 8. Cleanup

If the validation stack is still running:

```bash
docker compose -f docker-compose.yml -f docker-compose.validation.yml down --remove-orphans
```

If you intentionally ran the base Compose file without validation override:

```bash
docker compose down --remove-orphans
```
