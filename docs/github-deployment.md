# Публикация на GitHub и автодеплой на VPS

Репозиторий содержит GitHub Actions для проверки проекта и деплоя на VPS. Секреты, пароли, SSH-ключи и реальные `.env` файлы нельзя коммитить в репозиторий.

## Workflows

CI `Ansible syntax check` writes its temporary inventory into a per-run temp directory and removes it on exit.

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

Безопасная очистка диска/RAM на слабом VPS описана в `docs/vps-maintenance.md`.

Автоматическая smoke-проверка после deploy описана в `docs/post-deploy-smoke.md`.

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
Email__Mode=Smtp
Email__AllowDisabledInProduction=false
Auth__PasswordReset__Enabled=true
Email__Host=<smtp-host>
Email__Port=587
Email__UseSsl=true
Email__FromAddress=<no-reply-address>
Email__FromName=VPN Platform
Email__Username=<smtp-user>
Email__Password=<smtp-secret>
```

Workflow перед upload запускает `scripts/normalize-production-env.ps1 -RequireStartupReady` и принудительно сохраняет production-safe флаги в загружаемом `.env`: `ASPNETCORE_ENVIRONMENT=Production`, `AdminBootstrap__Enabled=false`, пустой `AdminBootstrap__Password`, `AdminBootstrap__ResetExistingPassword=false`, `Database__ApplyMigrationsOnStartup=false`, `Database__SeedDemoData=false`, `Swagger__Enabled=false`, `Email__AllowDisabledInProduction=false`. После нормализации preflight требует `Email__Mode=Smtp`, непустой `Email__Host`, порт `1..65535`, валидный `Email__FromAddress` и `Email__Password`, если задан `Email__Username`. Проверка выполняется before upload и сообщает только имена неполных настроек, не их значения. Первый admin/reset выполняйте отдельным `admin-bootstrap`/`admin-vps-bootstrap-smoke`, а не постоянным bootstrap в shared `.env`.

Платежные провайдеры, Telegram bot и live provisioning включай только после добавления реальных учетных данных. Для временного ручного запуска без SMTP включите workflow input `allow_disabled_email`; normalizer принудительно выставит `Email__Mode=Disabled`, `Email__AllowDisabledInProduction=true` и `Auth__PasswordReset__Enabled=false`. Такой API запускается без email worker, а password reset отвечает `503`; обычный deploy остаётся fail-closed.

Если новый release требует pending EF migrations, вручную включите `apply_database_migrations` только для systemd deploy. Workflow остановит API, запустит опубликованную команду `database-migrate` с тем же systemd `EnvironmentFile`, создаст custom dump в `/opt/vpn-platform/backups/db`, проверит его через `pg_restore --list` и лишь затем применит миграции. При любой ошибке старый API запускается обратно до замены release directories. Без явного input схема не меняется; наличие `pg_dump`, `pg_restore` и `systemd-run` обязательно.

`scripts/test-normalize-production-env.ps1` removes its autogenerated production.env fixtures and empty `tmp` directory after ordinary local runs; pass a custom `-OutputDirectory` when you need to keep fixtures for debugging.

## Как работает `deploy-vps`

1. На push в `main`/`master` запускается job `validate`.
2. Проверяются `.NET 9`, backend build/test, frontend typecheck/test/build и `docker compose config`.
3. Job `deploy` подключается к VPS по SSH.
4. В режиме `auto` workflow сам выбирает `docker` или `systemd`.
5. Workflow нормализует `PRODUCTION_ENV_FILE` в `production.env` и до upload проверяет SMTP либо явно выбранный degraded mode, чтобы stale секреты не включили Local/Swagger/demo seed/auto migrations/постоянный admin bootstrap и не остановили рабочий API поздним startup failure.
6. В режиме `docker` загружается архив исходников и нормализованный env, затем на VPS выполняется `docker compose up -d --build --remove-orphans`.
7. В режиме `systemd` GitHub Actions собирает self-contained API под `linux-x64`, собирает frontend и загружает архив с нормализованным env. При явном `apply_database_migrations` API останавливается, создаётся и проверяется PostgreSQL backup и применяются pending migrations; затем workflow заменяет `/opt/vpn-platform/api` и `/opt/vpn-platform/web` и перезапускает `vpn-platform-api`.
8. После деплоя проверяются `http://127.0.0.1:8080/health/live` и `/health/ready`.
9. При неудачном systemd health check workflow пытается откатить предыдущие папки `api` и `web`.

`Start Docker production stack` writes the remote Docker compose config check into a per-run temp directory and removes that temporary compose config artifact on exit.

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
