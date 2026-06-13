# Безопасная очистка VPS

Документ описывает maintenance-процедуру для слабого VPS: оценка диска/RAM, очистка старых release-артефактов, journal/app logs, apt cache и опционально Docker cache.

## Скрипт

Файл: `scripts/vps-maintenance.sh`.

По умолчанию скрипт работает в dry-run и ничего не удаляет:

```bash
APP_DIR=/opt/vpn-platform scripts/vps-maintenance.sh --dry-run
```

Реальная очистка включается только явно:

```bash
APP_DIR=/opt/vpn-platform scripts/vps-maintenance.sh --apply
```

Docker cache очищается отдельно:

```bash
APP_DIR=/opt/vpn-platform scripts/vps-maintenance.sh --apply --docker-prune
```

## Что делает скрипт

- Печатает `df -h`, `free -h`, `du -sh` для `APP_DIR` и `APP_DIR/releases` до и после очистки.
- Хранит последние `KEEP_RELEASES=5` release-директорий в `APP_DIR/releases`.
- Никогда не удаляет `APP_DIR`, `APP_DIR/shared`, `APP_DIR/current` и сам `APP_DIR/releases`.
- Не удаляет текущий release, если `APP_DIR/current` указывает на release-директорию.
- Удаляет только release-директории с именем git sha: `[0-9a-fA-F]{7,40}`.
- Удаляет старые `release.tar.gz` и `systemd-release.tar.gz` внутри `APP_DIR/releases`.
- Удаляет старые `*.log` и `*.log.*` только внутри `APP_DIR/logs`.
- Выполняет `journalctl --vacuum-time=14d`, если доступен `journalctl`.
- Выполняет `apt-get clean` и `apt-get autoclean`, если доступен `apt-get`.
- При `--docker-prune` выполняет только `docker builder prune`, `docker image prune` и `docker container prune` с фильтром возраста.
- Никогда не выполняет `docker volume prune`.

## Настройки

```bash
APP_DIR=/opt/vpn-platform
KEEP_RELEASES=5
LOG_RETENTION_DAYS=14
ARCHIVE_RETENTION_DAYS=7
JOURNAL_RETENTION_DAYS=14
DOCKER_PRUNE_UNTIL=168h
DOCKER_KEEP_STORAGE=2GB
```

## Рекомендуемый порядок

1. Запустить dry-run:

```bash
APP_DIR=/opt/vpn-platform scripts/vps-maintenance.sh --dry-run
```

2. Проверить вывод:

- `APP_DIR` указывает на правильную директорию;
- в release cleanup нет `shared`, `current`, `api`, `web`, `.env` или БД;
- Docker prune отключен, если не передан `--docker-prune`.

3. Запустить apply без Docker prune:

```bash
APP_DIR=/opt/vpn-platform scripts/vps-maintenance.sh --apply
```

4. Если Docker реально используется и нужно освободить место:

```bash
APP_DIR=/opt/vpn-platform scripts/vps-maintenance.sh --apply --docker-prune
```

5. После очистки проверить сервис:

```bash
systemctl status vpn-platform-api --no-pager
curl -fsS http://127.0.0.1:8080/health/live
curl -fsS http://127.0.0.1:8080/health/ready
```

## Доказательство для roadmap

Для `P8-CI-004` сохраняем только безопасные данные:

- `df -h` до и после;
- `free -h` до и после;
- `du -sh /opt/vpn-platform` до и после;
- список удаленных release-директорий без содержимого `.env`;
- факт, что `docker volume prune` не запускался.

Не сохраняем:

- production `.env`;
- private key;
- database dumps;
- содержимое логов с пользовательскими данными.
