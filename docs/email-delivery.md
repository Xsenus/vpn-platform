# Email-доставка

API материализует пользовательские email-уведомления из durable outbox в `NotificationDeliveries`. Отдельный worker захватывает pending-записи условным обновлением, восстанавливает stale lease, применяет exponential backoff и после пяти неудачных попыток переводит запись в `Failed`.

## Конфигурация

Локально доставка по умолчанию отключена:

```text
Email__Mode=Disabled
```

В Production startup validator требует рабочую SMTP-конфигурацию:

```text
Email__Mode=Smtp
Email__Host=smtp.provider.example
Email__Port=587
Email__UseSsl=true
Email__FromAddress=no-reply@example.com
Email__FromName=VPN Platform
Email__Username=<smtp-user>
Email__Password=<secret-manager-value>
```

Пароль SMTP передается только через secret manager или переменные окружения. Его нельзя добавлять в JSON, логи, audit или evidence reports.

## Сброс пароля

Одноразовый reset-код хранится в `PasswordResetTokens` только как SHA-256 hash. Для доставки исходный код помещается в outbox и `NotificationDelivery.PayloadJson` только в формате, защищенном `ISecretProtector`; API админки payload не возвращает. После отправки пользователь вводит код в существующую форму сброса пароля.

## Администрирование

В разделе «Аудит» отображаются шаблон, маскированный адрес, статус, число попыток, время следующей попытки и redacted ошибка. Пользователь с `adminWrite` может вернуть запись из `Failed` в `Pending`; повтор записывается в audit без полного адреса, payload и reset-кода.

## Проверка

Локальные SQLite/unit/browser проверки подтверждают materialization, deduplication, claim, stale lease, backoff, terminal failure, redaction и responsive UI. Они не подтверждают доступ к реальному SMTP-провайдеру. Для production acceptance нужен staging/VPS smoke с тестовым почтовым ящиком и фактом получения письма без публикации адреса, кода или SMTP credentials в evidence.
