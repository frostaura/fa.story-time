#!/usr/bin/env python3
from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
TEST_SUFFIXES = (".test.ts", ".test.tsx", ".spec.ts", ".spec.tsx", ".test.js", ".spec.js", ".cs")
PATTERNS = (
    ("focused", re.compile(r"\.only\(")),
    ("skipped", re.compile(r"\.skip\(")),
)


def main() -> int:
    violations: list[str] = []

    for path in (ROOT / "src").rglob("*"):
        if not path.is_file() or not str(path).endswith(TEST_SUFFIXES):
            continue

        content = path.read_text(encoding="utf-8")
        for label, pattern in PATTERNS:
            for match in pattern.finditer(content):
                line = content.count("\n", 0, match.start()) + 1
                violations.append(f"{path.relative_to(ROOT)}:{line}: {label} test marker")

    if violations:
        print("Test governance violations detected:")
        for violation in violations:
            print(violation)
        return 1

    print("Test governance check passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
