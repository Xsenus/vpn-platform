from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

import run_playbook  # noqa: E402


class RunPlaybookTests(unittest.TestCase):
    def make_args(self, **overrides):
        base = run_playbook.RunnerArgs(
            playbook="/tmp/playbook.yml",
            host="1.2.3.4",
            ssh_user="root",
            ssh_port=22,
            private_key_path="/tmp/id_ed25519",
            extra_vars_file="/tmp/extra.json",
            check=False,
            workdir="/tmp/workdir",
            inventory_name="node-01",
            python_interpreter="/usr/bin/python3",
            known_hosts_path=None,
            skip_host_key_checking=False,
            ansible_binary="ansible-playbook",
        )
        for key, value in overrides.items():
            setattr(base, key, value)
        return base

    def test_build_inventory_content_includes_ssh_data(self):
        args = self.make_args(skip_host_key_checking=True)
        content = run_playbook.build_inventory_content(args)
        self.assertIn("[vpn_nodes]", content)
        self.assertIn("node-01", content)
        self.assertIn("ansible_host=1.2.3.4", content)
        self.assertIn("ansible_user=root", content)
        self.assertIn("ansible_port=22", content)
        self.assertIn("ansible_ssh_private_key_file=/tmp/id_ed25519", content)
        self.assertIn("StrictHostKeyChecking=no", content)

    def test_build_command_adds_check_and_extra_vars(self):
        args = self.make_args(check=True)
        command = run_playbook.build_command(args, "/tmp/workdir/inventory.ini")
        self.assertEqual(command[:4], ["ansible-playbook", "--inventory", "/tmp/workdir/inventory.ini", "/tmp/playbook.yml"])
        self.assertIn("--extra-vars", command)
        self.assertIn("@/tmp/extra.json", command)
        self.assertIn("--check", command)

    @patch("run_playbook.subprocess.run")
    def test_execute_returns_machine_readable_result(self, mocked_run):
        mocked_run.return_value.returncode = 0
        mocked_run.return_value.stdout = "PLAY RECAP"
        mocked_run.return_value.stderr = ""

        with tempfile.TemporaryDirectory() as tmp:
            playbook = Path(tmp) / "playbook.yml"
            playbook.write_text("---\n- hosts: all\n  tasks: []\n", encoding="utf-8")
            args = self.make_args(playbook=str(playbook), workdir=str(Path(tmp) / "work"))
            result = run_playbook.execute(args)

        self.assertTrue(result.success)
        self.assertEqual(result.steps[0].stepName, "inventory")
        self.assertEqual(result.steps[1].stepName, "ansible-playbook")
        self.assertIn("succeeded", result.summaryLog)

    @patch("run_playbook.execute")
    def test_main_emits_json(self, mocked_execute):
        mocked_execute.return_value = run_playbook.RunnerResult(
            success=True,
            summaryLog="ok",
            workDirectory="/tmp/work",
            steps=[run_playbook.StepResult(stepName="inventory", success=True, output="/tmp/work/inventory.ini")],
        )

        with patch("builtins.print") as mocked_print:
            exit_code = run_playbook.main(["--playbook", "/tmp/x.yml", "--host", "1.2.3.4", "--ssh-user", "root", "--ssh-port", "22", "--workdir", "/tmp/work"])

        self.assertEqual(exit_code, 0)
        payload = json.loads(mocked_print.call_args.args[0])
        self.assertTrue(payload["success"])
        self.assertEqual(payload["summaryLog"], "ok")


if __name__ == "__main__":
    unittest.main()
