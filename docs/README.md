# Документация VPN Platform

Этот индекс помогает быстро найти актуальные инструкции по проекту. Главный roadmap находится в `docs/PRODUCT_COMPLETION_ROADMAP.md`.

## Основные руководства

- [README проекта](../README.md) - назначение платформы, локальный запуск, Docker/VPS контекст и текущий статус проверок.
- [Changelog](../CHANGELOG.md) - сводка заметных релизов и текущего состояния.
- [Roadmap](PRODUCT_COMPLETION_ROADMAP.md) - рабочая карта проекта с отметками выполнения и доказательствами.
- [Руководство администратора](admin-guide.md) - настройка тарифов, платежей, VPN, 3x-ui, Telegram, сценариев и VPS.
- [Руководство пользователя](user-guide.md) - покупка, оплата, подключение VPN, продление, Telegram и поддержка.
- [Руководство разработчика](developer-guide.md) - архитектура, доменные сущности, state machines, тесты, безопасность и добавление провайдеров.

## Запуск и проверка

- [Локальная проверка](local-validation.md)
- [Fresh local smoke](fresh-local-smoke.md)
- [Финальный runbook запуска и проверки](final-runbook.md)
- [Backend validation gate](backend-validation-gate.md)
- [Frontend validation gate](frontend-validation-gate.md)
- [Build validation gate](build-validation-gate.md)
- [Post-deploy smoke](post-deploy-smoke.md)
- [Staging validation runbook](STAGING_VALIDATION_RUNBOOK.md)

## Платежи

- [Провайдеры оплаты](payment-providers.md)
- [Управление провайдерами оплаты](payment-provider-management.md)
- [Контрактные тесты провайдеров](payment-provider-contract-tests.md)
- [YooKassa](phase-2-payments-yookassa.md)
- [TBank](payment-tbank.md)
- [CloudPayments](payment-cloudpayments.md)
- [Prodamus](payment-prodamus.md)
- [Stripe и PayPal](payment-stripe-paypal.md)
- [Telegram Stars](payment-telegram-stars.md)

## VPN, Telegram и provisioning

- [3x-ui panel setup](x3ui-panel-setup.md)
- [Managed VPN servers](managed-vpn-servers.md)
- [Managed 3x-ui panels](managed-x3ui-panels.md)
- [Telegram bot setup](telegram-bot-setup.md)
- [Telegram account linking](telegram-account-linking.md)
- [Provisioning](provisioning.md)
- [Provisioning modes](provisioning-modes.md)
- [Live provisioning runbook](live-provisioning-runbook.md)
- [VPS rollback](vps-provisioning-rollback.md)

## Безопасность и эксплуатация

- [RBAC matrix](rbac-policy-matrix.md)
- [Rate limiting](rate-limiting.md)
- [Security headers](security-headers.md)
- [Security final checklist](security-final-checklist.md)
- [Secret scan](secret-scan.md)
- [Secret rotation](secret-rotation.md)
- [Production secret storage](production-secret-storage.md)
- [PostgreSQL schema audit](postgres-schema-audit.md)
- [PostgreSQL backup/restore](postgres-backup-restore.md)

## Frontend и UX

- [UI/UX MVP review](UI_UX_MVP_REVIEW.md)
- [Public FAQ polish](public-faq-polish.md)
- [Cabinet auth polish](cabinet-auth-polish.md)
- [Cabinet main screen](cabinet-main-screen.md)
- [Cabinet orders and payments](cabinet-orders-payments.md)
- [Cabinet support](cabinet-support.md)
- [Playwright public E2E](playwright-public-e2e.md)
- [Playwright cabinet E2E](playwright-cabinet-e2e.md)
- [Playwright admin E2E](playwright-admin-e2e.md)
- [Mobile smoke](mobile-smoke.md)
- [No console errors smoke](no-console-errors-smoke.md)

## Правило обновления

Если задача закрывает пункт roadmap или меняет поведение продукта, нужно обновить roadmap, `TEST_RESULTS.md`, релиз в "Что нового" и соответствующее руководство из этого индекса.
