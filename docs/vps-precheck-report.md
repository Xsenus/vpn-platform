# Отчет precheck VPS

`P7-PROV-003` добавляет единый отчет precheck для пользовательских и админских VPS. Отчет создается при dry-run запуске подготовки сервера и сохраняется как шаг `Precheck report` в `ProvisioningStepRun`.

## Где смотреть

- Админка: раздел «Подготовка VPS», поле отчета внутри карточки запуска.
- API списка: `GET /api/admin/provisioning-runs`, поле `precheckReportPreview`.
- API деталей: `GET /api/admin/provisioning-runs/{id}`, поле `run.precheckReport` и шаг `steps[].stepName == "Precheck report"`.

## Что проверяется

- SSH: доступность подключения и корректность SSH-конфигурации.
- OS: Debian-family дистрибутив, совместимый с текущими playbook.
- Ports: состояние listening TCP ports и требуемые порты SSH, 443 и 2053.
- Disk: свободное место на root filesystem, минимум 1 GiB.
- RAM: минимум 512 MiB.
- Firewall: состояние UFW/firewall, чтобы оператор видел текущие правила до deploy.
- Docker: наличие Docker runtime как отчетная проверка. Текущий deploy не блокируется, если Docker не установлен.
- systemd: обязательная доступность `systemctl`, потому что сервисный bootstrap рассчитывает на systemd.
- 3x-ui: установленная панель или необходимость установки во время deploy.

## Формат

Отчет хранится в JSON:

```json
{
  "status": "passed",
  "summary": "Server precheck passed.",
  "checks": [
    {
      "key": "ssh",
      "label": "SSH connectivity",
      "status": "passed",
      "evidence": "Mock SSH config accepted. No socket was opened.",
      "requiredAction": null
    }
  ]
}
```

Статусы проверок:

- `passed` - проверка прошла или runner успешно завершился, но отдельного вывода по проверке нет.
- `failed` - проверка нашла блокирующую проблему.
- `not_reported` - runner не вернул данных по проверке, нужно смотреть output `ansible-playbook`.

## Безопасность

Отчет проходит через существующую redaction-цепочку provisioning. SSH credential, protected payload, temporary private key path, panel password и похожие секреты не должны сохраняться в `ExecutionLog`, `Precheck report` или output шагов.
