# Требования к окружению

## Минимум для запуска без Docker

| Инструмент | Версия | Зачем нужен |
|---|---:|---|
| .NET SDK | 9.x | Backend, API, tests, EF tooling |
| Node.js | 22+ | Vite/React приложения |
| npm | 10+ | Установка frontend-зависимостей |
| PowerShell | 5+ | Скрипты `start-local.ps1` и `stop-local.ps1` |

Локальный режим использует SQLite-файл `data/vpnplatform-local.db`, поэтому PostgreSQL, Redis, RabbitMQ, Docker и Ansible не обязательны.

## Полная проверка с инфраструктурой

| Инструмент | Версия | Зачем нужен |
|---|---:|---|
| Docker Desktop / Docker Engine | актуальная стабильная | PostgreSQL, Redis, RabbitMQ, observability и compose smoke |
| Docker Compose plugin | v2.x | Запуск `docker compose` |
| Python | 3.11+ | Unit-тесты Ansible runner |
| Ansible / ansible-core | актуальная | Syntax-check и реальный provisioning |
| Git | актуальная | Проверка drift миграций в CI |

## Режимы backend

- `Local` - запуск без Docker: SQLite, demo seed, demo admin, sandbox payments/VPN.
- `Development` - разработка с PostgreSQL и локальной инфраструктурой.
- `Production` - строгий режим без demo seed, sandbox и SQLite.

## Безопасные значения по умолчанию

- `TelegramBot:Enabled=false`.
- `Email:Mode=Disabled` локально; полноценный Production требует `Email:Mode=Smtp`, host, port и from address. Временный degraded запуск допускается только с `Email:AllowDisabledInProduction=true` и отключает password reset/email delivery.
- `Vpn:X3Ui:Mode=Sandbox` для локальной проверки.
- `Provisioning:LiveExecutionEnabled=false`.
- `Provisioning:AllowLiveDeploy=false`.
- Реальные токены Telegram, SMTP credentials, платёжные секреты, SSH-ключи и пароли 3x-ui нельзя хранить в репозитории.

Полный SMTP-контракт и порядок проверки описаны в [email-delivery.md](email-delivery.md).

## Быстрая диагностика

```powershell
dotnet --info
node --version
npm --version
docker --version
docker compose version
python --version
ansible-playbook --version
```

Отсутствие Docker или Ansible не блокирует Local-режим, но ограничивает полную инфраструктурную проверку.
