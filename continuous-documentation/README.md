# Continuous Documentation

Cursor plugin that keeps the repository `README.md` current by mining conversation transcripts for documentation-worthy changes, capturing intent and reasoning behind decisions.

## How it works

Three pieces work together:

| Piece | Role |
|--------|------|
| **`readme-updater` agent** | Subagent that runs the full workflow: git + transcripts + incremental index → update `README.md`. |
| **`continuous-documentation` skill** | README structure, inclusion/exclusion, slop filter, and intent guidance. Read by the agent at runtime — not loaded into the main agent's context. |
| **`stop` hook** | Tracks conversation cadence; when thresholds pass, tells the main agent to launch the `readme-updater` subagent. |

All heavy processing (reading git history, scanning transcripts, updating the README) happens inside a **subagent**. This keeps the main conversation context window clean.

Two sources of truth feed the sync:

- **Git history** — what changed (`git log`, `git diff` since the last indexed commit).
- **Conversation transcripts** — why it changed (reasoning, alternatives, constraints).

Processing is incremental — only new commits and changed transcripts are evaluated on each run.

## Design Choices

**Hook owns cadence, agent owns workflow, skill owns content rules.** The stop hook is deliberately minimal — it only decides *when* to fire and emits a one-line followup message. It has no knowledge of git commands, transcript scanning, or README structure. The agent definition contains the full workflow. The skill contains only content rules (structure, slop filter, inclusion bar). This avoids duplicating logic across layers and keeps each piece independently testable.

**Incremental index tracks `lastComparedOriginSha`, not local HEAD.** Local commits can be amended, rebased, or reset — making a stored SHA unreachable. Origin refs are stable. The agent compares `lastComparedOriginSha..origin/HEAD` for remote changes and `git diff HEAD` for local work separately. Before updating the stored SHA, `git merge-base --is-ancestor` verifies the new value is ahead of the current one — preventing regression when a developer's local origin is stale relative to another developer's run.

**First run reads full source code instead of git history.** Walking the entire commit history on a mature repo produces noise without context. On first run (no index), the agent reads the local source code to understand the project as-is, generates the README from that, and seeds the index with `origin/HEAD` as the baseline for future incremental diffs.

**Transcripts are re-processed by mtime, not excluded.** Rather than skipping the active conversation's transcript (which risks permanently missing it if the hook never fires again from a different conversation), the agent processes all transcripts including in-progress ones. The incremental index records each transcript's mtime. If a conversation continues after processing, the transcript's mtime advances and it will be re-evaluated on the next run.

**Subagent runs in the background.** The followup message instructs the main agent to launch the readme-updater with `run_in_background: true`. This keeps the user's conversation unblocked — they can continue chatting while the documentation sync runs.

**README updates target the appropriate level.** Projects may contain multiple READMEs. The agent identifies all READMEs and maps changes to the correct one.

## Prerequisites

The hook script runs with [Bun](https://bun.sh/). Ensure `bun` is available on `PATH`.

## State files

| File | Purpose |
|------|---------|
| `.cursor/hooks/state/continuous-documentation.json` | Cadence state (turn count, last run time, transcript mtime) |
| `.cursor/hooks/state/continuous-documentation-index.json` | Incremental index (last compared origin SHA, processed transcript mtimes) |

Both are workspace-local state files.

## Trigger cadence

| Setting | Default | Trial mode |
|---------|---------|------------|
| Minimum turns | 10 | 6 |
| Minimum minutes | 240 | 30 |
| Trial duration | — | 24 hours |

All conditions must be met simultaneously. Transcript mtime must also advance since the previous run.

## Configuration

All settings are optional environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `CONTINUOUS_DOCUMENTATION_MIN_TURNS` | Minimum completed turns before triggering | 10 |
| `CONTINUOUS_DOCUMENTATION_MIN_MINUTES` | Minimum minutes between runs | 240 |
| `CONTINUOUS_DOCUMENTATION_TRIAL_MODE` | Enable reduced thresholds for a trial window | false |
| `CONTINUOUS_DOCUMENTATION_TRIAL_MIN_TURNS` | Minimum turns in trial mode | 6 |
| `CONTINUOUS_DOCUMENTATION_TRIAL_MIN_MINUTES` | Minimum minutes in trial mode | 30 |
| `CONTINUOUS_DOCUMENTATION_TRIAL_DURATION_MINUTES` | Trial window length in minutes | 1440 |

## License

MIT
