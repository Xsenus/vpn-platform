# Проверка auto-detect deploy на VPS

Документ фиксирует, как workflow `.github/workflows/deploy-vps.yml` выбирает режим деплоя и как оператор проверяет это в GitHub Actions.

## Поддерживаемые режимы

- `auto` - режим по умолчанию. Workflow подключается к VPS по SSH и проверяет Docker CLI вместе с Docker Compose plugin.
- `docker` - принудительный Docker Compose deploy на VPS.
- `systemd` - принудительный deploy без Docker: GitHub Actions собирает API и frontend, загружает архив на VPS и перезапускает `vpn-platform-api` через `systemd`.

Режим задается одним из способов:

- вручную при запуске `workflow_dispatch` через поле `deploy_mode`;
- через repository secret `VPS_DEPLOY_MODE`;
- если значение не задано, используется `auto`.

Разрешены только `auto`, `docker`, `systemd`. Любое другое значение должно завершить workflow ошибкой до загрузки артефактов на VPS.

## Как работает auto-detect

Шаг `Detect deployment mode` выполняет команду на VPS:

```bash
command -v docker >/dev/null 2>&1 && docker compose version >/dev/null 2>&1
```

Если команда успешна, workflow выбирает `docker`. Если Docker или Compose plugin не найдены, выбирается `systemd`.

Для явных режимов `docker` и `systemd` detection не меняет выбор, а только пишет причину `manual: deploy_mode was set to ...`.

## Что должно быть в GitHub Actions log

В шаге `Detect deployment mode` должен появиться notice:

```text
Deploy mode: requested=auto selected=systemd docker_detected=false reason=auto: Docker Compose was not detected on VPS, using systemd release without Docker
```

или для VPS с Docker:

```text
Deploy mode: requested=auto selected=docker docker_detected=true reason=auto: Docker CLI and Docker Compose plugin were detected on VPS
```

Также workflow пишет блок в `$GITHUB_STEP_SUMMARY`:

```text
### VPS deploy mode
- Requested: `auto`
- Selected: `systemd`
- Docker detected: `false`
- Reason: auto: Docker Compose was not detected on VPS, using systemd release without Docker
```

Этого достаточно как доказательства для roadmap-пункта `P8-CI-001`: видно запрошенный режим, выбранный режим, результат Docker-detection и причину.

## Проверка ветки deploy

После `Detect deployment mode` должны запускаться только шаги выбранного режима:

- если `Selected: docker`, выполняются `Build Docker deployment archive`, `Upload Docker release and environment file`, `Start Docker production stack`;
- если `Selected: systemd`, выполняются `Setup .NET SDK for systemd release`, `Setup Node.js for systemd release`, `Build systemd deployment archive`, `Upload systemd release and environment file`, `Start systemd production release`.

Условия в workflow:

```yaml
if: steps.deploy_mode.outputs.mode == 'docker'
if: steps.deploy_mode.outputs.mode == 'systemd'
```

## Локальная статическая проверка

Перед коммитом запустите guard-тест:

```powershell
dotnet test backend/tests/VpnPlatform.UnitTests/VpnPlatform.UnitTests.csproj --filter "DeployWorkflowGuardTests"
```

Тест проверяет, что workflow содержит все три режима, SSH-проверку `docker compose version`, notice `Deploy mode`, `$GITHUB_STEP_SUMMARY` и условия запуска docker/systemd шагов.
