#!/usr/bin/env python3
"""Prepare a coarse 8-label NeuroSync training view of Google Research GoEmotions.

Downloads the official simplified train/dev/test TSV files at build time.
Raw source data is not committed to this repository.

Only labels with a defensible mapping to NeuroSync's current taxonomy are kept.
Examples whose labels map to multiple NeuroSync classes are dropped.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import pathlib
import urllib.request

BASE = "https://raw.githubusercontent.com/google-research/google-research/master/goemotions/data"
URLS = {
    "labels": f"{BASE}/emotions.txt",
    "train": f"{BASE}/train.tsv",
    "dev": f"{BASE}/dev.tsv",
    "test": f"{BASE}/test.tsv",
}

COARSE = {
    "anger": "angry",
    "annoyance": "frustrated",
    "fear": "anxious",
    "nervousness": "anxious",
    "sadness": "sad",
    "grief": "sad",
    "remorse": "sad",
    "disappointment": "sad",
    "joy": "happy",
    "gratitude": "happy",
    "amusement": "happy",
    "love": "happy",
    "excitement": "excited",
    "relief": "calm",
    "neutral": "neutral",
}


def download(url: str) -> str:
    req = urllib.request.Request(url, headers={"User-Agent": "NeuroSync-training/1.0"})
    with urllib.request.urlopen(req, timeout=60) as response:
        return response.read().decode("utf-8")


def stable_order(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def prepare_split(raw: str, labels: list[str], max_per_label: int) -> tuple[list[tuple[str, str]], dict[str, int]]:
    buckets: dict[str, dict[str, str]] = {}

    for line in raw.splitlines():
        if not line.strip():
            continue
        parts = line.split("\t")
        if len(parts) < 2:
            continue

        text = parts[0].strip()
        try:
            ids = [int(x) for x in parts[1].split(",") if x.strip()]
        except ValueError:
            continue

        mapped = {COARSE[labels[i]] for i in ids if 0 <= i < len(labels) and labels[i] in COARSE}
        # Keep only examples whose accepted annotations agree on one NeuroSync class.
        if len(mapped) != 1:
            continue

        label = next(iter(mapped))
        key = " ".join(text.lower().split())
        if not key:
            continue
        buckets.setdefault(label, {})[key] = text

    rows: list[tuple[str, str]] = []
    counts: dict[str, int] = {}
    for label in sorted(buckets):
        ordered = sorted(buckets[label].values(), key=stable_order)
        if max_per_label > 0:
            ordered = ordered[:max_per_label]
        counts[label] = len(ordered)
        rows.extend((text, label) for text in ordered)

    rows.sort(key=lambda item: stable_order(item[1] + "\0" + item[0]))
    return rows, counts


def write_csv(path: pathlib.Path, rows: list[tuple[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(["Text", "Label"])
        writer.writerows(rows)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-dir", required=True)
    parser.add_argument("--train-cap", type=int, default=5000)
    parser.add_argument("--dev-cap", type=int, default=1000)
    parser.add_argument("--test-cap", type=int, default=1000)
    args = parser.parse_args()

    output = pathlib.Path(args.output_dir)
    labels = [x.strip() for x in download(URLS["labels"]).splitlines() if x.strip()]

    for split, cap in (("train", args.train_cap), ("dev", args.dev_cap), ("test", args.test_cap)):
        rows, counts = prepare_split(download(URLS[split]), labels, cap)
        write_csv(output / f"goemotions-{split}.csv", rows)
        summary = ", ".join(f"{k}={v}" for k, v in sorted(counts.items()))
        print(f"{split}: {len(rows)} rows | {summary}")

    (output / "SOURCE.txt").write_text(
        "Google Research GoEmotions\n"
        "https://github.com/google-research/google-research/tree/master/goemotions\n"
        "Dataset license: CC BY 4.0\n"
        "NeuroSync transformation: selected and mapped labels to the product's 8-label coarse taxonomy.\n",
        encoding="utf-8",
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
