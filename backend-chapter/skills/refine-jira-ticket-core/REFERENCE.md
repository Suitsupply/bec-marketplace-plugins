# Tech Refinement — Shared Core

Layer a short technical brief onto a business-refined Jira ticket. Two readers:

- **The reporter (human)** — can push back. Surface anything that needs their judgement.
- **The implementing agent (downstream)** — can't push back. Treats the brief as the spec; anything missing gets re-derived and may diverge.

## The brief

The default outcome is **refined**: splice a brief with two ordered blocks.

1. **For the reporter** — flag anything that needs their judgement:

- **Story validity** — does the story itself stand up against the code? Clear value, testable acceptance criteria, right scope (not already covered, not too big, not too small), and consistent with how the system actually works today.
- **Solution validity** — is this the right kind of solution? Proportionate to the value, defensible against a plausible alternative, and not taking on conceptual debt or breaking the existing solution's shape — even when justified.
- **Solution scope** — does the planned work touch the right amount? Flag where it over-reaches (bigger than the ticket asked for) or under-reaches (smaller than the codebase warrants).
- **Agent validity** — could the AI agent actually do the work this brief implies? Surface gaps in context, access, or capability.

Omit any bullet when empty.

2. **The plan, for the implementing agent** — approach, affected files (with paths), prerequisite work. Specific enough the next agent builds the agreed thing, not a near-miss. Skip what the next agent will trivially discover; include anything where a wrong choice diverges from what was agreed. Add risks, testing notes, or technical AC only when they would change how the work is done.

Two alternative outcomes:

- **recommend-close** — brief is just `Recommend close: <reason, cited files>`. No blocks. Triggers: already implemented, conflicts with recent design, or belongs elsewhere.
- **blocked** — no splice. Report which guard tripped and the reporter's next step.

On a re-run, address the reporter by name and note what changed; stay silent if nothing did. If business AC is missing, flag it once.

## Principles

**Choosing the solution**

- **Prefer the right seam over a new one.** If existing code owns the responsibility, modify it. Don't open a parallel path to avoid touching it.
- **Prefer consolidation over preservation.** When the work touches duplication or a near-duplicate pattern, fold rather than add a third variant.
- **Prefer fixing the root over routing around it.** If the bug or limitation is upstream of where the ticket lands, name it and choose deliberately whether to fix here or scope out — don't silently shim.
- **Don't trade conceptual debt for narrower blast radius.** Wider blast radius can be the right answer; debt almost never is. When they conflict, surface the tradeoff, don't dodge it.
- **Size the surgery to the problem.** Default to the smallest change that doesn't violate the rules above. Don't grow scope to feel thorough.

**Writing the brief**

- **Codebase-grounded.** Cite files for code claims. Latest human comment = intent. Anything the brief leans on but can't verify — a code/intent conflict or silence, a guess about a non-functional need, an ownership call — becomes an `**Assumption:**` line. Humans resolve assumptions via comments; they drop on the next run.
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