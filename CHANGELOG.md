# Changelog

## 0.323.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-smoke-preflight-release-guard`.

### Fixed
- `scripts/admin-vps-smoke-preflight.ps1 -ReleaseId` now rejects unknown manual release ids before remote release checks, validator execution or writing preflight artifacts.

### Added
- `scripts/test-admin-vps-smoke-preflight-release-guard.ps1` proves that direct preflight fails fast, leaves no preflight/smoke JSON artifacts and does not leak the smoke password for an unknown `ReleaseId`.
- `P0-ADMIN-002BH` documents the local guard while `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until real VPS admin smoke evidence exists.

### Notes
- Roadmap progress is now `328/348` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke preflight release guard regression OK; targeted admin/docs/release suite `36/36`; backend full suite `618/618`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `531` files, `0` findings; markdown encoding check OK.

## 0.322.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-browser-smoke-direct-release-guard`.

### Fixed
- `scripts/admin-vps-browser-smoke.ps1 -ReleaseId` now rejects unknown manual release ids before setting smoke environment variables, running Playwright or writing report artifacts.

### Added
- `scripts/test-admin-vps-browser-smoke-direct-release-guard.ps1` proves that the direct browser runner fails fast, leaves no JSON artifact and does not leak the smoke password for an unknown `ReleaseId`.
- `P0-ADMIN-002BG` documents the local guard while `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until real VPS admin smoke evidence exists.

### Notes
- Roadmap progress is now `327/347` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS browser smoke direct release guard regression OK; targeted admin/docs/release suite `35/35`; backend full suite `617/617`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `530` files, `0` findings; markdown encoding check OK.

## 0.321.0 - 2026-07-01

Release entry: `2026-07-01-vps-production-smoke-generator-release-guard`.

### Fixed
- `scripts/new-vps-production-smoke-report.ps1 -ReleaseId` now rejects unknown manual release ids before writing a VPS production smoke draft.

### Added
- `scripts/test-vps-production-smoke-report-generator-release-guard.ps1` proves that the generator fails fast and leaves no JSON artifact for an unknown `ReleaseId`.
- `P11-ACC-075` documents the local guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `326/346` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: VPS production smoke generator release guard regression OK; targeted VPS/docs/release suite `24/24`; backend full suite `616/616`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `529` files, `0` findings; markdown encoding check OK.

## 0.320.0 - 2026-07-01

Release entry: `2026-07-01-staging-smoke-generator-release-guard`.

### Fixed
- `scripts/new-staging-smoke-report.ps1 -ReleaseId` now rejects unknown manual release ids before writing a staging smoke draft.

### Added
- `scripts/test-staging-smoke-report-generator-release-guard.ps1` proves that the generator fails fast and leaves no JSON artifact for an unknown `ReleaseId`.
- `P9-TST-007G` documents the local guard while `P9-TST-007` remains in progress until real staging/VPS smoke evidence exists.

### Notes
- Roadmap progress is now `325/345` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: staging smoke generator release guard regression OK; targeted staging/docs/release suite `25/25`; backend full suite `615/615`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `528` files, `0` findings; markdown encoding check OK.

## 0.319.0 - 2026-07-01

Release entry: `2026-07-01-payment-smoke-generator-release-guard`.

### Fixed
- `scripts/new-payment-provider-smoke-report.ps1 -ReleaseId` now rejects unknown manual release ids before writing a payment provider smoke draft.

### Added
- `scripts/test-payment-provider-smoke-report-generator-release-guard.ps1` proves that the generator fails fast and leaves no JSON artifact for an unknown `ReleaseId`.
- `P0-PAY-016` documents the local guard while `STATE-011` and `P0-PAY-002` ... `P0-PAY-009` remain open until real payment provider smoke evidence exists.

### Notes
- Roadmap progress is now `324/344` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: payment provider smoke generator release guard regression OK; targeted payment/docs/release suite `23/23`; backend full suite `614/614`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `527` files, `0` findings; markdown encoding check OK.

## 0.318.0 - 2026-07-01

Release entry: `2026-07-01-vpn-live-smoke-generator-release-guard`.

### Fixed
- `scripts/new-vpn-live-smoke-report.ps1 -ReleaseId` now rejects unknown manual release ids before writing a VPN live smoke draft.

### Added
- `scripts/test-vpn-live-smoke-report-generator-release-guard.ps1` proves that the generator fails fast and leaves no JSON artifact for an unknown `ReleaseId`.
- `P0-VPN-008` documents the local guard while `STATE-012` and `P0-VPN-001` ... `P0-VPN-005` remain open until real 3x-ui/VPN live smoke evidence exists.

### Notes
- Roadmap progress is now `323/343` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: VPN live smoke generator release guard regression OK; targeted VPN/docs/release suite `21/21`; backend full suite `613/613`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `526` files, `0` findings; markdown encoding check OK.

## 0.317.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-smoke-generator-release-guard`.

### Fixed
- `scripts/new-admin-vps-smoke-report.ps1 -ReleaseId` now rejects unknown manual release ids before writing a draft report.

### Added
- `scripts/test-admin-vps-smoke-report-generator-release-guard.ps1` proves that the generator fails fast and leaves no JSON artifact for an unknown `ReleaseId`.
- `P0-ADMIN-002BF` documents the local guard while `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until real VPS admin smoke evidence exists.

### Notes
- Roadmap progress is now `322/342` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke generator release guard regression OK; targeted admin/docs/release suite `34/34`; backend full suite `612/612`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `525` files, `0` findings; markdown encoding check OK.

## 0.316.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-bootstrap-smoke-evidence-latest-release-guard`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` now rejects paired bootstrap readiness/smoke evidence whose `releaseId` does not match the latest active release before accepting strict reports.

### Added
- `scripts/test-admin-vps-bootstrap-smoke-evidence-latest-release-guard.ps1` proves that a stale bootstrap evidence chain is rejected.
- `P0-ADMIN-001BV` documents the local guard while `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until real VPS admin bootstrap/smoke evidence exists.

### Notes
- Roadmap progress is now `321/341` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS bootstrap smoke evidence latest release guard regression OK; targeted admin/bootstrap/docs/release suite `46/46`; backend full suite `611/611`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `524` files, `0` findings; markdown encoding check OK.

## 0.315.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-smoke-evidence-latest-release-guard`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` now rejects paired admin VPS smoke evidence whose `releaseId` does not match the latest active release before accepting strict preflight/browser smoke reports.

### Added
- `scripts/test-admin-vps-smoke-evidence-latest-release-guard.ps1` proves that a stale paired evidence chain is rejected.
- `P0-ADMIN-002BE` documents the local guard while `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until real VPS admin smoke evidence exists.

### Notes
- Roadmap progress is now `320/340` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke evidence latest release guard regression OK; targeted admin/docs/release suite `33/33`; backend full suite `610/610`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `523` files, `0` findings; markdown encoding check OK.

## 0.314.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-smoke-preflight-latest-release-guard`.

### Fixed
- `scripts/validate-admin-vps-smoke-preflight-report.ps1 -RequireReady` now rejects ready preflight reports whose `releaseId` does not match the latest active release.

### Added
- `scripts/test-admin-vps-smoke-preflight-latest-release-guard.ps1` proves that a ready preflight report with stale `releaseId` is rejected before browser smoke can start.
- `P0-ADMIN-002BD` documents the local guard while `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until real VPS admin smoke evidence exists.

### Notes
- Roadmap progress is now `319/339` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke preflight latest release guard regression OK; targeted admin/docs/release suite `32/32`; backend full suite `609/609`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `522` files, `0` findings; markdown encoding check OK.

## 0.313.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-archive-latest-release-guard`.

### Fixed
- `scripts/validate-production-evidence-handoff-package-archive.ps1 -RequireProductionReady` now rejects handoff package ZIP artifacts whose package index `releaseId` is stale before archive acceptance can continue.

### Added
- `scripts/test-production-evidence-handoff-package-archive-latest-release-guard.ps1` proves that a handoff package archive with stale `releaseId` is rejected.
- `P11-ACC-074` documents the acceptance guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `318/338` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff package archive latest release guard regression OK; targeted production handoff/docs/release suite `81/81`; backend full suite `608/608`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `521` files, `0` findings; markdown encoding check OK.

## 0.312.0 - 2026-07-01

Release entry: `2026-07-01-production-evidence-bundle-latest-release-guard`.

### Fixed
- `scripts/validate-production-evidence-bundle.ps1 -RequireProductionReady` now rejects evidence bundles whose required reports carry stale `releaseId` values before final evidence packaging can continue.

### Added
- `scripts/test-production-evidence-bundle-latest-release-guard.ps1` proves that a production-ready bundle with stale report `releaseId` is rejected.
- `P11-ACC-073` documents the acceptance guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `317/337` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production evidence bundle latest release guard regression OK; targeted production handoff/docs/release suite `80/80`; backend full suite `607/607`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `520` files, `0` findings; markdown encoding check OK.

## 0.311.0 - 2026-07-01

Release entry: `2026-07-01-production-readiness-assertion-latest-release-guard`.

### Fixed
- `scripts/validate-production-readiness-assertion-result.ps1 -RequireProductionReady` now rejects production-ready assertion results whose `releaseId` does not match the latest active release in `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Added
- `scripts/test-production-readiness-assertion-result-latest-release-guard.ps1` proves that a production-ready assertion result with stale `releaseId` is rejected before linked report and Markdown checks.
- `P11-ACC-072` documents the acceptance guard while `P11-ACC-002` remains open until real VPS production smoke evidence exists.

### Notes
- Roadmap progress is now `316/336` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production readiness assertion result latest release guard regression OK; targeted production handoff/docs/release suite `79/79`; backend full suite `606/606`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `519` files, `0` findings; markdown encoding check OK.

## 0.310.0 - 2026-07-01

Release entry: `2026-07-01-production-readiness-summary-latest-release-guard`.

### Added

- `scripts/test-production-readiness-summary-latest-release-guard.ps1` proves that a production-ready summary with stale `releaseId` is rejected.
- Roadmap item `P11-ACC-071` documents the latest-release acceptance guard while keeping real VPS production smoke open.

### Changed

- `scripts/validate-production-readiness-summary.ps1 -RequireProductionReady` now requires the summary `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `315/335` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production readiness summary latest release guard regression OK; targeted production handoff/docs/release suite `78/78`; backend full suite `605/605`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `518` files, `0` findings; markdown encoding check OK.

## 0.309.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-ci-summary-latest-release-guard`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-summary-latest-release-guard.ps1` proves that a production-ready handoff CI summary with stale `releaseId` is rejected.
- Roadmap item `P11-ACC-070` documents the latest-release acceptance guard while keeping real VPS production smoke open.

### Changed

- `scripts/validate-production-evidence-handoff-package-archive-ci-summary.ps1 -RequireProductionReady` now requires the summary result `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `314/334` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff CI summary latest release guard regression OK; targeted production handoff/docs/release suite `77/77`; backend full suite `604/604`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `517` files, `0` findings; markdown encoding check OK.

## 0.308.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-ci-result-latest-release-guard`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-regression-result-latest-release-guard.ps1` proves that a production-ready handoff CI result with stale `releaseId` is rejected.
- Roadmap item `P11-ACC-069` documents the latest-release acceptance guard while keeping real VPS production smoke open.

### Changed

- `scripts/validate-production-evidence-handoff-package-archive-ci-regression-result.ps1 -RequireProductionReady` now requires the result `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `313/333` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff CI result latest release guard regression OK; targeted production handoff/docs/release suite `76/76`; backend full suite `603/603`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `516` files, `0` findings; markdown encoding check OK.

## 0.307.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-flow-result-latest-release-guard`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-flow-result-latest-release-guard.ps1` proves that a production-ready handoff archive flow result with stale `releaseId` is rejected.
- Roadmap item `P11-ACC-068` documents the latest-release acceptance guard while keeping real VPS production smoke open.

### Changed

- `scripts/validate-production-evidence-handoff-package-archive-flow-result.ps1 -RequireProductionReady` now requires the result `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `312/332` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff flow result latest release guard regression OK; targeted production handoff/docs/release suite `75/75`; backend full suite `602/602`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `515` files, `0` findings; markdown encoding check OK.

## 0.306.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-package-latest-release-guard`.

### Added

- `scripts/test-production-evidence-handoff-package-latest-release-guard.ps1` proves that a production-ready handoff package index with stale `releaseId` is rejected.
- Roadmap item `P11-ACC-067` documents the latest-release acceptance guard while keeping real VPS production smoke open.

### Changed

- `scripts/validate-production-evidence-handoff-package.ps1 -RequireProductionReady` now requires the package index `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `311/331` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff package latest release guard regression OK; targeted production handoff/docs/release suite `74/74`; backend full suite `601/601`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `514` files, `0` findings; markdown encoding check OK.

## 0.305.0 - 2026-07-01

Release entry: `2026-07-01-production-handoff-checklist-latest-release-guard`.

### Added

- `scripts/test-production-evidence-handoff-checklist-latest-release-guard.ps1` proves that a production-ready handoff checklist with stale `releaseId` is rejected.
- Roadmap item `P11-ACC-066` documents the latest-release acceptance guard while keeping real VPS production smoke open.

### Changed

- `scripts/validate-production-evidence-handoff-checklist.ps1 -RequireProductionReady` now requires the checklist `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `310/330` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: production handoff checklist latest release guard regression OK; targeted production handoff/docs/release suite `73/73`; backend full suite `600/600`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `513` files, `0` findings; markdown encoding check OK.

## 0.304.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-bootstrap-smoke-latest-release-guard`.

### Added

- `scripts/test-admin-vps-bootstrap-smoke-latest-release-guard.ps1` proves that ready/passed admin VPS bootstrap smoke reports with stale `releaseId` are rejected.
- Roadmap item `P0-ADMIN-001BU` documents the latest-release acceptance guard while keeping real VPS admin bootstrap/smoke items open for external evidence.

### Changed

- `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1 -RequireReady` now requires the readiness `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` now requires the final bootstrap smoke `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `309/329` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS bootstrap smoke latest release guard regression OK; targeted admin bootstrap/docs/release suite `27/27`; backend full suite `599/599`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `512` files, `0` findings; markdown encoding check OK.

## 0.303.0 - 2026-07-01

Release entry: `2026-07-01-admin-vps-smoke-latest-release-guard`.

### Added

- `scripts/test-admin-vps-smoke-report-latest-release-guard.ps1` proves that a fully passed admin VPS smoke report with stale `releaseId` is rejected.
- Roadmap item `P0-ADMIN-002BC` documents the latest-release acceptance guard while keeping real VPS admin smoke items open for external evidence.

### Changed

- `scripts/validate-admin-vps-smoke-report.ps1 -RequireAllPassed` now requires the report `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `308/328` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: admin VPS smoke latest release guard regression OK; targeted admin VPS/docs/release suite `31/31`; backend full suite `598/598`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `511` files, `0` findings; markdown encoding check OK.

## 0.302.0 - 2026-07-01

Release entry: `2026-07-01-vps-production-smoke-latest-release-guard`.

### Added

- `scripts/test-vps-production-smoke-report-latest-release-guard.ps1` proves that a fully passed VPS production smoke report with stale `releaseId` is rejected.
- Roadmap item `P11-ACC-065` documents the latest-release acceptance guard while keeping `P11-ACC-002` open for real VPS evidence.

### Changed

- `scripts/validate-vps-production-smoke-report.ps1 -RequireAllPassed` now requires the report `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `307/327` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: VPS production smoke latest release guard regression OK; targeted VPS production/docs/release suite `23/23`; backend full suite `597/597`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `510` files, `0` findings; markdown encoding check OK.

## 0.301.0 - 2026-07-01

Release entry: `2026-07-01-vpn-live-smoke-latest-release-guard`.

### Added

- `scripts/test-vpn-live-smoke-report-latest-release-guard.ps1` proves that a fully passed VPN live smoke report with stale `releaseId` is rejected.
- Roadmap item `P0-VPN-007` documents the latest-release acceptance guard while keeping real 3x-ui/VPN smoke items open for external evidence.

### Changed

- `scripts/validate-vpn-live-smoke-report.ps1 -RequireAllPassed` now requires the report `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `306/326` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: VPN live smoke latest release guard regression OK; targeted VPN live/docs/release suite `20/20`; backend full suite `596/596`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `509` files, `0` findings; markdown encoding check OK.

## 0.300.0 - 2026-07-01

Release entry: `2026-07-01-payment-smoke-latest-release-guard`.

### Added

- `scripts/test-payment-provider-smoke-report-latest-release-guard.ps1` proves that a fully passed payment provider smoke report with stale `releaseId` is rejected.
- Roadmap item `P0-PAY-015` documents the latest-release acceptance guard while keeping real provider smoke items open for external evidence.

### Changed

- `scripts/validate-payment-provider-smoke-report.ps1 -RequireAllPassed` now requires the report `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `305/325` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: payment provider smoke latest release guard regression OK; targeted payment provider/docs/release suite `22/22`; backend full suite `595/595`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `508` files, `0` findings; markdown encoding check OK.

## 0.299.0 - 2026-07-01

Release entry: `2026-07-01-staging-smoke-latest-release-guard`.

### Added

- `scripts/test-staging-smoke-report-latest-release-guard.ps1` proves that a fully passed staging smoke report with stale `releaseId` is rejected.
- Roadmap item `P9-TST-007F` documents the latest-release acceptance guard while keeping parent `P9-TST-007` open for real staging/VPS evidence.

### Changed

- `scripts/validate-staging-smoke-report.ps1 -RequireAllPassed` now requires the report `releaseId` to match the latest active release from `backend/src/VpnPlatform.Api/AppReleases/releases.json`.

### Notes

- Roadmap progress is now `304/324` closed, `19` open, `1` in progress and `0` blocked. The project remains `staging-ready baseline`, not production-ready.
- Verification: staging smoke latest release guard regression OK; targeted backend/docs/release suite `24/24`; backend full suite `594/594`; frontend tests `66/66`; frontend typecheck/build/audit OK; fresh local SQLite smoke OK; secret scan `507` files, `0` findings.

## Roadmap checkpoint - 2026-06-24

### Documentation

- `docs/PRODUCT_COMPLETION_ROADMAP.md` now records the temporary pause state, last verification date and remaining live/VPS blockers after release `2026-06-24-admin-vps-smoke-preflight-report-id-console`.

### Verification

- Roadmap/docs guard suite: OK, `15/15`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `3` changed/new files.

## 0.298.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-preflight-report-id-console`.

### Improved

- `scripts/admin-vps-smoke-preflight.ps1` now prints sanitized `Preflight report id` next to the preflight summary.
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` asserts that stdout report id matches the JSON `reportId` before browser smoke starts.

### Verification

- Admin VPS smoke tooling guard: OK, `AdminVpsSmokeReportTests` `15/15`.
- Admin VPS smoke flow wrapper regression: OK; failed preflight scenarios print `Preflight report id` and match it with JSON `reportId`.
- Admin VPS preflight validator regression: OK; standalone preflight prints `Preflight report id`.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-preflight-report-id-console`, console output includes `Preflight report id`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `18` changed/new files.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.297.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-remote-message-console`.

### Improved

- `scripts/admin-vps-smoke-preflight.ps1` now prints sanitized `Remote release message` next to remote release status, expected release and actual release.
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` asserts the unavailable remote release guidance before browser smoke starts.

### Verification

- Admin VPS smoke flow wrapper regression: OK; `remote-release-mismatch` console output includes the unavailable remote release guidance.
- Admin VPS preflight validator regression: OK; standalone preflight prints `Remote release message`.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-remote-message-console`, console output includes `Remote release message`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `18` changed/new files.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.296.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-preflight-check-counts`.

### Improved

- `scripts/admin-vps-smoke-preflight.ps1` now writes sanitized `checkCount`, `passedCheckCount` and `failedCheckCount` and prints total/passed/failed check counts before browser smoke.
- `scripts/validate-admin-vps-smoke-preflight-report.ps1` validates count consistency against the `checks` array before accepting preflight evidence.
- `scripts/test-admin-vps-smoke-preflight-validator.ps1` covers `mismatched-check-count` and `mismatched-passed-check-count`; wrapper regression asserts the count summary in both report and console output.

### Verification

- Admin VPS preflight validator regression: OK after fixture update; valid reports keep `checkCount=10`, `passedCheckCount=10`, `failedCheckCount=0`, and mismatched total/passed/failed counts are rejected.
- Admin VPS smoke flow wrapper regression: OK; failed preflight scenarios print `Check count`, `Passed checks` and `Failed check count`.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-preflight-check-counts`, preflight counts `10/10/0`, remote release status `matched`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `20` changed/new files.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.295.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-preflight-failed-count`.

### Improved

- `scripts/admin-vps-smoke-preflight.ps1` now writes sanitized `failedCheckCount` and prints `Failed check count` before browser smoke.
- `scripts/validate-admin-vps-smoke-preflight-report.ps1` validates that `failedCheckCount` matches failed `checks` and `failedChecks`.
- `scripts/test-admin-vps-smoke-preflight-validator.ps1` covers `mismatched-failed-check-count`; wrapper regression asserts the count in both report and console output.

### Verification

- Admin VPS preflight validator regression: OK; valid reports keep `failedCheckCount=0`, stale release evidence keeps `failedCheckCount=1`, and `mismatched-failed-check-count` is rejected.
- Admin VPS smoke flow wrapper regression: OK; failed preflight scenarios print `Failed check count` and keep it consistent with `failedChecks`.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-preflight-failed-count`, preflight `failedCheckCount=0`, `failedChecks=[]`, remote release status `matched`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `20` changed/new files.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.294.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-preflight-failed-checks`.

### Improved

- `scripts/admin-vps-smoke-preflight.ps1` now writes sanitized `failedChecks` and prints `Failed checks` in stdout before browser smoke.
- `scripts/validate-admin-vps-smoke-preflight-report.ps1` validates that `failedChecks` exactly matches failed `checks` entries and keeps `readyForLiveSmoke` consistent.
- `scripts/test-admin-vps-smoke-preflight-validator.ps1` covers `mismatched-failed-checks`; wrapper regression asserts the expected failed check in both report and console output.

### Verification

- Admin VPS preflight validator regression: OK; valid reports keep `failedChecks=[]`, stale release evidence keeps `failedChecks=["remote-latest-release"]`, and `mismatched-failed-checks` is rejected.
- Admin VPS smoke flow wrapper regression: OK; failed preflight scenarios print `Failed checks` and store the expected failed check in the preflight report.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-preflight-failed-checks`, preflight `failedChecks=[]`, remote release status `matched`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `20` changed/new files.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.293.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-remote-release-console-summary`.

### Improved

- `scripts/admin-vps-smoke-preflight.ps1` now prints safe remote release diagnostics in stdout: status, expected release and actual remote release.
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` asserts the console diagnostic for fail-closed remote release scenarios before browser smoke starts.

### Verification

- Admin VPS smoke tooling guard: OK, `AdminVpsSmokeReportTests` `15/15`.
- Admin VPS smoke flow wrapper regression: OK; `remote-release-mismatch` console output includes `Remote release status: unavailable`.
- Local SQLite admin VPS browser smoke: OK; console summary showed `Remote release status: matched`, latest release `2026-06-24-admin-vps-smoke-remote-release-console-summary`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `18` changed/new files.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.292.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-remote-release-diagnostics`.

### Improved

- `scripts/admin-vps-smoke-preflight.ps1` now records sanitized `remoteReleaseStatus` and `remoteReleaseMessage` fields for `not-required`, `matched`, `mismatch` and `unavailable` outcomes.
- `scripts/validate-admin-vps-smoke-preflight-report.ps1` validates the remote release diagnostic fields and rejects tampered matched reports when `remoteReleaseId` differs from the local smoke `releaseId`.
- Admin VPS wrapper regression now asserts remote release diagnostics before browser smoke starts.

### Verification

- Admin VPS smoke tooling guard: OK, `AdminVpsSmokeReportTests` `15/15`.
- Admin VPS preflight validator regression: OK; valid stale release evidence and `bad-remote-release-status` passed.
- Admin VPS smoke flow wrapper regression: OK; `remote-release-mismatch` stops before browser smoke and records `remoteReleaseStatus=unavailable`.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-remote-release-diagnostics`, preflight checks `10/10`, remote release status `matched`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `20` changed/new files.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.291.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-remote-release-preflight`.

### Fixed

- `scripts/admin-vps-smoke.ps1` now requires a remote release match before browser smoke.
- `scripts/admin-vps-smoke-preflight.ps1` authenticates through `/api/auth/login`, keeps the bearer token in memory and records only sanitized `remoteReleaseId`/`remoteReleaseMatched` evidence.
- Future-dated local release seeds no longer block the local SQLite admin VPS smoke; the `0.291.0` release timestamp is in the past relative to the 2026-06-24 verification run.

### Verification

- Admin VPS smoke tooling guard: OK, `AdminVpsSmokeReportTests` `15/15`.
- Admin VPS preflight validator regression: OK; `remote-latest-release` is required and secret markers such as `bearer ` are rejected.
- Admin VPS smoke flow wrapper regression: OK; `remote-release-mismatch` stops before browser smoke.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-remote-release-preflight`, preflight checks `10/10`, remote release matched, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Targeted admin/docs/release .NET suite: OK, `30/30`.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `20` changed/new files.
- Real VPS read-only inventory: systemd/nginx deploy is live at `83.147.222.145`, API health OK on port `8080`, but installed `/opt/vpn-platform/api/AppReleases/releases.json` does not contain the `0.291.0` release; this is stale-deploy evidence, not a passed smoke report.
- `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until the latest commits are deployed and a full passed VPS admin smoke report is captured.

## 0.290.0 - 2026-06-24

Release entry: `2026-06-24-admin-vps-smoke-navigation-fallback`.

### Fixed

- `frontend/e2e/admin-vps-smoke.spec.ts` now opens admin sections through `role=tab`, `role=link` or direct hash fallback from `docs/admin-vps-smoke-sections.json`.
- Legacy deployed admin UIs no longer turn missing navigation items into a generic 120-second Playwright timeout.

### Verification

- Admin VPS smoke tooling guard: OK, `AdminVpsSmokeReportTests` `15/15`.
- Local SQLite admin VPS browser smoke: OK; latest release `2026-06-24-admin-vps-smoke-navigation-fallback`, preflight checks `9/9`, smoke sections `16/16`, admin login passed, JS/unauthorized errors absent.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `18` changed/new files.
- Real VPS admin smoke attempt: preflight OK and admin login passed; sections through `support` passed, then required section `audit` failed to load on the currently deployed VPS UI. `STATE-013`, `P0-ADMIN-001` and `P0-ADMIN-002` remain open until a full passed VPS smoke report is captured after deploy.

## 0.289.0 - 2026-06-24

Release entry: `2026-06-24-deploy-production-env-normalizer`.

### Fixed

- `deploy-vps` now normalizes `PRODUCTION_ENV_FILE` before uploading shared `.env` to the VPS.
- Stale production secrets can no longer re-enable `ASPNETCORE_ENVIRONMENT=Local`, auto migrations, demo seed, Swagger or persistent admin bootstrap during Docker/systemd deploy.
- `AdminBootstrap__Password` is cleared from the uploaded shared env file, so one-shot admin reset stays outside the long-lived service configuration.

### Verification

- Deploy production env normalizer regression: OK.
- Targeted deploy/docs/release .NET suite: OK, `18/18`.
- Local SQLite admin bootstrap smoke: OK; latest release `2026-06-24-deploy-production-env-normalizer`, readiness checks `17/17`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- Backend full suite: OK, `593/593`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `564`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `19` changed/new files.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a full real VPS admin bootstrap/login smoke report is captured.

## 0.288.0 - 2026-06-23

Release entry: `2026-06-23-admin-bootstrap-readiness-password-env-validator`.

### Fixed

- `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1` now validates the `passwordEnvName` field itself, not only the generated readiness check.
- Tampered readiness reports with values like `Path` now fail validation even if `password-env-name-safe` is manually left passed.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` now covers `mismatched-readiness-password-env-name-safe`.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK; `16` scenarios passed, including `mismatched-readiness-password-env-name-safe`.
- Targeted docs/release .NET suite: OK, `41/41`.
- Local SQLite admin bootstrap smoke: OK; latest release `2026-06-23-admin-bootstrap-readiness-password-env-validator`, readiness checks `17/17`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- Backend full suite: OK, `592/592`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `562`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `19` changed/new files.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.287.0 - 2026-06-23

Release entry: `2026-06-23-admin-bootstrap-password-env-name-guard`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke.ps1` now rejects password env names that are not safe environment variable identifiers or do not contain `PASSWORD`.
- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now records a failed `password-env-name-safe` check and does not read unsafe env names.
- Wrapper/readiness regressions now cover unsafe password env names without leaking the password or creating smoke artifacts.

### Verification

- Admin VPS bootstrap smoke wrapper regression: OK; `25` scenarios passed, including `bad-password-env-name` fail-fast before smoke artifacts.
- Admin VPS bootstrap smoke readiness regression: OK; `15` scenarios passed, including `bad-password-env-name` with failed `password-env-name-safe` readiness check.
- Targeted docs/release .NET suite: OK, `41/41`.
- Local SQLite admin bootstrap smoke: OK; latest release `2026-06-23-admin-bootstrap-password-env-name-guard`, readiness checks `17/17`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- Backend full suite: OK, `592/592`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `562`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `22` changed/new files.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.286.0 - 2026-06-23

Release entry: `2026-06-23-admin-bootstrap-nonlocal-reset-guard`.

### Fixed

- `scripts/admin-bootstrap.ps1` now requires `-ConfirmBootstrapReset` before direct bootstrap/reset against any non-local database.
- The direct wrapper now fails fast when a non-local run has no `ConnectionString`, before process env setup or bootstrap execution.
- `scripts/test-admin-bootstrap-wrapper.ps1` now covers `missing-confirm-bootstrap-reset` and `missing-connection-string` without leaking the password.

### Verification

- Direct admin bootstrap wrapper regression: OK; scenarios `provider-case-normalized`, `local-sqlite-overrides-provider`, `bad-provider`, `missing-confirm-bootstrap-reset` and `missing-connection-string` passed.
- Targeted docs/release .NET suite: OK, `41/41`.
- Local SQLite admin bootstrap smoke: OK; latest release `2026-06-23-admin-bootstrap-nonlocal-reset-guard`, readiness checks `16/16`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- Backend full suite: OK, `592/592`.
- Frontend tests: OK, `66/66`; Playwright console E2E: OK, `9/9`.
- Frontend typecheck/build/audit: OK; audit high threshold found `0` vulnerabilities.
- Secret scan: OK, files scanned `562`, findings `0`; `git diff --check`: OK; strict UTF-8 without BOM: OK, checked `18` changed/new files.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.285.0 - 2026-06-23

Release entry: `2026-06-23-admin-bootstrap-provider-normalization`.

### Fixed

- `scripts/admin-bootstrap.ps1` now canonicalizes case-insensitive `Postgres`/`Sqlite` provider values and rejects unsupported providers before setting process env values.
- `-LocalSqlite` still forces provider `Sqlite`, so local bootstrap dry-runs cannot inherit an invalid CLI/env provider.
- Added `scripts/test-admin-bootstrap-wrapper.ps1` for provider normalization, local SQLite override and bad-provider fail-fast coverage without password leakage.

### Verification

- Direct admin bootstrap wrapper regression: OK, `provider-case-normalized`, `local-sqlite-overrides-provider` and `bad-provider` passed without password leakage.
- Targeted docs/release unit suite: 41/41.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-bootstrap-provider-normalization`, readiness checks `16/16`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- Backend full suite: 592/592.
- Frontend tests: 66/66.
- Frontend typecheck/build/audit: OK, audit 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: OK, files scanned 562, findings 0.
- Changed files encoding: strict UTF-8 without BOM, 17 files checked.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.284.0 - 2026-06-23

Release entry: `2026-06-23-admin-bootstrap-profile-normalization`.

### Fixed

- `scripts/admin-bootstrap.ps1` now trims `EnvironmentName`, `Email`, `DisplayName`, `RolesCsv` and `Provider` before process env setup and safe console output.
- `scripts/admin-vps-bootstrap-smoke.ps1` now forwards trimmed `DisplayName` and `RolesCsv` into the bootstrap step while leaving the password and connection string untouched.
- Regression coverage now includes `dry-run-admin-bootstrap-profile-normalized` and static guards for normalized bootstrap profile arguments.

### Verification

- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-admin-bootstrap-profile-normalized` prints `Roles: SuperAdmin` without surrounding whitespace and does not start browser smoke.
- Targeted docs/release unit suite: 40/40.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-bootstrap-profile-normalization`, readiness checks `16/16`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- Backend full suite: 591/591.
- Frontend tests: 66/66.
- Frontend typecheck/build/audit: OK, audit 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: OK, files scanned 561, findings 0.
- Changed files encoding: strict UTF-8 without BOM, 19 files checked.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.283.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-workspace-path-normalization`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke-readiness.ps1`, `scripts/admin-vps-smoke-preflight.ps1` and `scripts/admin-vps-browser-smoke.ps1` now trim `ProjectPath`/`FrontendPath` workspace path inputs before local file/directory checks.
- `scripts/admin-vps-bootstrap-smoke.ps1`, `scripts/admin-vps-smoke.ps1` and `scripts/admin-bootstrap.ps1` now pass trimmed workspace paths to downstream scripts while keeping internal path spaces intact.
- `DataProtectionKeyPath` is trimmed before being forwarded to the admin bootstrap process environment.
- Regression coverage now includes `workspace-paths-normalized`, `dry-run-workspace-paths-normalized` and `preflight-workspace-path-normalized`.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `workspace-paths-normalized` accepts padded `ProjectPath` and `FrontendPath`.
- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-workspace-paths-normalized` passes padded workspace paths through readiness without starting browser smoke.
- Admin VPS smoke flow wrapper regression: OK, `preflight-workspace-path-normalized` reaches the expected password guard instead of failing `frontend-directory`.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-workspace-path-normalization`, readiness checks `16/16`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.282.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-report-path-normalization`.

### Fixed

- `scripts/admin-vps-smoke.ps1` now trims `SmokeReportPath` and `PreflightReportPath` before distinct-path checks, preflight/browser args and evidence validation.
- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` and `scripts/admin-vps-bootstrap-smoke.ps1` now trim readiness, smoke, preflight and bootstrap smoke report paths before writing sanitized evidence links.
- Regression coverage now includes `report-paths-normalized`, `dry-run-report-paths-normalized` and `same-report-paths-normalized`.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `report-paths-normalized` writes trimmed report paths into readiness evidence.
- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-report-paths-normalized` passes trimmed report paths through the wrapper into readiness evidence.
- Admin VPS smoke flow wrapper regression: OK, `same-report-paths-normalized` rejects report paths that differ only by surrounding whitespace.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-report-path-normalization`, readiness checks `16/16`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.281.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-bootstrap-password-env-normalization`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now trims `AdminPasswordEnvName` before checking the password environment variable and writing `passwordEnvName` to readiness evidence.
- `scripts/admin-vps-bootstrap-smoke.ps1` now uses the same trimmed `AdminPasswordEnvName` for password lookup, readiness args and sanitized bootstrap smoke evidence.
- Regression coverage now includes `password-env-name-normalized` and `dry-run-password-env-name-normalized`.
- `scripts/local-admin-vps-bootstrap-smoke.ps1` now checks local ports through a loopback `TcpListener` and stops only its own launched process trees through `taskkill.exe`, avoiding local smoke cleanup hangs on WMI/CIM cmdlets.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `password-env-name-normalized` writes a trimmed password env name into readiness evidence.
- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-password-env-name-normalized` passes the trimmed password env name through the wrapper into readiness evidence.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-bootstrap-password-env-normalization`, readiness checks `16/16`, preflight checks `9/9`, smoke sections `16/16`, provider `Sqlite`, admin login passed, JS/unauthorized errors absent.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.280.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-smoke-wrapper-identity-normalization`.

### Fixed

- `scripts/admin-vps-smoke.ps1` now uses trimmed `ApiBaseUrl`, `AdminWebUrl` and `AdminEmail` in console output, preflight args and browser smoke args.
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` covers `preflight-identity-values-normalized`, so preflight evidence cannot keep accidental surrounding whitespace in admin identity fields.

### Verification

- Admin VPS smoke flow wrapper regression: OK, `preflight-identity-values-normalized` writes trimmed API/admin URLs and admin email into preflight evidence; fail-fast scenarios still cover max duration, unknown release id, URL/email, report path, operator/environment defaults, password and frontend guards.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-smoke-wrapper-identity-normalization`, readiness checks `16/16`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.279.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-bootstrap-wrapper-admin-email-normalization`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke.ps1` now uses a trimmed `AdminEmail` in console output, readiness args, admin bootstrap args, smoke args and sanitized bootstrap smoke evidence.
- `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` covers `dry-run-admin-email-normalized`, so the wrapper cannot pass accidental surrounding whitespace into readiness evidence.

### Verification

- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-admin-email-normalized` writes a trimmed admin email into readiness evidence and existing fail-fast scenarios remain covered.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-bootstrap-wrapper-admin-email-normalization`, readiness checks `16/16`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.278.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-bootstrap-wrapper-url-normalization`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke.ps1` now uses trimmed `ApiBaseUrl` and `AdminWebUrl` in console output, readiness args, smoke args and sanitized bootstrap smoke evidence.
- `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` covers `dry-run-url-values-normalized`, so the wrapper cannot pass accidental surrounding whitespace into readiness evidence.

### Verification

- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-url-values-normalized` writes trimmed API/admin URLs into readiness evidence and existing fail-fast scenarios remain covered.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-bootstrap-wrapper-url-normalization`, readiness checks `16/16`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.277.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-readiness-url-normalization`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now trims `ApiBaseUrl` and `AdminWebUrl` before validation output and sanitized readiness evidence.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` covers `url-values-normalized`, so evidence identity cannot keep accidental surrounding whitespace around API/admin URLs.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `url-values-normalized` writes trimmed API/admin URLs and existing fail-closed scenarios remain covered.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-readiness-url-normalization`, readiness checks `16/16`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.276.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-readiness-admin-email-normalization`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now trims `AdminEmail` before validation output and sanitized readiness evidence.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` covers `admin-email-normalized`, so evidence identity cannot keep accidental surrounding whitespace.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `admin-email-normalized` writes a trimmed admin email and existing fail-closed scenarios remain covered.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-readiness-admin-email-normalization`, readiness checks `16/16`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.275.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-readiness-environment-default`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now normalizes empty or whitespace `EnvironmentName` to `Production` before writing sanitized readiness evidence.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` covers `environment-default-normalized`, so standalone readiness reports keep a non-empty environment identity.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `environment-default-normalized` writes `Production` and existing fail-closed scenarios remain covered.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-readiness-environment-default`, readiness checks `16/16`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.274.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-readiness-provider-normalization`.

### Fixed

- `scripts/admin-vps-bootstrap-smoke-readiness.ps1` now canonicalizes case-insensitive `Postgres`/`Sqlite` provider values before writing sanitized readiness evidence.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` covers `provider-case-normalized`, so standalone readiness accepts `postgres` while unsupported providers remain fail-closed.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `provider-case-normalized` writes canonical `Postgres` and fail-closed scenarios remain covered.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-readiness-provider-normalization`, readiness checks `16/16`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.273.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-readiness-provider-mode`.

### Fixed

- `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1` now rejects readiness reports where `localSqlite=true` but `provider` is not `Sqlite`.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` covers `mismatched-readiness-local-provider`, so tampered local SQLite readiness evidence cannot be accepted.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `mismatched-readiness-local-provider` fails with `provider must be Sqlite when localSqlite is true`.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-readiness-provider-mode`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.272.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-readiness-ready-flag`.

### Fixed

- `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1` now rejects readiness reports where `readyForBootstrapSmoke` does not match the actual `checks` array.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` covers `mismatched-readiness-ready-flag` without `-RequireReady`, so standalone sanitized readiness evidence cannot claim bootstrap readiness with failed checks.

### Verification

- Admin VPS bootstrap smoke readiness regression: OK, `mismatched-readiness-ready-flag` fails with `readyForBootstrapSmoke must match checks`.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-readiness-ready-flag`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.271.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-bootstrap-provider-guard`.

### Fixed
- `scripts/admin-vps-bootstrap-smoke.ps1` now fail-fast rejects unsupported non-local `Provider` values before readiness, bootstrap reset and smoke artifacts.
- `-LocalSqlite` now uses a canonical `Sqlite` provider value across readiness, bootstrap and final bootstrap smoke evidence.

### Verified
- Admin VPS bootstrap smoke wrapper regression: OK, `bad-provider` stops before readiness evidence.
- Local CLI bootstrap admin smoke on SQLite: OK, latest release `2026-06-23-admin-vps-bootstrap-provider-guard`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.270.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-release-id-known-guard`.

### Fixed
- `scripts/admin-vps-smoke.ps1` now fail-fast rejects a manual `ReleaseId` that is absent from `backend/src/VpnPlatform.Api/AppReleases/releases.json` before preflight/browser smoke artifacts.
- `scripts/admin-vps-bootstrap-smoke.ps1` applies the same known release guard before readiness, bootstrap reset and smoke artifacts.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `unknown-release-id` stops before preflight evidence.
- Admin VPS bootstrap smoke wrapper regression: OK, `unknown-release-id` stops before readiness evidence.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.269.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-environment-default`.

### Fixed
- `scripts/admin-vps-smoke.ps1` now resolves blank `EnvironmentName` to `staging` before preflight/browser smoke and passes the same value to both reports.
- `scripts/admin-vps-bootstrap-smoke.ps1` now resolves blank `EnvironmentName` to `Production` before readiness/bootstrap/smoke and reuses it in the final bootstrap smoke report.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `default-environment-missing-password` writes `staging` to preflight evidence.
- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-default-environment` writes `Production` to readiness evidence.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.268.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-operator-default`.

### Fixed
- `scripts/admin-vps-smoke.ps1` now resolves missing `Operator` to `manual-operator` before preflight/browser smoke and passes the same value to both reports.
- `scripts/admin-vps-bootstrap-smoke.ps1` now resolves one operator value before readiness/bootstrap/smoke and reuses it in the final bootstrap smoke report.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `default-operator-missing-password` writes `manual-operator` to preflight evidence.
- Admin VPS bootstrap smoke wrapper regression: OK, `dry-run-default-operator` writes `manual-operator` to readiness evidence.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.267.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-report-path-guard`.

### Fixed
- `scripts/admin-vps-smoke.ps1` now fail-fast rejects duplicate `SmokeReportPath`/`PreflightReportPath` before preflight/browser smoke and smoke artifacts.
- `scripts/admin-vps-bootstrap-smoke.ps1` applies the same distinct report path guard to smoke, preflight, readiness and bootstrap smoke report paths before readiness, bootstrap reset and smoke artifacts.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `same-report-paths` does not create preflight/smoke artifacts.
- Admin VPS bootstrap smoke wrapper regression: OK, `same-report-paths` does not create readiness/bootstrap/smoke artifacts.
- `P0-ADMIN-001`, `P0-ADMIN-002` and `STATE-013` remain open until a real VPS bootstrap/login smoke report is captured.

## 0.266.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-email-guard`.

### Fixed
- `scripts/admin-vps-smoke.ps1` теперь fail-fast валидирует `AdminEmail` до preflight/browser smoke и не создает preflight report для невалидного email.
- `scripts/admin-vps-bootstrap-smoke.ps1` применяет тот же email guard до readiness, bootstrap reset, передачи пароля и smoke artifacts.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `bad-admin-email` не создает preflight/smoke artifacts.
- Admin VPS bootstrap smoke wrapper regression: OK, `bad-admin-email` не создает readiness/bootstrap/smoke artifacts.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.265.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-url-guard`.

### Fixed
- `scripts/admin-vps-smoke.ps1` теперь fail-fast валидирует `ApiBaseUrl` и `AdminWebUrl` как absolute http/https URL до preflight/browser smoke.
- `scripts/admin-vps-bootstrap-smoke.ps1` применяет тот же URL guard до readiness, bootstrap reset, передачи пароля и smoke artifacts.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `bad-api-url` и `bad-admin-web-url` не создают preflight/smoke artifacts.
- Admin VPS bootstrap smoke wrapper regression: OK, `bad-api-url` и `bad-admin-web-url` не создают readiness/bootstrap/smoke artifacts.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.264.0 - 2026-06-23

Release entry: `2026-06-23-local-admin-bootstrap-port-guard`.

### Fixed
- `scripts/local-admin-vps-bootstrap-smoke.ps1` теперь явно валидирует `ApiPort` и `AdminPort` как TCP-порты 1..65535 и требует разные значения.
- Локальный wrapper fail-fast останавливается до создания `tmp/local-admin-vps-bootstrap-smoke`, локальной SQLite DB, API/admin web и smoke artifacts при нечисловых, вне диапазона или совпадающих портах.

### Verified
- Local admin VPS bootstrap smoke wrapper regression: OK, `format-api-port`, `too-low-api-port`, `too-high-admin-port` и `same-api-admin-port` покрыты.
- `AdminBootstrapCliScriptTests`: 10/10.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.263.0 - 2026-06-23

Release entry: `2026-06-23-admin-vps-max-duration-format-guard`.

### Fixed
- `scripts/admin-vps-smoke.ps1`, `scripts/admin-vps-bootstrap-smoke.ps1` и `scripts/local-admin-vps-bootstrap-smoke.ps1` теперь явно парсят `MaxEvidenceChainMinutes` и возвращают единое сообщение `MaxEvidenceChainMinutes must be an integer.` для нечисловых CLI/env значений.
- `scripts/validate-admin-vps-smoke-evidence.ps1` и `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` получили такой же standalone format guard без PowerShell binding error.

### Verified
- Admin VPS smoke/bootstrap/local wrapper regressions: OK, `format-max-evidence-chain-minutes` и `format-env-max-evidence-chain-minutes` не создают preflight/readiness/local smoke artifacts.
- Admin VPS smoke/bootstrap evidence validator regressions: OK, `format-max-evidence-chain-minutes` покрыт.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-23-admin-vps-max-duration-format-guard`, smoke sections `16/16`, `MaxEvidenceChainMinutes=120`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

Все заметные изменения проекта фиксируются в этом файле и в разделе "Что нового" внутри приложения. Подробный рабочий roadmap находится в `docs/PRODUCT_COMPLETION_ROADMAP.md`.

## 0.262.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-evidence-explicit-max-duration-guard`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` и `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` больше не зависят от PowerShell `ValidateRange` для `MaxEvidenceChainMinutes`.
- Standalone evidence validators теперь явно отклоняют `MaxEvidenceChainMinutes <= 0` и `> 1440` с едиными fail-fast сообщениями до чтения evidence reports.

### Verified
- Admin VPS smoke/bootstrap evidence validator regressions: OK, `bad-max-evidence-chain-minutes` и `too-high-max-evidence-chain-minutes` покрыты.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-evidence-explicit-max-duration-guard`, smoke sections `16/16`, `MaxEvidenceChainMinutes=120`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.261.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-env-upper-bound-guard`.

### Fixed
- `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` покрывает env upper-bound сценарий `too-high-env-max-evidence-chain-minutes` для `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES=1441`.
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` и `scripts/test-local-admin-vps-bootstrap-smoke-wrapper.ps1` зеркально проверяют, что env upper-bound guard не создает preflight/local smoke artifacts.

### Verified
- Admin VPS bootstrap/smoke/local wrapper regressions: OK, CLI/env/upper-bound max duration guard scenarios покрыты.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-smoke-env-upper-bound-guard`, smoke sections `16/16`, `MaxEvidenceChainMinutes=120`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.260.0 - 2026-06-22

Release entry: `2026-06-22-local-admin-bootstrap-smoke-explicit-max-duration-guard`.

### Fixed
- `scripts/local-admin-vps-bootstrap-smoke.ps1` теперь fail-fast отклоняет `MaxEvidenceChainMinutes > 1440` до проверки портов, запуска API/admin web, создания локальной SQLite DB и smoke artifacts.
- `scripts/test-local-admin-vps-bootstrap-smoke-wrapper.ps1` покрывает `too-high-max-evidence-chain-minutes` и проверяет, что local smoke artifacts не создаются.

### Verified
- Local admin VPS bootstrap smoke wrapper regression: OK, CLI/env/upper-bound max duration guard scenarios покрыты.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.259.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-explicit-max-duration-guard`.

### Fixed
- `scripts/admin-vps-smoke.ps1` больше не зависит от PowerShell `ValidateRange` для `MaxEvidenceChainMinutes`: CLI/env ошибки теперь возвращают единые fail-fast сообщения до preflight/browser smoke artifacts.
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` покрывает `too-high-max-evidence-chain-minutes` и проверяет, что `MaxEvidenceChainMinutes > 1440` не создает preflight/smoke artifacts.

### Verified
- Admin VPS smoke flow wrapper regression: OK, CLI/env max duration guard и upper-bound scenario покрыты.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS admin smoke report.

## 0.258.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-env-guard`.

### Fixed
- `scripts/admin-vps-bootstrap-smoke.ps1` теперь явно fail-fast отклоняет неположительный `MaxEvidenceChainMinutes` из CLI или `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES` до readiness, bootstrap reset и smoke artifacts.
- `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` проверяет точное сообщение `MaxEvidenceChainMinutes must be greater than 0` и отсутствие readiness/smoke artifacts для CLI/env fail-fast сценариев.

### Verified
- Admin VPS bootstrap smoke wrapper regression: OK, `bad-max-evidence-chain-minutes` и `bad-env-max-evidence-chain-minutes` покрыты до readiness/smoke artifacts.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.257.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-env-max-duration`.

### Fixed
- `scripts/admin-vps-smoke.ps1` теперь явно fail-fast отклоняет `MaxEvidenceChainMinutes <= 0` до вывода "flow is ready", preflight report и browser smoke, включая default из `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES`.
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` покрывает `bad-env-max-evidence-chain-minutes` и проверяет, что env-лимит не создает preflight/smoke artifacts.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `bad-max-evidence-chain-minutes` и `bad-env-max-evidence-chain-minutes` покрыты.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS admin smoke report.

## 0.256.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-env-max-duration`.

### Fixed
- `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` покрывает `bad-env-max-evidence-chain-minutes`, чтобы неверный `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES` fail-fast останавливал production bootstrap smoke wrapper до readiness/smoke artifacts.
- Regression harness очищает env-лимит для остальных сценариев, чтобы CLI/env проверки `MaxEvidenceChainMinutes` не влияли друг на друга.

### Verified
- Admin VPS bootstrap smoke wrapper regression: OK, `bad-max-evidence-chain-minutes` и `bad-env-max-evidence-chain-minutes` покрыты без запуска smoke artifacts.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.255.0 - 2026-06-22

Release entry: `2026-06-22-local-admin-bootstrap-smoke-env-max-duration`.

### Fixed
- `scripts/local-admin-vps-bootstrap-smoke.ps1` теперь использует `ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES` как default для `MaxEvidenceChainMinutes`, если параметр не передан явно.
- `scripts/test-local-admin-vps-bootstrap-smoke-wrapper.ps1` покрывает `bad-env-max-evidence-chain-minutes`, чтобы неверный env-лимит fail-fast останавливал локальный wrapper до smoke artifacts.

### Verified
- Local admin VPS bootstrap smoke wrapper regression: OK, `bad-max-evidence-chain-minutes` и `bad-env-max-evidence-chain-minutes` покрыты.
- Local CLI bootstrap admin smoke на SQLite: OK с `MaxEvidenceChainMinutes=120`, smoke sections `16/16`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.254.0 - 2026-06-22

Release entry: `2026-06-22-local-admin-bootstrap-smoke-wrapper-regression`.

### Fixed
- Добавлен `scripts/test-local-admin-vps-bootstrap-smoke-wrapper.ps1`, который проверяет fail-fast сценарий `bad-max-evidence-chain-minutes` для локального SQLite bootstrap smoke wrapper.
- Regression harness доказывает, что неверный `MaxEvidenceChainMinutes` останавливает локальный wrapper до запуска API/admin web, browser smoke и создания `tmp/local-admin-vps-bootstrap-smoke` artifacts.

### Verified
- Local admin VPS bootstrap smoke wrapper regression: OK, `bad-max-evidence-chain-minutes` покрыт.
- `AdminBootstrapCliScriptTests`: покрывает новый regression harness.
- Backend full suite: 591/591.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.253.0 - 2026-06-22

Release entry: `2026-06-22-local-admin-bootstrap-smoke-max-duration`.

### Fixed
- `scripts/local-admin-vps-bootstrap-smoke.ps1` принимает `MaxEvidenceChainMinutes`, заранее отклоняет неположительный лимит и передает его в общий `scripts/admin-vps-bootstrap-smoke.ps1`.
- Локальная SQLite-проверка admin bootstrap/login теперь явно доказывает тот же fail-closed срок годности evidence chain, который используется для operator/VPS wrappers.

### Verified
- Local CLI bootstrap admin smoke на SQLite: OK с `MaxEvidenceChainMinutes=120`, smoke sections `16/16`.
- `AdminBootstrapCliScriptTests`: покрывает локальный wrapper и передачу `-MaxEvidenceChainMinutes`.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.252.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-wrapper-max-duration`.

### Fixed
- `scripts/admin-vps-smoke.ps1` и `scripts/admin-vps-bootstrap-smoke.ps1` принимают `MaxEvidenceChainMinutes`, печатают примененный лимит без секретов и передают его в evidence validators.
- Wrapper regression harness добавил fail-fast сценарии `bad-max-evidence-chain-minutes`, чтобы операторский запуск не стартовал preflight/smoke при неверном лимите.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `bad-max-evidence-chain-minutes` не создает preflight report.
- Admin VPS bootstrap smoke wrapper regression: OK, `bad-max-evidence-chain-minutes` не создает smoke artifacts.
- `AdminVpsSmokeReportTests|AdminBootstrapCliScriptTests`: 24/24.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.251.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-chain-max-duration`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` и `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` получили fail-closed лимит `MaxEvidenceChainMinutes`, чтобы standalone evidence не принимал слишком растянутую или устаревшую цепочку preflight/smoke/bootstrap.
- Success summary теперь показывает `maxEvidenceChainMinutes`, чтобы оператор видел примененный срок годности evidence bundle без секретов.

### Verified
- Admin VPS smoke evidence validator regression: OK, сценарий `evidence-chain-duration-exceeds-max` отклоняет цепочку длиннее лимита.
- Admin VPS bootstrap smoke evidence validator regression: OK, сценарий `evidence-chain-duration-exceeds-max` отклоняет readiness/preflight/smoke/bootstrap bundle длиннее лимита.
- `AdminVpsSmokeReportTests|AdminBootstrapCliScriptTests`: 24/24.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-chain-max-duration`, smoke sections `16/16`, smoke/bootstrap evidence validators with expected SHA256 and max duration guard OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.250.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-chronology-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет в sanitized summary standalone admin smoke evidence порядок `preflight|smoke`.
- Admin VPS smoke evidence summary теперь показывает `evidenceChainDurationSeconds`, чтобы оператор видел полную длительность от preflight до завершения smoke без секретов.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary содержит chronology fields и full-chain duration.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-chronology-summary`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and chronology summary OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.249.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-chronology-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` добавляет в sanitized summary полный порядок evidence chain `readiness|preflight|smoke|bootstrap`.
- Bootstrap evidence summary теперь показывает `readinessToPreflightSeconds`, `smokeToBootstrapSeconds` и `evidenceChainDurationSeconds`, чтобы оператор видел порядок и длительность всей цепочки без секретов.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary содержит chronology fields и новые duration metrics.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-chronology-summary`, smoke sections `16/16`, bootstrap evidence validator with expected SHA256 and chronology summary OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.248.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-report-id-timestamp-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` fail-closed сверяет timestamp-суффиксы readiness/bootstrap/preflight/smoke report ids с `generatedAt`/`startedAt` соответствующих reports.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает tamper-сценарии `mismatched-readiness-report-id-timestamp`, `mismatched-bootstrap-report-id-timestamp`, `mismatched-preflight-report-id-timestamp` и `mismatched-smoke-report-id-timestamp`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, report id timestamp mismatches отклоняются fail-closed для readiness/bootstrap/preflight/smoke reports.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-report-id-timestamp-link`, smoke sections `16/16`, bootstrap evidence validator with expected SHA256 and report id timestamp link checks OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.247.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-report-id-timestamp-link`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed сверяет timestamp-суффикс `preflight.reportId` с `preflight.generatedAt`, а `smoke.reportId` с `smoke.startedAt`.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает tamper-сценарии `mismatched-preflight-report-id-timestamp` и `mismatched-smoke-report-id-timestamp`.

### Verified
- Admin VPS smoke evidence validator regression: OK, `mismatched-preflight-report-id-timestamp` и `mismatched-smoke-report-id-timestamp` отклоняются fail-closed.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-report-id-timestamp-link`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and report id timestamp link checks OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.246.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-report-id-timestamp`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed отклоняет linked preflight/smoke evidence, если report ids имеют правильный префикс, но не соответствуют timestamp-формату `yyyyMMdd-HHmmss`.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` переведен на timestamp ids в valid fixtures и покрывает tamper-сценарии `bad-preflight-report-id-timestamp` и `bad-smoke-report-id-timestamp`.

### Verified
- Admin VPS smoke evidence validator regression: OK, bad report id timestamp suffixes отклоняются fail-closed.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-report-id-timestamp`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and report id timestamp checks OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.245.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-report-id-timestamp`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` fail-closed отклоняет bootstrap evidence chain, если readiness/bootstrap/preflight/smoke report ids имеют правильный префикс, но не соответствуют timestamp-формату `yyyyMMdd-HHmmss`.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` переведен на timestamp ids в valid fixtures и покрывает tamper-сценарии `bad-readiness-report-id-timestamp`, `bad-bootstrap-report-id-timestamp`, `bad-preflight-report-id-timestamp` и `bad-smoke-report-id-timestamp`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, bad report id timestamp suffixes отклоняются fail-closed.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-report-id-timestamp`, smoke sections `16/16`, bootstrap evidence validator with expected SHA256 and report id timestamp checks OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.244.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-report-id-prefix`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` fail-closed отклоняет bootstrap evidence chain, если readiness/bootstrap/preflight/smoke report ids совпадают или не соответствуют ожидаемым префиксам.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает tamper-сценарии `duplicate-report-id`, `bad-readiness-report-id-prefix`, `bad-bootstrap-report-id-prefix`, `bad-preflight-report-id-prefix` и `bad-smoke-report-id-prefix`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, duplicate/bad report id prefixes отклоняются fail-closed.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-report-id-prefix`, smoke sections `16/16`, bootstrap evidence validator with expected SHA256 and report id prefix checks OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.243.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-report-id-prefix`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed отклоняет linked preflight/smoke evidence, если `preflight.reportId` не начинается с `admin-vps-smoke-preflight-` или `smoke.reportId` использует чужой prefix.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает tamper-сценарии `bad-preflight-report-id-prefix` и `bad-smoke-report-id-prefix`.

### Verified
- Admin VPS smoke evidence validator regression: OK, bad report id prefixes отклоняются fail-closed.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-report-id-prefix`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and report id prefix checks OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.242.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-report-id-uniqueness`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed отклоняет linked preflight/smoke evidence, если `preflight.reportId` и `smoke.reportId` пустые или совпадают.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает tamper-сценарий `duplicate-report-id` и подтверждает, что paired evidence нельзя принять с одинаковыми report ids.

### Verified
- Admin VPS smoke evidence validator regression: OK, `duplicate-report-id` отклоняется fail-closed.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-report-id-uniqueness`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and unique report ids OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.241.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-gate-flags-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `accountBootstrapChecked`, `adminLoginPassed`, `noJsErrors` и `noUnauthorizedAfterLogin` в sanitized success summary.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет gate flags synthetic valid-сценария и их `true` значения.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary включает `accountBootstrapChecked`, `adminLoginPassed`, `noJsErrors` и `noUnauthorizedAfterLogin`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-gate-flags-summary`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and gate flags summary OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.240.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-status-counts-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `passed`, `failed`, `blocked` и `skipped` в sanitized success summary рядом с `sections`.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет status counters synthetic valid-сценария: `sections=16`, `passed=16`, `failed=0`, `blocked=0`, `skipped=0`.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary включает `sections=16`, `passed=16`, `failed=0`, `blocked=0`, `skipped=0`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-status-counts-summary`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and status counters summary OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.239.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-report-id-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `preflightReportId` и `smokeReportId` в sanitized success summary после сверки linked preflight/smoke reports.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет report id fields и значения synthetic valid-сценария.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary включает `preflightReportId` и `smokeReportId`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-report-id-summary`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and report id summary OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.238.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-identity-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` добавляет `adminEmail` и `operator` в sanitized success summary после сверки preflight/smoke identity.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет identity fields и значения synthetic valid-сценария.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary включает `adminEmail` и `operator`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-identity-summary`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and identity summary OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.237.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-duration-order`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed проверяет, что linked smoke report не завершился раньше `startedAt`, чтобы sanitized duration summary не мог принять отрицательную длительность.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает tamper-сценарий `smoke-completed-before-started`.

### Verified
- Admin VPS smoke evidence validator regression: OK, `smoke-completed-before-started` отклоняется fail-closed.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-duration-order`, smoke sections `16/16`, smoke evidence validator with expected SHA256 and positive duration order OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.236.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-duration-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` теперь добавляет `preflightGeneratedAt`, `smokeStartedAt`, `smokeCompletedAt`, `preflightToSmokeSeconds` и `smokeDurationSeconds` в sanitized success summary.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет duration metrics и ожидаемые значения synthetic valid-сценария.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary включает smoke timing и duration metrics.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-duration-summary`, smoke sections `16/16`, smoke evidence validator with SHA256 and duration metrics OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.235.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-expected-fingerprint`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` теперь добавляет `preflightReportSha256` и `smokeReportSha256` в sanitized success summary.
- `scripts/validate-admin-vps-smoke-evidence.ps1` принимает expected SHA256 fingerprints `ExpectedPreflightReportSha256` и `ExpectedSmokeReportSha256` и fail-closed отклоняет bundle при несовпадении.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет success-сценарий с корректными expected SHA256 и fail-сценарий при несовпадении preflight fingerprint.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid expected SHA256 accepted, mismatched expected preflight SHA256 rejected.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-smoke-evidence-expected-fingerprint`, smoke sections `16/16`, smoke evidence validator with expected SHA256 OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.234.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-expected-fingerprint`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь принимает expected SHA256 fingerprints `ExpectedReadinessReportSha256`, `ExpectedBootstrapSmokeReportSha256`, `ExpectedPreflightReportSha256` и `ExpectedSmokeReportSha256`.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет success-сценарий с корректными expected SHA256 и fail-closed сценарий при несовпадении readiness fingerprint.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid expected SHA256 accepted, mismatched expected readiness SHA256 rejected.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-expected-fingerprint`, smoke sections `16/16`, paired evidence validator with expected SHA256 OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.233.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-fingerprint-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет SHA256 fingerprints `readinessReportSha256`, `bootstrapSmokeReportSha256`, `preflightReportSha256` и `smokeReportSha256` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` и `AdminBootstrapCliScriptTests` закрепляют наличие SHA256 fingerprints в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает SHA256 fingerprints.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-fingerprint-summary`, smoke sections `16/16`, paired evidence validator OK, summary содержит SHA256 fingerprints.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.232.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-duration-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет duration metrics `preflightToSmokeSeconds`, `smokeDurationSeconds`, `bootstrapDurationSeconds` и `readinessToBootstrapSeconds` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет duration metrics и ожидаемые значения synthetic valid-сценария.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает duration metrics и счетчики sections.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-duration-summary`, smoke sections `16/16`, paired evidence validator OK.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.231.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-smoke-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет linked preflight/smoke ids, smoke timing и счетчики admin sections в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет smoke summary fields в valid-сценарии.
- Smoke/report wrappers сортируют latest release через `DateTimeOffset.Parse`, поэтому timestamp с миллисекундами в `releases.json` корректно выбирается как актуальный release.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает linked smoke/preflight ids, timing и счетчики sections.
- `AdminBootstrapCliScriptTests`: 9/9.
- Local CLI bootstrap admin smoke на SQLite: OK, latest release `2026-06-22-admin-vps-bootstrap-evidence-smoke-summary`, smoke sections `16/16`, paired evidence validator OK.
- Targeted release/docs suite: 65/65.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.230.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-timing-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `readinessReportId`, `bootstrapSmokeReportId`, `readinessGeneratedAt`, `bootstrapGeneratedAt` и `bootstrapCompletedAt` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет report id и timing fields в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает report id и timing fields.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил report id и timing fields в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.229.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-readiness-inputs-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `passwordEnvName`, `passwordLengthOk`, `connectionStringPresent` и `applyMigrations` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет readiness input fields в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает `passwordEnvName`, `passwordLengthOk`, `connectionStringPresent` и `applyMigrations`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил readiness input fields в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.228.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-reset-flags-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `passwordEnvPresent`, `confirmBootstrapReset` и `bootstrapResetConfirmed` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет reset flags в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает `passwordEnvPresent`, `confirmBootstrapReset` и `bootstrapResetConfirmed`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил reset flags в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.227.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-status-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `readyForBootstrapSmoke` и `bootstrapStatus` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет status-поля в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает `readyForBootstrapSmoke` и `bootstrapStatus`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил status-поля в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.226.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-operator-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `operator` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет `operator` в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает `apiBaseUrl`, `adminWebUrl`, `adminEmail`, `operator`, `preflightReportPath` и `sectionsContractPath`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил `operator` в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.225.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-identity-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `apiBaseUrl`, `adminWebUrl` и `adminEmail` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет identity-поля в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает `apiBaseUrl`, `adminWebUrl`, `adminEmail`, `preflightReportPath` и `sectionsContractPath`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил identity-поля в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.224.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-sections-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `sectionsContractPath` в sanitized success summary.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет `sectionsContractPath` в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает `sectionsContractPath`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил `sectionsContractPath` в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.223.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-sections-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` теперь добавляет `sectionsContractPath` в sanitized success summary.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` проверяет `sectionsContractPath` в captured valid output.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary включает `sectionsContractPath`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local admin VPS smoke на SQLite: OK; paired evidence validator подтвердил `sectionsContractPath` в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.222.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-preflight-summary`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` теперь добавляет `preflightReportPath` в sanitized success summary.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` захватывает PowerShell information stream и проверяет `preflightReportPath` в valid output.

### Verified
- Admin VPS smoke evidence validator regression: OK, valid summary включает `preflightReportPath`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local admin VPS smoke на SQLite: OK; paired evidence validator подтвердил `preflightReportPath` в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9 after rerun, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.221.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-evidence-preflight-summary`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь добавляет `preflightReportPath` в sanitized success summary рядом с readiness/bootstrap/smoke paths.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` проверяет `preflightReportPath` в valid-сценарии.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, valid summary включает `preflightReportPath`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; paired evidence validator подтвердил `preflightReportPath` в success summary.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.220.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-readiness-preflight-timing-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` теперь fail-closed требует, чтобы linked preflight `generatedAt` не был раньше readiness `generatedAt`.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает `preflight-generated-before-readiness`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `preflight-generated-before-readiness`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; bootstrap report validator подтвердил readiness -> preflight timing link.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.219.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-readiness-smoke-path-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` теперь fail-closed сверяет readiness `smokeReportPath` и `preflightReportPath` с итоговым bootstrap smoke report.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает `mismatched-readiness-smoke-report-path` и `mismatched-readiness-preflight-report-path`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-readiness-smoke-report-path` и `mismatched-readiness-preflight-report-path`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; bootstrap report validator подтвердил readiness linked smoke/preflight paths.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.218.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-timing-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` теперь fail-closed требует, чтобы `generatedAt` итогового bootstrap smoke report не был раньше linked smoke `completedAt`.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает `bootstrap-generated-before-smoke-completed` и фиксирует новый timing gate в bootstrap evidence chain.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `bootstrap-generated-before-smoke-completed`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; bootstrap report validator подтвердил timing link.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, tests 66/66, console E2E 9/9, audit 0 vulnerabilities.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.217.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-evidence-timing-link`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` теперь fail-closed требует, чтобы smoke `startedAt` не был раньше preflight `generatedAt`.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` пишет `smokeReportPath` в synthetic smoke fixture и покрывает `smoke-started-before-preflight`.

### Verified
- Admin VPS smoke evidence validator regression: OK, включая `smoke-started-before-preflight`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; admin smoke evidence validator подтвердил timing link.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, console E2E 9/9.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.216.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-readiness-metadata-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` теперь сверяет `provider`, `passwordEnvName`, `localSqlite` и `confirmBootstrapReset` итогового bootstrap smoke report с readiness evidence.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-readiness-provider`, `mismatched-readiness-password-env-name`, `mismatched-readiness-local-sqlite` и `mismatched-readiness-confirm-bootstrap-reset`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая новые readiness metadata mismatch сценарии.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; bootstrap report validator подтвердил readiness metadata link.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, console E2E 9/9.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.215.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-readiness-chain-validate`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` теперь валидирует связанный readiness report перед paired evidence validation.
- Bootstrap smoke report validator сверяет `apiBaseUrl`, `adminWebUrl`, `environmentName`, `operator`, `adminEmail`, `releaseId` и `readiness.bootstrapSmokeReportPath` с readiness evidence.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает fail-closed `missing-bootstrap-readiness-report-link` и `mismatched-readiness-bootstrap-report-path`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `missing-bootstrap-readiness-report-link` и `mismatched-readiness-bootstrap-report-path`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; bootstrap report validator подтвердил readiness-chain link.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, console E2E 9/9.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.214.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-readiness-self-validate`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1 -RequireReady` теперь сверяет `readinessReportPath` с фактическим `-ReportPath`.
- `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` покрывает fail-closed `mismatched-readiness-report-self-link` для standalone readiness validation.

### Verified
- Admin VPS bootstrap smoke readiness regression: OK, включая `mismatched-readiness-report-self-link`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; standalone readiness validator подтвердил `readinessReportPath`.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, console E2E 9/9.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.213.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-report-self-validate`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1 -RequirePassed` теперь сверяет `bootstrapSmokeReportPath` с фактическим `-ReportPath` до paired evidence validation.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` фиксирует, что `mismatched-bootstrap-smoke-report-path` падает на standalone bootstrap report self-link.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-bootstrap-smoke-report-path`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; standalone bootstrap report validator подтвердил `bootstrapSmokeReportPath`.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, console E2E 9/9.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.212.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-environment-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` теперь сверяет `apiBaseUrl`, `adminWebUrl`, `environmentName` и `operator` итогового bootstrap smoke report с preflight и browser smoke reports.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-bootstrap-environment`, чтобы нельзя было смешать bootstrap evidence одного окружения со smoke evidence другого.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-bootstrap-environment`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; readiness/bootstrap/preflight/browser smoke reports связаны по окружению, URL, оператору, `adminEmail` и `releaseId`.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, console E2E 9/9.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.211.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-admin-email-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` теперь сверяет `adminEmail` итогового bootstrap smoke report с preflight и browser smoke reports.
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` пишет `smokeReportPath` в synthetic smoke report и покрывает fail-closed `mismatched-bootstrap-admin-email`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-bootstrap-admin-email`.
- `AdminBootstrapCliScriptTests`: 9/9.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK; readiness/bootstrap/preflight/browser smoke reports связаны по `adminEmail`.
- Backend full suite: 590/590.
- Frontend tests/typecheck/build/audit/console E2E: OK, console E2E 9/9.
- Secret scan, strict UTF-8 without BOM для измененных/новых файлов и `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.210.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-report-self-link`.

### Fixed
- `frontend/e2e/admin-vps-smoke.spec.ts`, `docs/admin-vps-smoke-report.template.json` и `scripts/new-admin-vps-smoke-report.ps1` теперь пишут `smokeReportPath` в browser smoke report.
- `scripts/validate-admin-vps-smoke-report.ps1` требует `smokeReportPath` и сверяет его с фактически проверяемым smoke JSON.
- `frontend/e2e/admin.spec.ts` больше не использует внешний `pay.example.test` в sandbox payment fixture, чтобы console E2E не зависел от DNS.

### Added
- `scripts/test-admin-vps-smoke-report-validator.ps1` покрывает fail-closed `mismatched-smoke-report-path`.

### Verified
- Admin VPS smoke report validator regression: OK, включая `mismatched-smoke-report-path`.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: 40/40.
- Local CLI bootstrap admin smoke на SQLite: OK, `smokeReportPath` связан с фактическим smoke JSON.
- Backend full suite: 590/590; frontend tests: 66/66; console E2E: 9/9.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.209.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-smoke-preflight-self-link`.

### Fixed
- `scripts/admin-vps-smoke-preflight.ps1` теперь пишет `preflightReportPath` в preflight report.
- `scripts/validate-admin-vps-smoke-preflight-report.ps1` требует `preflightReportPath`, а `scripts/validate-admin-vps-smoke-evidence.ps1` сверяет его с фактически проверяемым preflight JSON.

### Added
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-preflight-report-path`.

### Verified
- Admin VPS smoke evidence validator regression: OK, включая `mismatched-preflight-report-path`.
- `AdminVpsSmokeReportTests`: 15/15.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.208.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-smoke-report-self-link`.

### Fixed
- `scripts/admin-vps-bootstrap-smoke.ps1` теперь пишет `bootstrapSmokeReportPath` в итоговый bootstrap smoke report.
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` требует `bootstrapSmokeReportPath`, а `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` сверяет его с фактически проверяемым bootstrap JSON.

### Added
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-bootstrap-smoke-report-path`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-bootstrap-smoke-report-path`.
- `AdminBootstrapCliScriptTests`: 9/9.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.207.0 - 2026-06-22

Release entry: `2026-06-22-admin-vps-bootstrap-readiness-report-link`.

### Fixed
- `scripts/admin-vps-bootstrap-smoke.ps1` теперь пишет `readinessReportPath` в итоговый bootstrap smoke report.
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` требует `readinessReportPath`, а `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` сверяет его с фактически проверяемым readiness JSON.

### Added
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-bootstrap-readiness-report-path`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-bootstrap-readiness-report-path`.
- `AdminBootstrapCliScriptTests`: 9/9.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.206.0 - 2026-06-20

Release entry: `2026-06-20-admin-vps-smoke-admin-email-evidence`.

### Fixed
- `scripts/validate-admin-vps-smoke-evidence.ps1` теперь сверяет `adminEmail` preflight report с `adminEmail` browser smoke report.
- `scripts/validate-admin-vps-smoke-report.ps1` требует `adminEmail` в smoke report.

### Added
- `frontend/e2e/admin-vps-smoke.spec.ts`, `scripts/new-admin-vps-smoke-report.ps1` и `docs/admin-vps-smoke-report.template.json` пишут sanitized `adminEmail` в smoke report.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-admin-email`.

### Verified
- Admin VPS smoke report validator regression: OK.
- Admin VPS smoke evidence validator regression: OK, включая `mismatched-admin-email`.
- Admin VPS bootstrap smoke evidence validator regression: OK.
- `AdminVpsSmokeReportTests|AdminBootstrapCliScriptTests`: 24/24.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.205.0 - 2026-06-20

Release entry: `2026-06-20-admin-vps-bootstrap-smoke-readiness-path-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь сверяет `readinessReportPath` внутри readiness report с фактическим readiness JSON, переданным в validator.

### Added
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-readiness-report-path`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-readiness-report-path`.
- `AdminBootstrapCliScriptTests`: 9/9.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.204.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-report-release-link`.

### Fixed
- `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` теперь сверяет `releaseId` итогового bootstrap smoke report с `releaseId` preflight и smoke reports после успешной проверки smoke evidence.

### Added
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает `mismatched-smoke-release-id`, где preflight/smoke согласованы между собой, но не совпадают с bootstrap report.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-release-id` и `mismatched-smoke-release-id`.
- `AdminBootstrapCliScriptTests`: 9/9.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.203.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-release-id-chain`.

### Changed
- `scripts/admin-vps-bootstrap-smoke.ps1` вычисляет latest release один раз и передает общий `releaseValue` в readiness gate, admin VPS smoke и итоговый bootstrap smoke report.
- `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` теперь сверяет `releaseId` readiness/bootstrap reports и отклоняет mismatched evidence.

### Added
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` покрывает fail-closed `mismatched-release-id`.
- `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` проверяет, что dry-run readiness report получает непустой release id без запуска smoke.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, включая `mismatched-release-id`.
- Admin VPS bootstrap smoke wrapper regression: OK, dry-run readiness содержит непустой release id.
- `AdminBootstrapCliScriptTests`: 9/9.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.202.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-unified-release-id`.

### Changed
- `scripts/admin-vps-smoke.ps1` вычисляет latest release один раз и передает общий `releaseValue` в preflight и browser smoke.
- `scripts/admin-vps-browser-smoke.ps1` получил PowerShell fallback на latest release и печатает выбранный release id без секретов.

### Added
- `scripts/test-admin-vps-smoke-flow-wrapper.ps1` проверяет, что fail-closed preflight reports получают непустой release id до запуска browser smoke.

### Verified
- Admin VPS smoke flow wrapper regression: OK, `missing-password`, `bad-api-url`, `missing-frontend`, все с непустым release id.
- `AdminVpsSmokeReportTests`: 15/15.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.201.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-preflight-release-id`.

### Changed
- `scripts/admin-vps-smoke-preflight.ps1` теперь подставляет latest release из `backend/src/VpnPlatform.Api/AppReleases/releases.json`, если `-ReleaseId` не передан.
- `scripts/validate-admin-vps-smoke-preflight-report.ps1` требует непустой `releaseId`.
- `scripts/validate-admin-vps-smoke-evidence.ps1` fail-closed отклоняет preflight/smoke evidence без release id.

### Added
- `scripts/test-admin-vps-smoke-preflight-validator.ps1` покрывает `empty-release-id`.
- `scripts/test-admin-vps-smoke-evidence-validator.ps1` покрывает `missing-preflight-release-id`.

### Verified
- Admin VPS smoke preflight validator regression: OK, включая `empty-release-id`.
- Admin VPS smoke evidence validator regression: OK, включая `missing-preflight-release-id`.
- `AdminVpsSmokeReportTests`: 15/15.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.200.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-route-regression`.

### Fixed
- `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` снова проходит valid-сценарий после route contract: synthetic smoke report генерирует route в формате `/admin/#<id>`.

### Added
- Regression harness покрывает fail-closed tamper-сценарий `bad-smoke-route`, который доказывает, что bootstrap evidence chain не примет smoke report с route вне `docs/admin-vps-smoke-sections.json`.

### Verified
- Admin VPS bootstrap smoke evidence validator regression: OK, `valid`, `mismatched-admin-url`, `readiness-not-ready`, `bad-timing`, `bad-smoke-route`.
- `AdminBootstrapCliScriptTests|AdminVpsSmokeReportTests`: 24/24.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.199.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-report-route-contract`.

### Changed
- `scripts/validate-admin-vps-smoke-report.ps1` теперь читает `docs/admin-vps-smoke-sections.json` и проверяет, что route каждого раздела smoke report совпадает с sections contract.
- `scripts/validate-admin-vps-smoke-sections-contract.ps1` обновлен под manifest-driven report validator.

### Added
- `scripts/test-admin-vps-smoke-report-validator.ps1` покрывает fail-closed tamper-сценарий `bad-route`.

### Verified
- Admin VPS smoke report validator regression: OK, включая `bad-route`.
- Admin VPS smoke sections contract validator/regression: OK.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted release/docs suite: OK.
- Local CLI bootstrap admin smoke на SQLite: OK, readiness/bootstrap/smoke/preflight reports UTF-8 without BOM, bootstrap smoke report valid, paired evidence validator OK, preflight report valid, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: 590/590.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.198.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-sections-contract`.

### Added
- Добавлен `docs/admin-vps-smoke-sections.json` как единый manifest обязательных admin sections для VPS smoke evidence.
- Добавлен `scripts/validate-admin-vps-smoke-sections-contract.ps1` для сверки manifest, report template, report validator, VPS Playwright smoke и all-screens smoke.
- Добавлен `scripts/test-admin-vps-smoke-sections-contract.ps1` с fail-closed tamper-сценариями `duplicate-section`, `bad-route`, `template-missing-section`, `browser-spec-no-manifest` и `all-screens-missing-section`.

### Changed
- `frontend/e2e/admin-vps-smoke.spec.ts` берет id/route разделов из `docs/admin-vps-smoke-sections.json`, чтобы browser report не расходился с contract.
- `docs/admin-vps-smoke.md` описывает section contract validator и regression harness.

### Verified
- Admin VPS smoke sections contract validator: OK.
- Admin VPS smoke sections contract regression: OK, `6/6` scenarios.
- `AdminVpsSmokeReportTests`: 15/15.
- Targeted docs/admin suite: OK.
- Local CLI bootstrap admin smoke на SQLite: OK, readiness/bootstrap/smoke/preflight reports UTF-8 without BOM, bootstrap smoke report valid, paired evidence validator OK, preflight report valid, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: 590/590.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.197.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-evidence`.

### Added
- Добавлен `scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1` для парной проверки readiness report и итогового bootstrap+smoke report.
- Добавлен `scripts/test-admin-vps-bootstrap-smoke-evidence-validator.ps1` для fail-closed regression сценариев `valid`, `mismatched-admin-url`, `readiness-not-ready` и `bad-timing`.

### Changed
- `scripts/admin-vps-bootstrap-smoke.ps1` после успешного smoke теперь валидирует, что readiness и bootstrap reports относятся к одному запуску.

### Verified
- `AdminBootstrapCliScriptTests`: 9/9.
- Admin VPS bootstrap smoke evidence validator regression: OK.
- Local CLI bootstrap admin smoke на SQLite: OK, readiness report valid, bootstrap smoke report valid, paired evidence validator OK, preflight report valid, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: 589/589.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.196.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-readiness`.

### Added
- Добавлен `scripts/admin-vps-bootstrap-smoke-readiness.ps1` для fail-closed проверки параметров live bootstrap+smoke до reset-а.
- Добавлен `scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1` для проверки sanitized readiness report.
- Добавлен `scripts/test-admin-vps-bootstrap-smoke-readiness.ps1` с regression-сценариями `local-ready`, `missing-password`, `missing-confirm-bootstrap-reset` и `missing-connection-string`.

### Changed
- `scripts/admin-vps-bootstrap-smoke.ps1` теперь запускает readiness gate до `admin-bootstrap.ps1` и пишет `admin-vps-bootstrap-smoke-readiness-report.json` без пароля и connection string.
- `scripts/local-admin-vps-bootstrap-smoke.ps1` теперь доказывает readiness/bootstrap/smoke цепочку на временной SQLite-БД.

### Verified
- `AdminBootstrapCliScriptTests`: 8/8.
- Admin VPS bootstrap smoke readiness regression: OK.
- Local CLI bootstrap admin smoke на SQLite: OK, readiness report valid, bootstrap smoke report valid, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- Backend full suite: 588/588.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.195.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-report`.

### Added
- Добавлен `scripts/validate-admin-vps-bootstrap-smoke-report.ps1` для проверки sanitized bootstrap+smoke report.
- `scripts/admin-vps-bootstrap-smoke.ps1` теперь после успешного smoke пишет `admin-vps-bootstrap-smoke-report.json` без пароля, cookie и auth headers.

### Changed
- `scripts/local-admin-vps-bootstrap-smoke.ps1` теперь проверяет сам bootstrap+smoke wrapper и его sanitized report на временной SQLite-БД.

### Verified
- `AdminBootstrapCliScriptTests`: 7/7.
- Local CLI bootstrap admin smoke на SQLite: OK, bootstrap smoke report valid, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- Backend full suite: 587/587.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.194.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-wrapper-regression`.

### Added
- Добавлен `scripts/test-admin-vps-bootstrap-smoke-wrapper.ps1` для fail-closed regression проверки bootstrap+smoke wrapper.
- Regression покрывает `missing-password`, `missing-confirm-bootstrap-reset`, `missing-connection-string` и `dry-run-no-smoke`.

### Changed
- `docs/admin-bootstrap.md` и `docs/admin-vps-smoke.md` теперь содержат команду regression-проверки bootstrap+smoke wrapper.

### Verified
- `AdminBootstrapCliScriptTests`: 6/6.
- Admin VPS bootstrap smoke wrapper regression: OK, tested scenarios `4/4`.
- Local CLI bootstrap admin smoke на SQLite: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- Backend full suite: 586/586.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.193.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-bootstrap-smoke-wrapper`.

### Added
- Добавлен `scripts/admin-vps-bootstrap-smoke.ps1`, который запускает admin bootstrap/reset и затем admin VPS smoke под тем же аккаунтом.
- Добавлен `scripts/local-admin-vps-bootstrap-smoke.ps1`, который доказывает flow на временной SQLite-БД: CLI bootstrap создает admin, API стартует с `AdminBootstrap__Enabled=false`, затем проходит admin smoke.

### Changed
- `docs/admin-bootstrap.md` и `docs/admin-vps-smoke.md` описывают единый bootstrap+smoke проход без вывода пароля.

### Verified
- `AdminBootstrapCliScriptTests`: 5/5.
- Local CLI bootstrap admin smoke на SQLite: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- Backend full suite: 585/585.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.192.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-evidence-validator`.

### Added
- Добавлен `scripts/validate-admin-vps-smoke-evidence.ps1`, который валидирует preflight report и smoke report вместе и сверяет их связь.
- Добавлен `scripts/test-admin-vps-smoke-evidence-validator.ps1` с fail-closed сценариями для mismatched URL/path/release/timing и failed smoke report.

### Changed
- `scripts/admin-vps-smoke.ps1` теперь после browser smoke запускает парный evidence validator.

### Verified
- `AdminVpsSmokeReportTests`: 14/14.
- Admin VPS smoke evidence validator regression: OK, tested failures `5/5`.
- Local SQLite admin browser smoke через `admin-vps-smoke.ps1`: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`, evidence validator OK.
- Backend full suite: 583/583.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.191.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-flow-wrapper-regression`.

### Added
- Добавлен `scripts/test-admin-vps-smoke-flow-wrapper.ps1`, который проверяет fail-closed поведение `scripts/admin-vps-smoke.ps1` до запуска browser smoke.
- Regression harness покрывает `missing-password`, `bad-api-url` и `missing-frontend`, проверяет отсутствие smoke report после failed preflight и отсутствие пароля в stdout/stderr.

### Verified
- `AdminVpsSmokeReportTests`: 13/13.
- Admin VPS smoke flow wrapper regression: OK, tested failures `3/3`.
- Local SQLite admin browser smoke через `admin-vps-smoke.ps1`: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: 582/582.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.190.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-flow-wrapper`.

### Added
- Добавлен `scripts/admin-vps-smoke.ps1`, единый fail-closed wrapper для admin VPS smoke: сначала preflight с `-RequirePassword`, затем browser smoke с `-RequireAllPassed`.
- `scripts/local-admin-vps-browser-smoke.ps1` теперь проверяет тот же preflight+browser flow на временной SQLite-БД.

### Verified
- `AdminVpsSmokeReportTests`: 12/12.
- Local SQLite admin browser smoke через `admin-vps-smoke.ps1`: OK, preflight report valid, Playwright `1/1`, report validator `16 passed`.
- Backend full suite: 581/581.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.189.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-report-validator-regression`.

### Added
- Добавлен `scripts/test-admin-vps-smoke-report-validator.ps1`, который проверяет happy path основного admin VPS smoke report validator и fail-closed tamper-сценарии.
- Regression harness проверяет `bad-http-status`, `placeholder-evidence`, `failed-status`, `missing-section`, `false-gate` и `secret-marker`.

### Verified
- `AdminVpsSmokeReportTests`: 11/11.
- Admin VPS smoke report validator regression: OK.
- Local SQLite admin browser smoke: OK, 16/16 admin sections passed.
- Backend full suite: 580/580.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.188.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-preflight-validator-regression`.

### Added
- Добавлен `scripts/test-admin-vps-smoke-preflight-validator.ps1`, который проверяет happy path preflight validator и fail-closed tamper-сценарии.
- Regression harness проверяет `bad-ready-flag`, `failed-check`, `missing-check`, `duplicate-check` и `secret-marker`, а также контролирует, что тестовый пароль не попал в JSON.

### Verified
- `AdminVpsSmokeReportTests`: 10/10.
- Admin VPS smoke preflight validator regression: OK.
- Local SQLite admin browser smoke: OK, 16/16 admin sections passed.
- Backend full suite: 579/579.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.187.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-preflight-validator`.

### Added
- Добавлен `scripts/validate-admin-vps-smoke-preflight-report.ps1` для fail-closed проверки sanitized preflight evidence перед реальным admin VPS smoke.
- `scripts/admin-vps-smoke-preflight.ps1` теперь запускает validator preflight-отчета с `-RequireReady` перед разрешением live smoke.

### Verified
- `AdminVpsSmokeReportTests`: 9/9.
- Admin VPS smoke preflight validator: OK на тестовых URL и process env password.
- Local SQLite admin browser smoke: OK, 16/16 admin sections passed.
- Backend full suite: 578/578.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.186.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-preflight`.

### Added
- Добавлен `scripts/admin-vps-smoke-preflight.ps1` для проверки live URL, admin email, password env, frontend runner, npm command и validator перед реальным admin VPS smoke.
- Preflight пишет sanitized JSON `admin-vps-smoke-preflight-report.json` с `readyForLiveSmoke` и `passwordEnvPresent`, но не принимает пароль параметром и не выводит секрет.

### Verified
- `AdminVpsSmokeReportTests`: 8/8.
- Admin VPS smoke preflight: OK на тестовых URL и process env password.
- Local SQLite admin browser smoke: OK, 16/16 admin sections passed.
- Backend full suite: 577/577.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.185.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-smoke-acceptance-evidence`.

### Changed
- `scripts/validate-admin-vps-smoke-report.ps1 -RequireAllPassed` теперь требует успешный `httpStatus` по каждой секции админки.
- Acceptance mode отклоняет placeholder evidence вроде `TODO`, `Not checked yet`, `safe screenshot name` и шаблонных browser smoke notes.

### Verified
- `AdminVpsSmokeReportTests`: 7/7.
- Local SQLite admin browser smoke: OK, 16/16 admin sections passed.
- Backend full suite: 576/576.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.184.0 - 2026-06-19

Release entry: `2026-06-19-local-admin-vps-browser-smoke`.

### Added
- Добавлен `scripts/local-admin-vps-browser-smoke.ps1` для полной локальной проверки admin browser smoke на временной SQLite-БД.
- Локальный harness поднимает API с `AdminBootstrap__Enabled=true`, admin-panel через Vite с `VITE_API_BASE_URL` на временный API, запускает `scripts/admin-vps-browser-smoke.ps1 -RequireAllPassed` и удаляет временные файлы по умолчанию.

### Fixed
- Cleanup локального smoke останавливает дерево процессов, чтобы дочерний Vite `node.exe` не оставался слушать порт после проверки.

### Verified
- `AdminVpsSmokeReportTests`: 6/6.
- Local SQLite admin browser smoke: OK, 16/16 admin sections passed.
- Backend full suite: 575/575.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Secret scan: 0 findings.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.183.0 - 2026-06-19

Release entry: `2026-06-19-admin-vps-browser-smoke`.

### Added
- Добавлен `frontend/e2e/admin-vps-smoke.spec.ts` для явного live-smoke входа в админку и обхода всех обязательных разделов VPS admin UI.
- Добавлен `frontend/playwright.vps-smoke.config.ts` без локального webServer, trace, video и screenshots, чтобы live-прогон не сохранял пароль, cookie или токены в artifacts.
- Добавлен `scripts/admin-vps-browser-smoke.ps1`, который принимает URL/email, берет пароль только из `ADMIN_VPS_SMOKE_ADMIN_PASSWORD`, печатает `Password: [hidden]` и валидирует JSON через `validate-admin-vps-smoke-report.ps1`.

### Verified
- `AdminVpsSmokeReportTests`: 5/5.
- Playwright admin VPS smoke test discovery: OK.
- Backend full suite: 574/574.
- Frontend tests: 66/66.
- Frontend typecheck/build: OK.
- `npm audit --audit-level=high`: 0 vulnerabilities.
- Playwright console E2E: 9/9.
- Changed files encoding: strict UTF-8 without BOM.
- `git diff --check`: OK.
- `P0-ADMIN-001`, `P0-ADMIN-002` и `STATE-013` остаются открытыми до реального VPS bootstrap/login smoke report.

## 0.182.0 - 2026-06-19

Release entry: `2026-06-19-admin-bootstrap-wrapper`.

### Added
- Добавлен `scripts/admin-bootstrap.ps1` для one-shot создания или сброса администратора без запуска HTTP-сервера.
- Добавлен `docs/admin-bootstrap.md` с локальным SQLite и production/Postgres сценариями запуска.
- Добавлены `AdminBootstrapCliScriptTests`, которые закрепляют dry-run, скрытие пароля и связь roadmap/test results/"Что нового".

### Verified
- `AdminBootstrapCliScriptTests`: 3/3.
- Admin bootstrap dry-run: OK, password hidden.
- Local SQLite admin bootstrap/reset: OK.
- Targeted release/docs suite: 23/23.
- Backend full suite: 573/573.

## 0.181.0 - 2026-06-19

Release entry: `2026-06-19-staging-smoke-report-evidence-placeholders`.

### Changed
- `scripts/validate-staging-smoke-report.ps1` в режиме `-RequireAllPassed` теперь отклоняет checks с `TODO` в evidence.
- `docs/staging-smoke-checklist.md` уточняет, что `status = passed` с placeholder evidence не является приемочным staging smoke.
- Roadmap и release docs синхронизированы с backend suite `570/570` и latest release `0.181.0`.

### Verified
- `StagingSmokeChecklistTests`: 8/8.
- Staging smoke report generator/validator smoke: OK.
- Expected fail-closed `-RequireAllPassed` для passed checks с TODO evidence: OK.
- Targeted release/docs suite: 94/94.
- Backend full suite: 570/570.

## 0.180.0 - 2026-06-19

Release entry: `2026-06-19-payment-provider-smoke-report-acceptance-gates`.

### Changed
- `scripts/validate-payment-provider-smoke-report.ps1` при `-RequireAllPassed` теперь требует `true` для всех provider gates: account, checkout, provider confirmation, webhook, subscription и refund.
- `docs/payment-provider-smoke.md` уточняет, что `status = passed` без закрытых boolean gates не является приемочным evidence.
- Roadmap и release docs синхронизированы с backend suite `569/569` и latest release `0.180.0`.

### Verified
- `PaymentProviderSmokeReportTests`: 6/6.
- Payment provider smoke report generator/validator smoke: OK.
- Expected fail-closed `-RequireAllPassed`: OK.
- Targeted release/docs suite: 86/86.
- Backend full suite: 569/569.

## 0.179.0 - 2026-06-19

Release entry: `2026-06-19-vps-production-smoke-report-contract`.

### Added
- Добавлен `docs/vps-production-smoke-report.template.json` для безопасной фиксации live/staging VPS production smoke.
- Добавлены `scripts/new-vps-production-smoke-report.ps1` и `scripts/validate-vps-production-smoke-report.ps1`.

### Changed
- VPS production smoke теперь имеет fail-closed report contract: `-RequireAllPassed` требует полный deploy -> health -> admin login -> order -> payment -> subscription -> VPN access flow.
- Roadmap и release docs синхронизированы с backend suite `568/568` и latest release `0.179.0`.

### Verified
- `VpsProductionSmokeTests`: 7/7.
- VPS production smoke report generator/validator smoke: OK.
- Targeted release/docs suite: 80/80.
- Backend full suite: 568/568.

## 0.178.0 - 2026-06-19

Release entry: `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-ci-step-guards-regression`.

### Added
- Aggregate fail-closed validator теперь проверяет tamper-сценарии `missing-aggregate-ci-step-guard-command` и `missing-aggregate-ci-step-validator`.

### Changed
- `scripts/test-production-ci-workflow-artifacts-guards-validator.ps1` покрывает CI-step guard command/regression step вместе с readiness/evidence artifact contracts.
- Roadmap и release docs синхронизированы с backend suite `564/564` и latest release `0.178.0`.

### Verified
- Production CI workflow artifacts aggregate guard validator: OK, включая CI-step tamper cases.
- `ProductionReadinessGateTests`: 57/57.
- Targeted release/docs suite: 73/73.
- Backend full suite: 564/564.

## 0.177.0 - 2026-06-19

Release entry: `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-ci-step-guards`.

### Added
- Aggregate production workflow artifacts guard теперь запускает `test-production-ci-workflow-artifacts-guards-ci-step.ps1` и `test-production-ci-workflow-artifacts-guards-ci-step-validator.ps1`.

### Changed
- `scripts/test-production-ci-workflow-artifacts-guards.ps1` покрывает 6 guards вместо 4, включая CI wiring guard и его fail-closed regression.
- Roadmap и release docs синхронизированы с backend suite `563/563` и latest release `0.177.0`.

### Verified
- Production CI workflow artifacts guards aggregate: OK, `guardsCount = 6`.
- `ProductionReadinessGateTests`: 56/56.
- Targeted release/docs suite: 72/72.
- Backend full suite: 563/563.

## 0.176.0 - 2026-06-19

Release entry: `2026-06-19-production-ci-workflow-artifacts-guards-ci-step-regression`.

### Added
- Добавлен `scripts/test-production-ci-workflow-artifacts-guards-ci-step-validator.ps1` для fail-closed regression проверки aggregate CI step guard.

### Changed
- Backend CI запускает `Guard production CI workflow artifacts guard steps regression` после CI-step guard и до aggregate guard.
- Roadmap и release docs синхронизированы с backend suite `562/562` и latest release `0.176.0`.

### Verified
- Production CI workflow artifacts aggregate CI step guard validator: OK.
- `ProductionReadinessGateTests`: 55/55.
- Targeted release/docs suite: 71/71.
- Backend full suite: 562/562.

## 0.175.0 - 2026-06-19

Release entry: `2026-06-19-production-ci-workflow-artifacts-guards-ci-step-guard`.

### Added
- Добавлен `scripts/test-production-ci-workflow-artifacts-guards-ci-step.ps1` для проверки GitHub Actions wiring aggregate production workflow artifacts steps.

### Changed
- Backend CI запускает `Guard production CI workflow artifacts guard steps` после checkout, до aggregate guard, aggregate validator и .NET setup.
- Roadmap и release docs синхронизированы с backend suite `561/561` и latest release `0.175.0`.

### Verified
- Production CI workflow artifacts aggregate CI step guard: OK.
- `ProductionReadinessGateTests`: 54/54.
- Targeted release/docs suite: 70/70.
- Backend full suite: 561/561.

## 0.174.0 - 2026-06-19

Release entry: `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-regression-ci-step`.

### Added

- GitHub Actions backend job запускает `scripts/test-production-ci-workflow-artifacts-guards-validator.ps1 -WriteJson` отдельным step до backend setup/build/test.

### Changed

- Production readiness gate docs описывают порядок aggregate guard -> aggregate validator -> backend setup.
- Roadmap и release docs синхронизированы с backend suite `560/560` и latest release `0.174.0`.

### Verified

- `ProductionReadinessGateTests`: 53/53.
- Production CI workflow artifacts aggregate validator CI step guard: OK.
- Targeted release/docs suite: 69/69.
- Backend full suite: 560/560.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- CI теперь запускает fail-closed aggregate validator сразу после aggregate guard, поэтому broken workflow artifact contract должен падать до тяжелых backend-команд.

## 0.173.0 - 2026-06-19

Release entry: `2026-06-19-production-ci-workflow-artifacts-guards-aggregate-regression`.

### Added

- `scripts/test-production-ci-workflow-artifacts-guards-validator.ps1` проверяет fail-closed поведение aggregate production CI workflow artifacts guard.

### Changed

- Production readiness gate docs описывают aggregate guard regression для tampered `.github/workflows/ci.yml`.
- Roadmap и release docs синхронизированы с backend suite `559/559` и latest release `0.173.0`.

### Verified

- `ProductionReadinessGateTests`: 52/52.
- Production CI workflow artifacts aggregate guard validator smoke: OK.
- Targeted release/docs suite: 68/68.
- Backend full suite: 559/559.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет `missing-readiness-guard-step`, `missing-readiness-assertion-log-artifact`, `missing-production-evidence-result-artifact` и `missing-if-no-files-found-error`.

## 0.172.0 - 2026-06-19

Release entry: `2026-06-19-production-ci-workflow-artifacts-guards-aggregate`.

### Added

- `scripts/test-production-ci-workflow-artifacts-guards.ps1` запускает оба production workflow artifacts guards и оба fail-closed validators одной командой.

### Changed

- GitHub Actions backend job запускает aggregate step `Guard production CI workflow artifacts contracts` сразу после checkout.
- Roadmap и release docs синхронизированы с backend suite `558/558` и latest release `0.172.0`.

### Verified

- `ProductionReadinessGateTests`: 51/51.
- Production CI workflow artifacts guards aggregate smoke: OK.
- Targeted release/docs suite: 67/67.
- Backend full suite: 558/558.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Aggregate guard проверяет readiness assertion и production evidence published artifacts contracts вместе с tamper regression harnesses.

## 0.171.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-workflow-artifacts-guard-regression`.

### Added

- `scripts/test-production-readiness-assertion-ci-workflow-artifacts-validator.ps1` проверяет fail-closed поведение readiness assertion workflow artifacts guard.

### Changed

- Production readiness gate docs описывают readiness workflow guard regression для tampered `.github/workflows/ci.yml`.
- Roadmap и release docs синхронизированы с backend suite `557/557` и latest release `0.171.0`.

### Verified

- `ProductionReadinessGateTests`: 50/50.
- Production readiness assertion CI workflow artifacts guard validator smoke: OK.
- Targeted release/docs suite: 66/66.
- Backend full suite: 557/557.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет `missing-guard-step`, `missing-assertion-log-artifact`, `bad-artifact-name` и `missing-if-no-files-found-error`.

## 0.170.0 - 2026-06-19

Release entry: `2026-06-19-production-evidence-ci-workflow-artifacts-guard-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-workflow-artifacts-validator.ps1` проверяет fail-closed поведение production evidence workflow artifacts guard.

### Changed

- Production readiness gate docs описывают workflow guard regression для tampered `.github/workflows/ci.yml`.
- Roadmap и release docs синхронизированы с backend suite `556/556` и latest release `0.170.0`.

### Verified

- `ProductionReadinessGateTests`: 49/49.
- Production evidence CI workflow artifacts guard validator smoke: OK.
- Targeted release/docs suite: 65/65.
- Backend full suite: 556/556.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет `missing-guard-step`, `missing-result-json-artifact`, `bad-artifact-name` и `missing-if-no-files-found-error`.

## 0.169.0 - 2026-06-19

Release entry: `2026-06-19-production-evidence-ci-workflow-artifacts-guard`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1` проверяет published artifacts contract для GitHub Actions job `production-evidence`.

### Changed

- GitHub Actions job `production-evidence` запускает workflow artifacts guard до archive regression wrapper.
- Roadmap и release docs синхронизированы с backend suite `555/555` и latest release `0.169.0`.

### Verified

- `ProductionReadinessGateTests`: 48/48.
- Production evidence CI workflow artifacts guard smoke: OK.
- Targeted release/docs suite: 64/64.
- Backend full suite: 555/555.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- CI теперь fail-closed проверяет, что `production-evidence` публикует JSON/Markdown result artifacts handoff archive regression.

## 0.168.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-workflow-guard-step`.

### Added

- GitHub Actions job `production-readiness-assertion` запускает `scripts/test-production-readiness-assertion-ci-workflow-artifacts.ps1 -WriteJson` до readiness assertion wrapper.

### Changed

- `ProductionReadinessGateTests` закрепляет порядок CI шагов: workflow artifacts guard, readiness assertion wrapper, artifacts upload.
- Roadmap и release docs синхронизированы с backend suite `554/554` и latest release `0.168.0`.

### Verified

- `ProductionReadinessGateTests`: 47/47.
- Production readiness assertion CI workflow artifacts guard smoke: OK.
- Targeted release/docs suite: 63/63.
- Backend full suite: 554/554.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- CI теперь fail-closed проверяет contract published artifacts до запуска wrapper и upload step.

## 0.167.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-workflow-artifacts`.

### Added

- `scripts/test-production-readiness-assertion-ci-workflow-artifacts.ps1` проверяет, что CI workflow публикует полный artifact-директорий readiness assertion.

### Changed

- Production readiness gate docs описывают workflow guard для `.github/workflows/ci.yml`.
- Roadmap и release docs синхронизированы с backend suite `553/553` и latest release `0.167.0`.

### Verified

- `ProductionReadinessGateTests`: 46/46.
- Production readiness assertion CI workflow artifacts guard smoke: OK.
- Targeted release/docs suite: 62/62.
- Backend full suite: 553/553.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Guard проверяет job `production-readiness-assertion`, `needs: backend`, wrapper command, artifact name, `if-no-files-found: error` и пять обязательных published files.

## 0.166.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-summary-artifacts-regression`.

### Added

- `scripts/test-production-readiness-assertion-ci-summary-validator.ps1` теперь проверяет tamper-сценарий `bad-ci-artifacts-validator-regression`.

### Changed

- `scripts/test-production-readiness-assertion-ci-regression.ps1` запускает artifacts validator regression до summary validator regression, чтобы summary harness проверял строку `CI artifacts validator regression`.
- `scripts/validate-production-readiness-assertion-ci-regression-result.ps1` требует failure-case `bad-ci-artifacts-validator-regression` внутри `ciSummaryValidatorRegression`.
- Roadmap и release docs синхронизированы с backend suite `552/552` и latest release `0.166.0`.

### Verified

- `ProductionReadinessGateTests`: 45/45.
- Production readiness assertion CI summary artifacts regression smoke: OK.
- Targeted release/docs suite: 61/61.
- Backend full suite: 552/552.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Новый regression-case защищает GitHub Step Summary от потери строки `CI artifacts validator regression`.

## 0.165.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-artifacts-validator-regression`.

### Added

- `scripts/test-production-readiness-assertion-ci-artifacts-validator.ps1` проверяет fail-closed поведение validator всего readiness assertion CI artifact-директория.

### Changed

- `scripts/test-production-readiness-assertion-ci-regression.ps1` запускает artifacts validator regression автоматически и записывает `ciArtifactsValidatorRegression` в итоговый JSON/Markdown.
- CI result и summary validators теперь проверяют `ciArtifactsValidatorRegression`, если этот блок присутствует в result artifact.
- Roadmap и release docs синхронизированы с backend suite `551/551` и latest release `0.165.0`.

### Verified

- `ProductionReadinessGateTests`: 44/44.
- Production readiness assertion CI artifacts validator regression smoke: OK.
- Targeted release/docs suite: 60/60.
- Backend full suite: 551/551.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет `missing-required-artifact`, `bad-output-directory`, `bad-assertion-log-path`, `bad-result-markdown` и `bad-step-summary`.

## 0.164.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-artifacts-validator`.

### Added

- `scripts/validate-production-readiness-assertion-ci-artifacts.ps1` проверяет весь artifact-директорий readiness assertion CI одной командой.

### Changed

- `scripts/test-production-readiness-assertion-ci-regression.ps1` запускает artifact-directory validator перед выводом результата.
- Roadmap и release docs синхронизированы с backend suite `550/550` и latest release `0.164.0`.

### Verified

- `ProductionReadinessGateTests`: 43/43.
- Production readiness assertion CI artifacts validator smoke: OK.
- Targeted release/docs suite: 59/59.
- Backend full suite: 550/550.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Validator проверяет пять обязательных файлов, согласованность путей в result JSON, result validator, summary validator и optional `StepSummaryPath`.

## 0.163.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-step-summary-smoke`.

### Added

- `scripts/test-production-readiness-assertion-ci-step-summary.ps1` проверяет реальный файл `GITHUB_STEP_SUMMARY` для readiness assertion CI wrapper.

### Changed

- Roadmap и release docs синхронизированы с backend suite `549/549` и latest release `0.163.0`.

### Verified

- `ProductionReadinessGateTests`: 42/42.
- Production readiness assertion CI step summary smoke: OK.
- Targeted release/docs suite: 58/58.
- Backend full suite: 549/549.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Smoke выставляет `GITHUB_STEP_SUMMARY`, запускает CI wrapper, валидирует summary, сверяет его с result Markdown и проверяет строки summary/result validator regression.

## 0.162.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-summary-validator`.

### Added

- `scripts/validate-production-readiness-assertion-ci-summary.ps1` проверяет GitHub Step Summary readiness assertion CI wrapper.
- `scripts/test-production-readiness-assertion-ci-summary-validator.ps1` проверяет fail-closed поведение summary validator.

### Changed

- `scripts/test-production-readiness-assertion-ci-regression.ps1` валидирует result Markdown как summary, запускает summary validator regression, записывает `ciSummaryValidatorRegression` и проверяет реальный `GITHUB_STEP_SUMMARY`, если он доступен.
- Roadmap и release docs синхронизированы с backend suite `548/548` и latest release `0.162.0`.

### Verified

- `ProductionReadinessGateTests`: 41/41.
- Production readiness assertion CI summary validator regression smoke: OK.
- Targeted release/docs suite: 57/57.
- Backend full suite: 548/548.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Summary validator сверяет status, assertion status, result validator, regression statuses и artifact paths, чтобы GitHub Actions summary не расходился с опубликованным CI artifact.

## 0.161.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-result-validator-regression`.

### Added

- `scripts/test-production-readiness-assertion-ci-regression-result-validator.ps1` проверяет fail-closed поведение validator итогового production readiness assertion CI result.

### Changed

- `scripts/test-production-readiness-assertion-ci-regression.ps1` запускает новый harness автоматически, сохраняет `ciResultValidatorRegression` в JSON/Markdown result и повторно валидирует итоговый artifact.
- `scripts/validate-production-readiness-assertion-ci-regression-result.ps1` проверяет `ciResultValidatorRegression`, если секция уже присутствует в result.
- Roadmap и release docs синхронизированы с backend suite `547/547` и latest release `0.161.0`.

### Verified

- `ProductionReadinessGateTests`: 40/40.
- Production readiness assertion CI result validator regression smoke: OK.
- Targeted release/docs suite: 56/56.
- Backend full suite: 547/547.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Harness покрывает bad status, неверный assertion exit code, пропавший regression failure, сломанный Markdown и неверный `evidenceReportsCount`.

## 0.160.0 - 2026-06-19

Release entry: `2026-06-19-production-readiness-assertion-ci-result-validator`.

### Added

- `scripts/validate-production-readiness-assertion-ci-regression-result.ps1` проверяет итоговый JSON/Markdown artifact production readiness assertion CI regression.

### Changed

- `scripts/test-production-readiness-assertion-ci-regression.ps1` запускает validator после записи result JSON/Markdown.
- Production readiness gate документация описывает отдельную проверку скачанного CI regression result artifact.
- Roadmap и release docs синхронизированы с backend suite `546/546` и latest release `0.160.0`.

### Verified

- `ProductionReadinessGateTests`: 39/39.
- Production readiness assertion CI result validator smoke: OK.
- Targeted release/docs suite: 55/55.
- Backend full suite: 546/546.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Validator сверяет status, assertion exit code, linked assertion JSON/Markdown/log, result validator, validator regression failure-сценарии и Markdown-пару.

## 0.159.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-assertion-ci-regression`.

### Added

- `scripts/test-production-readiness-assertion-ci-regression.ps1` запускает production readiness assertion, result validator и validator regression в одном CI-friendly flow.
- `.github/workflows/ci.yml` получил job `production-readiness-assertion` после backend job.

### Changed

- GitHub Actions validation публикует artifact `production-readiness-assertion-ci-regression` с assertion JSON/Markdown/log и итоговым CI regression result.
- Production readiness gate документация описывает локальный запуск wrapper и artifact в CI.
- Roadmap и release docs синхронизированы с backend suite `545/545` и latest release `0.159.0`.

### Verified

- `ProductionReadinessGateTests`: 38/38.
- Production readiness assertion CI regression smoke: OK.
- Targeted release/docs suite: 54/54.
- Backend full suite: 545/545.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Wrapper добавляет Markdown-итог в `GITHUB_STEP_SUMMARY`, если переменная доступна, и не закрывает live-блокеры без реального VPS/payment/3x-ui evidence.

## 0.158.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-assertion-result-validator-regression`.

### Added

- `scripts/test-production-readiness-assertion-result-validator.ps1` проверяет fail-closed поведение standalone validator production readiness assertion result artifacts.

### Changed

- Production readiness gate документация получила команду regression-проверки assertion result validator.
- Roadmap и release docs синхронизированы с backend suite `544/544` и latest release `0.158.0`.

### Verified

- `ProductionReadinessGateTests`: 37/37.
- Production readiness assertion result validator regression smoke: OK.
- Targeted release/docs suite: 53/53.
- Backend full suite: 544/544.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Harness портит только временные копии JSON/Markdown artifacts и ожидает ошибки для bad status, неверного `failedEvidenceReportsCount`, missing `vpn-live`, сломанного Markdown и `-RequireProductionReady` на blocked result.

## 0.157.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-assertion-result-validator`.

### Added

- `scripts/validate-production-readiness-assertion-result.ps1` проверяет JSON/Markdown result artifacts production readiness assertion без повторного запуска gate.

### Changed

- `scripts/assert-production-readiness.ps1` теперь запускает validator сразу после записи result artifacts и до fail-closed ошибки.
- Production readiness gate документация описывает отдельную проверку скачанного assertion result artifact.
- Roadmap и release docs синхронизированы с backend suite `543/543` и latest release `0.157.0`.

### Verified

- `ProductionReadinessGateTests`: 36/36.
- Blocked production readiness assertion artifact smoke with standalone validator: OK.
- Targeted release/docs suite: 52/52.
- Backend full suite: 543/543.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Validator проверяет статус, четыре evidence report entries, счетчики, пути reports, roadmap/release decision и Markdown-пару.

## 0.156.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-assertion-result-artifacts`.

### Added

- `scripts/assert-production-readiness.ps1` получил `-OutputPath`, `-JsonOutputPath` и `-Force` для сохранения JSON/Markdown result artifacts.

### Changed

- Production readiness gate теперь пишет result artifacts даже при ожидаемом `blocked`, а затем продолжает fail-closed падать.
- Result JSON/Markdown фиксирует `failedEvidenceReportsCount`, `blockersCount`, пути всех evidence reports, `evidenceReports`, `blockers`, `resultJsonPath` и `resultMarkdownPath`.
- Roadmap и release docs синхронизированы с backend suite `542/542` и latest release `0.156.0`.

### Verified

- `ProductionReadinessGateTests`: 35/35.
- Blocked production readiness assertion artifact smoke: OK.
- Targeted release/docs suite: 51/51.
- Backend full suite: 542/542.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Artifacts не делают проект production-ready: live/VPS/payment/3x-ui blockers остаются открытыми до реальных passed evidence reports.

## 0.155.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-result-validator-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-regression-result-validator.ps1` проверяет fail-closed поведение standalone CI result validator на испорченных JSON/Markdown artifacts.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь запускает CI result validator regression, сохраняет `ciResultValidatorRegression` в итоговом JSON/Markdown и повторно валидирует финальный result artifact.
- Production readiness gate документация описывает отдельный regression harness для CI result validator.
- Roadmap и release docs синхронизированы с backend suite `541/541` и latest release `0.155.0`.

### Verified

- `ProductionReadinessGateTests`: 34/34.
- Production evidence handoff package archive CI wrapper smoke with result validator regression: OK.
- Standalone CI result validator regression: OK.
- Backend full suite: 541/541.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет ошибки для неверного общего статуса, пустого `releaseId`, отсутствующего failure-сценария summary validator и сломанного Markdown.

## 0.154.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-result-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package-archive-ci-regression-result.ps1` проверяет итоговый CI regression JSON/Markdown artifact.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь запускает result validator после финальной записи artifacts.
- Production readiness gate документация описывает отдельную проверку скачанного CI result artifact.
- Roadmap и release docs синхронизированы с backend suite `540/540` и latest release `0.154.0`.

### Verified

- `ProductionReadinessGateTests`: 33/33.
- Production evidence handoff package archive CI wrapper smoke with result validator: OK.
- Backend full suite: 540/540.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Validator проверяет все вложенные statuses, `ciSummaryValidatorRegression`, Markdown-пару и обязательные artifact paths.

## 0.153.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-summary-validator.ps1` проверяет fail-closed поведение CI summary validator на испорченных JSON/Markdown artifacts.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь запускает summary validator regression и сохраняет `ciSummaryValidatorRegression` в result artifacts.
- Production readiness gate документация описывает regression harness для CI summary validator.
- Roadmap и release docs синхронизированы с backend suite `539/539` и latest release `0.153.0`.

### Verified

- `ProductionReadinessGateTests`: 32/32.
- Production evidence handoff package archive CI wrapper smoke with summary validator regression: OK.
- Backend full suite: 539/539.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет неверный main flow status, чужой release id в summary, отсутствующий artifact path и неверный long-path status.

## 0.152.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package-archive-ci-summary.ps1` проверяет CI summary Markdown против JSON result artifact.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь запускает validator для result Markdown и для `GITHUB_STEP_SUMMARY`, если summary-файл доступен.
- Production readiness gate документация описывает fail-closed проверку summary.
- Roadmap и release docs синхронизированы с backend suite `538/538` и latest release `0.152.0`.

### Verified

- `ProductionReadinessGateTests`: 31/31.
- Production evidence handoff package archive CI wrapper smoke with summary validator: OK.
- Backend full suite: 538/538.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Summary validator проверяет статусы, release id и пути artifacts; live/VPS/payment blockers остаются внешними smoke-задачами.

## 0.151.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-summary`.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` теперь дописывает Markdown-результат в `GITHUB_STEP_SUMMARY`, если wrapper запускается в GitHub Actions.
- Production readiness gate документация описывает GitHub Actions job summary и локальную проверку через временный summary-файл.
- Roadmap и release docs синхронизированы с backend suite `537/537` и latest release `0.151.0`.

### Verified

- `ProductionReadinessGateTests`: 30/30.
- Production evidence handoff package archive CI regression wrapper smoke with `GITHUB_STEP_SUMMARY`: OK.
- Backend full suite: 537/537.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Job summary ускоряет диагностику CI evidence gate, но финальные JSON/Markdown artifacts остаются основным handoff-доказательством.

## 0.150.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-workflow`.

### Added

- В `.github/workflows/ci.yml` добавлен job `production-evidence`, который запускает production evidence handoff package archive CI regression после backend-проверок.
- GitHub Actions публикует artifact `production-evidence-handoff-package-archive-ci-regression` с JSON/Markdown результатами wrapper.

### Changed

- Production readiness gate документация теперь описывает, где в GitHub Actions брать CI evidence artifacts.
- Roadmap и release docs синхронизированы с backend suite `536/536` и latest release `0.150.0`.

### Verified

- `ProductionReadinessGateTests`: 29/29.
- Production evidence handoff package archive CI regression wrapper smoke: OK.
- Backend full suite: 536/536.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- CI job проверяет локальные evidence regressions в validation pipeline, но live/VPS/payment blockers все еще закрываются только реальными smoke reports.

## 0.149.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-ci-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1` объединяет локальные archive flow regression harnesses в один CI-friendly запуск.
- Wrapper сохраняет `production-evidence-handoff-package-archive-ci-regression-result.json` и `.md`.

### Changed

- Production readiness gate документация теперь описывает единый CI wrapper для основного flow, result validator regression и long-path regression.
- Roadmap и release docs синхронизированы с backend suite `535/535` и latest release `0.149.0`.

### Verified

- `ProductionReadinessGateTests`: 28/28.
- Production evidence handoff package archive CI regression wrapper smoke: OK.
- Backend full suite: 535/535.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- CI wrapper остается локальным evidence regression gate; live/VPS/payment blockers закрываются только реальными smoke reports.

## 0.148.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-long-path-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-long-path.ps1` запускает полный handoff flow в длинной production-evidence директории.
- Long-path harness проверяет hash-based имя handoff package ZIP и сохранение полного `releaseId` в result JSON.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку Windows path-limit regression.
- Roadmap и release docs синхронизированы с backend suite `534/534` и latest release `0.148.0`.

### Verified

- `ProductionReadinessGateTests`: 27/27.
- Production evidence handoff package archive long path regression smoke: OK.
- Backend full suite: 534/534.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Проверка не закрывает live/VPS/payment blockers; она защищает локальный и CI evidence flow от Windows path-limit на длинных release id.

## 0.147.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow-result-validator-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-flow-result-validator.ps1` проверяет fail-closed поведение validator результата полного handoff flow.
- Regression harness ожидает ошибки для испорченного `status`, неверного SHA256 handoff archive, отсутствующего tamper-сценария и Markdown без блока `Tested failures`.

### Changed

- Production readiness gate документация теперь описывает отдельный regression harness для result validator.
- Default-имя handoff package ZIP теперь использует короткий hash release id, чтобы длинные release id не ломали сборку на Windows path-limit.
- Roadmap и release docs синхронизированы с backend suite `533/533` и latest release `0.147.0`.

### Verified

- `ProductionReadinessGateTests`: 26/26.
- Production evidence handoff package archive flow result validator regression smoke: OK.
- Backend full suite: 533/533.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет только целостность локального evidence result validator; live/VPS/payment blockers остаются открытыми до реальных smoke reports.

## 0.146.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow-result-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package-archive-flow-result.ps1` проверяет JSON/Markdown итог полного production evidence handoff flow.
- Flow автоматически запускает result validator после записи `production-evidence-handoff-package-archive-flow-result.json` и `.md`.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку result artifacts полного flow.
- Roadmap и release docs синхронизированы с backend suite `532/532` и latest release `0.146.0`.

### Verified

- `ProductionReadinessGateTests`: 25/25.
- Production evidence handoff package archive flow result validator smoke: OK.
- Backend full suite: 532/532.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Result validator не закрывает внешние live-блокеры; он проверяет локальный evidence flow outcome и целостность ссылок на handoff artifacts.

## 0.145.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow-result`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-flow.ps1` сохраняет `production-evidence-handoff-package-archive-flow-result.json` и `.md`.
- Result artifacts фиксируют release id, package status, SHA256 production evidence archive, SHA256 handoff package archive, пути artifacts и tamper-сценарии regression harness.

### Changed

- Production readiness gate документация теперь описывает result artifacts полного flow.
- Roadmap и release docs синхронизированы с backend suite `531/531` и latest release `0.145.0`.

### Verified

- `ProductionReadinessGateTests`: 24/24.
- Production evidence handoff package archive flow result artifacts smoke: OK.
- Backend full suite: 531/531.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Result artifacts не заменяют live/VPS/payment evidence reports; они фиксируют локальный flow outcome и SHA256 handoff artifacts.

## 0.144.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow-safety`.

### Changed

- `scripts/test-production-evidence-handoff-package-archive-flow.ps1` теперь проверяет output directory перед рекурсивной очисткой.
- Flow запрещает корень файловой системы, корень репозитория и папку без явного `production-evidence` в имени.

### Added

- Production readiness gate документация описывает безопасный шаблон output directory для локальных и CI evidence-проверок.
- Roadmap получил пункт `P11-ACC-029` для защиты `-Force` в end-to-end flow.

### Verified

- `ProductionReadinessGateTests`: 23/23.
- Guarded production evidence handoff package archive flow smoke: OK.
- Backend full suite: 530/530.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Safety guard не меняет формат evidence artifacts; он защищает только выбор директории для перезаписи при `-Force`.

## 0.143.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-flow`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-flow.ps1` собирает весь локальный production evidence handoff flow одной командой.
- Flow создает evidence bundle, summary, manifest, production evidence ZIP, handoff receipt, checklist, package, финальный ZIP и запускает archive validator regression.

### Changed

- Production readiness gate документация теперь предлагает одну команду для полной локальной evidence handoff проверки.
- Roadmap и release docs синхронизированы с backend suite `529/529` и latest release `0.143.0`.

### Verified

- `ProductionReadinessGateTests`: 22/22.
- Production evidence handoff package archive end-to-end flow smoke: OK.
- Backend full suite: 529/529.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Flow harness проверяет локально собранный blocked/staging evidence; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.142.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-validator-regression`.

### Added

- `scripts/test-production-evidence-handoff-package-archive-validator.ps1` запускает happy path и tamper-сценарии для финального ZIP-архива handoff package.
- Regression harness создает временные испорченные копии архива и проверяет ошибки для неверного SHA256, лишнего entry и отсутствующего `SHA256SUMS.txt`.

### Changed

- Production readiness gate документация теперь описывает regression-проверку archive validator.
- Roadmap и release docs синхронизированы с backend suite `528/528` и latest release `0.142.0`.

### Verified

- `ProductionReadinessGateTests`: 21/21.
- Production evidence handoff package archive validator regression smoke: OK.
- Backend full suite: 528/528.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Regression harness проверяет локально собранный ZIP; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.141.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package-archive.ps1` проверяет финальный ZIP-архив handoff package.
- Валидатор сверяет SHA256 внешнего ZIP, запрещает неожиданные и вложенные entries, временно извлекает package и повторно запускает package validator.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку финального ZIP-архива handoff package.
- Roadmap и release docs синхронизированы с backend suite `527/527` и latest release `0.141.0`.

### Verified

- `ProductionReadinessGateTests`: 20/20.
- Production evidence handoff package archive validator smoke: OK.
- Backend full suite: 527/527.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Archive validator проверяет локально собранный ZIP; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.140.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-archive`.

### Added

- `scripts/new-production-evidence-handoff-package-archive.ps1` упаковывает проверенный handoff package в единый ZIP.
- Package archive generator повторно запускает package validator, добавляет только разрешенные package files и возвращает SHA256/размер архива.

### Changed

- Production readiness gate документация теперь описывает финальную упаковку handoff package в ZIP.
- Roadmap и release docs синхронизированы с backend suite `526/526` и latest release `0.140.0`.

### Verified

- `ProductionReadinessGateTests`: 19/19.
- Production evidence handoff package archive smoke: OK.
- Backend full suite: 526/526.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Archive generator упаковывает локальный проверенный package; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.139.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package-validator`.

### Added

- `scripts/validate-production-evidence-handoff-package.ps1` проверяет готовый каталог handoff package после сборки.
- Валидатор проверяет whitelist файлов, `production-evidence-handoff-package-index.json`, `SHA256SUMS.txt`, SHA256 каждого artifact и повторно запускает checklist validator.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку handoff package.
- Roadmap и release docs синхронизированы с backend suite `525/525` и latest release `0.139.0`.

### Verified

- `ProductionReadinessGateTests`: 18/18.
- Production evidence handoff package validator smoke: OK.
- Backend full suite: 525/525.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Package validator доказывает целостность локального handoff package; production-ready по-прежнему требует реальные live/VPS/payment evidence reports.

## 0.138.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-package`.

### Added

- `scripts/new-production-evidence-handoff-package.ps1` собирает минимальный handoff package после проверки checklist.
- Package содержит production evidence ZIP, JSON/Markdown receipt, JSON/Markdown checklist, `production-evidence-handoff-package-index.json`, `.md` и `SHA256SUMS.txt`.

### Changed

- Production readiness gate документация теперь описывает финальный package step после checklist validator.
- Roadmap и release docs синхронизированы с backend suite `524/524` и latest release `0.138.0`.

### Verified

- `ProductionReadinessGateTests`: 17/17.
- Production evidence handoff package smoke: OK.
- Backend full suite: 524/524.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Package generator копирует только проверенные artifacts и не должен использоваться как замена реальному live/VPS/payment evidence.

## 0.137.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-checklist-validator`.

### Added

- `scripts/validate-production-evidence-handoff-checklist.ps1` проверяет JSON/Markdown checklist после генерации handoff artifact.
- Валидатор повторно запускает receipt validator и сверяет release id, SHA256 архива, SHA256 manifest, gates, operator actions и Markdown-пару checklist.

### Changed

- Production readiness gate документация теперь описывает отдельную проверку checklist и строгий режим `-RequireProductionReady`.
- Roadmap и release docs синхронизированы с backend suite `523/523` и latest release `0.137.0`.

### Verified

- `ProductionReadinessGateTests`: 16/16.
- Production evidence handoff checklist validator smoke: OK.
- Backend full suite: 523/523.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Validator проверяет локальный handoff artifact. Production-ready по-прежнему требует реальные live payment/VPS/3x-ui evidence reports.

## 0.136.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-checklist`.

### Added

- `scripts/new-production-evidence-handoff-checklist.ps1` формирует JSON/Markdown checklist для передачи production evidence после проверки receipt.
- Checklist запускает `validate-production-evidence-handoff-receipt.ps1`, читает `production-readiness-summary.json` и фиксирует gates, статус handoff, release id, SHA256 архива и SHA256 manifest.

### Changed

- Production readiness gate документация теперь описывает финальный operator handoff step после ZIP, receipt и receipt validation.
- Roadmap и release docs синхронизированы с backend suite `522/522` и latest release `0.136.0`.

### Verified

- `ProductionReadinessGateTests`: 15/15.
- Production evidence handoff checklist smoke: OK.
- Backend full suite: 522/522.
- Frontend tests/typecheck/audit/build/console E2E: OK.
- Fresh local SQLite smoke and local VPS smoke dry-run: OK.

### Notes

- Checklist не закрывает внешние production-блокеры сам по себе: для production-ready по-прежнему нужны live payment/VPS/3x-ui evidence reports.

## 0.135.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-receipt-validator`.

### Added

- `scripts/validate-production-evidence-handoff-receipt.ps1` проверяет JSON/Markdown receipt против ZIP-архива production evidence.
- Валидатор повторно запускает archive validator и сверяет release id, SHA256 архива, SHA256 manifest, размер архива, entries и verified files.
- Markdown-пара receipt проверяется на ключевые hash-данные, чтобы handoff artifact был проверяем без ручного сравнения.

### Verified

- `ProductionReadinessGateTests`: 14/14.
- Production evidence handoff receipt validator smoke: OK.
- Backend full suite: 521/521.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Receipt validation доказывает целостность локального handoff artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.134.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-handoff-receipt`.

### Added

- `scripts/new-production-evidence-handoff-receipt.ps1` создает JSON/Markdown receipt для проверенного ZIP-архива production evidence.
- Receipt запускает archive validator, затем фиксирует release id, SHA256 архива, SHA256 manifest, размер архива, entries и verified files.
- Receipt не копирует содержимое evidence reports и подходит для передачи вместе с ZIP в CI или операторский handoff.

### Verified

- `ProductionReadinessGateTests`: 13/13.
- Production evidence handoff receipt smoke: OK, JSON/Markdown receipt создан после archive validation.
- Backend full suite: 520/520.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Receipt фиксирует локальный handoff artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.133.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-archive-validator`.

### Added

- `scripts/validate-production-evidence-archive.ps1` проверяет ZIP-архив production evidence bundle перед публикацией или передачей оператору.
- Валидатор читает `production-evidence-manifest.json` из архива, запрещает лишние entries и сверяет обязательные файлы.
- Для каждого entry проверяются размер, `totalBytes`, SHA256 и безопасный relative path; для CI добавлен `-ExpectedArchiveSha256`.

### Verified

- `ProductionReadinessGateTests`: 12/12.
- Production evidence archive validator smoke: OK, ZIP проверен по manifest и expected archive SHA256.
- Backend full suite: 519/519.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Archive validation доказывает целостность локального ZIP artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.132.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-archive`.

### Added

- `scripts/new-production-evidence-archive.ps1` собирает ZIP-архив production evidence bundle после успешной проверки manifest.
- Архиватор добавляет в ZIP сам `production-evidence-manifest.json` и только файлы, перечисленные в manifest.
- Результат содержит SHA256 архива, SHA256 manifest, release id, размер архива и список entries.

### Verified

- `ProductionReadinessGateTests`: 11/11.
- Production evidence archive smoke: OK, ZIP создан после manifest validation.
- Backend full suite: 518/518.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Архив фиксирует локальный handoff artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.131.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-manifest-validator`.

### Added

- `scripts/validate-production-evidence-manifest.ps1` проверяет `production-evidence-manifest.json` перед handoff или CI-публикацией.
- Валидатор перечитывает manifest, проверяет schema, release id, обязательные файлы, relative paths, размеры, timestamps, total files и total bytes.
- Для каждого файла bundle пересчитывается SHA256, чтобы поймать изменение evidence artifact после генерации manifest.

### Verified

- `ProductionReadinessGateTests`: 10/10.
- Production evidence manifest validator smoke: OK, manifest проверен с `-RequireAllFiles`.
- Backend full suite: 517/517.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Manifest validation доказывает целостность локального handoff bundle; production-ready по-прежнему требует реальные passed evidence reports и закрытие live/VPS/payment blockers.

## 0.130.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-manifest`.

### Added

- `scripts/new-production-evidence-manifest.ps1` создает безопасный manifest для handoff production evidence bundle.
- Manifest валидирует bundle перед созданием, затем записывает `production-evidence-manifest.json` с release id, relative paths, SHA256, размерами файлов и UTC timestamps.
- Manifest фиксирует состав evidence bundle без копирования содержимого отчетов и без сохранения секретов.

### Verified

- `ProductionReadinessGateTests`: 9/9.
- Production evidence manifest smoke: OK, 6 файлов с SHA256.
- Backend full suite: 516/516.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Manifest фиксирует локальный handoff artifact; production-ready по-прежнему требует реальные passed evidence reports и закрытые live/VPS/payment blockers.

## 0.129.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-bundle-validator`.

### Added

- `scripts/validate-production-evidence-bundle.ps1` проверяет весь каталог production evidence bundle одной командой.
- Bundle validator запускает validators для staging/VPS, payment providers, admin VPS, VPN live и опционального production readiness summary.
- Добавлены режимы `-RequireSummary`, `-RequireReportFiles` через summary validator и `-RequireProductionReady` для строгого production handoff.

### Verified

- `ProductionReadinessGateTests`: 8/8.
- Production evidence bundle validator smoke: OK.
- Backend full suite: 515/515.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- `-RequireProductionReady` ожидаемо падает до реальных passed evidence reports и закрытия live/VPS/payment blockers.

## 0.128.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-summary-validator`.

### Added

- `scripts/validate-production-readiness-summary.ps1` проверяет Markdown/JSON summary перед handoff оператору или CI.
- Валидатор требует четыре reports (`staging-vps`, `payment-providers`, `admin-vps`, `vpn-live`), корректные статусы, счетчики checks, required flags, report paths и roadmap blockers.
- Добавлены режимы `-RequireReportFiles` и `-RequireProductionReady` для строгой проверки артефактов и финального production handoff.

### Verified

- `ProductionReadinessGateTests`: 7/7.
- Production readiness summary validator smoke: OK, status `blocked` для generated drafts.
- Backend full suite: 514/514.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- `-RequireProductionReady` пройдет только после реальных passed evidence reports и закрытия live/VPS/payment roadmap blockers.

## 0.127.0 - 2026-06-18

Release entry: `2026-06-18-production-readiness-summary`.

### Added

- `scripts/new-production-readiness-summary.ps1` создает Markdown и JSON summary по полному production evidence bundle.
- Summary показывает статус staging/VPS, payment providers, admin VPS и VPN live reports, количество passed/blocked/failed checks и required flags.
- Summary отдельно выводит все платежные провайдеры и открытые roadmap blockers, чтобы оператор видел, почему production-ready еще заблокирован.

### Verified

- `ProductionReadinessGateTests`: 6/6.
- Production readiness summary smoke: OK, status `blocked` для generated drafts.
- Backend full suite: 513/513.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Summary является локальным operator artifact. Production-ready по-прежнему требует реальные sanitized evidence после live/staging прогонов VPS, 3x-ui и платежных провайдеров.

## 0.126.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-bundle-generator`.

### Added

- `scripts/new-production-evidence-bundle.ps1` создает весь production evidence bundle одной командой: staging/VPS, payment provider, admin VPS и VPN live reports.
- Генератор вызывает существующие безопасные генераторы отчетов, прогоняет их validators и при `-RunProductionGate` возвращает статус агрегированного production gate.
- Документация `docs/production-readiness-gate.md` получила команду создания полного bundle без ручного копирования JSON.

### Verified

- `ProductionReadinessGateTests`: 5/5.
- Bundle generator smoke: OK, созданы 4 JSON-отчета.
- Expected aggregate gate status for generated drafts: `blocked`.
- Backend full suite: 512/512.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Generated reports остаются черновиками `blocked`, пока оператор не заменит TODO на реальные sanitized evidence после live/staging прогонов.

## 0.125.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-aggregate-gate`.

### Changed

- `scripts/assert-production-readiness.ps1` теперь запускает все четыре evidence validators независимо и не останавливается на первой ошибке.
- Fail-closed payload `Production readiness blocked` содержит массив `evidenceReports` с `name`, `status`, `reportPath`, `validatorPath` и `message` по staging/VPS, payment providers, admin VPS и VPN live reports.
- Roadmap/release blockers продолжают попадать в тот же payload, поэтому оператор видит одновременно недостающие отчеты и незакрытые production blockers.
- Current status обновлен до backend `511/511`, latest release `2026-06-18-production-evidence-aggregate-gate`.

### Verified

- `ProductionReadinessGateTests`: 4/4.
- `assert-production-readiness.ps1` на blocked templates возвращает агрегированный fail-closed payload с `evidenceReports`.
- Backend full suite: 511/511.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Production-ready все еще требует реальных passed reports по VPS/staging, платежам, админке VPS и live VPN/3x-ui.

## 0.124.0 - 2026-06-18

Release entry: `2026-06-18-production-evidence-bundle-gate`.

### Changed

- `scripts/assert-production-readiness.ps1` теперь требует полный production evidence bundle: staging/VPS smoke report, payment provider smoke report, admin VPS smoke report и VPN live smoke report.
- Gate принимает `PaymentProviderReportPath`, `AdminVpsReportPath` и `VpnLiveReportPath`; если пути не переданы, используются стандартные шаблоны из `docs/`.
- Blocking/summary payload теперь показывает пути всех evidence reports, чтобы было понятно, какой отчет не готов.
- `frontend/package-lock.json` обновлен через `npm audit fix`, текущий frontend audit возвращает `0 vulnerabilities`.
- `frontend/scripts/playwright-webservers.mjs` больше не зависит от локального `apps/*/node_modules/vite` и корректно запускает E2E при hoisted workspace-зависимостях.
- Current status обновлен до backend `510/510`, latest release `2026-06-18-production-evidence-bundle-gate`.

### Verified

- `ProductionReadinessGateTests`: 3/3.
- `assert-production-readiness.ps1` остается fail-closed на текущих blocked templates.
- Backend full suite: 510/510.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Frontend audit: `0 vulnerabilities`.
- Encoding guard and secret scan: OK.

### Remaining

- Production-ready все еще требует реальных passed reports по VPS/staging, платежам, админке VPS и live VPN/3x-ui.

## 0.123.0 - 2026-06-14

Release entry: `2026-06-14-vpn-live-smoke-report`.

### Added

- `docs/vpn-live-smoke-report.template.json` фиксирует обязательный smoke-отчет для production-like VPN выдачи через реальную 3x-ui/x-ui панель.
- `scripts/new-vpn-live-smoke-report.ps1` создает безопасный blocked-черновик отчета с latest release, API URL, admin URL, 3x-ui URL и оператором.
- `scripts/validate-vpn-live-smoke-report.ps1` проверяет URL, даты, top-level VPN gates, обязательные checks и forbidden secret markers, включая полные VPN URI.
- `docs/vpn-live-smoke.md` описывает, как пройти 3x-ui/inbound/node/order/webhook/subscription/client/fail-closed smoke без сохранения секретов.

### Changed

- Current status обновлен до backend `509/509`, latest release `2026-06-14-vpn-live-smoke-report`.
- `P0-VPN-001` ... `P0-VPN-005` остаются открытыми до реальной 3x-ui проверки, но теперь у них есть обязательный формат safe evidence.

### Verified

- Generated VPN live smoke report passes normal validation.
- Generated blocked report fails `-RequireAllPassed` as expected.
- `VpnLiveSmokeReportTests`: 4/4.
- Backend full suite: 509/509.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Нужно пройти реальную 3x-ui/x-ui панель, inbound, production VPN node и production-like order smoke, затем заполнить отчет safe evidence.

## 0.122.0 - 2026-06-14

Release entry: `2026-06-14-admin-vps-smoke-report`.

### Added

- `docs/admin-vps-smoke-report.template.json` фиксирует обязательный smoke-отчет для проверки всех разделов админки на VPS.
- `scripts/new-admin-vps-smoke-report.ps1` создает безопасный blocked-черновик отчета с latest release, API URL, admin URL и оператором.
- `scripts/validate-admin-vps-smoke-report.ps1` проверяет URL, даты, общие login/console/API gates, все admin sections и forbidden secret markers.
- `docs/admin-vps-smoke.md` описывает, как пройти VPS admin smoke без сохранения секретов.

### Changed

- Current status обновлен до backend `505/505`, latest release `2026-06-14-admin-vps-smoke-report`.
- `P0-ADMIN-002` остается открытым до реального VPS admin smoke, но теперь у него есть обязательный формат безопасного evidence.

### Verified

- Generated admin VPS smoke report passes normal validation.
- Generated blocked report fails `-RequireAllPassed` as expected.
- `AdminVpsSmokeReportTests`: 4/4.
- Backend full suite: 505/505.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Нужно пройти `/admin/` на реальном VPS под production admin-аккаунтом и заполнить отчет safe evidence по всем разделам.

## 0.121.0 - 2026-06-14

Release entry: `2026-06-14-payment-provider-smoke-generator`.

### Added

- `scripts/new-payment-provider-smoke-report.ps1` создает безопасный черновик payment provider smoke report из `docs/payment-provider-smoke-report.template.json`.
- Генератор принимает `EnvironmentName`, `Operator`, `ReleaseId`, `Mode` (`sandbox` или `live`) и подставляет latest release из seed "Что нового", если `ReleaseId` не задан.
- Все провайдеры создаются со статусом `blocked`, пустыми gate-флагами и TODO evidence, поэтому real provider smoke остается fail-closed до внешней проверки.

### Changed

- `docs/payment-provider-smoke.md` теперь рекомендует начинать отчет через генератор, а не ручное копирование JSON.
- Current status обновлен до backend `501/501`, latest release `2026-06-14-payment-provider-smoke-generator`.

### Verified

- Generated payment provider smoke report passes normal validation.
- Generated blocked report fails `-RequireAllPassed` as expected.
- `PaymentProviderSmokeReportTests`: 5/5.
- Backend full suite: 501/501.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Реальные provider smoke reports для YooKassa, RoboKassa, YooMoney, CloudPayments, TBank, Prodamus, Stripe и PayPal еще нужно заполнить после внешних sandbox/live проверок.

## 0.120.0 - 2026-06-14

Release entry: `2026-06-14-payment-provider-smoke-report`.

### Added

- `docs/payment-provider-smoke-report.template.json` фиксирует обязательную smoke-матрицу для YooKassa, RoboKassa, YooMoney, CloudPayments, TBankAcquiring, Prodamus, Stripe и PayPal.
- `scripts/validate-payment-provider-smoke-report.ps1` проверяет структуру отчета, даты, дубли провайдеров, обязательные payment gates, безопасные evidence и forbidden secret markers.
- `docs/payment-provider-smoke.md` описывает, как заполнять provider smoke report и почему Telegram Stars проверяется отдельным Telegram invoice flow.
- `PaymentProviderSmokeReportTests` закрепляет fail-closed шаблон и связь отчета с roadmap.

### Changed

- Current status обновлен до backend `500/500`, latest release `2026-06-14-payment-provider-smoke-report`.
- `STATE-011` и `P0-PAY-002` ... `P0-PAY-009` остаются открытыми до реального sandbox/live отчета по внешним кабинетам.

### Verified

- Payment provider smoke report validator: OK.
- `-RequireAllPassed` для blocked шаблона: expected failure.
- `PaymentProviderSmokeReportTests`: 4/4.
- Backend full suite: 500/500.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Реальные YooKassa, RoboKassa, YooMoney, CloudPayments, TBank, Prodamus, Stripe и PayPal кабинеты еще нужно пройти и приложить safe evidence без секретов.

## 0.119.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-report-generator`.

### Added

- `scripts/new-staging-smoke-report.ps1` создает безопасный черновик staging/VPS smoke report из `docs/staging-smoke-report.template.json`.
- Генератор принимает `ApiBaseUrl`, web URL-ы, `EnvironmentName`, `Operator`, `ReleaseId` и подставляет latest release из seed "Что нового", если `ReleaseId` не задан.
- Все обязательные checks создаются со статусом `blocked` и TODO evidence, поэтому production readiness gate остается fail-closed до реального прогона.

### Changed

- `docs/staging-smoke-checklist.md` теперь рекомендует начинать заполнение отчета через генератор, а не ручное копирование JSON.
- Current status обновлен до backend `496/496`, latest release `2026-06-14-staging-smoke-report-generator`.

### Verified

- `StagingSmokeChecklistTests`: 7/7.
- Generated staging smoke report passes normal validation.
- Generated blocked report fails `-RequireAllPassed` as expected.
- Backend full suite: 496/496.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Реальный staging/VPS smoke report пока не заполнен; live-платежи, 3x-ui и production-ready решение остаются внешними блокерами.

## 0.118.0 - 2026-06-14

Release entry: `2026-06-14-telegram-stars-invoice-gate`.

### Changed

- Telegram Stars теперь считается готовым для bot checkout только при явном `ExtraSettingsJson.status = "invoice-flow"`.
- Режим `bot-only` остается безопасным состоянием: Stars скрыт из web checkout и не появляется в Telegram-клавиатуре оплаты как готовый способ.
- Проверка подключения платежного провайдера в админке показывает Stars как `Unhealthy` для `bot-only` и `Healthy` для явного `invoice-flow`.
- Production-настройка Telegram Stars больше не требует web secret key, потому что Stars работает через Telegram invoice update flow.
- Current status обновлен до backend `495/495`, latest release `2026-06-14-telegram-stars-invoice-gate`.

### Verified

- Targeted payment/Telegram suite: 61/61.
- Backend full suite: 495/495.
- Fresh local SQLite smoke: OK.
- Local SQLite VPS smoke dry-run: OK.
- Frontend tests/typecheck/build/E2E console: OK.
- Encoding guard and secret scan: OK.

### Remaining

- Live BotFather/Telegram Stars smoke с реальным BotToken и Telegram окружением остается внешним production-блокером вместе с live-платежами и VPS/3x-ui проверками.

## 0.117.0 - 2026-06-14

Release entry: `2026-06-14-telegram-webhook-boundary`.

### Добавлено

- Guard-тесты `TelegramBotProcessBoundaryTests`, которые запрещают возвращать `/telegram/webhook` в standalone bot-процесс и проверяют документацию по основному API webhook.
- Roadmap-пункт `P1-TG-006` для явной границы ответственности между основным API и standalone Telegram bot process.

### Обновлено

- `VpnPlatform.TelegramBot` больше не мапит webhook route и остается для LongPolling, очереди Telegram-уведомлений и health endpoints.
- `docs/phase-3-telegram-foundation.md`, `docs/telegram-bot-setup.md`, README и production example указывают webhook на `/api/channels/telegram/webhook` основного API.
- Current status обновлен до backend `493/493`, latest release `2026-06-14-telegram-webhook-boundary`.

### Проверено

- Targeted Telegram boundary/API suite: 41/41.
- Standalone TelegramBot build: OK, предупреждений 0.
- Backend full suite: 493/493.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Реальный Telegram/BotFather webhook и Telegram Stars live/sandbox smoke остаются внешними production-блокерами `STATE-011`, `P11-ACC-002` и `P9-TST-007`.

## 0.116.0 - 2026-06-14

Release entry: `2026-06-14-api-telegram-webhook`.

### Добавлено

- API endpoint `/api/channels/telegram/webhook` теперь обрабатывает Telegram updates в основном backend вместо `501 NotImplemented`.
- Runtime-настройки Telegram-бота читаются из админки/БД с fallback на `appsettings`, включая protected BotToken и webhook secret.
- Infrastructure получил `TelegramBotHttpClient`, который отправляет Telegram Stars invoice, отвечает на `pre_checkout_query` и отправляет сообщения через общий `ITelegramInvoiceProvider`.
- Guard-тесты `ChannelWebhooksControllerTests` проверяют успешную обработку webhook, duplicate update и выключенный Telegram-бот.

### Обновлено

- Current status обновлен до backend `491/491`, latest release `2026-06-14-api-telegram-webhook`.
- Roadmap получил закрытый пункт `P1-TG-005` для Telegram webhook в основном API.

### Проверено

- Targeted Telegram/API suite: 39/39.
- Backend full suite: 491/491.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Live Telegram webhook с реальным BotFather/Bot API и реальные Telegram Stars платежи остаются частью production/staging smoke: `STATE-011`, `P11-ACC-002` и `P9-TST-007`; live smoke не закрывался.

## 0.115.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-report-url-validation`.

### Добавлено

- Guard-проверка в `StagingSmokeChecklistTests`, которая закрепляет обязательные absolute http/https URL для `apiBaseUrl`, `publicWebUrl`, `cabinetWebUrl` и `adminWebUrl`.
- Roadmap-подпункт `P9-TST-007C` для локально закрытого URL validation слоя.

### Обновлено

- `scripts/validate-staging-smoke-report.ps1` теперь отклоняет пустой или невалидный `apiBaseUrl`, а также непустые web URL без абсолютной `http`/`https` схемы.
- `docs/staging-smoke-checklist.md` описывает URL-правила для staging smoke report.
- Current status обновлен до backend `489/489`, latest release `2026-06-14-staging-smoke-report-url-validation`.

### Проверено

- Backend full suite: 489/489.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- `P9-TST-007` остается `[~]`: URL validation закрыт локально, но реальный staging/VPS smoke report все еще нужен.

## 0.114.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-report-consistency`.

### Добавлено

- Guard-проверка в `StagingSmokeChecklistTests`, которая закрепляет запрет на `completedAt` раньше `startedAt` и duplicate check id в staging smoke report.
- Roadmap-подпункт `P9-TST-007B` для локально закрытого consistency guard.

### Обновлено

- `scripts/validate-staging-smoke-report.ps1` теперь проверяет хронологию `startedAt`/`completedAt` и не принимает повторяющиеся check id.
- `docs/staging-smoke-checklist.md` описывает эти правила как обязательную часть report validation.
- Current status обновлен до backend `488/488`, latest release `2026-06-14-staging-smoke-report-consistency`.

### Проверено

- Backend full suite: 488/488.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- `P9-TST-007` остается `[~]`: consistency guard закрыт локально, но реальный staging/VPS smoke report все еще нужен.

## 0.113.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-secret-sanitizer`.

### Добавлено

- Guard-проверка в `StagingSmokeChecklistTests`, которая закрепляет forbidden-маркеры для cookies, `.env`, client secrets, API keys, private headers, Telegram secret header и GitHub/VPS secret names.
- Roadmap-подпункт `P9-TST-007A` для локально закрытой части staging smoke report sanitizer.

### Обновлено

- `scripts/validate-staging-smoke-report.ps1` теперь дополнительно блокирует `Cookie:`, `Set-Cookie:`, `.env`, `client_secret`, `api_key`, `private header`, `X-Telegram-Bot-Api-Secret-Token`, `PRODUCTION_ENV_FILE` и `VPS_SSH_KEY`.
- `docs/staging-smoke-checklist.md` и `docs/production-readiness-gate.md` уточняют, какие данные нельзя сохранять в smoke-отчет.
- Current status обновлен до backend `487/487`, latest release `2026-06-14-staging-smoke-secret-sanitizer`.

### Проверено

- Backend full suite: 487/487.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- `P9-TST-007` остается `[~]`: sanitizer закрыт локально, но реальный staging/VPS smoke report все еще нужен.

## 0.112.0 - 2026-06-14

Release entry: `2026-06-14-production-readiness-gate`.

### Добавлено

- Fail-closed gate `scripts/assert-production-readiness.ps1`, который проверяет staging/VPS smoke report через `validate-staging-smoke-report.ps1 -RequireAllPassed` и дополнительно блокирует production-ready при открытых P0/P11/STATE blockers в roadmap или текущем решении `staging-ready baseline`.
- Документ `docs/production-readiness-gate.md` с инструкцией запуска и объяснением, почему текущий baseline должен падать до реального smoke-отчета.
- Guard-тест `ProductionReadinessGateTests`, который закрепляет наличие скрипта, документации, roadmap-пункта `P11-ACC-008`, release seed и TEST_RESULTS.

### Обновлено

- README, финальный runbook, release decision, docs index и master roadmap синхронизированы с latest release `0.112.0`.
- Current status обновлен до backend `486/486`, latest release `2026-06-14-production-readiness-gate`.

### Проверено

- Backend full suite: 486/486.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.
- `assert-production-readiness.ps1` на текущем шаблоне ожидаемо падает fail-closed, потому что smoke checks еще `blocked`.

### Ограничения

- Gate не закрывает live-платежи, реальный 3x-ui и VPS admin/live smoke; он только запрещает пометить проект production-ready без их доказательств.

## 0.111.0 - 2026-06-14

Release entry: `2026-06-14-product-admin-roadmap-sync`.

### Добавлено

- Guard-тест `ProductAdminUiRoadmapSyncTests`, который проверяет актуальность продуктового UI-roadmap и отсутствие старых незакрытых чекбоксов по уже покрытым локальным UX/API/E2E задачам.

### Обновлено

- `docs/product-admin-ui-roadmap.md` переписан как компактный актуальный продуктовый срез: локальный сайт, кабинет, админка, UX, API и smoke-проверки закрыты, а live-платежи, реальный 3x-ui и VPS smoke оставлены открытыми.
- Current status обновлен до backend `484/484`, latest release `0.111.0`.

### Проверено

- Backend full suite: 484/484.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Product/UI roadmap не закрывает production-ready: P0/live-задачи по VPS, платежам и 3x-ui остаются в master roadmap.

## 0.110.0 - 2026-06-14

Release entry: `2026-06-14-provisioning-secret-bug-sync`.

### Добавлено

- Guard-тест `ProvisioningSecretStatusConsistencyTests`, который связывает `BUG-006`, security-документацию, `ProvisioningSecretMaterializer` и открытые live smoke блокеры.

### Исправлено

- `BUG-006` больше не числится открытым из-за secret materialization: protected `ssh_key` временно материализуется только через `ProvisioningSecretMaterializer`, runner получает path, а файл удаляется в `finally`.
- `docs/SECURITY_HARDENING_MVP.md` больше не содержит устаревшую формулировку, что protected SSH credentials невозможно передать в Ansible.

### Проверено

- Backend full suite: 483/483.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Полный live provisioning smoke, VPS production smoke, реальные 3x-ui и платежные кабинеты остаются открытыми P0/P11-блокерами.

## 0.109.0 - 2026-06-14

Release entry: `2026-06-14-roadmap-bug-register-sync`.

### Добавлено

- Guard-тест `BugRegisterConsistencyTests`, который проверяет, что локально закрытые баги в журнале ошибок не остаются в статусе `open`.

### Исправлено

- `BUG-004` в roadmap больше не числится открытым: полный browser E2E public/cabinet/admin уже закрыт через `P9-TST-008`, all-screens и console smoke.
- `BUG-005` в roadmap больше не числится открытым: синхронизация документации и проверка кодировки закрыты через `P10-DOC-005`, `STATE-014` и guard-тесты.

### Проверено

- Backend full suite: 482/482.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- `BUG-001`, `BUG-002`, `BUG-003`, `BUG-006` и P0/live-задачи остаются открытыми до реального VPS, платежных кабинетов, 3x-ui и provisioning smoke.

## 0.108.0 - 2026-06-14

Release entry: `2026-06-14-roadmap-current-state-sync`.

### Добавлено

- Guard-тест `RoadmapCurrentStateTests`, который закрепляет актуальный верхний статус roadmap и связь с README, release decision, final runbook, TEST_RESULTS, changelog и seed "Что нового".
- Запись "Что нового" `2026-06-14-roadmap-current-state-sync`.

### Обновлено

- Верхний блок `docs/PRODUCT_COMPLETION_ROADMAP.md` синхронизирован с текущими проверками: backend `480/480`, frontend `65/65`, browser console smoke `9/9`, latest release `0.108.0`.
- README, `docs/final-runbook.md` и `docs/release-decision.md` теперь показывают один и тот же latest release.

### Проверено

- Backend full suite: 480/480.
- Frontend unit tests: 65/65.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Encoding guard: OK.
- Secret scan: OK.

### Ограничения

- Production-ready статус все еще заблокирован live-платежами, реальной 3x-ui выдачей, VPS admin/live smoke и заполненным staging/VPS smoke report.

## 0.107.0 - 2026-06-14

Release entry: `2026-06-14-all-screens-browser-smoke`.

### Добавлено

- `frontend/e2e/all-screens.spec.ts` с mock-based browser smoke для всех основных экранов public web, кабинета и админки.
- Playwright project `all-screens`.
- npm-скрипт `e2e:all-screens`.
- Документация `docs/all-screens-browser-smoke.md`.
- Guard-тест `AllScreensBrowserSmokeTests`.

### Проверяется

- public routes `/`, `/tariffs`, `/faq`, `/help`, `/account`;
- cabinet auth screen и авторизованный dashboard;
- admin sections `dashboard`, `users`, `payments`, `tariffs`, `subscriptions`, `vpn`, `nodes`, `panels`, `support`, `audit`, `bot`, `releases`, `faq`, `content`, `scenarios`, `provisioning`;
- отсутствие пустого body;
- отсутствие `console.error` и `pageerror`.

### Проверено

- Backend full suite: 478/478.
- `npm run e2e:all-screens --prefix frontend`: 3/3.
- Browser console smoke: 9/9.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.

### Ограничения

- Smoke использует mock API и не подтверждает live-платежи, live 3x-ui или реальный VPS.

## 0.106.0 - 2026-06-14

Release entry: `2026-06-14-staging-smoke-checklist`.

### Добавлено

- `docs/staging-smoke-checklist.md` с обязательным staging smoke checklist для покупки, оплаты, подписки, VPN-доступа, админки, поддержки и отсутствия browser console errors.
- `docs/staging-smoke-report.template.json` как безопасный шаблон отчета без секретов.
- `scripts/validate-staging-smoke-report.ps1` для структурной проверки отчета и fail-closed release gate через `-RequireAllPassed`.
- Guard-тест `StagingSmokeChecklistTests`.

### Проверяется

- обязательные check id для deploy, health, public/cabinet/admin web, admin login, tariffs, payment providers, checkout, payment init, provider confirmation, subscription, VPN access, support, console, secret rotation и no secret leak;
- запрет на пароли, bearer-токены, private keys и webhook secrets в отчете;
- связка docs index, changelog, TEST_RESULTS и seed "Что нового".

### Проверено

- Backend full suite: 476/476.
- Staging smoke report validator: OK.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Browser console smoke: 6/6.

### Ограничения

- Реальный staging/VPS smoke report еще должен быть заполнен после deploy и настройки внешних sandbox-интеграций.
- Production-ready статус остается заблокированным до live evidence по платежам, 3x-ui и VPS.

## 0.105.0 - 2026-06-14

### Добавлено

- `scripts/vps-production-smoke.ps1` для полного HTTP-smoke против VPS или staging API.
- Документация `docs/vps-production-smoke.md`.
- Guard-тест `VpsProductionSmokeTests`.

### Проверяется

- health live/ready;
- optional public/cabinet/admin web URLs;
- optional admin login и dashboard;
- public checkout session;
- user registration;
- order claim;
- payment init;
- sandbox webhook только в non-Production;
- active subscription;
- VPN access URI.

### Ограничения

- Live VPS smoke должен запускаться отдельно после deploy и ротации раскрытых секретов.
- `-AllowSandboxWebhook` запрещен, если API сообщает `Production`.

### Проверено

- Backend full suite: 473/473.
- Local SQLite VPS smoke dry-run: OK.
- Fresh local SQLite smoke: OK.
- Browser console smoke: 6/6.

## 0.104.0 - 2026-06-14

### Добавлено

- Release decision `docs/release-decision.md`.
- Guard-тест `ReleaseDecisionTests`, который закрепляет статус `staging-ready baseline` и блокеры production-ready.
- Release entry `2026-06-14-release-decision` для раздела "Что нового".

### Решение

- Текущий статус: **staging-ready baseline, не production-ready**.
- Причина: `P11-ACC-002 VPS production smoke` остается открытым, а production требует live VPS smoke, ротации раскрытых секретов, реального домена/HTTPS, provider-specific sandbox smoke и реальной 3x-ui проверки.

### Проверено

- Backend full suite: 470/470.
- Fresh local SQLite smoke: OK.
- Browser console smoke: 6/6.
- Frontend unit tests: 65/65.
- Frontend typecheck/build: OK.
- High-severity frontend audit: OK; остаются 2 moderate advisory по `react-router`.

## 0.103.0 - 2026-06-14

### Добавлено

- Финальный runbook `docs/final-runbook.md`: локальный запуск без Docker, полный validation gate, browser smoke, security gate, deploy на VPS и post-deploy smoke.
- Guard-тест `FinalDocsChangelogTests`, который связывает README, docs index, roadmap, changelog, `TEST_RESULTS.md` и seed "Что нового".
- Release entry `2026-06-14-final-docs-changelog` для админского раздела "Что нового".

### Проверено

- Backend full suite: 467/467.
- Fresh local SQLite smoke: OK.
- Browser console smoke: 6/6.
- Frontend unit tests: 65/65.
- Frontend typecheck/build: OK.
- API Release build: OK.
- High-severity frontend audit: OK; остаются 2 moderate advisory по `react-router`.
- UTF-8/encoding guard: OK.

### Ограничения

- Проект находится на уровне локально подтвержденного staging-ready baseline.
- Production-ready решение требует отдельного live VPS smoke, ротации раскрытых секретов, production домена/HTTPS, реальных sandbox-кабинетов платежных провайдеров и проверки 3x-ui панели.

## 0.102.0 - 2026-06-14

### Добавлено

- Финальный security checklist `docs/security-final-checklist.md`.
- Guard `SecurityFinalChecklistTests` для admin auth policies, headers, rate limits, secrets и webhook/security gates.

### Исправлено

- `scan-secrets.ps1` и `scan-secrets.sh` исключают generated Playwright artifacts, чтобы E2E-прогоны не ломали actual secret scan исчезающими временными файлами.

## 0.101.0 - 2026-06-14

### Добавлено

- Browser console smoke `npm run e2e:console --prefix frontend`.
- Проверка desktop/mobile public, cabinet и admin поверхностей на отсутствие `console.error` и `pageerror`.

## 0.100.0 - 2026-06-14

### Добавлено

- Mobile smoke для public, cabinet и admin.
- Mobile Playwright-проекты и PNG-артефакты для основных экранов.

## 0.99.0 - 2026-06-13

### Добавлено

- Fresh local smoke на чистой SQLite-БД: health, seed, sandbox payment, webhook, subscription и VPN access.

### Исправлено

- SQLite-сортировка `/api/me/orders` по `DateTimeOffset` перенесена после materialize.
