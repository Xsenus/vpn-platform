# Публикация на GitHub и автодеплой на VPS

Репозиторий содержит GitHub Actions для проверки проекта и деплоя на VPS. Секреты, пароли, SSH-ключи и реальные `.env` файлы нельзя коммитить в репозиторий.

## Workflows

- `.github/workflows/ci.yml` проверяет backend, frontend, provisioning и Docker-сборку.
- `.github/workflows/staging-validation.yml` запускает расширенную staging-проверку.
- `.github/workflows/deploy-vps.yml` валидирует проект и деплоит на VPS при push в `main`/`master` или ручном запуске.

## Режимы деплоя

Workflow `deploy-vps` поддерживает три режима:

- `auto` - режим по умолчанию. GitHub Actions подключается к VPS по SSH и проверяет `docker compose`. Если Docker доступен, используется Docker-деплой. Если Docker не найден, используется systemd/nginx-деплой без Docker.
- `docker` - принудительно использовать Docker Compose на VPS.
- `systemd` - принудительно использовать деплой без Docker: backend и frontend собираются в GitHub Actions, затем выкладываются на VPS в `/opt/vpn-platform`, API перезапускается через `systemd`, frontend отдается через `nginx`.

Режим можно задать двумя способами:

- При ручном запуске workflow выбрать `deploy_mode`.
- Через repository secret `VPS_DEPLOY_MODE` со значением `auto`, `docker` или `systemd`.

Если `VPS_DEPLOY_MODE` не задан, используется `auto`.

Подробная проверка auto-detect, ожидаемые строки GitHub Actions log и пример блока `$GITHUB_STEP_SUMMARY` описаны в `docs/deploy-vps-auto-detect.md`.

Обязательные checks для `main`/`master`, конфиг branch protection и скрипт применения описаны в `docs/github-required-checks.md`.

Аудит имен GitHub Actions secrets без раскрытия значений описан в `docs/github-secrets-audit.md`.

## GitHub Secrets

В GitHub открой `Settings -> Secrets and variables -> Actions -> New repository secret` и добавь:

- `VPS_HOST` - IP или домен VPS.
- `VPS_USER` - SSH-пользователь, например `root`.
- `VPS_PORT` - SSH-порт. Если не задан, используется `22`.
- `VPS_APP_DIR` - директория приложения. Если не задана, используется `/opt/vpn-platform`.
- `VPS_SSH_KEY` - приватный SSH-ключ без пароля для деплоя.
- `PRODUCTION_ENV_FILE` - полный production `.env`.
- `VPS_DEPLOY_MODE` - необязательно: `auto`, `docker` или `systemd`.
- `VITE_API_BASE_URL` - необязательно для systemd-сборки frontend. Если не задан, будет `http://<VPS_HOST>:8080`.
- `VITE_PUBLIC_WEB_URL` - необязательно для systemd-сборки frontend. Если не задан, будет `http://<VPS_HOST>`.

Пароль от VPS лучше не использовать в GitHub Actions. Создай отдельный SSH-ключ:

```bash
ssh-keygen -t ed25519 -C "github-actions-vpn-platform" -f ~/.ssh/vpn-platform-deploy
ssh-copy-id -i ~/.ssh/vpn-platform-deploy.pub root@<VPS_HOST>
```

Содержимое приватного файла `~/.ssh/vpn-platform-deploy` вставь в secret `VPS_SSH_KEY`.

## Требования к VPS

Для режима `docker`:

- Docker Engine.
- Docker Compose plugin.
- `curl`.
- Достаточно памяти и места для сборки контейнеров.

Для режима `systemd`:

- Ubuntu/Debian с `systemd`.
- `nginx`.
- `curl`.
- PostgreSQL, если база запускается не в Docker.
- Настроенный `PRODUCTION_ENV_FILE` с подключением к реальной базе.

На слабом VPS лучше использовать `systemd`, потому что Docker-сборка на сервере требует больше RAM и диска.

## Production `.env`

Для Docker-варианта база обычно находится внутри compose-сети:

```env
Database__Provider=Postgres
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=vpnplatform;Username=vpnplatform;Password=<password>
DATABASE_URL=postgres://vpnplatform:<password>@postgres:5432/vpnplatform
REDIS_CONNECTION=redis:6379
```

Для systemd-варианта база обычно локальная на VPS:

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
Database__Provider=Postgres
Database__ApplyMigrationsOnStartup=true
Database__SeedDemoData=false
ConnectionStrings__DefaultConnection=Host=127.0.0.1;Port=5432;Database=vpnplatform;Username=vpnplatform;Password=<password>
Jwt__Issuer=VpnPlatform
Jwt__Audience=VpnPlatform.Clients
Jwt__SigningKey=<long-random-secret>
Security__SecretEncryptionKey=<long-random-secret>
PUBLIC_WEB_ORIGIN=http://<VPS_HOST>
CABINET_ORIGIN=http://<VPS_HOST>:5174
ADMIN_PANEL_ORIGIN=http://<VPS_HOST>:5175
Cors__AllowedOrigins__0=http://<VPS_HOST>
Cors__AllowedOrigins__1=http://<VPS_HOST>:5174
Cors__AllowedOrigins__2=http://<VPS_HOST>:5175
Swagger__Enabled=false
AdminBootstrap__Enabled=false
Provisioning__LiveExecutionEnabled=false
Provisioning__AllowLiveDeploy=false
TelegramBot__Enabled=false
```

Платежные провайдеры, Telegram bot и live provisioning включай только после добавления реальных учетных данных.

## Как работает `deploy-vps`

1. На push в `main`/`master` запускается job `validate`.
2. Проверяются `.NET 9`, backend build/test, frontend typecheck/test/build и `docker compose config`.
3. Job `deploy` подключается к VPS по SSH.
4. В режиме `auto` workflow сам выбирает `docker` или `systemd`.
5. В режиме `docker` загружается архив исходников и на VPS выполняется `docker compose up -d --build --remove-orphans`.
6. В режиме `systemd` GitHub Actions собирает self-contained API под `linux-x64`, собирает frontend, загружает архив на VPS, заменяет `/opt/vpn-platform/api` и `/opt/vpn-platform/web`, затем перезапускает `vpn-platform-api`.
7. После деплоя проверяются `http://127.0.0.1:8080/health/live` и `/health/ready`.
8. При неудачном systemd health check workflow пытается откатить предыдущие папки `api` и `web`.

## Проверка после деплоя

На VPS:

```bash
systemctl status vpn-platform-api --no-pager
curl -fsS http://127.0.0.1:8080/health/live
curl -fsS http://127.0.0.1:8080/health/ready
curl -I http://127.0.0.1/
```

Для Docker-режима:

```bash
cd /opt/vpn-platform/current
docker compose --project-name vpnplatform --env-file .env ps
docker compose --project-name vpnplatform --env-file .env logs --tail=100 backend-api telegram-bot
```
