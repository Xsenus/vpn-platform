#!/usr/bin/env python3
"""Minimal ansible playbook runner used by backend provisioning workers.

The script generates a single-host inventory, executes ansible-playbook and prints
machine-readable JSON to stdout.
"""
from __future__ import annotations

import argparse
import json
import os
import subprocess
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Iterable


@dataclass
class StepResult:
    stepName: str
    success: bool
    output: str = ""
    errorText: str | None = None


@dataclass
class RunnerResult:
    success: bool
    summaryLog: str
    workDirectory: str
    steps: list[StepResult]
    errorText: str | None = None


@dataclass
class RunnerArgs:
    playbook: str
    host: str
    ssh_user: str
    ssh_port: int
    private_key_path: str | None
    extra_vars_file: str | None
    check: bool
    workdir: str
    inventory_name: str
    python_interpreter: str
    known_hosts_path: str | None
    skip_host_key_checking: bool
    ansible_binary: str


def parse_args(argv: Iterable[str] | None = None) -> RunnerArgs:
    parser = argparse.ArgumentParser(description="Run ansible playbook for a single VPN node")
    parser.add_argument("--playbook", required=True)
    parser.add_argument("--host", required=True)
    parser.add_argument("--ssh-user", required=True)
    parser.add_argument("--ssh-port", required=True, type=int)
    parser.add_argument("--private-key-path")
    parser.add_argument("--extra-vars-file")
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--workdir", required=True)
    parser.add_argument("--inventory-name", default="vpn-node")
    parser.add_argument("--python-interpreter", default="/usr/bin/python3")
    parser.add_argument("--known-hosts-path")
    parser.add_argument("--skip-host-key-checking", action="store_true")
    parser.add_argument("--ansible-binary", default="ansible-playbook")

    ns = parser.parse_args(list(argv) if argv is not None else None)
    return RunnerArgs(
        playbook=ns.playbook,
        host=ns.host,
        ssh_user=ns.ssh_user,
        ssh_port=ns.ssh_port,
        private_key_path=ns.private_key_path,
        extra_vars_file=ns.extra_vars_file,
        check=ns.check,
        workdir=ns.workdir,
        inventory_name=ns.inventory_name,
        python_interpreter=ns.python_interpreter,
        known_hosts_path=ns.known_hosts_path,
        skip_host_key_checking=ns.skip_host_key_checking,
        ansible_binary=ns.ansible_binary,
    )


def build_inventory_content(args: RunnerArgs) -> str:
    parts = [
        f"ansible_host={args.host}",
        f"ansible_user={args.ssh_user}",
        f"ansible_port={args.ssh_port}",
        f"ansible_python_interpreter={args.python_interpreter}",
    ]

    if args.private_key_path:
        parts.append(f"ansible_ssh_private_key_file={args.private_key_path}")

    common_args: list[str] = []
    if args.skip_host_key_checking:
        common_args.extend([
            "-o StrictHostKeyChecking=no",
            "-o UserKnownHostsFile=/dev/null",
        ])
    elif args.known_hosts_path:
        common_args.append(f"-o UserKnownHostsFile={args.known_hosts_path}")

    if common_args:
        escaped = " ".join(common_args).replace('"', '\\"')
        parts.append(f'ansible_ssh_common_args="{escaped}"')

    return "[vpn_nodes]\n" + args.inventory_name + " " + " ".join(parts) + "\n"


def build_command(args: RunnerArgs, inventory_path: str) -> list[str]:
    command = [args.ansible_binary, "--inventory", inventory_path, args.playbook]
    if args.extra_vars_file:
        command.extend(["--extra-vars", f"@{args.extra_vars_file}"])
    if args.check:
        command.append("--check")
    return command


def execute(args: RunnerArgs) -> RunnerResult:
    workdir = Path(args.workdir)
    workdir.mkdir(parents=True, exist_ok=True)

    playbook = Path(args.playbook)
    if not playbook.exists():
        raise FileNotFoundError(f"Playbook not found: {args.playbook}")

    inventory_path = workdir / "inventory.ini"
    inventory_path.write_text(build_inventory_content(args), encoding="utf-8")

    command = build_command(args, str(inventory_path))

    env = os.environ.copy()
    if args.skip_host_key_checking:
        env["ANSIBLE_HOST_KEY_CHECKING"] = "False"

    completed = subprocess.run(
        command,
        cwd=workdir,
        env=env,
        capture_output=True,
        text=True,
        check=False,
    )

    steps = [
        StepResult(stepName="inventory", success=True, output=str(inventory_path)),
        StepResult(
            stepName="ansible-playbook",
            success=completed.returncode == 0,
            output=completed.stdout,
            errorText=completed.stderr or None,
        ),
    ]

    success = completed.returncode == 0
    summary = f"Ansible playbook {'succeeded' if success else 'failed'} for host {args.host}."
    error_text = None if success else (completed.stderr or completed.stdout or f"exit code {completed.returncode}")

    return RunnerResult(
        success=success,
        summaryLog=summary,
        workDirectory=str(workdir),
        steps=steps,
        errorText=error_text,
    )


def main(argv: Iterable[str] | None = None) -> int:
    try:
        args = parse_args(argv)
        result = execute(args)
    except Exception as exc:  # pragma: no cover - exercised by runtime path
        fallback = RunnerResult(
            success=False,
            summaryLog="Ansible runner crashed before playbook execution.",
            workDirectory=str(Path.cwd()),
            steps=[StepResult(stepName="runner", success=False, output="", errorText=str(exc))],
            errorText=str(exc),
        )
        print(json.dumps(asdict(fallback), ensure_ascii=False))
        return 1

    print(json.dumps(asdict(result), ensure_ascii=False))
    return 0 if result.success else 1


if __name__ == "__main__":
    raise SystemExit(main())
