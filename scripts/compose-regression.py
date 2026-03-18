#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid


def request_json(method: str, url: str, body: dict | None = None, headers: dict[str, str] | None = None) -> tuple[int, dict]:
    encoded = None if body is None else json.dumps(body).encode("utf-8")
    request = urllib.request.Request(url, data=encoded, method=method)
    request.add_header("Content-Type", "application/json")
    for key, value in (headers or {}).items():
        request.add_header(key, value)

    try:
        with urllib.request.urlopen(request, timeout=20) as response:
            payload = response.read().decode("utf-8")
            return response.status, json.loads(payload) if payload else {}
    except urllib.error.HTTPError as error:
        payload = error.read().decode("utf-8")
        return error.code, json.loads(payload) if payload else {}


def request_text(url: str) -> tuple[int, str]:
    try:
        with urllib.request.urlopen(url, timeout=20) as response:
            return response.status, response.read().decode("utf-8")
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode("utf-8")


def expect(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    parser = argparse.ArgumentParser(description="Compose-backed regression checks for StoryTime.")
    parser.add_argument("--api-base", default="http://localhost:8080", help="Base URL for the API service.")
    parser.add_argument("--frontend-base", default="http://localhost:4173", help="Base URL for the frontend service.")
    parser.add_argument("--webhook-secret", default="", help="Webhook secret for premium upgrade coverage.")
    args = parser.parse_args()

    soft_user_id = f"compose-{uuid.uuid4().hex[:12]}"
    api_base = args.api_base.rstrip("/")
    frontend_base = args.frontend_base.rstrip("/")

    status, html = request_text(frontend_base)
    expect(status == 200 and "StoryTime" in html, "Frontend root did not return the StoryTime shell.")

    status, home = request_json("GET", f"{api_base}/api/home/status")
    expect(status == 200, "Home status did not return HTTP 200.")
    expect(home.get("quickGenerateVisible") is True, "Quick Generate should be visible.")

    status, unauthorized_webhook = request_json(
        "POST",
        f"{api_base}/api/subscription/webhook",
        {
            "softUserId": soft_user_id,
            "tier": "Premium",
            "resetCooldown": True,
        },
    )
    expect(status == 401, "Webhook without secret must be rejected.")
    expect(unauthorized_webhook == {}, "Unauthorized webhook should not return a success payload.")

    status, story = request_json(
        "POST",
        f"{api_base}/api/stories/generate",
        {
            "softUserId": soft_user_id,
            "childName": "Ari",
            "mode": "series",
            "durationMinutes": 6,
            "favorite": False,
            "reducedMotion": True,
        },
    )
    expect(status == 200, "Series generate flow failed.")
    story_id = story["storyId"]

    status, approval = request_json(
        "POST",
        f"{api_base}/api/stories/{urllib.parse.quote(story_id)}/approve",
        {
            "softUserId": soft_user_id,
            "gateToken": "missing-gate-token",
        },
    )
    expect(status == 401, "Story approval should reject a missing or invalid parent gate token.")
    expect(approval == {}, "Unauthorized story approval should not return an unlock payload.")

    status, _ = request_json(
        "PUT",
        f"{api_base}/api/stories/{urllib.parse.quote(story_id)}/favorite",
        {"isFavorite": True},
    )
    expect(status == 200, "Favorite update failed.")

    status, library = request_json("GET", f"{api_base}/api/library/{urllib.parse.quote(soft_user_id)}")
    expect(status == 200, "Library read failed.")
    expect(any(item["storyId"] == story_id for item in library.get("recent", [])), "Recent shelf is missing the generated story.")
    expect(any(item["storyId"] == story_id for item in library.get("favorites", [])), "Favorites shelf is missing the favorited story.")

    status, paywall = request_json(
        "POST",
        f"{api_base}/api/stories/generate",
        {
            "softUserId": soft_user_id,
            "childName": "Ari",
            "mode": "series",
            "durationMinutes": 15,
            "favorite": False,
            "reducedMotion": False,
        },
    )
    expect(status == 402, "Trial paywall boundary did not return HTTP 402.")
    expect("paywall" in paywall and paywall["paywall"]["upgradeTier"] in {"Plus", "Premium"}, "Paywall metadata is incomplete.")

    status, _ = request_json(
        "POST",
        f"{api_base}/api/subscription/{urllib.parse.quote(soft_user_id)}/checkout/session",
        {
            "gateToken": "missing-gate-token",
            "upgradeTier": "Plus",
            "returnUrl": f"{frontend_base}/#checkout",
        },
    )
    expect(status == 401, "Checkout session should reject an unauthorized gate token.")

    if args.webhook_secret:
        status, _ = request_json(
            "POST",
            f"{api_base}/api/subscription/webhook",
            {
                "softUserId": soft_user_id,
                "tier": "Premium",
                "resetCooldown": True,
            },
            headers={"X-StoryTime-Webhook-Secret": args.webhook_secret},
        )
        expect(status == 200, "Premium webhook upgrade failed.")

        status, premium_story = request_json(
            "POST",
            f"{api_base}/api/stories/generate",
            {
                "softUserId": soft_user_id,
                "childName": "Ari",
                "mode": "series",
                "durationMinutes": 15,
                "favorite": False,
                "reducedMotion": True,
            },
        )
        expect(status == 200, "Premium entitlement did not unlock long-form generation.")
        time.sleep(0.1)
        status, _ = request_json(
            "POST",
            f"{api_base}/api/stories/{urllib.parse.quote(premium_story['storyId'])}/approve",
            {
                "softUserId": soft_user_id,
                "gateToken": "missing-gate-token",
            },
        )
        expect(status == 401, "Premium long-form approval should still require a parent gate token.")

    status, audit = request_json("GET", f"{api_base}/api/library/{urllib.parse.quote(soft_user_id)}/storage-audit")
    expect(status == 200, "Storage audit failed.")
    expect(audit.get("containsNarrativeText") is False, "Storage audit detected narrative text leakage.")
    expect(audit.get("containsNarrativeAudioPayload") is False, "Storage audit detected audio payload leakage.")

    print(json.dumps({
        "frontend": frontend_base,
        "api": api_base,
        "softUserId": soft_user_id,
        "storyId": story_id,
        "premiumUpgradeCovered": bool(args.webhook_secret),
        "storageAudit": audit,
    }, indent=2))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as error:
        print(f"compose regression failed: {error}", file=sys.stderr)
        raise SystemExit(1)
