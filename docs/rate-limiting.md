# Rate limiting API

Rate limiting защищает публичные точки входа, которые чаще всего используются для перебора, спама или повторной доставки внешних событий.

## Политики

| Policy | Лимит | Окно | Где применяется |
| --- | --- | --- | --- |
| `auth-sensitive` | 10 запросов | 60 секунд | `register`, `login`, `refresh`, `forgot-password`, `reset-password` |
| `public-checkout` | 20 запросов | 60 секунд | создание публичной checkout-сессии и legacy `POST /api/public/orders` |
| `webhook` | 120 запросов | 60 секунд | платежные webhook и channel webhook endpoints |

Ключ лимита строится из policy, IP-адреса и пути запроса. Если API работает за reverse proxy, сначала используется первый адрес из `X-Forwarded-For`, иначе `RemoteIpAddress`.

## Поведение при превышении

API возвращает HTTP `429 Too Many Requests` и JSON problem response:

```json
{
  "type": "https://httpstatuses.com/429",
  "title": "Too many requests",
  "status": 429,
  "detail": "Request rate limit exceeded. Please retry later."
}
```

## Правила изменения

1. Новую policy нужно добавить в `ApiRateLimitPolicies`.
2. Опасный публичный endpoint должен получить `[EnableRateLimiting(...)]`.
3. Для новой policy нужно добавить тест в `RateLimitingSecurityTests`.
4. Не нужно лимитировать `/health/*` и `/metrics`: эти endpoints нужны мониторингу.

## Проверка

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter RateLimitingSecurityTests --logger "console;verbosity=minimal"
```
