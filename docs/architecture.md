# Архитектура платформы

## Репозиторий

- `backend/src/VpnPlatform.Domain` — доменные сущности и статусы
- `backend/src/VpnPlatform.Application` — use cases, сервисы, контракты
- `backend/src/VpnPlatform.Infrastructure` — EF Core, адаптеры платежей, 3x-ui, auth, hosted workers
- `backend/src/VpnPlatform.Api` — HTTP API, webhooks и фоновые задачи
- `frontend/apps/public-web` — маркетинговый сайт
- `frontend/apps/cabinet` — личный кабинет
- `frontend/apps/admin-panel` — админка
- `frontend/packages/api-client` — TS API client
- `frontend/packages/ui` — shared UI components

## Границы модулей

1. Auth / Users
2. Catalog / Tariffs
3. Orders
4. Payments
5. Subscriptions
6. VPN Provisioning
7. Channel Integrations
8. Notifications
9. Referrals / Promo
10. VPN Nodes / Servers
11. Provisioning / Auto-deployment
12. Monitoring / Audit / Backups

## Source of truth

Источник истины по заказам, оплатам, подпискам, пользователям и доступам — **backend + PostgreSQL**.

Внешние системы:
- платежный провайдер — источник событий об оплате
- 3x-ui — внешний исполнительный контур
- бот/сайт/админка — интерфейсы доступа к ядру

## Паттерны надежности

- idempotency-key на критичных POST
- dedup webhook через inbox table
- outbox pattern для уведомлений и внешних побочных действий
- background retry workers внутри API host
- health checks
- correlation id в логах
