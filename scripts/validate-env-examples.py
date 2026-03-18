#!/usr/bin/env python3
from __future__ import annotations

import pathlib
import sys
import json

ROOT = pathlib.Path(__file__).resolve().parents[1]
BACKEND_APPSETTINGS = ROOT / "src/backend/StoryTime.Api/appsettings.json"
FRONTEND_ENV_PATHS = [
    ROOT / "src/frontend/.env.example",
    ROOT / "src/frontend/.env.development",
    ROOT / "src/frontend/.env.test",
    ROOT / "src/frontend/.env.production",
]

EXPECTED = {
    ROOT / ".env.example": {
        "API_PORT",
        "FRONTEND_PORT",
        "FRONTEND_API_BASE_URL",
        "STORYTIME_PARENT_STATE_FILE_PATH",
        "STORYTIME_SUBSCRIPTION_STATE_FILE_PATH",
        "STORYTIME_CHECKOUT_WEBHOOK_SHARED_SECRET",
        "STORYTIME_REQUIRE_USER_VERIFICATION",
        "STORYTIME_POSTER_PROVIDER_ENDPOINT",
        "STORYTIME_NARRATION_PROVIDER_ENDPOINT",
        "STORYTIME_AI_PROVIDER_ENDPOINT",
        "STORYTIME_CHECKOUT_PROVIDER_ENDPOINT",
    },
    ROOT / "src/frontend/.env.example": {
        "VITE_API_BASE_URL",
        "VITE_STORAGE_KEY_STORY_ARTIFACTS",
        "VITE_STORAGE_KEY_SOFT_USER_ID",
        "VITE_STORAGE_KEY_CHILD_PROFILE",
        "VITE_STORAGE_KEY_PARENT_CREDENTIAL",
        "VITE_STORAGE_KEY_PENDING_CHECKOUT",
    },
}


def parse_env(path: pathlib.Path) -> dict[str, str]:
    values: dict[str, str] = {}
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, _ = line.split("=", 1)
        values[key.strip()] = _.strip()
    return values


def backend_runtime_mirrors() -> dict[str, str]:
    appsettings = json.loads(BACKEND_APPSETTINGS.read_text(encoding="utf-8"))
    story_time = appsettings["StoryTime"]
    api_routes = story_time["ApiRoutes"]
    ui = story_time["Ui"]

    library_route = api_routes["Library"]
    library_base = library_route.rsplit("/{softUserId}", 1)[0]

    return {
        "VITE_API_ROUTE_HOME_STATUS": api_routes["HomeStatus"],
        "VITE_API_ROUTE_STORIES_BASE": api_routes["StoriesGenerate"].rsplit("/generate", 1)[0],
        "VITE_API_ROUTE_STORIES_GENERATE": api_routes["StoriesGenerate"],
        "VITE_API_ROUTE_SUBSCRIPTION_BASE": api_routes["SubscriptionWebhook"].rsplit("/webhook", 1)[0],
        "VITE_API_ROUTE_PARENT_BASE": api_routes["ParentSettings"].rsplit("/{softUserId}/settings", 1)[0],
        "VITE_API_ROUTE_LIBRARY_BASE": library_base,
        "VITE_DEFAULT_DURATION_MINUTES": str(ui["DurationMinMinutes"]),
        "VITE_DEFAULT_DURATION_MAX_MINUTES": str(ui["DurationMaxMinutes"]),
        "VITE_DEFAULT_DURATION_SELECTION": str(ui["DurationDefaultMinutes"]),
        "VITE_DEFAULT_CHILD_NAME": ui["DefaultChildName"],
        "VITE_HOME_STATUS_QUICK_GENERATE_VISIBLE": str(ui["QuickGenerateVisible"]).lower(),
        "VITE_HOME_STATUS_DURATION_SLIDER_VISIBLE": str(ui["DurationSliderVisible"]).lower(),
        "VITE_HOME_STATUS_PARENT_CONTROLS_ENABLED": str(ui["ParentControlsEnabled"]).lower(),
    }


def main() -> int:
    failures: list[str] = []
    for path, expected_keys in EXPECTED.items():
        if not path.exists():
            failures.append(f"{path.relative_to(ROOT)} is missing")
            continue
        actual_keys = set(parse_env(path))
        for key in sorted(expected_keys - actual_keys):
            failures.append(f"{path.relative_to(ROOT)} is missing {key}")

    mirrored_values = backend_runtime_mirrors()
    for path in FRONTEND_ENV_PATHS:
        if not path.exists():
            failures.append(f"{path.relative_to(ROOT)} is missing")
            continue

        actual_values = parse_env(path)
        for key, expected_value in mirrored_values.items():
            actual_value = actual_values.get(key)
            if actual_value is None:
                failures.append(f"{path.relative_to(ROOT)} is missing {key}")
                continue
            if actual_value != expected_value:
                failures.append(
                    f"{path.relative_to(ROOT)} has {key}={actual_value!r}, expected {expected_value!r} to match backend config"
                )

    if failures:
        print("Env example validation failed:")
        for failure in failures:
            print(f"- {failure}")
        return 1

    print("Env example validation passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
