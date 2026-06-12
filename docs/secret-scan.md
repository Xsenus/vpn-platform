# Проверка утечек секретов

В проект добавлен воспроизводимый secret scan для репозитория, документации, env-шаблонов, конфигов и log-like текстовых файлов.

## Команды

Windows / PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
```

Linux / CI:

```bash
bash ./scripts/scan-secrets.sh
```

## Что проверяется

Scanner ищет признаки реальных секретов:

- Telegram bot token;
- Stripe/OpenAI style API key;
- GitHub token;
- GitLab token;
- AWS access key;
- Google API key;
- Slack token;
- PEM private key.

## Что исключается

Из обхода исключены тяжелые и генерируемые каталоги: `.git`, `node_modules`, `bin`, `obj`, `dist`, `build`, `TestResults`, `artifacts`, `coverage`, `playwright-report`, `backups`.

Разрешены только очевидные fixture/placeholders: `example`, `change-me`, `local-dev`, `local-validation`, `schema-audit`, `ef-drift`, `dummy`, `fixture`, `must-not-leak`, `redacted`, а также тестовые директории `backend/tests` и `frontend/tests`.

## Validation gate

`scripts/validate-backend.sh` и `scripts/validate-all.sh` запускают `scan-secrets.sh` до build/test шагов. `check-validation-safety.sh` проверяет наличие scanner и базовых семейств паттернов.

## Текущий результат

На 2026-06-12 PowerShell scan прошел локально:

```text
[OK] secret scan completed. Files scanned: 385. Findings: 0.
```

В текущей Windows-среде `bash` недоступен, поэтому bash-скрипт покрыт unit-тестами и рассчитан на Linux CI.
