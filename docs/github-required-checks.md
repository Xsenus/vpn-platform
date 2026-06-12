# Required checks для main

Документ описывает обязательные GitHub checks для веток `main` и `master` и способ применить branch protection без ручной настройки в интерфейсе.

## Что защищаем

Файл конфигурации: `.github/branch-protection.required-checks.json`.

Обязательные checks берутся из workflow `.github/workflows/ci.yml`, потому что он запускается на `pull_request` и не выполняет production deploy:

- `backend restore, build, test, EF`
- `frontend install, typecheck, build, test`
- `provisioning runner and Ansible syntax`
- `docker compose config and image build`

Workflow `.github/workflows/deploy-vps.yml` не добавлен в required checks: он предназначен для push-деплоя на VPS и не должен блокировать pull request до merge.

## Правила branch protection

Скрипт `scripts/configure-branch-protection.ps1` применяет:

- required status checks со строгой синхронизацией ветки (`strict=true`);
- запрет force push;
- запрет удаления защищенной ветки;
- enforcement для администраторов;
- обязательное разрешение conversations;
- минимум один approving review;
- сброс approval после новых коммитов.

## Локальная проверка payload

Без изменения GitHub:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/configure-branch-protection.ps1 -DryRun
```

Ожидаемый вывод содержит:

```text
Repository: Xsenus/vpn-platform
Branches: main, master
Required checks:
  - backend restore, build, test, EF
  - frontend install, typecheck, build, test
  - provisioning runner and Ansible syntax
  - docker compose config and image build
Dry run: branch protection payload was built but not sent to GitHub.
```

## Применение в GitHub

Нужен token с правом управления настройками репозитория. Значение токена нельзя писать в файлы, логи или commit.

```powershell
$env:GITHUB_TOKEN = "<token-with-repository-administration-write>"
powershell -ExecutionPolicy Bypass -File scripts/configure-branch-protection.ps1
Remove-Item Env:GITHUB_TOKEN
```

Для проверки отдельной ветки:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/configure-branch-protection.ps1 -Branch main
```

Для другого репозитория:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/configure-branch-protection.ps1 -Repository owner/repo -Branch main
```

## Доказательство для roadmap

После применения сохраните в задаче без секретов:

- дату и время запуска;
- repository/branch;
- список required checks из вывода скрипта;
- строку `Branch 'main' protection is configured.`;
- скриншот GitHub `Settings -> Branches -> Branch protection rules`, если нужен визуальный audit trail.

Не публикуйте token, HTTP headers и raw API response, если в нем есть приватные данные.
