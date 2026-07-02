using System.Text.Json;
using Xunit;

namespace VpnPlatform.UnitTests;

public class ReleaseDocumentationGuardTests
{
    private static readonly ReleaseNoteExpectation[] Expectations =
    [
        new("P2-ADM-CNT-001", "2026-06-11-admin-home-content-readiness"),
        new("P2-ADM-FAQ-001", "2026-06-11-admin-faq-management"),
        new("P2-ADM-REL-001", "2026-06-11-app-version-management"),
        new("P2-ADM-REL-002", "2026-06-11-release-note-guard"),
        new("P7-PROV-005", "2026-06-12-live-provisioning-runbook"),
        new("P8-CI-001", "2026-06-12-deploy-mode-auto-detect"),
        new("P8-CI-002", "2026-06-12-required-checks-main"),
        new("P8-CI-003", "2026-06-13-github-secrets-audit"),
        new("P8-CI-004", "2026-06-13-vps-maintenance-safe-cleanup"),
        new("P8-CI-005", "2026-06-13-post-deploy-smoke"),
        new("P8-CI-006", "2026-06-24-deploy-production-env-normalizer"),
        new("P8-CI-007", "2026-07-02-production-env-normalizer-cleanup"),
        new("P8-CI-008", "2026-07-02-docker-validation-tmp-cleanup"),
        new("P8-CI-009", "2026-07-02-deploy-vps-docker-tmp-cleanup"),
        new("P8-CI-010", "2026-07-02-ci-ansible-tmp-cleanup"),
        new("P8-CI-011", "2026-07-02-validate-repo-ansible-tmp-cleanup"),
        new("P8-CI-012", "2026-07-02-provision-node-wrapper-cleanup"),
        new("P9-TST-001", "2026-06-13-backend-validation-gate"),
        new("P9-TST-002", "2026-06-13-frontend-validation-gate"),
        new("P9-TST-003", "2026-06-13-playwright-public-e2e"),
        new("P9-TST-004", "2026-06-13-playwright-cabinet-e2e"),
        new("P9-TST-005", "2026-06-13-playwright-admin-e2e"),
        new("P9-TST-006", "2026-06-13-payment-provider-contract-tests"),
        new("P9-TST-007A", "2026-06-14-staging-smoke-secret-sanitizer"),
        new("P9-TST-007B", "2026-06-14-staging-smoke-report-consistency"),
        new("P9-TST-007C", "2026-06-14-staging-smoke-report-url-validation"),
        new("P9-TST-007D", "2026-06-14-staging-smoke-report-generator"),
        new("P9-TST-007E", "2026-06-19-staging-smoke-report-evidence-placeholders"),
        new("P9-TST-007F", "2026-07-01-staging-smoke-latest-release-guard"),
        new("P9-TST-007G", "2026-07-01-staging-smoke-generator-release-guard"),
        new("P9-TST-007H", "2026-07-02-staging-smoke-latest-release-guard-cleanup"),
        new("P9-TST-007I", "2026-07-02-staging-smoke-generator-release-guard-cleanup"),
        new("P0-ADMIN-001B", "2026-06-19-admin-bootstrap-wrapper"),
        new("P0-ADMIN-001C", "2026-06-19-admin-vps-bootstrap-smoke-wrapper"),
        new("P0-ADMIN-001D", "2026-06-19-admin-vps-bootstrap-smoke-wrapper-regression"),
        new("P0-ADMIN-001E", "2026-06-19-admin-vps-bootstrap-smoke-report"),
        new("P0-ADMIN-001F", "2026-06-19-admin-vps-bootstrap-smoke-readiness"),
        new("P0-ADMIN-001G", "2026-06-19-admin-vps-bootstrap-smoke-evidence"),
        new("P0-ADMIN-001H", "2026-06-19-admin-vps-bootstrap-smoke-route-regression"),
        new("P0-ADMIN-001I", "2026-06-19-admin-vps-bootstrap-smoke-release-id-chain"),
        new("P0-ADMIN-001J", "2026-06-19-admin-vps-bootstrap-smoke-report-release-link"),
        new("P0-ADMIN-001K", "2026-06-20-admin-vps-bootstrap-smoke-readiness-path-link"),
        new("P0-ADMIN-001L", "2026-06-22-admin-vps-bootstrap-readiness-report-link"),
        new("P0-ADMIN-001M", "2026-06-22-admin-vps-bootstrap-smoke-report-self-link"),
        new("P0-ADMIN-001N", "2026-06-22-admin-vps-bootstrap-smoke-admin-email-link"),
        new("P0-ADMIN-001O", "2026-06-22-admin-vps-bootstrap-smoke-environment-link"),
        new("P0-ADMIN-001P", "2026-06-22-admin-vps-bootstrap-smoke-report-self-validate"),
        new("P0-ADMIN-001Q", "2026-06-22-admin-vps-bootstrap-readiness-self-validate"),
        new("P0-ADMIN-001R", "2026-06-22-admin-vps-bootstrap-readiness-chain-validate"),
        new("P0-ADMIN-001S", "2026-06-22-admin-vps-bootstrap-readiness-metadata-link"),
        new("P0-ADMIN-001T", "2026-06-22-admin-vps-bootstrap-smoke-timing-link"),
        new("P0-ADMIN-001U", "2026-06-22-admin-vps-bootstrap-readiness-smoke-path-link"),
        new("P0-ADMIN-001V", "2026-06-22-admin-vps-bootstrap-readiness-preflight-timing-link"),
        new("P0-ADMIN-001W", "2026-06-22-admin-vps-bootstrap-evidence-preflight-summary"),
        new("P0-ADMIN-001X", "2026-06-22-admin-vps-bootstrap-evidence-sections-summary"),
        new("P0-ADMIN-001Y", "2026-06-22-admin-vps-bootstrap-evidence-identity-summary"),
        new("P0-ADMIN-001Z", "2026-06-22-admin-vps-bootstrap-evidence-operator-summary"),
        new("P0-ADMIN-001AA", "2026-06-22-admin-vps-bootstrap-evidence-status-summary"),
        new("P0-ADMIN-001AB", "2026-06-22-admin-vps-bootstrap-evidence-reset-flags-summary"),
        new("P0-ADMIN-001AC", "2026-06-22-admin-vps-bootstrap-evidence-readiness-inputs-summary"),
        new("P0-ADMIN-001AD", "2026-06-22-admin-vps-bootstrap-evidence-timing-summary"),
        new("P0-ADMIN-001AE", "2026-06-22-admin-vps-bootstrap-evidence-smoke-summary"),
        new("P0-ADMIN-001AF", "2026-06-22-admin-vps-bootstrap-evidence-duration-summary"),
        new("P0-ADMIN-001AG", "2026-06-22-admin-vps-bootstrap-evidence-fingerprint-summary"),
        new("P0-ADMIN-001AH", "2026-06-22-admin-vps-bootstrap-evidence-expected-fingerprint"),
        new("P0-ADMIN-001AI", "2026-06-22-admin-vps-bootstrap-evidence-report-id-prefix"),
        new("P0-ADMIN-001AJ", "2026-06-22-admin-vps-bootstrap-evidence-report-id-timestamp"),
        new("P0-ADMIN-001AK", "2026-06-22-admin-vps-bootstrap-evidence-report-id-timestamp-link"),
        new("P0-ADMIN-001AL", "2026-06-22-admin-vps-bootstrap-evidence-chronology-summary"),
        new("P0-ADMIN-001AM", "2026-06-22-admin-vps-smoke-wrapper-max-duration"),
        new("P0-ADMIN-001AN", "2026-06-22-local-admin-bootstrap-smoke-max-duration"),
        new("P0-ADMIN-001AO", "2026-06-22-local-admin-bootstrap-smoke-wrapper-regression"),
        new("P0-ADMIN-001AP", "2026-06-22-local-admin-bootstrap-smoke-env-max-duration"),
        new("P0-ADMIN-001AQ", "2026-06-22-admin-vps-bootstrap-smoke-env-max-duration"),
        new("P0-ADMIN-001AR", "2026-06-22-admin-vps-bootstrap-smoke-env-guard"),
        new("P0-ADMIN-001AS", "2026-06-22-local-admin-bootstrap-smoke-explicit-max-duration-guard"),
        new("P0-ADMIN-001AT", "2026-06-22-admin-vps-bootstrap-smoke-env-upper-bound-guard"),
        new("P0-ADMIN-001AU", "2026-06-22-admin-vps-evidence-explicit-max-duration-guard"),
        new("P0-ADMIN-001AV", "2026-06-23-admin-vps-max-duration-format-guard"),
        new("P0-ADMIN-001AW", "2026-06-23-local-admin-bootstrap-port-guard"),
        new("P0-ADMIN-001AX", "2026-06-23-admin-vps-url-guard"),
        new("P0-ADMIN-001AY", "2026-06-23-admin-vps-email-guard"),
        new("P0-ADMIN-001AZ", "2026-06-23-admin-vps-report-path-guard"),
        new("P0-ADMIN-001BA", "2026-06-23-admin-vps-operator-default"),
        new("P0-ADMIN-001BB", "2026-06-23-admin-vps-environment-default"),
        new("P0-ADMIN-001BC", "2026-06-23-admin-vps-release-id-known-guard"),
        new("P0-ADMIN-001BD", "2026-06-23-admin-vps-bootstrap-provider-guard"),
        new("P0-ADMIN-001BE", "2026-06-23-admin-vps-readiness-ready-flag"),
        new("P0-ADMIN-001BF", "2026-06-23-admin-vps-readiness-provider-mode"),
        new("P0-ADMIN-001BG", "2026-06-23-admin-vps-readiness-provider-normalization"),
        new("P0-ADMIN-001BH", "2026-06-23-admin-vps-readiness-environment-default"),
        new("P0-ADMIN-001BI", "2026-06-23-admin-vps-readiness-admin-email-normalization"),
        new("P0-ADMIN-001BJ", "2026-06-23-admin-vps-readiness-url-normalization"),
        new("P0-ADMIN-001BK", "2026-06-23-admin-vps-bootstrap-wrapper-url-normalization"),
        new("P0-ADMIN-001BL", "2026-06-23-admin-vps-bootstrap-wrapper-admin-email-normalization"),
        new("P0-ADMIN-001BM", "2026-06-23-admin-vps-bootstrap-password-env-normalization"),
        new("P0-ADMIN-001BN", "2026-06-23-admin-vps-report-path-normalization"),
        new("P0-ADMIN-001BO", "2026-06-23-admin-vps-workspace-path-normalization"),
        new("P0-ADMIN-001BP", "2026-06-23-admin-bootstrap-profile-normalization"),
        new("P0-ADMIN-001BQ", "2026-06-23-admin-bootstrap-provider-normalization"),
        new("P0-ADMIN-001BR", "2026-06-23-admin-bootstrap-nonlocal-reset-guard"),
        new("P0-ADMIN-001BS", "2026-06-23-admin-bootstrap-password-env-name-guard"),
        new("P0-ADMIN-001BT", "2026-06-23-admin-bootstrap-readiness-password-env-validator"),
        new("P0-ADMIN-001BU", "2026-07-01-admin-vps-bootstrap-smoke-latest-release-guard"),
        new("P0-ADMIN-001BV", "2026-07-01-admin-vps-bootstrap-smoke-evidence-latest-release-guard"),
        new("P0-ADMIN-001BW", "2026-07-01-admin-vps-bootstrap-readiness-release-guard"),
        new("P0-ADMIN-001BX", "2026-07-02-admin-vps-bootstrap-release-guard-cleanup"),
        new("P0-ADMIN-001BY", "2026-07-02-admin-vps-bootstrap-release-guard-cleanup"),
        new("P0-ADMIN-001BZ", "2026-07-02-admin-vps-bootstrap-release-guard-cleanup"),
        new("P0-ADMIN-001CA", "2026-07-02-admin-bootstrap-wrapper-cleanup"),
        new("P0-ADMIN-001CB", "2026-07-02-admin-vps-bootstrap-smoke-evidence-validator-cleanup"),
        new("P0-ADMIN-001CC", "2026-07-02-admin-vps-bootstrap-smoke-wrapper-cleanup"),
        new("P0-ADMIN-001CD", "2026-07-02-local-admin-vps-bootstrap-smoke-wrapper-cleanup"),
        new("P0-ADMIN-001CE", "2026-07-02-local-admin-vps-bootstrap-smoke-cleanup"),
        new("P0-ADMIN-001CF", "2026-07-02-admin-vps-bootstrap-smoke-readiness-cleanup"),
        new("P0-ADMIN-002A", "2026-06-19-admin-vps-browser-smoke"),
        new("P0-ADMIN-002B", "2026-06-19-local-admin-vps-browser-smoke"),
        new("P0-ADMIN-002C", "2026-06-19-admin-vps-smoke-acceptance-evidence"),
        new("P0-ADMIN-002D", "2026-06-19-admin-vps-smoke-preflight"),
        new("P0-ADMIN-002E", "2026-06-19-admin-vps-smoke-preflight-validator"),
        new("P0-ADMIN-002F", "2026-06-19-admin-vps-smoke-preflight-validator-regression"),
        new("P0-ADMIN-002G", "2026-06-19-admin-vps-smoke-report-validator-regression"),
        new("P0-ADMIN-002H", "2026-06-19-admin-vps-smoke-flow-wrapper"),
        new("P0-ADMIN-002I", "2026-06-19-admin-vps-smoke-flow-wrapper-regression"),
        new("P0-ADMIN-002J", "2026-06-19-admin-vps-smoke-evidence-validator"),
        new("P0-ADMIN-002K", "2026-06-19-admin-vps-smoke-sections-contract"),
        new("P0-ADMIN-002L", "2026-06-19-admin-vps-smoke-report-route-contract"),
        new("P0-ADMIN-002M", "2026-06-19-admin-vps-smoke-preflight-release-id"),
        new("P0-ADMIN-002N", "2026-06-19-admin-vps-smoke-unified-release-id"),
        new("P0-ADMIN-002O", "2026-06-20-admin-vps-smoke-admin-email-evidence"),
        new("P0-ADMIN-002P", "2026-06-22-admin-vps-smoke-preflight-self-link"),
        new("P0-ADMIN-002Q", "2026-06-22-admin-vps-smoke-report-self-link"),
        new("P0-ADMIN-002R", "2026-06-22-admin-vps-smoke-evidence-timing-link"),
        new("P0-ADMIN-002S", "2026-06-22-admin-vps-smoke-evidence-preflight-summary"),
        new("P0-ADMIN-002T", "2026-06-22-admin-vps-smoke-evidence-sections-summary"),
        new("P0-ADMIN-002U", "2026-06-22-admin-vps-smoke-evidence-expected-fingerprint"),
        new("P0-ADMIN-002V", "2026-06-22-admin-vps-smoke-evidence-duration-summary"),
        new("P0-ADMIN-002W", "2026-06-22-admin-vps-smoke-evidence-duration-order"),
        new("P0-ADMIN-002X", "2026-06-22-admin-vps-smoke-evidence-identity-summary"),
        new("P0-ADMIN-002Y", "2026-06-22-admin-vps-smoke-evidence-report-id-summary"),
        new("P0-ADMIN-002Z", "2026-06-22-admin-vps-smoke-evidence-status-counts-summary"),
        new("P0-ADMIN-002AA", "2026-06-22-admin-vps-smoke-evidence-gate-flags-summary"),
        new("P0-ADMIN-002AB", "2026-06-22-admin-vps-smoke-evidence-report-id-uniqueness"),
        new("P0-ADMIN-002AC", "2026-06-22-admin-vps-smoke-evidence-report-id-prefix"),
        new("P0-ADMIN-002AD", "2026-06-22-admin-vps-smoke-evidence-report-id-timestamp"),
        new("P0-ADMIN-002AE", "2026-06-22-admin-vps-smoke-evidence-report-id-timestamp-link"),
        new("P0-ADMIN-002AF", "2026-06-22-admin-vps-smoke-evidence-chronology-summary"),
        new("P0-ADMIN-002AG", "2026-06-22-admin-vps-smoke-evidence-chain-max-duration"),
        new("P0-ADMIN-002AH", "2026-06-22-admin-vps-smoke-wrapper-max-duration"),
        new("P0-ADMIN-002AI", "2026-06-22-admin-vps-smoke-env-max-duration"),
        new("P0-ADMIN-002AJ", "2026-06-22-admin-vps-smoke-explicit-max-duration-guard"),
        new("P0-ADMIN-002AK", "2026-06-22-admin-vps-evidence-explicit-max-duration-guard"),
        new("P0-ADMIN-002AL", "2026-06-23-admin-vps-max-duration-format-guard"),
        new("P0-ADMIN-002AM", "2026-06-23-admin-vps-url-guard"),
        new("P0-ADMIN-002AN", "2026-06-23-admin-vps-email-guard"),
        new("P0-ADMIN-002AO", "2026-06-23-admin-vps-report-path-guard"),
        new("P0-ADMIN-002AP", "2026-06-23-admin-vps-operator-default"),
        new("P0-ADMIN-002AQ", "2026-06-23-admin-vps-environment-default"),
        new("P0-ADMIN-002AR", "2026-06-23-admin-vps-release-id-known-guard"),
        new("P0-ADMIN-002AS", "2026-06-23-admin-vps-smoke-wrapper-identity-normalization"),
        new("P0-ADMIN-002AT", "2026-06-24-admin-vps-smoke-navigation-fallback"),
        new("P0-ADMIN-002AU", "2026-06-24-admin-vps-smoke-remote-release-preflight"),
        new("P0-ADMIN-002AV", "2026-06-24-admin-vps-smoke-remote-release-diagnostics"),
        new("P0-ADMIN-002AW", "2026-06-24-admin-vps-smoke-remote-release-console-summary"),
        new("P0-ADMIN-002AX", "2026-06-24-admin-vps-smoke-preflight-failed-checks"),
        new("P0-ADMIN-002AY", "2026-06-24-admin-vps-smoke-preflight-failed-count"),
        new("P0-ADMIN-002AZ", "2026-06-24-admin-vps-smoke-preflight-check-counts"),
        new("P0-ADMIN-002BA", "2026-06-24-admin-vps-smoke-remote-message-console"),
        new("P0-ADMIN-002BB", "2026-06-24-admin-vps-smoke-preflight-report-id-console"),
        new("P0-ADMIN-002BC", "2026-07-01-admin-vps-smoke-latest-release-guard"),
        new("P0-ADMIN-002BD", "2026-07-01-admin-vps-smoke-preflight-latest-release-guard"),
        new("P0-ADMIN-002BE", "2026-07-01-admin-vps-smoke-evidence-latest-release-guard"),
        new("P0-ADMIN-002BF", "2026-07-01-admin-vps-smoke-generator-release-guard"),
        new("P0-ADMIN-002BG", "2026-07-01-admin-vps-browser-smoke-direct-release-guard"),
        new("P0-ADMIN-002BH", "2026-07-01-admin-vps-smoke-preflight-release-guard"),
        new("P0-ADMIN-002BI", "2026-07-02-admin-vps-smoke-release-guard-cleanup"),
        new("P0-ADMIN-002BJ", "2026-07-02-admin-vps-smoke-release-guard-cleanup"),
        new("P0-ADMIN-002BK", "2026-07-02-admin-vps-smoke-release-guard-cleanup"),
        new("P0-ADMIN-002BL", "2026-07-02-admin-vps-smoke-release-guard-cleanup"),
        new("P0-ADMIN-002BM", "2026-07-02-admin-vps-smoke-release-guard-cleanup"),
        new("P0-ADMIN-002BN", "2026-07-02-admin-vps-browser-smoke-direct-release-guard-cleanup"),
        new("P0-ADMIN-002BO", "2026-07-02-admin-vps-smoke-report-validator-cleanup"),
        new("P0-ADMIN-002BP", "2026-07-02-admin-vps-smoke-preflight-validator-cleanup"),
        new("P0-ADMIN-002BQ", "2026-07-02-admin-vps-smoke-evidence-validator-cleanup"),
        new("P0-ADMIN-002BR", "2026-07-02-admin-vps-smoke-flow-wrapper-cleanup"),
        new("P0-ADMIN-002BS", "2026-07-02-local-admin-vps-browser-smoke-cleanup"),
        new("P0-ADMIN-002BT", "2026-07-02-admin-vps-smoke-sections-contract-cleanup"),
        new("P0-ADMIN-003", "2026-06-14-admin-vps-smoke-report"),
        new("P0-VPN-006", "2026-06-14-vpn-live-smoke-report"),
        new("P0-VPN-007", "2026-07-01-vpn-live-smoke-latest-release-guard"),
        new("P0-VPN-008", "2026-07-01-vpn-live-smoke-generator-release-guard"),
        new("P0-VPN-009", "2026-07-02-vpn-live-smoke-generator-release-guard-cleanup"),
        new("P0-VPN-010", "2026-07-02-vpn-live-smoke-latest-release-guard-cleanup"),
        new("P0-PAY-012", "2026-06-14-payment-provider-smoke-report"),
        new("P0-PAY-013", "2026-06-14-payment-provider-smoke-generator"),
        new("P0-PAY-014", "2026-06-19-payment-provider-smoke-report-acceptance-gates"),
        new("P0-PAY-015", "2026-07-01-payment-smoke-latest-release-guard"),
        new("P0-PAY-016", "2026-07-01-payment-smoke-generator-release-guard"),
        new("P0-PAY-017", "2026-07-02-payment-smoke-release-guard-cleanup"),
        new("P0-PAY-018", "2026-07-02-payment-smoke-release-guard-cleanup"),
        new("P0-PAY-010", "2026-06-14-telegram-stars-invoice-gate"),
        new("P1-TG-005", "2026-06-14-api-telegram-webhook"),
        new("P1-TG-006", "2026-06-14-telegram-webhook-boundary"),
        new("P10-DOC-001", "2026-06-13-readme-russian-local-runbook"),
        new("P10-DOC-002", "2026-06-13-admin-operator-guide"),
        new("P10-DOC-003", "2026-06-13-user-help-pages"),
        new("P10-DOC-004", "2026-06-13-developer-guide"),
        new("P10-DOC-005", "2026-06-13-docs-encoding-guard"),
        new("P11-ACC-001", "2026-06-13-fresh-local-smoke"),
        new("P11-ACC-003", "2026-06-14-mobile-smoke"),
        new("P11-ACC-004", "2026-06-14-no-console-errors-smoke"),
        new("P11-ACC-005", "2026-06-14-security-final-checklist"),
        new("P11-ACC-006", "2026-06-14-final-docs-changelog"),
        new("P11-ACC-007", "2026-06-14-release-decision"),
        new("P11-ACC-008", "2026-06-14-production-readiness-gate"),
        new("P11-ACC-009", "2026-06-18-production-evidence-bundle-gate"),
        new("P11-ACC-010", "2026-06-18-production-evidence-aggregate-gate"),
        new("P11-ACC-011", "2026-06-18-production-evidence-bundle-generator"),
        new("P11-ACC-012", "2026-06-18-production-readiness-summary"),
        new("P11-ACC-013", "2026-06-18-production-readiness-summary-validator"),
        new("P11-ACC-014", "2026-06-18-production-evidence-bundle-validator"),
        new("P11-ACC-015", "2026-06-18-production-evidence-manifest"),
        new("P11-ACC-016", "2026-06-18-production-evidence-manifest-validator"),
        new("P11-ACC-017", "2026-06-18-production-evidence-archive"),
        new("P11-ACC-018", "2026-06-18-production-evidence-archive-validator"),
        new("P11-ACC-019", "2026-06-18-production-evidence-handoff-receipt"),
        new("P11-ACC-020", "2026-06-18-production-evidence-handoff-receipt-validator"),
        new("P11-ACC-021", "2026-06-18-production-evidence-handoff-checklist"),
        new("P11-ACC-022", "2026-06-18-production-evidence-handoff-checklist-validator"),
        new("P11-ACC-023", "2026-06-18-production-evidence-handoff-package"),
        new("P11-ACC-024", "2026-06-18-production-evidence-handoff-package-validator"),
        new("P11-ACC-025", "2026-06-18-production-evidence-handoff-package-archive"),
        new("P11-ACC-026", "2026-06-18-production-evidence-handoff-package-archive-validator"),
        new("P11-ACC-027", "2026-06-18-production-evidence-handoff-package-archive-validator-regression"),
        new("P11-ACC-028", "2026-06-18-production-evidence-handoff-package-archive-flow"),
        new("P11-ACC-029", "2026-06-18-production-evidence-handoff-package-archive-flow-safety"),
        new("P11-ACC-030", "2026-06-18-production-evidence-handoff-package-archive-flow-result"),
        new("P11-ACC-031", "2026-06-18-production-evidence-handoff-package-archive-flow-result-validator"),
        new("P11-ACC-032", "2026-06-18-production-evidence-handoff-package-archive-flow-result-validator-regression"),
        new("P11-ACC-033", "2026-06-18-production-evidence-handoff-package-archive-long-path-regression"),
        new("P11-ACC-034", "2026-06-18-production-evidence-handoff-package-archive-ci-regression"),
        new("P11-ACC-035", "2026-06-18-production-evidence-handoff-package-archive-ci-workflow"),
        new("P11-ACC-036", "2026-06-18-production-evidence-handoff-package-archive-ci-summary"),
        new("P11-ACC-037", "2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator"),
        new("P11-ACC-038", "2026-06-18-production-evidence-handoff-package-archive-ci-summary-validator-regression"),
        new("P11-ACC-039", "2026-06-18-production-evidence-handoff-package-archive-ci-result-validator"),
        new("P11-ACC-040", "2026-06-18-production-evidence-handoff-package-archive-ci-result-validator-regression"),
        new("P11-ACC-041", "2026-06-18-production-readiness-assertion-result-artifacts"),
        new("P11-ACC-042", "2026-06-18-production-readiness-assertion-result-validator"),
        new("P11-ACC-043", "2026-06-18-production-readiness-assertion-result-validator-regression"),
        new("P11-ACC-044", "2026-06-18-production-readiness-assertion-ci-regression"),
        new("P11-ACC-045", "2026-06-19-production-readiness-assertion-ci-result-validator"),
        new("P11-ACC-046", "2026-06-19-production-readiness-assertion-ci-result-validator-regression"),
        new("P11-ACC-047", "2026-06-19-production-readiness-assertion-ci-summary-validator"),
        new("P11-ACC-048", "2026-06-19-production-readiness-assertion-ci-step-summary-smoke"),
        new("P11-ACC-049", "2026-06-19-production-readiness-assertion-ci-artifacts-validator"),
        new("P11-ACC-050", "2026-06-19-production-readiness-assertion-ci-artifacts-validator-regression"),
        new("P11-ACC-051", "2026-06-19-production-readiness-assertion-ci-summary-artifacts-regression"),
        new("P11-ACC-052", "2026-06-19-production-readiness-assertion-ci-workflow-artifacts"),
        new("P11-ACC-053", "2026-06-19-production-readiness-assertion-ci-workflow-guard-step"),
        new("P11-ACC-054", "2026-06-19-production-evidence-ci-workflow-artifacts-guard"),
        new("P11-ACC-055", "2026-06-19-production-evidence-ci-workflow-artifacts-guard-regression"),
        new("P11-ACC-056", "2026-06-19-production-readiness-assertion-ci-workflow-artifacts-guard-regression"),
        new("P11-ACC-057", "2026-06-19-production-ci-workflow-artifacts-guards-aggregate"),
        new("P11-ACC-058", "2026-06-19-production-ci-workflow-artifacts-guards-aggregate-regression"),
        new("P11-ACC-059", "2026-06-19-production-ci-workflow-artifacts-guards-aggregate-regression-ci-step"),
        new("P11-ACC-060", "2026-06-19-production-ci-workflow-artifacts-guards-ci-step-guard"),
        new("P11-ACC-061", "2026-06-19-production-ci-workflow-artifacts-guards-ci-step-regression"),
        new("P11-ACC-062", "2026-06-19-production-ci-workflow-artifacts-guards-aggregate-ci-step-guards"),
        new("P11-ACC-063", "2026-06-19-production-ci-workflow-artifacts-guards-aggregate-ci-step-guards-regression"),
        new("P11-ACC-064", "2026-06-19-vps-production-smoke-report-contract"),
        new("P11-ACC-065", "2026-07-01-vps-production-smoke-latest-release-guard"),
        new("P11-ACC-066", "2026-07-01-production-handoff-checklist-latest-release-guard"),
        new("P11-ACC-067", "2026-07-01-production-handoff-package-latest-release-guard"),
        new("P11-ACC-068", "2026-07-01-production-handoff-flow-result-latest-release-guard"),
        new("P11-ACC-069", "2026-07-01-production-handoff-ci-result-latest-release-guard"),
        new("P11-ACC-070", "2026-07-01-production-handoff-ci-summary-latest-release-guard"),
        new("P11-ACC-071", "2026-07-01-production-readiness-summary-latest-release-guard"),
        new("P11-ACC-072", "2026-07-01-production-readiness-assertion-latest-release-guard"),
        new("P11-ACC-073", "2026-07-01-production-evidence-bundle-latest-release-guard"),
        new("P11-ACC-074", "2026-07-01-production-handoff-package-archive-latest-release-guard"),
        new("P11-ACC-075", "2026-07-01-vps-production-smoke-generator-release-guard"),
        new("P11-ACC-076", "2026-07-01-production-evidence-bundle-generator-release-guard"),
        new("P11-ACC-077", "2026-07-01-production-evidence-manifest-release-guard"),
        new("P11-ACC-078", "2026-07-01-production-evidence-archive-release-guard"),
        new("P11-ACC-079", "2026-07-01-production-handoff-receipt-release-guard"),
        new("P11-ACC-080", "2026-07-01-production-handoff-checklist-release-guard"),
        new("P11-ACC-081", "2026-07-01-production-handoff-receipt-verified-files-guard"),
        new("P11-ACC-082", "2026-07-01-production-handoff-receipt-markdown-verified-files-guard"),
        new("P11-ACC-083", "2026-07-01-production-handoff-checklist-markdown-gates-guard"),
        new("P11-ACC-084", "2026-07-01-production-handoff-package-markdown-files-guard"),
        new("P11-ACC-085", "2026-07-01-production-handoff-package-archive-duplicate-entry-guard"),
        new("P11-ACC-086", "2026-07-01-production-handoff-package-archive-nested-entry-guard"),
        new("P11-ACC-087", "2026-07-01-production-handoff-package-archive-directory-entry-guard"),
        new("P11-ACC-088", "2026-07-01-production-handoff-package-archive-backslash-entry-guard"),
        new("P11-ACC-089", "2026-07-01-production-handoff-package-archive-dotdot-entry-guard"),
        new("P11-ACC-090", "2026-07-01-production-handoff-package-archive-dot-entry-guard"),
        new("P11-ACC-091", "2026-07-01-production-handoff-package-archive-rooted-entry-guard"),
        new("P11-ACC-092", "2026-07-01-production-handoff-package-archive-whitespace-entry-guard"),
        new("P11-ACC-093", "2026-07-01-production-handoff-package-archive-entry-case-guard"),
        new("P11-ACC-094", "2026-07-01-production-handoff-package-archive-entry-case-cleanup"),
        new("P11-ACC-095", "2026-07-01-production-handoff-package-archive-duplicate-entry-cleanup"),
        new("P11-ACC-096", "2026-07-01-production-handoff-package-archive-nested-entry-cleanup"),
        new("P11-ACC-097", "2026-07-01-production-handoff-package-archive-directory-entry-cleanup"),
        new("P11-ACC-098", "2026-07-01-production-handoff-package-archive-backslash-entry-cleanup"),
        new("P11-ACC-099", "2026-07-01-production-handoff-package-archive-dotdot-entry-cleanup"),
        new("P11-ACC-100", "2026-07-01-production-handoff-package-archive-dot-entry-cleanup"),
        new("P11-ACC-101", "2026-07-01-production-handoff-package-archive-rooted-entry-cleanup"),
        new("P11-ACC-102", "2026-07-01-production-handoff-package-archive-whitespace-entry-cleanup"),
        new("P11-ACC-103", "2026-07-01-production-handoff-package-archive-entry-guard-cleanup-coverage"),
        new("P11-ACC-104", "2026-07-01-production-handoff-package-archive-flow-default-cleanup"),
        new("P11-ACC-105", "2026-07-01-production-handoff-package-archive-ci-regression-default-cleanup"),
        new("P11-ACC-106", "2026-07-01-production-handoff-package-archive-long-path-default-cleanup"),
        new("P11-ACC-107", "2026-07-01-production-readiness-assertion-ci-regression-default-cleanup"),
        new("P11-ACC-108", "2026-07-01-production-readiness-assertion-result-latest-release-guard-cleanup"),
        new("P11-ACC-109", "2026-07-01-production-readiness-summary-latest-release-guard-cleanup"),
        new("P11-ACC-110", "2026-07-01-production-handoff-checklist-latest-release-guard-cleanup"),
        new("P11-ACC-111", "2026-07-01-production-handoff-flow-result-latest-release-guard-cleanup"),
        new("P11-ACC-112", "2026-07-01-production-handoff-ci-result-latest-release-guard-cleanup"),
        new("P11-ACC-113", "2026-07-02-production-handoff-ci-summary-latest-release-guard-cleanup"),
        new("P11-ACC-114", "2026-07-02-production-handoff-package-archive-latest-release-guard-cleanup"),
        new("P11-ACC-115", "2026-07-02-production-evidence-bundle-latest-release-guard-cleanup"),
        new("P11-ACC-116", "2026-07-02-production-evidence-manifest-release-guard-cleanup"),
        new("P11-ACC-117", "2026-07-02-production-evidence-archive-release-guard-cleanup"),
        new("P11-ACC-118", "2026-07-02-production-handoff-receipt-release-guard-cleanup"),
        new("P11-ACC-119", "2026-07-02-production-handoff-checklist-release-guard-cleanup"),
        new("P11-ACC-120", "2026-07-02-production-handoff-package-latest-release-guard-cleanup"),
        new("P11-ACC-121", "2026-07-02-production-handoff-receipt-verified-files-guard-cleanup"),
        new("P11-ACC-122", "2026-07-02-production-handoff-receipt-markdown-verified-files-guard-cleanup"),
        new("P11-ACC-123", "2026-07-02-production-handoff-checklist-markdown-gates-guard-cleanup"),
        new("P11-ACC-124", "2026-07-02-production-handoff-package-markdown-files-guard-cleanup"),
        new("P11-ACC-125", "2026-07-02-production-evidence-bundle-generator-release-guard-cleanup"),
        new("P11-ACC-126", "2026-07-02-vps-production-smoke-generator-release-guard-cleanup"),
        new("P11-ACC-127", "2026-07-02-vps-production-smoke-latest-release-guard-cleanup"),
        new("P11-ACC-128", "2026-07-02-production-readiness-ci-step-summary-cleanup"),
        new("P11-ACC-129", "2026-07-02-fresh-local-smoke-cleanup"),
        new("P11-ACC-130", "2026-07-02-vps-production-smoke-report-self-link"),
        new("P11-ACC-131", "2026-07-02-production-readiness-summary-self-link"),
        new("P11-ACC-132", "2026-07-02-production-readiness-assertion-result-self-link"),
        new("P11-ACC-133", "2026-07-02-roadmap-progress-counter-guard"),
        new("P11-ACC-134", "2026-07-02-roadmap-progress-percent-guard"),
        new("P11-ACC-135", "2026-07-02-roadmap-progress-remaining-guard"),
        new("P11-ACC-136", "2026-07-02-agent-instructions-guard"),
        new("P11-ACC-137", "2026-07-02-external-evidence-open-guard"),
        new("P11-ACC-138", "2026-07-02-product-ui-external-evidence-open-guard"),
        new("P11-ACC-139", "2026-07-02-release-decision-external-evidence-open-guard"),
        new("P11-ACC-140", "2026-07-02-final-runbook-external-evidence-open-guard"),
        new("P11-ACC-141", "2026-07-02-test-results-external-evidence-open-guard"),
        new("P11-ACC-142", "2026-07-02-changelog-external-evidence-open-guard"),
        new("P11-ACC-143", "2026-07-02-readme-external-evidence-open-guard"),
        new("P11-ACC-144", "2026-07-02-product-roadmap-external-evidence-open-guard"),
        new("P11-ACC-145", "2026-07-02-roadmap-external-evidence-open-set-guard"),
        new("P11-ACC-146", "2026-07-02-status-docs-latest-release-seed-guard"),
        new("P11-ACC-147", "2026-07-02-changelog-test-results-top-release-guard"),
        new("P11-ACC-148", "2026-07-02-latest-release-seed-order-guard"),
        new("P11-ACC-149", "2026-07-02-release-seed-identity-guard"),
        new("P11-ACC-150", "2026-07-02-release-seed-version-order-guard"),
        new("P11-ACC-151", "2026-07-02-release-seed-file-order-guard"),
        new("P11-ACC-152", "2026-07-02-release-seed-secret-literal-guard"),
        new("P9-TST-007J", "2026-07-02-staging-smoke-report-self-link"),
        new("P0-PAY-019", "2026-07-02-payment-provider-smoke-report-self-link"),
        new("P0-VPN-011", "2026-07-02-vpn-live-smoke-report-self-link")
    ];

    [Fact]
    public void Completed_Roadmap_Items_Should_Have_Whats_New_Release_And_Test_Result()
    {
        var root = FindRepositoryRoot();
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));
        using var releasesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "AppReleases", "releases.json")));
        var releaseIds = releasesJson.RootElement
            .EnumerateArray()
            .Select(x => x.GetProperty("releaseId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var expectation in Expectations)
        {
            Assert.Contains($"[x] `{expectation.RoadmapId}`", roadmap, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(expectation.ReleaseId, releaseIds);
            Assert.Contains(expectation.ReleaseId, testResults, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Whats_New_Seed_Should_Keep_User_Facing_Release_Items()
    {
        var root = FindRepositoryRoot();
        using var releasesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "AppReleases", "releases.json")));

        foreach (var release in releasesJson.RootElement.EnumerateArray())
        {
            var releaseId = release.GetProperty("releaseId").GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(releaseId));
            Assert.False(string.IsNullOrWhiteSpace(release.GetProperty("version").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(release.GetProperty("title").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(release.GetProperty("summary").GetString()));

            var items = release.GetProperty("items").EnumerateArray().ToArray();
            Assert.NotEmpty(items);
            Assert.All(items, item =>
            {
                Assert.Contains(item.GetProperty("type").GetString(), new[] { "new", "improved", "fixed", "important" });
                Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("text").GetString()));
            });
        }
    }

    [Fact]
    public void Live_Provisioning_Runbook_Should_Cover_Operator_Gates()
    {
        var root = FindRepositoryRoot();
        var runbook = File.ReadAllText(Path.Combine(root, "docs", "live-provisioning-runbook.md"));

        Assert.Contains("Provisioning__LiveExecutionEnabled=true", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Provisioning__AllowLiveDeploy=true", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("explicit-live-provisioning:true", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validation-mode:false", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Provisioning__KnownHostsPath", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ansible-playbook --syntax-check", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST /api/admin/servers/{id}/precheck", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST /api/admin/provisioning-runs/{id}/deploy", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Precheck report", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Rollback node state", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Fail-closed", runbook, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".env.example")) && Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for release documentation guard tests.");
    }

    private sealed record ReleaseNoteExpectation(string RoadmapId, string ReleaseId);
}
