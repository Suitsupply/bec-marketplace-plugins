# Continuous Documentation

Cursor plugin that keeps the repository `README.md` current by mining conversation transcripts for documentation-worthy changes, capturing intent and reasoning behind decisions.

## How it works

Three pieces work together:

| Piece | Role |
|--------|------|
| **`update-repository-readme` command** | Delegates to a subagent that runs the full workflow: git + transcripts + incremental index → update `README.md`. Run from the command palette anytime; does not require the hook. |
| **`continuous-documentation` skill file** | README structure, inclusion/exclusion, slop filter, and intent guidance. Read by the subagent at runtime — not loaded into the main agent's context. |
| **`stop` hook** | Tracks conversation cadence; when thresholds pass, tells the main agent to launch a subagent. |

All heavy processing (reading git history, scanning transcripts, updating the README) happens inside a **subagent** launched via the Task tool. This keeps the main conversation context window clean.

Two sources of truth feed the sync:

- **Git history** — what changed (`git log`, `git diff` since the last indexed commit).
- **Conversation transcripts** — why it changed (reasoning, alternatives, constraints).

Processing is incremental — only new commits and changed transcripts are evaluated on each run.

## Prerequisites

The hook script runs with [Bun](https://bun.sh/). Ensure `bun` is available on `PATH`.

## State files

| File | Purpose |
|------|---------|
| `.cursor/hooks/state/continuous-documentation.json` | Cadence state (turn count, last run time, transcript mtime) |
| `.cursor/hooks/state/continuous-documentation-index.json` | Incremental index (last commit SHA, processed transcript mtimes) |

Both are workspace-local state files.

## Trigger cadence

| Setting | Default | Trial mode |
|---------|---------|------------|
| Minimum turns | 20 | 6 |
| Minimum minutes | 240 | 30 |
| Trial duration | — | 24 hours |

All conditions must be met simultaneously. Transcript mtime must also advance since the previous run.

## Configuration

All settings are optional environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `CONTINUOUS_DOCUMENTATION_MIN_TURNS` | Minimum completed turns before triggering | 20 |
| `CONTINUOUS_DOCUMENTATION_MIN_MINUTES` | Minimum minutes between runs | 240 |
| `CONTINUOUS_DOCUMENTATION_TRIAL_MODE` | Enable reduced thresholds for a trial window | false |
| `CONTINUOUS_DOCUMENTATION_TRIAL_MIN_TURNS` | Minimum turns in trial mode | 6 |
| `CONTINUOUS_DOCUMENTATION_TRIAL_MIN_MINUTES` | Minimum minutes in trial mode | 30 |
| `CONTINUOUS_DOCUMENTATION_TRIAL_DURATION_MINUTES` | Trial window length in minutes | 1440 |

## License

MIT
