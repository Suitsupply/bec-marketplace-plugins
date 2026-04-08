---
name: update-repository-readme
description: >-
  Sync repository readme.md from git changes and Cursor agent transcripts using
  the incremental index. Runs in a subagent to preserve your context window.
---

# Update repository README

Delegate this to a subagent so the work does not consume your context window.

Use the **Task** tool with:
- `subagent_type`: `"generalPurpose"`
- `description`: `"Update repository README"`
- `prompt`: the instructions below

## Subagent prompt

> Read the continuous-documentation skill at `<CURSOR_PLUGIN_ROOT>/skills/continuous-documentation/SKILL.md` for the complete rules (README structure, slop filter, inclusion bar, exclusions, intent guidance).
>
> Then execute:
>
> 1. Read the existing `readme.md` at the workspace root.
> 2. Load the incremental index from `.cursor/hooks/state/continuous-documentation-index.json` if it exists.
> 3. Run `git log --oneline` from the last indexed commit to HEAD. For commits touching documentation-relevant areas, run `git diff`.
> 4. Scan agent-transcripts for files with mtime newer than indexed. Extract stated reasoning and correlate with git changes.
> 5. Merge What (git) + Why (transcripts) into `readme.md` updates following every rule in the skill.
> 6. Write back the incremental index with the latest commit SHA and transcript mtimes.
>
> If no meaningful documentation updates exist, respond exactly: `No documentation updates needed.`

Replace `<CURSOR_PLUGIN_ROOT>` with the actual plugin cache path visible in the skill listing or derived from the plugin installation.
