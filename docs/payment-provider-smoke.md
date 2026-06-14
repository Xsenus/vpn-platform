# Smoke-проверка платежных провайдеров

Этот документ описывает безопасную проверку реальных или sandbox-кабинетов платежных провайдеров. Локальные unit-тесты подтверждают регистрацию адаптеров, sandbox seed и fail-closed поведение, но не доказывают, что внешний кабинет YooKassa, RoboKassa, YooMoney, CloudPayments, TBank, Prodamus, Stripe или PayPal реально принимает платежи.

## Что проверять

Для каждого web-провайдера нужно пройти одинаковую цепочку:

1. Аккаунт провайдера настроен в админке и проходит кнопку "Проверить подключение".
2. Checkout создается через кабинет или публичный checkout flow.
3. Пользователь доходит до подтверждения на стороне провайдера.
4. Webhook или callback принят API и прошел проверку подписи.
5. Заказ перешел в оплаченный статус.
6. Подписка активировалась и выдала VPN-доступ.
7. Для провайдеров с refund flow отдельно проверен возврат или явно зафиксирован внешний блокер.

Telegram Stars не входит в этот отчет: это не web checkout, а отдельный Telegram invoice flow через `sendInvoice`, `pre_checkout_query` и `successful_payment`.

## Шаблон отчета

Шаблон находится в `docs/payment-provider-smoke-report.template.json`. В нем перечислены все обязательные web-провайдеры:

- `YooKassa`
- `RoboKassa`
- `YooMoney`
- `CloudPayments`
- `TBankAcquiring`
- `Prodamus`
- `Stripe`
- `PayPal`

Поля `status` должны быть одним из значений:

- `passed` - провайдер реально проверен и доказательства заполнены;
- `failed` - проверка проведена, но сценарий сломан;
- `blocked` - проверка не проведена из-за внешнего блокера;
- `skipped` - провайдер сознательно не входит в текущий релиз.

В `evidence` можно писать только безопасные идентификаторы: payment/order/webhook/subscription id, URL без query-секретов, краткий результат проверки. Нельзя сохранять токены, пароли, cookies, private headers, webhook secrets, SSH-ключи или raw payload с секретами.

## Валидатор

Обычная структурная проверка:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-payment-provider-smoke-report.ps1 -ReportPath docs\payment-provider-smoke-report.template.json
```

Production gate для заполненного отчета:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\validate-payment-provider-smoke-report.ps1 -ReportPath docs\payment-provider-smoke-report.template.json -RequireAllPassed
```

`-RequireAllPassed` должен падать на шаблоне, потому что все провайдеры изначально находятся в статусе `blocked`. Перед production-ready решением нужно создать отдельный заполненный отчет, заменить статусы на `passed` только после реальной проверки и приложить безопасные доказательства.

## Как использовать в roadmap

- Пока нет заполненного отчета с `passed` по всем web-провайдерам, `STATE-011` и пункты `P0-PAY-002` ... `P0-PAY-009` остаются открытыми.
- Если провайдер временно не используется в продукте, это нужно явно отметить как `skipped` и отразить в release decision.
- Если проверка невозможна из-за кабинета, договора, DNS, webhook URL или отсутствия тестовых денег, ставится `blocked` с коротким описанием причины.
