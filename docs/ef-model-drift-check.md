# Проверка EF model drift

В backend test suite добавлен тест `EfModelDriftTests`.

Он строит текущую модель `ApplicationDbContext` на PostgreSQL-провайдере и сравнивает ее с последним `ApplicationDbContextModelSnapshot` через EF migrations differ.

Если разработчик изменит сущности или конфигурацию EF и забудет добавить миграцию, тест упадет с перечислением типов найденных отличий.

## Как проверить

```powershell
dotnet test backend\VpnPlatform.sln --configuration Release --filter EfModelDriftTests
```

Тест не подключается к реальному Postgres и не требует поднятой БД.
