# RBAC-матрица админки

Документ фиксирует текущую модель прав в административной части платформы. Матрица используется backend API напрямую через `AdminPolicies.PolicyRoles`, поэтому изменение ролей должно проходить через код, тесты и эту документацию одновременно.

## Роли

- `SuperAdmin`: полный доступ ко всем административным действиям.
- `Admin`: полный операционный доступ, включая настройки и критичные изменения.
- `Operator`: операционная роль для VPN, provisioning, ботов, пользователей и поддержки без доступа к финансовым списаниям и системным настройкам.
- `FinanceManager`: финансовая роль для просмотра и обработки платежей без доступа к VPN/provisioning/ботам/настройкам.
- `SupportAgent`: поддержка пользователей и обращений без доступа к финансам, VPN/provisioning, ботам и настройкам.
- `ReadOnly`: только чтение административных данных.
- `User`: обычный пользователь кабинета, не входит ни в одну admin-policy.

## Политики

| Policy | Допущенные роли | Назначение |
| --- | --- | --- |
| `AdminOnly` | `SuperAdmin`, `Admin`, `Operator`, `FinanceManager`, `SupportAgent`, `ReadOnly` | Наследуемый общий вход в админские зоны, без выдачи прав на запись сам по себе. |
| `AdminRead` | `SuperAdmin`, `Admin`, `Operator`, `FinanceManager`, `SupportAgent`, `ReadOnly` | Чтение общих административных данных. |
| `AdminWrite` | `SuperAdmin`, `Admin`, `Operator` | Общие операции записи, которые не требуют отдельной специализированной политики. |
| `FinanceRead` | `SuperAdmin`, `Admin`, `FinanceManager`, `ReadOnly` | Просмотр платежей, заказов, финансовой аналитики. |
| `FinanceWrite` | `SuperAdmin`, `Admin`, `FinanceManager` | Повторная проверка платежей, возвраты, финансовые изменения. |
| `SupportRead` | `SuperAdmin`, `Admin`, `Operator`, `SupportAgent`, `ReadOnly` | Просмотр обращений и пользовательского контекста поддержки. |
| `SupportWrite` | `SuperAdmin`, `Admin`, `Operator`, `SupportAgent` | Ответы поддержки, закрытие и переоткрытие обращений. |
| `ProvisioningManage` | `SuperAdmin`, `Admin`, `Operator` | Precheck/provision/deploy/cancel/retry серверного provisioning. |
| `VpnManage` | `SuperAdmin`, `Admin`, `Operator` | Управление VPN-доступами, синхронизация, включение/отключение, сброс трафика. |
| `BotManage` | `SuperAdmin`, `Admin`, `Operator` | Настройки Telegram-бота и write-only секреты бота. |
| `SettingsManage` | `SuperAdmin`, `Admin` | Системные настройки, которые не должны быть доступны оператору. |

## Правила изменения

1. Новую административную policy нужно добавить в `AdminPolicies.PolicyRoles`.
2. `UserRoles.User` не должен появляться ни в одной admin-policy.
3. Опасные действия должны использовать специализированные policy: финансы через `FinanceWrite`, VPN через `VpnManage`, provisioning через `ProvisioningManage`, боты через `BotManage`, настройки через `SettingsManage`.
4. После изменения матрицы нужно обновить `AdminAuthorizationPolicyTests`, чтобы были и позитивные, и негативные проверки runtime authorization.

## Проверка

Минимальная проверка RBAC:

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter AdminAuthorizationPolicyTests --logger "console;verbosity=minimal"
```

Полная проверка перед релизом:

```powershell
dotnet test backend\VpnPlatform.sln --configuration Release --no-restore --logger "console;verbosity=minimal"
```
