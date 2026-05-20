# Tech Refinement — Shared Core

> Shared logic for the tech-refinement skills. Not a skill itself — read by [`refine-jira-ticket`](../refine-jira-ticket/SKILL.md) and [`refine-jira-ticket-cloud`](../refine-jira-ticket-cloud/SKILL.md).

Layer a technical brief onto a business-refined Jira ticket.

## Traits

A tech refinement is:

- **Appended below the marker.** Reporter content above the marker `---\n\n## Tech Refinement` is never edited; agent content below is regenerated each run.
- **Opinionated, not stenographic.** Challenges the requirement when code reality or a simpler design materially improves the outcome.
- **Supplementary, not restating.** Skips subsections the reporter already covered (e.g. `Acceptance Criteria`, `Dependencies / Risks`, `Test Plan`) or that have nothing material to add.
- **Substantive on three axes:**
  - **Fit** → `Approach`. Extend an existing abstraction or add alongside; state the chosen direction and the trade-off. For bugs, fit follows from the defective path and recent changes.
  - **Prerequisites** → `Prerequisites`. Refactors, migrations, or upgrades that must land first.
  - **Requirement shape** → `Proposed Requirement Changes`. Changes to AC, scope, or shape that materially improve the outcome.
- **Codebase-grounded.** Codebase = current behavior. Latest human comment = intent. Conflicts become assumptions.
- **Explicit about uncertainty.** Unconfirmed claims are marked `**Assumption:**`. Humans resolve them via comments; resolved assumptions drop on the next run.
- **Analysis only.** No code, no commits, no branches.

## Flow

Setup: resolve `cloudId` once via `getAccessibleAtlassianResources`; use markdown format for `getJiraIssue` (`responseContentFormat`) and `editJiraIssue` (`contentFormat`).

1. **Read** via `getJiraIssue`: status, status category, issue type, description, all comments.
2. **Guard (→ blocked).** No splice if any trip:
   - `fields.status.statusCategory.key === "done"`.
   - Issue type `Epic` or `fields.issuetype.subtask === true`.
   - Description empty.
3. **Analyze** per the Traits; render the brief from the template.
4. **Splice** via `editJiraIssue`. New description = `<reporter content>\n\n---\n\n## Tech Refinement\n\n<section>`, where `<reporter content>` is the existing description up to (but not including) the `---` preceding `## Tech Refinement`, or the entire description if the marker is absent. Only irreversible side effect.

## Outcomes

One outcome per run; each has its own report shape.

- **refined** -- substantive brief spliced.
  - *First run:* summary (chosen direction, prerequisites, proposed requirement changes), `**Assumption:**` lines, and any blocking question.
  - *Re-run:* skip unless something needs human attention; address the human by name and describe what changed.
- **recommend-close** -- `Recommend close: <reason, cited files>` brief spliced; most technical subsections omitted. Triggers: work already implemented, conflicts with recent design, or belongs elsewhere.
- **blocked** -- no splice; a guard tripped. Report which guard and the reporter's next step.

If business AC is missing, flag it once.

## Brief template

```markdown
---

## Tech Refinement
_Maintained by the tech-refinement agent._

### Affected Areas
- [Modules / files with paths]

### Approach
[The chosen direction and the trade-off that drove it, 2–4 sentences. Name the alternative briefly so the reviewer sees the choice was deliberate. Use `Recommend close: <reason, cited files>` when no implementation is warranted.]

### Prerequisites
- [Refactor / migration / upgrade that must land before this change is safe or sensible, with cited files. Omit when none.]

### Proposed Requirement Changes
- [Each proposed change to the reporter's AC, scope, or shape, with reasoning. Omit when none. The reporter resolves by editing their content above the marker or pushing back in a comment.]

### Technical Acceptance Criteria
- [Tech-only AC: observability, perf budget, migration order, feature-flag rollout, etc.]

### Considerations
- [Technical risk / trade-off]

### Testing Impact
- [New tests, tests that break, manual verification]

### Dependencies
- [Technical dependencies: services, migrations, libraries, tickets]

### Assumptions
- **Assumption:** [Anything unconfirmed. Resolved by a comment, then dropped on the next re-run.]
```
