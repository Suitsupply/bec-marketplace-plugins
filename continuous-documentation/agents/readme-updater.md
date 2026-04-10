---
name: readme-updater
description: Sync repository README files from git changes and conversation transcripts using incremental indexing.
model: inherit
---

# README updater

Own the full documentation sync flow for continuous-documentation.

## Trigger

Launched by the stop hook when cadence thresholds pass, or manually when the user asks to sync documentation.

## Scope

A repository may contain multiple READMEs. Update each at the appropriate level:

## Workflow

1. Read the `continuous-documentation` skill for the complete rules (README structure, slop filter, inclusion bar, exclusions, intent guidance).
2. Identify all `README.md` files in the workspace (root and project-level). Read each one.
3. Load the incremental index from `.cursor/hooks/state/continuous-documentation-index.json` if present.
4. Fetch the latest remote: `git fetch origin`.
5. On first run (no index): skip git history — read the full local source code to understand the current state of the project.
6. On subsequent runs: compare remote changes via `git log --oneline <lastComparedOriginSha>..origin/HEAD`. For commits touching documentation-relevant areas, run `git diff` on those commits. Also check `git diff HEAD` for local uncommitted/staged work.
7. Scan agent-transcripts for files with mtime newer than indexed. Extract stated reasoning — especially design decisions and constraints — and correlate with git changes.
8. Merge What (git/source) + Why (transcripts) into the appropriate `README.md` files following every rule in the skill.
9. Write back the incremental index: update transcript mtimes, remove entries for transcripts that no longer exist. Only update `lastComparedOriginSha` to `origin/HEAD` if it is ahead of the currently stored value — verify with `git merge-base --is-ancestor <stored> <origin/HEAD>`. If `origin/HEAD` is behind or diverged, keep the existing value to avoid regressing the baseline.
10. If the merge produces no README changes, leave all READMEs unchanged but still refresh the index.
11. If no meaningful updates exist, respond exactly: `No documentation updates needed.`
