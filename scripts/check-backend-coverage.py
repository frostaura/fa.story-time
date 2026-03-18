#!/usr/bin/env python3
from __future__ import annotations

import glob
import pathlib
import sys
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[1]
THRESHOLD = 0.80


def main() -> int:
    reports = glob.glob(str(ROOT / "src/backend/**/TestResults/**/coverage.cobertura.xml"), recursive=True)
    if not reports:
        print("No backend coverage files found.", file=sys.stderr)
        return 1

    best_rate: float | None = None
    for report in reports:
        root = ET.parse(report).getroot()
        for package in root.findall(".//package"):
            if package.attrib.get("name") != "StoryTime.Api":
                continue

            rate = float(package.attrib.get("line-rate", "0"))
            best_rate = rate if best_rate is None else max(best_rate, rate)

    if best_rate is None:
        print("No StoryTime.Api package line-rate found in coverage report.", file=sys.stderr)
        return 1

    print(f"StoryTime.Api line coverage: {best_rate:.2%}")
    if best_rate < THRESHOLD:
        print(
            f"Coverage below threshold: required {THRESHOLD:.0%}, got {best_rate:.2%}.",
            file=sys.stderr,
        )
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
