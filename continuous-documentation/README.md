# Continuous Documentation

Cursor and Claude Code plugin that keeps the repository `README.md` current. When new commits land, it documents them — pairing the diff (the "what") with the reasoning from the live conversation (the "why").

## How it works

Three pieces work together:

| Piece | Role |
|--------|------|
| **stop hook** | Runs at the end of each agent turn. Compares `HEAD` to the last commit that touched a README; if they differ, it tells the main agent to sync the docs for the new range. |
| **`continuous-documentation` agent** | Subagent that updates `README.md` from the commit range plus a "why" summary passed to it. |
| **`documentation-standards` skill** | README structure, inclusion/exclusion, slop filter, and intent guidance. Read by the agent at runtime — not loaded into the main agent's context. |

The flow when `HEAD` has moved:

1. At turn end the hook compares `git rev-parse HEAD` to the last commit that touched any `README.md` (`git log -1 --format=%H -- ':(glob)**/README.md' 'README.md'`) — the derived baseline.
2. If they differ (and no README edits are already pending in the working tree), the **main agent** distills a short "why" summary from the live conversation — the design decisions and constraints behind the change.
3. It launches the `continuous-documentation` subagent in the background, passing that summary. The subagent reads `git diff <base>..HEAD` for the "what", applies the skill, and updates the appropriate `README.md`.
4. When the user commits the updated README, that commit becomes the new baseline — so the same commits are never documented twice.

The "why" comes from the conversation in context, not from scraping transcript files off disk. That removes the only host-specific dependency and makes the plugin work identically under Cursor, Claude Code, and cloud agents.

## Design Choices

**Detect "HEAD moved", not "the command was `git commit`".** Commits are created by more than `git commit` — `merge`, `pull`, `rebase`, `cherry-pick`, `revert`, and aliases all produce commits, so matching a command string is brittle. Comparing `HEAD` to the derived baseline is agnostic to *how* the commit was made. The check runs at the end of each agent turn (`stop`/`Stop`), so it is not a real-time commit event: a commit made outside the agent (e.g. the editor's Source Control UI or an external terminal) is only picked up on the next agent turn, and not at all if the agent is never used again. When it does run after several new commits, it documents the range `base..HEAD` in one pass. History rewrites (`amend`, `rebase`, `reset`) can leave the baseline off the current history; the range is then imperfect and the worst case is a no-op sync, not a crash.

**Deliver through the `stop` hook, not `postToolUse`.** Cursor's `postToolUse` `additional_context` is accepted and logged by the hook runner but [not surfaced to the model](https://forum.cursor.com/t/native-posttooluse-hooks-accept-and-log-additional-context-successfully-but-the-injected-context-is-not-surfaced-to-the-model/155689) — a hook there would be a silent no-op. The `stop` hook's `followup_message` (Cursor) and the `Stop` hook's `decision: "block"` (Claude) are the channels that actually reach the model, so detection runs at turn boundaries instead.

**The baseline is statless and derived from git.** The "last documented commit" is computed on the fly as the most recent commit that touched any `README.md` (`git log -1 --format=%H -- '**/README.md'`), so it lives in shared git history rather than a workspace-local file. Every clone derives the same baseline, which means teammates never re-document what someone else already wrote up, and there is no marker file to drift, gitignore, or seed.

Two behaviors follow directly from defining the baseline as "the last commit that touched a README":

- **A commit that touches a README *is* the new baseline.** When such a commit is `HEAD`, `base == HEAD`, so the hook stays silent. Committing the doc update converges the loop, and a commit that only changes a README — or one that bundles a README edit with code — never triggers a sync of itself. A commit that warrants *no* README change does not advance the baseline, so the hook will re-offer a sync on later commits until either docs or new work are committe
- **An uncommitted README edit counts as a pass already pending.** While the sync's edits sit in the working tree (not yet committed), the hook suppresses re-prompting until you commit them, so it does not nag every turn during the window between editing and committing the docs.
- **Agent-Friendly** The hook has been optimized for agentic coding. It only fires on an agent-triggered commit. If the readme has been incorrectly or incompletely updated it still counts as an update. Humans are more likely to do this. Agents can be expected to update the readme in full, but may overlook to do as such.
- **First Init** If there is no readme at all in the repo, it will be treated as a first init and a readme will be generated according to the documentation standards.

**Heavy work runs in a background subagent.** The main agent only writes a short summary; the subagent does the git reading and README editing with `run_in_background: true`, keeping the user's conversation unblocked and the main context window clean.

**The sync never commits.** Editing the README does not move `HEAD`, so it does not re-trigger. Committing the README is left to the user, which keeps the loop from feeding itself.

**First run is detected by the absence of a committed README.** When no commit has ever touched a `README.md`, the subagent reads the full source tree and generates documentation from scratch. On a repo that already has a committed README, the baseline is simply the last commit that touched it — so the first sync only covers commits made since the docs were last updated, not all of history.

**README updates target the appropriate level.** Projects may contain multiple READMEs. The agent maps the committed changes to the correct one (root or project level).

## Prerequisites

The hook script runs with [Bun](https://bun.sh/). Ensure `bun` is available on `PATH`.

## State

None. The baseline is derived from git history (`git log -1 --format=%H -- ':(glob)**/README.md' 'README.md'`), so there is no marker file to store, gitignore, or keep in sync.

> Upgrading from an earlier version? The old `.continuous-documentation/` folder and its `.gitignore` entry are no longer used and can be deleted.

## License

MIT
