# План работ: доведение проекта до рабочего delivery-пакета

Дата: 2026-03-25

## Цель этого прохода

Довести репозиторий от архитектурного skeleton до состояния, в котором:

- есть реальный provisioning pipeline для VPN-ноды через SSH/Ansible;
- есть очередь и worker, которые берут provisioning-задачи из backend и исполняют их;
- есть расширенный admin-flow для создания сервера, precheck и запуска provisioning;
- есть проверяемые локально тесты/валидации для frontend, ansible и provisioning runner;
- есть актуальная документация по запуску и проверке.

## Ограничения текущей среды

- отсутствует .NET SDK, поэтому `dotnet build` / `dotnet test` нельзя подтвердить в этой среде;
- отсутствует доступ к реальному внешнему VPN-серверу пользователя, поэтому полноценный end-to-end provisioning по SSH можно подготовить и протестировать только на стороне проекта после передачи репозитория.

## План выполнения

### 1. Закрыть provisioning gap
- [x] Ввести явную модель серверных SSH/panel-параметров.
- [x] Добавить Python runner для запуска ansible-playbook с генерацией inventory.
- [x] Добавить unit-тесты runner-а.
- [x] Добавить playbook precheck.
- [x] Расширить основной playbook provisioning.
- [x] Добавить CLI-скрипт для локального запуска provisioning вне backend.

### 2. Закрыть backend orchestration gap
- [x] Добавить `IProvisioningExecutor` и DTO результата выполнения.
- [x] Добавить инфраструктурный `AnsibleProvisioningExecutor`.
- [x] Добавить background worker обработки provisioning runs.
- [x] Расширить admin API: queue provision, precheck, provisioning runs, retry.
- [x] Обновить seed/configuration/README под новые поля узлов.

### 3. Закрыть admin UX gap
- [x] Расширить api-client методами для серверов и provisioning.
- [x] Добавить в admin panel форму создания сервера.
- [x] Добавить precheck/provision actions.
- [x] Добавить просмотр списка provisioning runs.

### 4. Прогон локально проверяемых проверок
- [ ] Frontend typecheck — не подтверждено в текущей контейнерной среде из-за timeout на `npm install`.
- [ ] Frontend tests — не подтверждено в текущей контейнерной среде из-за timeout на `npm install`.
- [ ] Frontend build — не подтверждено в текущей контейнерной среде из-за timeout на `npm install`.
- [x] Python unit tests provisioning runner.
- [ ] Ansible syntax-check — не подтверждено, `ansible-playbook` отсутствует в текущей среде.
- [x] YAML/JSON sanity checks.

### 5. Упаковать delivery
- [x] Обновить документацию.
- [x] Обновить отчет о тестах.
- [ ] Упаковать итоговый архив.

## Что остается вне этого прохода

- compile-verified `dotnet build` / `dotnet test`;
- настоящая интеграция с реальными YooMoney / YooKassa / Robokassa;
- реальный production-grade 3x-ui API client с полной end-to-end валидацией на живой панели;
- production secret vault / шифрование секретов узлов;
- полноформатные e2e тесты на живых окружениях.
