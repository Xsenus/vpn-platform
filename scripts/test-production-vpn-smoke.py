#!/usr/bin/env python3
import importlib.util
import argparse
import unittest
from pathlib import Path


SCRIPT = Path(__file__).with_name("run-production-vpn-smoke.py")
SPEC = importlib.util.spec_from_file_location("production_vpn_smoke", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class ProductionVpnSmokeTests(unittest.TestCase):
    def test_panel_payload_uses_api_token_mode(self):
        payload = MODULE.panel_payload("http://127.0.0.1:2053/example", "masked-token", 4)
        self.assertEqual("ApiToken", payload["authenticationMode"])
        self.assertEqual("masked-token", payload["password"])
        self.assertEqual("Disabled", payload["sslVerificationMode"])
        self.assertEqual(4, payload["revision"])
        self.assertEqual("http://127.0.0.1:2053/example/", payload["baseUrl"])

    def test_find_named_is_exact(self):
        items = [{"name": "production-vps-3xui-old"}, {"name": "production-vps-3xui", "id": "ok"}]
        self.assertEqual("ok", MODULE.find_named(items, "production-vps-3xui")["id"])

    def test_report_removes_private_panel_base_path(self):
        args = argparse.Namespace(
            api_base_url="http://api.test",
            admin_web_url="http://admin.test",
            panel_url="http://127.0.0.1:54321/private-path/",
            output="artifacts/report.json",
            release_id="release",
            operator="test",
        )
        checks = {check_id: ("blocked", "safe") for check_id in MODULE.CHECK_IDS}
        report = MODULE.build_report(args, "2026-08-31T00:00:00+00:00", checks)
        self.assertEqual("http://127.0.0.1:54321", report["x3uiPanelUrl"])

    def test_ready_node_marks_production_without_sandbox_fallback(self):
        args = argparse.Namespace(
            api_base_url="http://api.test",
            admin_web_url="http://admin.test",
            panel_url="http://127.0.0.1:54321/private/",
            output="artifacts/report.json",
            release_id="release",
            operator="test",
        )
        checks = {check_id: ("blocked", "safe") for check_id in MODULE.CHECK_IDS}
        checks["node-ready"] = ("passed", "safe")
        report = MODULE.build_report(args, "2026-08-31T00:00:00+00:00", checks)
        self.assertTrue(report["nodeReady"])
        self.assertTrue(report["productionProvisioningEnabled"])
        self.assertTrue(report["noSandboxFallback"])


if __name__ == "__main__":
    unittest.main()
