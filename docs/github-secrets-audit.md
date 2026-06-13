# Аудит GitHub Secrets

Документ описывает список GitHub Actions secrets, нужных для текущего production deploy, и безопасную проверку их наличия без вывода значений.

## Конфиг аудита

Файл: `.github/github-secrets.audit.json`.

Required secrets для `.github/workflows/deploy-vps.yml`:

- `VPS_HOST` - IP или DNS VPS.
- `VPS_USER` - SSH-пользователь для deploy.
- `VPS_PORT` - SSH-порт для `ssh`, `scp` и `ssh-keyscan`.
- `VPS_APP_DIR` - директория приложения на VPS.
- `VPS_SSH_KEY` - приватный SSH-ключ deploy-пользователя.
- `PRODUCTION_ENV_FILE` - полный production `.env`, который копируется на VPS в shared `.env`.

Optional secrets:

- `VPS_DEPLOY_MODE` - `auto`, `docker` или `systemd`; если не задан, используется `auto`.
- `VITE_API_BASE_URL` - URL API для systemd-сборки frontend.
- `VITE_PUBLIC_WEB_URL` - URL публичного сайта для systemd-сборки frontend.

Registry secrets сейчас не требуются: workflows собирают образы локально и не пушат их в container registry.

## Локальная проверка без GitHub-доступа

```powershell
powershell -ExecutionPolicy Bypass -File scripts/audit-github-secrets.ps1 -DryRun
```

Dry-run проверяет:

- конфиг JSON читается;
- required/optional names не пустые;
- все names из конфига реально используются в `.github/workflows/deploy-vps.yml`;
- в выводе показываются только имена secrets, без значений.

## Live audit через GitHub API

Нужен token с правом читать repository Actions secrets metadata. Значения secrets GitHub API не возвращает.

```powershell
$env:GITHUB_TOKEN = "<token-with-actions-secrets-read>"
powershell -ExecutionPolicy Bypass -File scripts/audit-github-secrets.ps1
Remove-Item Env:GITHUB_TOKEN
```

Скрипт вызывает GitHub REST API `GET /repos/{owner}/{repo}/actions/secrets`, получает только names, сравнивает их с `.github/github-secrets.audit.json` и завершает работу ошибкой, если отсутствует required secret.

## Что сохранять как доказательство

Для roadmap-пункта `P8-CI-003` достаточно сохранить:

- дату проверки;
- repository;
- список required secret names;
- список optional secret names;
- список missing required names, если они есть;
- строку `GitHub secrets audit passed.`, если live audit прошел.

Нельзя сохранять:

- значения secrets;
- SSH private key;
- production `.env`;
- полный `Authorization` header;
- token, использованный для API.

## Проверка после изменения workflow

Если в `.github/workflows/deploy-vps.yml` добавлен новый `${{ secrets.NAME }}`, нужно:

1. Добавить `NAME` в `.github/github-secrets.audit.json`.
2. Обновить этот документ.
3. Запустить:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/audit-github-secrets.ps1 -DryRun
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "GitHubSecretsAuditTests"
```
