# Этап 4: интеграция 3x-ui / x-ui

Status: working implementation draft. Do not call Phase 4 complete until backend build/test, EF drift check and frontend validation pass.

## Implemented vertical slice

Goal: admin connects an existing 3x-ui panel, backend imports inbounds, subscription activation creates or renews a real client, cabinet/Telegram can show real config data.

Implemented:

- `VpnPanel`, `VpnInbound`, `VpnClient`
- `PanelHealthCheck`, `PanelSyncRun`, `PanelSyncEvent`
- `AccessCredentialHistory`
- encrypted panel password through `ISecretProtector`
- `IX3UiClient`
- `X3UiHttpClient`
- admin API for panel/inbound/client/health/sync operations
- panel health worker foundation
- panel sync worker foundation
- production `X3UiVpnProvider` path using DB panels/inbounds
- VLESS config URI generator
- payload-only `IQrCodeGenerator` implementation

## Fail-closed rules

- Production cannot use sandbox VPN provider.
- If no active healthy `VpnPanel` exists, activation fails; it must not silently succeed.
- If no inbound exists and `AutoCreateInbound=false`, activation fails safely.
- If config URI cannot be generated from panel/inbound data, `VpnClient.SyncStatus=RequiresAdminReview` and no fake config is returned.
- `node.example.com` is not used.
- Panel password and session cookie are not logged.
- Panel sync is read-only and does not repair remote panel state yet.

## Subscription activation

1. Payment succeeds idempotently.
2. `SubscriptionService` activates or renews subscription.
3. `X3UiVpnProvider` selects active healthy panel.
4. It selects default active inbound with capacity.
5. If missing and `AutoCreateInbound=true`, inbound is created from `DefaultInboundTemplateJson`.
6. New subscription creates a 3x-ui client and stores `VpnClient`, `AccessCredential` and `AccessCredentialHistory`.
7. Renewal updates existing `VpnClient` expiry/traffic/enabled status.
8. Duplicate webhook cannot create duplicate VPN client because `VpnClient.SubscriptionId` is unique and payment activation is guarded.
9. Config URI and QR payload are saved into access records.
10. If Telegram is linked, a deduplicated `vpn_access_ready` notification is queued.

## Sync and health

`PanelHealthWorker`:

- logs in to panel;
- fetches version/status;
- saves `PanelHealthCheck`;
- updates `VpnPanel.HealthStatus`.

`PanelSyncWorker`:

- fetches inbounds;
- imports/updates local inbound records;
- detects missing inbounds;
- parses clients from inbound settings when available;
- detects orphan clients, missing clients, expiry mismatch and enabled mismatch;
- saves `PanelSyncRun` and `PanelSyncEvent`;
- does not auto-repair.

## Admin API

```http
GET    /api/admin/vpn-panels
POST   /api/admin/vpn-panels
GET    /api/admin/vpn-panels/{id}
PATCH  /api/admin/vpn-panels/{id}
DELETE /api/admin/vpn-panels/{id}
POST   /api/admin/vpn-panels/{id}/test-connection
POST   /api/admin/vpn-panels/{id}/health-check
POST   /api/admin/vpn-panels/{id}/sync
GET    /api/admin/vpn-panels/{id}/inbounds
POST   /api/admin/vpn-panels/{id}/inbounds
PATCH  /api/admin/vpn-inbounds/{id}
POST   /api/admin/vpn-inbounds/{id}/set-default
GET    /api/admin/vpn-panels/{id}/clients
GET    /api/admin/vpn-panels/{id}/sync-runs
GET    /api/admin/vpn-panel-sync-runs/{id}/events
GET    /api/admin/vpn-panels/{id}/health-checks
```

## Validation

Run:

```bash
./scripts/validate-all.sh
```

Focused tests:

```bash
cd backend
dotnet test --filter X3Ui
```
