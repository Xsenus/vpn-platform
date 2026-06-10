# Привязка Telegram к аккаунту

Раздел закрывает roadmap-блок `P1-TG-002`: пользователь может привязать Telegram к существующему аккаунту, проверить статус привязки и отвязать Telegram из личного кабинета.

## Сценарий пользователя

1. Пользователь входит в личный кабинет.
2. В блоке `Telegram` нажимает `Создать ссылку на бота`.
3. API создает одноразовый deep link вида `https://t.me/<bot>?start=link_<token>`.
4. Пользователь открывает ссылку в Telegram.
5. Бот принимает `/start link_<token>`, проверяет hash токена, срок действия и одноразовость.
6. TelegramAccount связывается с User, а token помечается использованным.
7. В кабинете `GET /api/me/telegram/status` возвращает `isLinked=true`.
8. Пользователь может нажать `Отвязать Telegram`; `DELETE /api/me/telegram/unlink` очищает `UserId` и `LinkedAt` у TelegramAccount.

## Защита от ошибок

- Deep link token хранится только как SHA-256 hash.
- Token действует 10 минут.
- Использованный token нельзя применить повторно.
- Если Telegram уже привязан к другому аккаунту, бот не перепривязывает его.
- Если у пользователя уже есть привязанный Telegram, новый link-token не создается.
- Если заранее созданный второй link открыть после успешной привязки, бот отвечает, что Telegram уже привязан, и помечает link использованным.

## Проверка

Backend:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter "TelegramBotFoundationTests"
```

Frontend:

```powershell
cd frontend
npm test
```

Полная проверка перед релизом:

```powershell
dotnet test backend\VpnPlatform.sln --configuration Release
cd frontend
npm run typecheck
npm run build
```
