# Email Agent Learning

Cursor plugin that builds a local `EMAIL-AGENT.md` knowledge file by mining agent conversation transcripts. Every 10 agent turns it scans new transcripts, extracts email domain learnings, and merges them into the document.

`EMAIL-AGENT.md` lives only in your workspace. It is never committed or shared.

## How it works

Three pieces work together:

| Piece | Role |
|-------|------|
| **`update-email-agent` command** | Delegates to a subagent that scans all new transcripts and updates `EMAIL-AGENT.md`. Run from the command palette anytime. |
| **`email-agent-learning` skill file** | Defines the document structure, extraction rules, merge rules, and slop filter. Read by the subagent at runtime. |
| **`stop` hook** | Counts completed agent turns; when the threshold is reached, tells the main agent to launch a subagent for the update. |

All processing happens inside a **subagent** via the Task tool. The main conversation context window is not affected.

## What goes into EMAIL-AGENT.md

Seven sections, cumulative and never reset:

| Section | What it captures |
|---------|-----------------|
| Infrastructure | CIO, SendAPI, Azure, Parcel.io, environments, API constraints |
| Email Projects | Campaigns, transactionals, templates — location, status, languages |
| Tools & Apps | Every script and app in the workspace, what it does, how to invoke it |
| Workflows | Repeatable processes: deploy, QA, export, backtracking, data collection |
| Patterns & Conventions | Liquid patterns, HTML structure, naming rules, conditional rendering |
| Decisions | Design choices and the reasoning behind them |
| Recent Context | Rolling summary of active work and open threads |

## Prerequisites

The hook script runs with [Bun](https://bun.sh/). Ensure `bun` is available on `PATH`.

## State files

| File | Purpose |
|------|---------|
| `.cursor/hooks/state/email-agent-learning.json` | Turn counter and last-run timestamp |
| `.cursor/hooks/state/email-agent-learning-index.json` | Processed transcript mtimes |

Both are workspace-local. Add `EMAIL-AGENT.md` to your workspace `.gitignore` to keep learnings private.

## Trigger cadence

| Setting | Default |
|---------|---------|
| Minimum turns | 10 |
| Minimum minutes between runs | 1 |

Both conditions must be met and at least one transcript must have advanced since the previous run.

## Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `EMAIL_AGENT_LEARNING_MIN_TURNS` | Minimum completed turns before triggering | 10 |
| `EMAIL_AGENT_LEARNING_MIN_MINUTES` | Minimum minutes between runs | 1 |

## License

MIT
