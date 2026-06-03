---
name: refine-jira-ticket
description: >-
  Non-interactive tech-refinement agent for Jira tickets. Layers a technical brief onto a business-refined Jira ticket by splicing a `## Tech Refinement` section into the description below a marker; reporter content above the marker is never touched.
---

# Refine Jira Ticket

Layer a short technical brief onto a business-refined Jira ticket. Two readers:

- **The reporter (human)** — can push back. Surface anything that needs their judgement.
- **The implementing agent (downstream)** — can't push back. Treats the brief as the spec; anything missing gets re-derived and may diverge.

## The brief

The default outcome is **refined**: splice a brief with two ordered blocks.

1. **For the reporter** — flag anything that needs their judgement:

- **Story validity** — does the story itself stand up against the code? Clear value, testable acceptance criteria, right scope (not already covered, not too big, not too small), and consistent with how the system actually works today.
- **Solution validity** — is this the *right kind* of solution? Defensible against a plausible alternative, and not taking on conceptual debt or breaking the existing solution's shape — surface these even when they look justified.
- **Solution scope** — does it touch the *right amount*? Proportionate to the value; flag where it over-reaches (bigger than the ticket asked for) or under-reaches (smaller than the codebase warrants). When the work splits cleanly into a separable piece, recommend carving that into its own ticket (as a dependency or follow-up).
- **Agent validity** — could the AI agent actually do the work this brief implies? Surface gaps in context, access, or capability.

Omit any bullet when empty.


2. **The plan, for the implementing agent** — approach, affected files (with paths), testing approach, prerequisite work. Specific enough the next agent builds the agreed thing, not a near-miss. Skip what the next agent will trivially discover; include anything where a wrong choice diverges from what was agreed. Add risks or technical AC only when they would change how the work is done.

Two alternative outcomes:

- **recommend-close** — brief is just `Recommend close: <reason, cited files>`. No blocks. Triggers: already implemented, conflicts with recent design, or belongs elsewhere.
- **blocked** — no splice. Report which guard tripped and the reporter's next step.

## Comments
On an initial tech refinement, note key findings. 
On a re-run, note key changes.
Address the reporter by name.

## Principles

**Choosing the solution**

- **Prefer the right seam over a new one.** If existing code owns the responsibility, modify it. Don't open a parallel path to avoid touching it.
- **Prefer consolidation over preservation.** When the work touches duplication or a near-duplicate pattern, fold rather than add a third variant.
- **Prefer fixing the root over routing around it.** If the bug or limitation is upstream of where the ticket lands, name it and choose deliberately whether to fix here or scope out — don't silently shim.
- **Don't trade conceptual debt for narrower blast radius.** Wider blast radius can be the right answer; debt almost never is. When they conflict, surface the tradeoff, don't dodge it.
- **Size the surgery to the problem.** Default to the smallest change that doesn't violate the rules above. Don't grow scope to feel thorough.

**Writing the brief**
- **Non-blocking** Missing or ambiguous input is a reportable outcome, never a blocker for agent completion.
- **Codebase-grounded.** Cite files for code claims. Human comment from JIRA = intent. Anything the brief leans on but can't verify — a code/intent conflict or silence, a guess about a non-functional need, an ownership call — is `**Assumption:**` that needs to be validated.
- **Opinionated.** Take a position; name the alternative briefly when the choice was non-obvious.
- **Analysis only.** No code changes, commits, or branches.

## Flow

Resolve `cloudId` once via `getAccessibleAtlassianResources`. Use markdown for `getJiraIssue` (`responseContentFormat`) and `editJiraIssue` (`contentFormat`).

1. **Read** the ticket: status, status category, issue type, description, all comments.
2. **Guard (→ blocked, no splice)** if any of:
   - `fields.status.statusCategory.key === "done"`
   - Issue type `Epic` or `fields.issuetype.subtask === true`
   - Description empty
3. **Transition** Transition to `REFINING` before analysis (`getTransitionsForJiraIssue` + `transitionJiraIssue`). Skip transition when unavailable.
4. **Analyze.** Read enough code to back up any claim.
5. **Test analysis.** Apply **analyze-test-suite** SKILL. Incorporate test recommendations into implementation block and the suite health flag (when not `Healthy`) into reporter block.
6. **Splice** via `editJiraIssue`. New description = `<reporter content>\n\n---\n\n## Tech Refinement\n\n<brief>`, where `<reporter content>` is everything before the `---` preceding any prior `## Tech Refinement`, or the whole description if no marker exists. The only irreversible side effect.
7. **Transition** Transition to `SPEC REVIEW` after the splice (`getTransitionsForJiraIssue` + `transitionJiraIssue`). Skip transition when unavailable.