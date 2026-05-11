---
layout: default
title: Action Setup
nav_order: 3
---

# Action Setup

AssetCoroner runs as a GitHub Action inside GitHub's own runner infrastructure.
No external server, no Docker, no GitHub App installation required.

## Basic Setup

```yaml
name: AssetCoroner
on:
  push:
    branches: [main]
  pull_request:
    types: [opened, synchronize, reopened]
  schedule:
    - cron: "0 3 * * 1"

jobs:
  assetcoroner:
    name: AssetCoroner Analysis
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

## Inputs

| Input | Required | Default | Description |
|-------|----------|---------|-------------|
| `github-token` | Yes | `${{ github.token }}` | GitHub token for API access |
| `config-path` | No | `.github/assetcoroner.yml` | Path to config file |

## Permissions Required

- `contents: read`: to read repository files
- `pull-requests: write`: to post PR comments
- `checks: write`: to create check runs

