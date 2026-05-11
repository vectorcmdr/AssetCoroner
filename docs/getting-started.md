---
layout: default
title: Getting Started
nav_order: 2
---

# Getting Started with AssetCoroner

AssetCoroner is a GitHub Action for game development repositories. It runs three engines automatically based on the type of event that triggers the workflow.

## How each engine is triggered

**Audit** runs on every push to your default branch and on any scheduled cron interval you configure. It scans the full repository tree, classifies binary assets, reports size threshold violations, recommends Git LFS for oversized files, and publishes the results as a GitHub Check Run on the triggering commit.

**Review** runs on every pull request. It compares binary asset files between the PR base and head commits, building a size delta table for each changed asset. It also inspects Unity asset files for binary serialization and flags new assets that should be moved to Git LFS. The results are posted as a PR comment and as a GitHub Check Run.

**Scan** runs on every pull request alongside Review. It builds a complete GUID index from every `.meta` file in the repository at the PR head commit, then checks every changed `.prefab`, `.unity`, `.asset`, and `.mat` file for unresolvable GUID references. Broken references are reported with file path, line number, and GUID. The results are posted as a PR comment and as a GitHub Check Run that can be configured to block merge.

## What you will see

After adding AssetCoroner to your repository, each push to your default branch will produce an "AssetCoroner" check run in the Checks tab of that commit. The check includes the full audit report.

Each pull request will receive two PR comments (one from the Review engine, one from the Scan engine) and two GitHub Check Runs. The check runs appear in the PR status checks area and can be required to pass before merging.

## Installation

Add a workflow file at `.github/workflows/assetcoroner.yml` in your repository:

```yaml
name: AssetCoroner
on:
  push:
    branches: [main]
  pull_request:
  schedule:
    - cron: "0 3 * * 1"

jobs:
  assetcoroner:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      pull-requests: write
      checks: write
    steps:
      - uses: actions/checkout@v4
      - uses: {{ site.action_ref }}
        with:
          github-token: ${{ secrets.GITHUB_TOKEN }}
```

No external accounts, no App installation, no secrets beyond the standard
runner-provided `GITHUB_TOKEN`.

## Configuration

Create `.github/assetcoroner.yml` in your repository to customise behaviour.
See [Configuration Reference](configuration).
