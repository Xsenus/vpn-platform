# Security headers API и frontend

Платформа выставляет защитные HTTP-заголовки на backend API и на frontend SPA, которые раздаются через nginx Docker images.

## Backend API

Middleware `SecurityHeadersMiddleware` добавляет:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`
- `Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=()`
- `Content-Security-Policy: default-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'none'`
- `Strict-Transport-Security: max-age=31536000; includeSubDomains` в `Production`

Для development Swagger UI CSP не выставляется, чтобы не ломать локальную документацию API.

## Frontend Docker images

Все frontend Dockerfiles копируют общий `frontend/nginx.security.conf` в `/etc/nginx/conf.d/default.conf`.

nginx добавляет:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`
- `Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=()`
- `Strict-Transport-Security: max-age=31536000; includeSubDomains`
- `Content-Security-Policy` для SPA: скрипты и стили с собственного origin, изображения `self/data/blob`, подключения к `self/http/https/ws/wss`

SPA fallback остается включенным через `try_files $uri $uri/ /index.html`.

## CORS

Production CORS остается allow-list based через `Cors:AllowedOrigins`. `StartupSafetyValidator` блокирует production startup, если origins пустые, wildcard или localhost.

## Проверка

```powershell
dotnet test backend\tests\VpnPlatform.UnitTests\VpnPlatform.UnitTests.csproj --configuration Release --filter SecurityHeadersTests --logger "console;verbosity=minimal"
```

Для ручной проверки API:

```powershell
curl -I https://api.example.com/health/live
```

Для frontend:

```powershell
curl -I https://example.com/
```
