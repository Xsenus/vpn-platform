# Публикация на GitHub и автодеплой на VPS

Эта инструкция описывает подготовку репозитория к GitHub и автоматический деплой через GitHub Actions. Реальные пароли, токены, SSH-ключи и `.env` нельзя коммитить в репозиторий.

## Что уже настроено

- `.gitignore` исключает локальные логи, скриншоты, базы, артефакты тестов, `.env`, `node_modules`, `bin/obj` и временные файлы Codex/Playwright.
- `.dockerignore` и `frontend/.dockerignore` уменьшают Docker build context и не отправляют секреты/мусор в Docker daemon.
- `.github/workflows/ci.yml` проверяет backend, frontend, provisioning и Docker-сборку.
- `.github/workflows/staging-validation.yml` делает расширенную ручную проверку staging/runtime smoke.
- `.github/workflows/deploy-vps.yml` валидирует проект и деплоит на VPS при push в `main`/`master` или ручном запуске.
- `docker-compose.yml` читает безопасный шаблон `.env.example` и, если рядом есть приватный `.env`, переопределяет значения из него.

## Первый push в GitHub

```powershell
git init
git branch -M main
git add .
git commit -m "Initial production-ready VPN platform"
git remote add origin git@github.com:<owner>/<repo>.git
git push -u origin main
```

Перед коммитом проверь, что в выводе `git status` нет `.env`, логов, скриншотов, локальных баз и архивов.

## GitHub Secrets для автодеплоя

В GitHub открой `Settings -> Secrets and variables -> Actions -> New repository secret` и добавь:

- `VPS_HOST` - IP или домен VPS.
- `VPS_USER` - пользователь для SSH, например `root` или отдельный deploy-пользователь.
- `VPS_PORT` - SSH-порт. Если не задан, workflow использует `22`.
- `VPS_APP_DIR` - директория приложения на VPS. Если не задана, используется `/opt/vpn-platform`.
- `VPS_SSH_KEY` - приватный SSH-ключ без пароля для деплоя.
- `PRODUCTION_ENV_FILE` - полный production `.env` для сервера.

Пароль от VPS лучше не использовать в GitHub Actions. Создай отдельный SSH-ключ и добавь публичную часть в `~/.ssh/authorized_keys` на VPS:

```bash
ssh-keygen -t ed25519 -C "github-actions-vpn-platform" -f ~/.ssh/vpn-platform-deploy
ssh-copy-id -i ~/.ssh/vpn-platform-deploy.pub root@<VPS_HOST>
```

Содержимое приватного файла `~/.ssh/vpn-platform-deploy` вставь в secret `VPS_SSH_KEY`.

## Минимальные требования к VPS

На сервере должны быть установлены:

- Docker Engine.
- Docker Compose plugin (`docker compose version` должен работать).
- `curl`.
- открытые порты для нужных сервисов или настроенный reverse proxy.

Базовая установка на Ubuntu/Debian:

```bash
apt update
apt upgrade -y
apt install -y ca-certificates curl gnupg
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
chmod a+r /etc/apt/keyrings/docker.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" > /etc/apt/sources.list.d/docker.list
apt update
apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
docker compose version
```

## Production `.env`

`PRODUCTION_ENV_FILE` должен быть полноценным `.env`, основанным на `.env.example`, но с production-значениями:

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Database__Provider=Postgres
Database__ApplyMigrationsOnStartup=true
Database__SeedDemoData=false

POSTGRES_DB=vpnplatform
POSTGRES_USER=vpnplatform
POSTGRES_PASSWORD=<strong-password>
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=vpnplatform;Username=vpnplatform;Password=<strong-password>
DATABASE_URL=postgres://vpnplatform:<strong-password>@postgres:5432/vpnplatform
REDIS_CONNECTION=redis:6379

Jwt__Issuer=VpnPlatform
Jwt__Audience=VpnPlatform.Clients
Jwt__SigningKey=<long-random-secret>
Security__SecretEncryptionKey=<long-random-secret>

VITE_API_BASE_URL=https://api.example.com
VITE_PUBLIC_WEB_URL=https://example.com
PUBLIC_WEB_ORIGIN=https://example.com
CABINET_ORIGIN=https://cabinet.example.com
ADMIN_PANEL_ORIGIN=https://admin.example.com
Cors__AllowedOrigins__0=https://example.com
Cors__AllowedOrigins__1=https://cabinet.example.com
Cors__AllowedOrigins__2=https://admin.example.com

Swagger__Enabled=false
AdminBootstrap__Enabled=false
Provisioning__LiveExecutionEnabled=false
Provisioning__AllowLiveDeploy=false
```

Платежные провайдеры, Telegram bot и 3x-ui включай только после добавления реальных учетных данных.

## Как работает deploy workflow

1. На push в `main`/`master` запускается job `validate`.
2. Проверяются `.NET 9`, backend build/test, frontend typecheck/test/build и `docker compose config`.
3. Если проверки прошли, job `deploy` подключается к VPS по SSH.
4. Workflow загружает архив релиза в `$VPS_APP_DIR/releases/<sha>`.
5. Secret `PRODUCTION_ENV_FILE` сохраняется на сервере как `$VPS_APP_DIR/shared/.env`.
6. На VPS выполняется `docker compose --project-name vpnplatform --env-file .env up -d --build --remove-orphans`.
7. Workflow проверяет `http://127.0.0.1:8080/health/live` и `/health/ready`.
8. Последние 5 релизов сохраняются в `$VPS_APP_DIR/releases`, старые удаляются.

## Ручная проверка после деплоя

На VPS:

```bash
cd /opt/vpn-platform/current
docker compose --project-name vpnplatform --env-file .env ps
docker compose --project-name vpnplatform --env-file .env logs --tail=100 backend-api telegram-bot
curl -fsS http://127.0.0.1:8080/health/live
curl -fsS http://127.0.0.1:8080/health/ready
```

Если используется reverse proxy, дополнительно проверь публичные адреса API, сайта, кабинета и админки.
