---
layout: home
title: Home
nav_order: 1
---

# 🔬 AssetCoroner

> *"To observe attentively is to remember distinctly."*

**AssetCoroner** is a GitHub Action purpose-built for game development and 3D asset-heavy repositories.

GitHub's native CI and diff tooling is designed for text. Game development repositories are not. Binary assets like `.fbx`, `.prefab`, `.unity`, `.png`, and `.wav` are opaque to standard diff. Broken assets, runaway repo sizes, and silent Unity reference failures routinely slip through PR review undetected.

**AssetCoroner closes that gap** by performing a thorough post-mortem on every push and pull request.

---

## Three Engines

| Engine | Trigger | What it does |
|--------|---------|--------------|
| **Audit** | Push to default branch | Repository-wide binary asset auditing, bloat detection, and LFS recommendations |
| **Scan** | Pull Request | Unity prefab and scene broken reference and missing GUID detection |
| **Review** | Pull Request | PR-level binary asset change summarisation and structured diff commenting |

---

### Audit

The Audit engine runs on every push to your default branch and on any scheduled interval you configure. It fetches the full recursive Git tree from the GitHub API, classifies every blob by file extension, and produces a repository-wide inventory of binary assets.

What it checks:

- Files that exceed configurable warn and critical size thresholds
- Files that should be moved to Git LFS but are not yet tracked
- The ten largest binary assets in the repository
- Overall binary asset footprint as a percentage of total repository size

Results are published as a GitHub Check Run attached to the triggering commit. If `report_commit` is enabled the report is also committed back to the repository as a Markdown file.

**Example output:**

```
## AssetCoroner: Audit Report

**Status:** Warning

- **Total binary asset size:** 237.45 MB
- **Total repo size:** 312.18 MB
- **Binary asset footprint:** 76.1%
- **Binary assets found:** 143

### Files Exceeding Size Thresholds

| File | Category | Size |
|------|----------|------|
| `Assets/Art/Environments/LargeLevel.unity` | Scene | 52.30 MB |
| `Assets/Art/Characters/HeroRig.fbx` | Mesh | 28.14 MB |
| `Assets/Audio/Music/MainTheme.wav` | Audio | 18.77 MB |

### Git LFS Recommendations

The following files exceed the LFS threshold and are not yet LFS-tracked:

- `Assets/Art/Characters/HeroRig.fbx` (28.14 MB)
- `Assets/Audio/Music/MainTheme.wav` (18.77 MB)

### Top Largest Assets

| Rank | File | Category | Size |
|------|------|----------|------|
| 1 | `Assets/Art/Environments/LargeLevel.unity` | Scene | 52.30 MB |
| 2 | `Assets/Art/Characters/HeroRig.fbx` | Mesh | 28.14 MB |
| 3 | `Assets/Audio/Music/MainTheme.wav` | Audio | 18.77 MB |
| 4 | `Assets/Art/Textures/WorldAtlas.psd` | Texture | 15.60 MB |
| 5 | `Assets/Art/Characters/EnemyRig.fbx` | Mesh | 11.25 MB |
```

---

### Review

The Review engine runs on every pull request. It compares binary assets between the base and head commits and builds a per-file size delta table. For each changed binary asset it reports the change type (new, replacement, or deletion), the previous and new sizes, and the signed byte delta. Unity asset files are inspected to detect whether binary serialization is in use, and a warning is added when text serialization is not enabled. The engine also identifies new assets that should be moved to Git LFS.

Results are posted as a comment on the pull request and as a GitHub Check Run.

**Example output:**

```
## AssetCoroner: Binary Asset Post-Mortem

| File | Category | Change | Previous | New | Delta |
|------|----------|--------|----------|-----|-------|
| `Assets/Art/Characters/Hero_v2.fbx` | Mesh | New | - | 14.22 MB | +14.22 MB |
| `Assets/Art/Environments/TestLevel.unity` | Scene | Replacement | 8.10 MB | 12.40 MB | +4.30 MB |
| `Assets/Audio/OldSFX.wav` | Audio | Deletion | 2.05 MB | - | -2.05 MB |

**Total PR binary delta: +16.47 MB**

> Consider Git LFS for:
> - `Assets/Art/Characters/Hero_v2.fbx`

> The following Unity files are binary-serialized. Consider switching to text
> serialization for proper version control diffing:
> - `Assets/Art/Environments/TestLevel.unity`
```

---

### Scan

The Scan engine runs on every pull request alongside the Review engine. It builds a complete GUID index by reading every `.meta` file in the repository at the PR head commit. It then inspects every changed `.prefab`, `.unity`, `.asset`, and `.mat` file in the PR, extracting GUID references and checking each one against the index. Any reference that cannot be resolved is reported as a broken reference, with the file path, line number, GUID string, and reference kind. Files using binary serialization cannot be read and are listed separately.

Results are posted as a comment on the pull request and as a GitHub Check Run that can be configured to block merge on failure.

**Example output:**

```
## AssetCoroner: Unity Reference Scan

**Status:** Failure

### Broken References

| File | Line | GUID | Kind |
|------|------|------|------|
| `Assets/Prefabs/Enemy.prefab` | 47 | `a1b2c3d4e5f67890a1b2c3d4e5f67890` | MissingGuid |
| `Assets/Scenes/Level01.unity` | 312 | `deadbeef00001234deadbeef00001234` | MissingGuid |

### Binary-Serialized Files (Cannot Inspect)

- `Assets/Prefabs/LegacyBoss.prefab`
```

A passing scan looks like this:

```
## AssetCoroner: Unity Reference Scan

**Status:** Success

No broken Unity references detected.
```

---

## Quick Start

Add a workflow file to your repository:

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
          github-token: ${{ "{{" }} secrets.GITHUB_TOKEN {{ "}}" }}
```

No external accounts, no App installation, no secrets beyond the standard runner-provided `GITHUB_TOKEN`.

---

## Documentation

- [Getting Started](getting-started) - Installation and first steps
- [Configuration Reference](configuration) - All `assetcoroner.yml` settings
- [Action Setup](action-setup) - Workflow file options and inputs
