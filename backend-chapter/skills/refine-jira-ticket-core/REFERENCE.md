# Tech Refinement — Shared Core

Layer a short technical brief onto a business-refined Jira ticket. Two readers:

- **The reporter (human)** — can push back. Surface anything that needs their judgement.
- **The implementing agent (downstream)** — can't push back. Treats the brief as the spec; anything missing gets re-derived and may diverge.

## The brief

The default outcome is **refined**: splice a brief with two ordered blocks.

1. **For the reporter** — flag anything that needs their judgement:

   - Do the business requirements hold up against how the code works today?
   - Should any be reconsidered to make the implementation simpler or more consistent?
   - Does the plan take on conceptual debt (parallel patterns, leaky abstractions, "we'll clean up later") or break with the existing solution's shape — even when justified?

   Omit when empty.

2. **The plan, for the implementing agent** — approach, affected files (with paths), prerequisite work. Specific enough the next agent builds the agreed thing, not a near-miss. Skip what the next agent will trivially discover; include anything where a wrong choice diverges from what was agreed. Add risks, testing notes, or technical AC only when they would change how the work is done.

Two alternative outcomes:

- **recommend-close** — brief is just `Recommend close: <reason, cited files>`. No blocks. Triggers: already implemented, conflicts with recent design, or belongs elsewhere.
- **blocked** — no splice. Report which guard tripped and the reporter's next step.

On a re-run, address the reporter by name and note what changed; stay silent if nothing did. If business AC is missing, flag it once.

## Principles

- **Codebase-grounded.** Cite files for code claims. Latest human comment = intent. Conflicts become `**Assumption:**` lines, which humans resolve via comments and which drop on the next run.
- **Opinionated.** Take a position; name the alternative briefly when the choice was non-obvious.
- **Analysis only.** No code changes, commits, or branches.

## Flow

Resolve `cloudId` once via `getAccessibleAtlassianResources`. Use markdown for `getJiraIssue` (`responseContentFormat`) and `editJiraIssue` (`contentFormat`).

1. **Read** the ticket: status, status category, issue type, description, all comments.
2. **Guard (→ blocked, no splice)** if any of:
   - `fields.status.statusCategory.key === "done"`
   - Issue type `Epic` or `fields.issuetype.subtask === true`
   - Description empty
3. **Analyze.** Read enough code to back up any claim.
4. **Splice** via `editJiraIssue`. New description = `<reporter content>\n\n---\n\n## Tech Refinement\n\n<brief>`, where `<reporter content>` is everything before the `---` preceding any prior `## Tech Refinement`, or the whole description if no marker exists. The only irreversible side effect.
