# UI/UX ревью MVP: этап 9

Stage 9 is a polish pass for the existing frontend applications. It does not introduce live Telegram, payment provider, 3x-ui, SSH/Ansible or VPS calls and does not change the product architecture.

## Audit summary

| UI area | Current status | Problem | Required polish |
|---|---|---|---|
| Admin dashboard | MVP functional | Metrics existed but lacked context and operational triage | Add validation-mode banner, recent orders and attention queue |
| Admin users | MVP functional | Empty state was plain text and user detail was easy to miss | Add clearer empty state and sidebar navigation |
| Admin orders/payments | MVP functional | Technical cards, limited empty states | Add consistent empty states, statuses and safer payment wording |
| Admin payment providers | MVP functional | Secret fields were write-only but not visually explicit enough | Add shared `SecretField`, readiness reasons and configured flags |
| Admin tariffs | MVP functional | Good enough; needs production pagination later | Keep CRUD and disable confirmation |
| Admin subscriptions/accesses | MVP functional | Access copy/QR actions existed, but UX was inconsistent | Add shared copy button, QR preview messaging and empty states |
| Admin nodes/panels | MVP functional | Live/sandbox behavior warning not prominent | Add validation-mode warnings and write-only credential fields |
| Admin provisioning | MVP functional | Logs and live warnings could be clearer | Add validation-mode warning, redacted log emphasis and empty state |
| Admin support inbox | MVP functional | Empty state missing | Add empty state and keep reply/note actions |
| Admin Telegram settings | MVP functional | Token is masked; texts editable | Keep masked/configured UX and improve bot text constants |
| Cabinet profile/session | MVP functional | The old label was too technical (`JWT token`) | Rename to session/payment, add validation mode badge |
| Cabinet subscriptions | MVP functional | Empty state missing | Add empty state and clearer renew/payment state |
| Cabinet VPN keys/access | MVP functional | Keys were mostly inside subscriptions | Add dedicated access cards with copy URI and QR SVG preview |
| Cabinet payments/orders | MVP functional | Empty states missing | Add empty states and keep retry payment action |
| Public tariffs/checkout | MVP functional | Tariff loading state was missing; provider empty text could be clearer | Add tariff loading/empty/error states and safer checkout summary |
| Public auth | MVP functional | Password reset endpoint existed but public UI lacked form | Add small validation-mode password reset form |
| Telegram texts | MVP functional | Some strings were technical and security warnings were terse | Improve Russian copy, own-VPS warnings and key safety wording |

## Implemented UI polish

### Shared UI helpers

Added reusable helpers in `frontend/packages/ui/src/index.tsx`:

- `SectionCard`
- `EmptyState`
- `LoadingBlock`
- `ErrorBlock`
- `CopyButton`
- `ConfirmButton`
- `SecretField`
- `ValidationModeBadge`
- `DataTableLite`

They are intentionally lightweight and inline-style based, so no new UI library or architecture migration is required.

### Admin panel

Improvements:

- sticky sidebar-style navigation for all admin sections;
- page intro with explicit validation-mode badge;
- dashboard now includes recent orders and an operational attention queue;
- consistent loading, error and empty states in important sections;
- shared write-only `SecretField` for payment provider, SSH and panel secrets;
- shared `CopyButton` for VPN URI;
- clearer validation-mode warnings for VPN panel/provisioning actions;
- stronger responsive styles.

Safety preserved:

- dangerous actions still use confirmation prompts;
- secrets remain write-only/configured flags;
- no raw panel password, SSH credential, bot token or payment secret is displayed;
- validation mode remains visible in the UI.

### Cabinet

Improvements:

- renamed technical `JWT token` block to session/payment block;
- added page intro and validation-mode badge;
- added dedicated `VPN keys / accesses` cards;
- added QR SVG preview and copy-friendly URI actions;
- added empty states for subscriptions, accesses, orders, payments and referrals;
- kept logout/refresh/password reset MVP flows.

### Public web

Improvements:

- clearer hero and trust blocks without production promises;
- tariff loading and empty states;
- provider loading/empty/error states;
- safer checkout summary that does not dump raw payment response;
- small password reset MVP form in account page;
- FAQ empty/error states.

### Telegram texts

Improved Russian texts for:

- `/start` and main menu;
- unlinked account onboarding;
- own-VPS requirements and validation-mode warnings;
- payment provider unavailable states;
- payment-created message;
- subscription/access empty states;
- VPN connection instructions;
- post-payment access-ready message.

Telegram text safety:

- no credentials are echoed back;
- own-VPS warnings explicitly say password/key will not be shown again;
- keys are marked as private and not to be shared.

## Tests added/updated

Frontend/static tests cover:

- shared UI polish components rendering;
- admin dashboard/attention/sidebar source coverage;
- write-only secret fields;
- dangerous confirmation prompts;
- copy URI and QR source coverage;
- cabinet access empty/copy/QR surfaces;
- public provider/tariff loading/error/empty states;
- public password reset form;
- Telegram text constants do not contain obvious secret placeholders.

Backend tests were not expanded in this stage because the requested work is frontend/UX polish and `.NET SDK` is still unavailable in the current environment.

## Remaining UI limitations

This is still an MVP UI. Production UI still needs:

- backend pagination/sorting/filtering for large tables;
- true table component with sticky headers and column controls;
- route-based admin sections instead of one large admin `App.tsx`;
- real toast system instead of inline notices;
- modal component instead of `window.confirm`;
- detailed order/payment/subscription/access drawers;
- audit log screen;
- richer provisioning timeline;
- support assignment/SLA UX;
- Playwright smoke tests against Docker runtime;
- visual regression/accessibility checks.

## Manual checks after Docker/runtime

After the backend and Docker gates are actually green, manually verify:

1. Admin login and dashboard loading.
2. Every sidebar anchor scrolls to the expected section.
3. Empty states display with a clean database.
4. Provider account create/update keeps secrets write-only.
5. Tariff disable hides it from public/Telegram surfaces.
6. Subscription extend/block/unblock/cancel requires confirmation.
7. VPN access copy/QR/enable/disable/sync/reset actions are visible and audited.
8. Provisioning logs are redacted and validation-mode warning is visible.
9. Support reply queues Telegram notification without live send in validation mode.
10. Cabinet shows QR/access/copy and handles empty subscriptions/accesses.
11. Public checkout handles no providers and pending checkout after login.

Do not mark staging-ready or production-ready from UI tests alone.
