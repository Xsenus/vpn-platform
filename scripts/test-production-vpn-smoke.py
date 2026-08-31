#!/usr/bin/env python3
import importlib.util
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
        self.assertEqual(4, payload["revision"])
        self.assertEqual("http://127.0.0.1:2053/example/", payload["baseUrl"])

    def test_find_named_is_exact(self):
        items = [{"name": "production-vps-3xui-old"}, {"name": "production-vps-3xui", "id": "ok"}]
        self.assertEqual("ok", MODULE.find_named(items, "production-vps-3xui")["id"])


if __name__ == "__main__":
    unittest.main()

