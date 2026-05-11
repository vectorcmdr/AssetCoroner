---
layout: default
title: Configuration
nav_order: 3
---

# Configuration Reference

AssetCoroner is configured via `.github/assetcoroner.yml` in your repository.
All settings are optional - defaults are used if the file is absent.

```yaml
audit:
  enabled: true                    # Enable/disable the Audit engine
  schedule: "0 3 * * 1"            # Cron schedule for periodic audits
  warn_threshold_mb: 5             # Warn when a file exceeds this size (MB)
  critical_threshold_mb: 25        # Fail when a file exceeds this size (MB)
  lfs_recommend_threshold_mb: 10   # Recommend LFS for files above this size
  report_commit: false             # Commit audit report to repo

scan:
  enabled: true                    # Enable/disable the Scan engine (Unity refs)
  fail_on_broken_refs: true        # Fail check run on broken GUID references
  warn_on_orphaned_meta: true      # Warn on orphaned .meta files

review:
  enabled: true                    # Enable/disable the Review engine
  post_comment: true               # Post PR comment with binary delta table
  block_on_critical: false         # Block PR merge on critical binary size delta
  critical_delta_mb: 50            # Critical threshold for binary delta (MB)
  extensions:                      # File extensions treated as binary assets
    - .fbx
    - .obj
    - .stl
    - .prefab
    - .unity
    - .wav
    - .png
    - .psd
    - .unitypackage
```

## Audit settings

`warn_threshold_mb` and `critical_threshold_mb` control which files appear in the "Files Exceeding Size Thresholds" table in the audit report. Files above the warn threshold produce a Warning conclusion. Files above the critical threshold produce a Failure conclusion and will fail the Check Run.

`lfs_recommend_threshold_mb` sets the size above which a file is listed in the "Git LFS Recommendations" section of the audit report. This is independent of the warn/critical thresholds and does not affect the check conclusion on its own.

`report_commit` controls whether the Audit engine commits the generated Markdown report back to the repository on the default branch. This is disabled by default.

## Scan settings

`fail_on_broken_refs` controls whether the Scan Check Run is marked as failed when broken GUID references are found. When set to `false`, broken references are still reported but the check is marked as neutral rather than failed, so it will not block a protected branch merge rule.

`warn_on_orphaned_meta` controls whether `.meta` files that have no corresponding asset file are reported as warnings.

## Review settings

`post_comment` controls whether the Review engine posts a PR comment. The GitHub Check Run is always created regardless of this setting.

`block_on_critical` and `critical_delta_mb` work together. When `block_on_critical` is `true`, any PR that adds binary assets totalling more than `critical_delta_mb` megabytes will have its Check Run marked as failed, which can block merge if that check is required.

`extensions` overrides the built-in list of file extensions that the Review engine tracks. When this list is provided, only files with matching extensions are included in the delta table and delta total. Use this to focus the review on specific asset types or to add project-specific extensions.

