# Continuous Documentation

Cursor and Claude Code plugin that keeps the repository `README.md` current. When new commits land, it documents them — pairing the diff (the "what") with the reasoning from the live conversation (the "why").

## How it works

Three pieces work together:

| Piece | Role |
|--------|------|
| **stop hook** | Runs at the end of each agent turn. Compares `HEAD` to the last documented commit; if they differ, it tells the main agent to sync the docs for the new range. |
| **`continuous-documentation` agent** | Subagent that updates `README.md` from the commit range plus a "why" summary passed to it. |
| **`documentation-standards` skill** | README structure, inclusion/exclusion, slop filter, and intent guidance. Read by the agent at runtime — not loaded into the main agent's context. |

The flow when `HEAD` has moved:

1. At turn end the hook compares `git rev-parse HEAD` to the SHA in `.continuous-documentation/last-documented-sha`.
2. If they differ, the **main agent** distills a short "why" summary from the live conversation — the design decisions and constraints behind the change.
3. It launches the `continuous-documentation` subagent in the background, passing that summary. The subagent reads `git diff <base>..HEAD` for the "what", applies the skill, and updates the appropriate `README.md`.
4. The hook advances the marker to `HEAD` so the same commits are never documented twice.

The "why" comes from the conversation in context, not from scraping transcript files off disk. That removes the only host-specific dependency and makes the plugin work identically under Cursor, Claude Code, and cloud agents.

## Design Choices

**Detect "HEAD moved", not "the command was `git commit`".** Commits are created by more than `git commit` — `merge`, `pull`, `rebase`, `cherry-pick`, `revert`, and aliases all produce commits, so matching a command string is brittle. Comparing `HEAD` to the last documented SHA is agnostic to *how* the commit was made. The check runs at the end of each agent turn (`stop`/`Stop`), so it is not a real-time commit event: a commit made outside the agent (e.g. the editor's Source Control UI or an external terminal) is only picked up on the next agent turn, and not at all if the agent is never used again. When it does run after several new commits, it documents the range `base..HEAD` in one pass. History rewrites (`amend`, `rebase`, `reset`) can leave the stored SHA off the current history; the range is then imperfect and the worst case is a no-op sync, not a crash.

**Deliver through the `stop` hook, not `postToolUse`.** Cursor's `postToolUse` `additional_context` is accepted and logged by the hook runner but [not surfaced to the model](https://forum.cursor.com/t/native-posttooluse-hooks-accept-and-log-additional-context-successfully-but-the-injected-context-is-not-surfaced-to-the-model/155689) — a hook there would be a silent no-op. The `stop` hook's `followup_message` (Cursor) and the `Stop` hook's `decision: "block"` (Claude) are the channels that actually reach the model, so detection runs at turn boundaries instead.

**One SHA of state.** The only thing remembered between runs is the last documented commit, in `.continuous-documentation/last-documented-sha`. No cadence counters, no transcript index, no `git fetch` or `merge-base` bookkeeping. The hook adds the folder to `.gitignore` on first run.

**Heavy work runs in a background subagent.** The main agent only writes a short summary; the subagent does the git reading and README editing with `run_in_background: true`, keeping the user's conversation unblocked and the main context window clean.

**The sync never commits.** Editing the README does not move `HEAD`, so it does not re-trigger. Committing the README is left to the user, which keeps the loop from feeding itself.

**First run is detected by the absence of a root README.** With no root `README.md` and no marker, the subagent reads the full source tree and generates documentation from scratch. On a fresh install of a repo that already has a README, the hook adopts the current commit as the baseline silently, so existing history is not re-documented.

**README updates target the appropriate level.** Projects may contain multiple READMEs. The agent maps the committed changes to the correct one (root or project level).

## Prerequisites

The hook script runs with [Bun](https://bun.sh/). Ensure `bun` is available on `PATH`.

## State

| File | Purpose |
|------|---------|
| `.continuous-documentation/last-documented-sha` | The last commit documented. Workspace-local; added to `.gitignore` automatically. |

## License

MIT
