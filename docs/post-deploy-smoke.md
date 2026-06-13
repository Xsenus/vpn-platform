# Post-deploy smoke

Post-deploy smoke проверяет production после успешного `deploy-vps`: API health, readiness, Prometheus metrics, публичные платежные провайдеры и доступность трех frontend-приложений.

## Скрипт

Файл: `scripts/post-deploy-smoke.sh`.

Обязательные переменные:

```bash
API_BASE_URL=http://127.0.0.1:8080
PUBLIC_WEB_URL=http://127.0.0.1:5173
CABINET_WEB_URL=http://127.0.0.1:5174
ADMIN_WEB_URL=http://127.0.0.1:5175
```

Запуск:

```bash
API_BASE_URL=http://127.0.0.1:8080 \
PUBLIC_WEB_URL=http://127.0.0.1:5173 \
CABINET_WEB_URL=http://127.0.0.1:5174 \
ADMIN_WEB_URL=http://127.0.0.1:5175 \
scripts/post-deploy-smoke.sh
```

## Что проверяется

- `GET /health/live` возвращает JSON со `status: ok`.
- `GET /health/ready` возвращает JSON со `status: Ready`.
- `GET /metrics` содержит `vpnplatform_http_requests_total`.
- `GET /api/public/payments/providers` возвращает JSON array.
- По умолчанию endpoint providers должен содержать хотя бы один объект с полем `provider`.
- `PUBLIC_WEB_URL`, `CABINET_WEB_URL`, `ADMIN_WEB_URL` возвращают HTML/Vite SPA entrypoint.

Если staging временно проверяется без платежного провайдера, можно явно отключить проверку непустого списка:

```bash
REQUIRE_PUBLIC_PAYMENT_PROVIDERS=false scripts/post-deploy-smoke.sh
```

Для production значение должно оставаться `true`.

## GitHub Actions

Workflow `.github/workflows/deploy-vps.yml` запускает шаг `Post-deploy smoke` после docker или systemd deploy.

Default URL:

- API: `http://$VPS_HOST:8080`
- public web в docker-режиме: `http://$VPS_HOST:5173`
- public web в systemd-режиме: `VITE_PUBLIC_WEB_URL` или `http://$VPS_HOST`
- cabinet: `http://$VPS_HOST:5174`
- admin: `http://$VPS_HOST:5175`

Optional secrets для переопределения внешних URL:

- `POST_DEPLOY_API_URL`
- `POST_DEPLOY_PUBLIC_WEB_URL`
- `POST_DEPLOY_CABINET_WEB_URL`
- `POST_DEPLOY_ADMIN_WEB_URL`

Скрипт пишет в `$GITHUB_STEP_SUMMARY` блок `Post-deploy smoke` со всеми проверенными URL и результатом.

## Доказательство для roadmap

Для `P8-CI-005` достаточно сохранить GitHub Actions log:

- шаг `Post-deploy smoke`;
- строки `[ok] API live health`, `[ok] API ready health`, `[ok] Public payment providers`;
- строки `[ok] Public web`, `[ok] Cabinet web`, `[ok] Admin web`;
- блок `$GITHUB_STEP_SUMMARY` `Post-deploy smoke`.

Не сохраняйте cookies, tokens, production `.env` или приватные headers.
