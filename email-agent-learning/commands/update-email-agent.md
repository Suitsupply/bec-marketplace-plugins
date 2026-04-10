---
name: update-email-agent
description: >-
  Scan agent transcripts for email domain learnings and update the local
  EMAIL-AGENT.md knowledge file. Runs in a subagent to preserve your context window.
---

# Update EMAIL-AGENT.md

Delegate this to a subagent so the work does not consume your context window.

Use the **Task** tool with:
- `subagent_type`: `"generalPurpose"`
- `description`: `"Update EMAIL-AGENT.md"`
- `prompt`: the instructions below

## Subagent prompt

> Read the email-agent-learning skill at `<CURSOR_PLUGIN_ROOT>/skills/email-agent-learning/SKILL.md` for the complete rules (document structure, extraction rules, merge rules, slop filter, exclusions).
>
> Then execute:
>
> 1. Read the existing `EMAIL-AGENT.md` at the workspace root. If it does not exist, create it from scratch using the section structure defined in the skill.
> 2. Load the incremental index from `.cursor/hooks/state/email-agent-learning-index.json` if it exists.
> 3. List all transcript files in the agent-transcripts folder. Find files with mtime newer than the last indexed mtime (or all files if no index exists).
> 4. For each new transcript: read it and extract learnings following the extraction rules in the skill. Focus on email infrastructure, projects, tools, workflows, patterns, and decisions.
> 5. Merge extracted learnings into `EMAIL-AGENT.md` following the merge rules in the skill. Apply the slop filter to every sentence written.
> 6. Write back the incremental index with updated transcript mtimes.
>
> If no new transcripts contain extractable learnings, respond exactly: `No learnings to add.`

Replace `<CURSOR_PLUGIN_ROOT>` with the actual plugin cache path visible in the skill listing or derived from the plugin installation.
