# Настройка панели 3x-ui

Status: draft.

## Adding an existing panel

Admin-panel → VPN Panels:

- name;
- base URL, for example `https://panel.example.com:2053`;
- login;
- password;
- region;
- capacity;
- SSL verification mode;
- API variant;
- auto-create inbound flag;
- default inbound template JSON.

Password is encrypted before persistence and never returned by API.

## SSL / self-signed mode

`SslVerificationMode`:

- `Strict`: default, validates TLS normally.
- `AllowSelfSigned`: allows self-signed panel certificates.
- `Disabled`: disables verification and should be limited to controlled environments.

Self-signed/disabled mode is explicit per panel.

## API variants

`ApiVariant` is stored for compatibility:

- `X3UiOfficial`
- `ThreeXUi`
- `LegacyXUi`
- `Custom`

The current HTTP implementation supports the common session-cookie API shape. Unknown/unsupported responses fail closed with clear errors.

## Inbound requirements

For VLESS URI generation, inbound data should include:

- protocol `vless`;
- port;
- stream settings with network/security;
- optional SNI;
- optional WS path or gRPC serviceName;
- client UUID/flow.

If required data is missing, access is marked `RequiresAdminReview` and no fake config is given to user.

## Auto-create inbound

When subscription activation cannot find an active default inbound:

- `AutoCreateInbound=false`: activation fails safely.
- `AutoCreateInbound=true`: backend creates inbound from `DefaultInboundTemplateJson` and saves it locally.

Template must be real 3x-ui-compatible JSON. Do not use placeholder hostnames in production.

## Subscription and renewal

New subscription:

1. backend selects panel/inbound;
2. creates 3x-ui client;
3. stores `VpnClient`;
4. stores `AccessCredential`;
5. stores config URI and QR payload.

Renewal:

- if current subscription is active, period is added to current expiry;
- if expired, period starts from now;
- existing client is updated and re-enabled if needed;
- duplicate webhook does not create another client.

## Sync / health troubleshooting

- Health failure: check panel URL/login/password/TLS mode/network.
- Sync shows `inbound_missing`: inbound exists in DB but not on panel.
- Sync shows `orphan_client`: client exists on panel but not in DB.
- Sync shows `expiry_mismatch` or `enabled_mismatch`: DB and panel differ; auto-repair is intentionally not enabled yet.

## Production safety

- Do not use sandbox VPN provider in Production.
- Do not log panel password/session cookie.
- Do not return fake config URI if panel is unavailable.
