# Контрактные тесты платежных провайдеров

Документ описывает backend gate `P9-TST-006`: автоматическую проверку контрактов платежных провайдеров без реальных денег, внешних API и live-секретов.

## Что проверяется

- Для каждого значения `PaymentProvider` зарегистрирована ровно одна реализация `IPaymentProvider`.
- `IPaymentProviderFactory` возвращает провайдера для каждого enum-значения и падает раньше CI, если новый провайдер забыли подключить в DI.
- Для всех web-провайдеров зарегистрированы `IPaymentWebhookVerifier` и `IPaymentStatusMapper`.
- Local sandbox checkout для YooMoney, YooKassa, RoboKassa, CloudPayments, TBank, Prodamus, Stripe и PayPal проходит без сетевых вызовов.
- Telegram Stars явно закреплен как bot-only provider: он не поддерживает web checkout и остается fail-closed до полноценного Telegram invoice flow.
- Capability rules содержат одинаковый набор ключей для всех провайдеров и читаемые labels без признаков битой кодировки.

## Как запустить

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --configuration Release --filter "PaymentProviderContractTests"
```

Полная backend-проверка:

```powershell
dotnet test backend/VpnPlatform.sln --configuration Release
```

## Ограничения

Это не live smoke реальных платежей. Тесты не ходят в YooKassa, Stripe, PayPal, TBank и другие внешние API. Их задача - зафиксировать внутренний контракт платформы: регистрации, capability matrix, безопасный local sandbox и fail-closed поведение неподдержанных сценариев.

Live/sandbox smoke по реальным кабинетам провайдеров остается отдельными roadmap-пунктами `P0-PAY-*`.
