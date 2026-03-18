#!/usr/bin/env python3
from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
TRACEABILITY_MATRIX = ROOT / "docs/specs/traceability_matrix.md"
TESTING_README = ROOT / "docs/testing/README.md"
TESTING_HOW_TO_RUN = ROOT / "docs/testing/how-to-run.md"
FLAKY_TEMPLATE = ROOT / ".github/ISSUE_TEMPLATE/flaky-test.yml"
COMPLETION_INDEX = ROOT / "docs/testing/TEST-003-completion-evidence-index.md"
PATH_PREFIXES = (
    "src/",
    "docs/",
    "scripts/",
    ".github/",
    "Makefile",
    "docker-compose.yml",
    "README.md",
    "ROADMAP.md",
    "COMPLETION_REPORT.md",
    "IMPLEMENTATION_SUMMARY.md",
)


def extract_backtick_paths(cell: str) -> list[pathlib.Path]:
    candidates: list[pathlib.Path] = []
    for match in re.findall(r"`([^`]+)`", cell):
        token = match.strip()
        if token.startswith("/") or "://" in token or " " in token:
            continue
        if not token.startswith(PATH_PREFIXES):
            continue
        candidates.append(ROOT / token)

    return candidates


def main() -> int:
    missing: list[str] = []

    for required_path in (TRACEABILITY_MATRIX, TESTING_README, TESTING_HOW_TO_RUN, FLAKY_TEMPLATE, COMPLETION_INDEX):
        if not required_path.exists():
            missing.append(str(required_path.relative_to(ROOT)))

    if TRACEABILITY_MATRIX.exists():
        lines = TRACEABILITY_MATRIX.read_text(encoding="utf-8").splitlines()
        rows = [line for line in lines if line.startswith("| ") and "`" in line]
        for row in rows:
            cells = [part.strip() for part in row.strip().strip("|").split("|")]
            if len(cells) < 4:
                continue
            for path in extract_backtick_paths(cells[2]) + extract_backtick_paths(cells[3]):
                if not path.exists():
                    missing.append(str(path.relative_to(ROOT)))

    testing_readme_text = TESTING_README.read_text(encoding="utf-8") if TESTING_README.exists() else ""
    if "TEST-003-completion-evidence-index.md" not in testing_readme_text:
        missing.append("docs/testing/README.md -> TEST-003 reference")

    how_to_run_text = TESTING_HOW_TO_RUN.read_text(encoding="utf-8") if TESTING_HOW_TO_RUN.exists() else ""
    if "make traceability" not in how_to_run_text:
        missing.append("docs/testing/how-to-run.md -> make traceability reference")

    if missing:
        print("Traceability check failed:")
        for item in sorted(set(missing)):
            print(f"- missing: {item}")
        return 1

    print("Traceability check passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
