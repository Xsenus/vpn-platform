#!/usr/bin/env python3
"""Run sanitized application-level checks against a real production 3x-ui panel."""

from __future__ import annotations

import argparse
import json
import os
import sys
import urllib.error
import urllib.request
import urllib.parse
from datetime import datetime, timezone
from pathlib import Path


CHECK_IDS = (
    "panel-connection",
    "inbound-sync",
    "node-ready",
    "order-create",
    "payment-webhook",
    "subscription-activated",
    "vpn-client-created",
    "access-uri-qr",
    "fail-closed-disabled-inbound",
)


class SmokeError(RuntimeError):
    pass


def request_json(base_url: str, path: str, *, method: str = "GET", token: str = "", body=None):
    headers = {"Accept": "application/json"}
    data = None
    if token:
        headers["Authorization"] = f"Bearer {token}"
    if body is not None:
        headers["Content-Type"] = "application/json"
        data = json.dumps(body, separators=(",", ":")).encode("utf-8")
    request = urllib.request.Request(
        f"{base_url.rstrip('/')}/{path.lstrip('/')}",
        data=data,
        headers=headers,
        method=method,
    )
    try:
        with urllib.request.urlopen(request, timeout=45) as response:
            raw = response.read().decode("utf-8")
    except urllib.error.HTTPError as error:
        raw = error.read().decode("utf-8", errors="replace")
        raise SmokeError(f"{method} {path} returned HTTP {error.code}: {raw[:300]}") from error
    except urllib.error.URLError as error:
        raise SmokeError(f"{method} {path} failed: {error.reason}") from error
    if not raw.strip():
        return None, raw
    try:
        return json.loads(raw), raw
    except json.JSONDecodeError as error:
        raise SmokeError(f"{method} {path} returned invalid JSON") from error


def find_named(items, name: str):
    return next((item for item in items if item.get("name") == name), None)


def panel_payload(panel_url: str, api_token: str, revision=None):
    payload = {
        "name": "production-vps-3xui",
        "baseUrl": panel_url.rstrip("/") + "/",
        "login": "api-token",
        "password": api_token,
        "region": "production",
        "capacity": 5000,
        "sslVerificationMode": "Disabled",
        "apiVariant": "X3UiOfficial",
        "authenticationMode": "ApiToken",
        "autoCreateInbound": False,
        "defaultInboundTemplateJson": "{}",
    }
    if revision is not None:
        payload["revision"] = revision
        payload["status"] = "Active"
    return payload


def build_report(args, started_at: str, checks: dict[str, tuple[str, str]]):
    completed_at = datetime.now(timezone.utc).isoformat()
    parsed_panel_url = urllib.parse.urlsplit(args.panel_url)
    sanitized_panel_url = urllib.parse.urlunsplit(
        (parsed_panel_url.scheme, parsed_panel_url.netloc, "", "", "")
    )
    return {
        "reportId": f"vpn-live-smoke-{datetime.now(timezone.utc).strftime('%Y%m%d-%H%M%S')}",
        "environmentName": "production-vps",
        "apiBaseUrl": args.api_base_url.rstrip("/"),
        "adminWebUrl": args.admin_web_url.rstrip("/"),
        "x3uiPanelUrl": sanitized_panel_url,
        "smokeReportPath": str(Path(args.output).resolve()),
        "startedAt": started_at,
        "completedAt": completed_at,
        "releaseId": args.release_id,
        "operator": args.operator,
        "notes": "Application-level production VPS smoke. Evidence is sanitized; credentials, headers, cookies and VPN URIs are excluded.",
        "panelConnected": checks["panel-connection"][0] == "passed",
        "inboundSynced": checks["inbound-sync"][0] == "passed",
        "nodeReady": checks["node-ready"][0] == "passed",
        "productionProvisioningEnabled": False,
        "noSandboxFallback": False,
        "failClosedChecked": checks["fail-closed-disabled-inbound"][0] == "passed",
        "checks": [
            {"id": check_id, "status": checks[check_id][0], "evidence": checks[check_id][1]}
            for check_id in CHECK_IDS
        ],
    }


def run(args):
    started_at = datetime.now(timezone.utc).isoformat()
    admin_password = os.environ.get("PRODUCTION_ACCEPTANCE_ADMIN_PASSWORD", "")
    panel_token = os.environ.get("X3UI_API_TOKEN", "")
    if len(admin_password) < 16 or not panel_token:
        raise SmokeError("Required acceptance credentials are missing.")

    auth, _ = request_json(
        args.api_base_url,
        "/api/auth/login",
        method="POST",
        body={"email": args.admin_email, "password": admin_password},
    )
    access_token = str(auth.get("accessToken", ""))
    if not access_token:
        raise SmokeError("Admin login response did not contain an access token.")

    panels, _ = request_json(args.api_base_url, "/api/admin/vpn-panels", token=access_token)
    existing = find_named(panels, "production-vps-3xui")
    if existing:
        panel, _ = request_json(
            args.api_base_url,
            f"/api/admin/vpn-panels/{existing['id']}",
            method="PATCH",
            token=access_token,
            body=panel_payload(args.panel_url, panel_token, existing["revision"]),
        )
    else:
        panel, _ = request_json(
            args.api_base_url,
            "/api/admin/vpn-panels",
            method="POST",
            token=access_token,
            body=panel_payload(args.panel_url, panel_token),
        )

    panel_id = panel["id"]
    health, _ = request_json(
        args.api_base_url,
        f"/api/admin/vpn-panels/{panel_id}/test-connection",
        method="POST",
        token=access_token,
    )
    if str(health.get("status", "")).lower() not in {"healthy", "success"}:
        raise SmokeError("3x-ui connection check was not healthy.")

    sync, _ = request_json(
        args.api_base_url,
        f"/api/admin/vpn-panels/{panel_id}/sync",
        method="POST",
        token=access_token,
    )
    inbounds, _ = request_json(
        args.api_base_url,
        f"/api/admin/vpn-panels/{panel_id}/inbounds",
        token=access_token,
    )
    active = [item for item in inbounds if item.get("isActive") and item.get("protocol")]
    if not active:
        raise SmokeError("No active real 3x-ui inbound was synchronized.")

    sanitized_panel, panel_raw = request_json(
        args.api_base_url,
        f"/api/admin/vpn-panels/{panel_id}",
        token=access_token,
    )
    if panel_token in panel_raw or any(key.lower() in {"password", "encryptedpassword"} for key in sanitized_panel):
        raise SmokeError("VPN panel API response exposed a secret field or token.")

    checks = {
        check_id: ("blocked", "Requires a real paid order and production provisioning acceptance run.")
        for check_id in CHECK_IDS
    }
    checks["panel-connection"] = (
        "passed",
        f"Application health-check succeeded for panel {panel_id}; API response omitted credentials.",
    )
    checks["inbound-sync"] = (
        "passed",
        f"Application sync run {sync.get('id', 'completed')} imported {len(active)} active inbound(s).",
    )
    checks["node-ready"] = ("blocked", "A production-ready VPN node has not yet been accepted through an order flow.")
    report = build_report(args, started_at, checks)
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return report


def parse_args(argv=None):
    parser = argparse.ArgumentParser()
    parser.add_argument("--api-base-url", required=True)
    parser.add_argument("--admin-web-url", required=True)
    parser.add_argument("--panel-url", required=True)
    parser.add_argument("--admin-email", required=True)
    parser.add_argument("--release-id", required=True)
    parser.add_argument("--operator", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args(argv)


def main(argv=None):
    try:
        report = run(parse_args(argv))
        passed = sum(check["status"] == "passed" for check in report["checks"])
        print(f"production VPN smoke completed: {passed}/{len(report['checks'])} checks passed")
        return 0
    except SmokeError as error:
        print(f"production VPN smoke failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
