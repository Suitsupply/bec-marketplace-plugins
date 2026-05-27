---
name: split-jira-ticket
description: >-
  Create a split-off Jira ticket from an original ticket, with correct content and metadata. 
  Use this skill **after** the decision has been made that a piece of work must live in its own Jira ticket.
---

# Split-off Jira Ticket

Use this skill **after** the decision has been made that a piece of work must live in its own Jira ticket. This skill does not make that decision; it executes the split correctly.

Resolve `cloudId` once via `getAccessibleAtlassianResources` before any other Atlassian call. All writes use `contentFormat: "markdown"`; fetch with `responseContentFormat: "markdown"`.

## Inputs

- **Original ticket key** — the ticket the split is being carved out of.
- **Split rationale** — one sentence on why this work was carved out (used in the comment on the original).
- **Split content** — the actual work to capture in the new ticket.

## Workflow

1. **Fetch the original** (`getJiraIssue`) — capture: epic link, parent (if subtask), sprint, status, issue type, project key.

2. **Classify dependency vs follow-up** using the rule below. Record the result; it drives both the link type and the sprint decision.

3. **Draft the new ticket content** following the [Ticket content rules](#ticket-content). The new ticket must be self-contained — a developer should not need to open the original to act on it.

4. **Create the new ticket** (`createJiraIssue`) in the same project as the original. Set:
   - **Issue type:** same as the original (Story / Task / Bug / Agentic). If the original is a subtask, use the subtask's own type and link to the original's **parent epic**, not the original.
   - **Summary:** concise, role-only where applicable. Do not append the original summary.
   - **Description:** per [Ticket content rules](#ticket-content).
   - **Labels:** include `story-builder-assisted` (merge, do not overwrite).
   - **Epic link:** same epic as the original (resolve from the original's epic-link field, or — if the original is a subtask — from its parent).
   - **GitHub repository URL** (`customfield_16182`): copy from the original **only if** the split-off work lives in the same repository **and** the field is set on the original. Skip otherwise.

5. **Link to the original**:
   - **Dependency:** create an `is blocked by` link from the original to the new ticket (i.e. the original is blocked by the split-off).
   - **Follow-up:** create a `relates to` link between the original and the new ticket, and prefix the new ticket's summary with `Follow-up:` so the relationship is visible in lists.

6. **Sprint placement** (dependency only):
   - If it's a dependency **and** the original is in an active or future sprint, set the new ticket's sprint to the **same sprint** as the original.
   - If it's a follow-up, leave sprint unset — the team plans it separately.
   - If it's a dependency but the original is not in any sprint, leave sprint unset.

7. **Comment on the original** (`addCommentToJiraIssue`, one comment) — state the new ticket key, the classification (dependency or follow-up), the one-sentence rationale, and what sections (if any) of the original were narrowed as a result.

8. **Chat summary** — new ticket key + title, classification, epic, sprint (or "none"), repo URL copied (yes/no), original ticket key.

---

## The universal rule — dependency vs follow-up

> **If the original ticket's acceptance criteria cannot be met without the split-off being completed first, the split-off is a *dependency*. Otherwise, it is a *follow-up*.**

Apply the rule against the **original's own AC as written**, not against a hypothetical broader scope. If meeting the original's AC requires the split-off work to already be in place, it's a dependency — no matter how small the split-off is. If the original's AC can be signed off while the split-off remains open, it's a follow-up — no matter how related the work feels.

Edge cases:

- **Prep refactor that unblocks the original** → dependency (the original cannot land cleanly without it).
- **Cleanup or hardening discovered while implementing the original** → follow-up (the original's AC are already met).
- **Parallel sibling work in the same epic, neither blocks the other** → follow-up.
- **Behavior the agent considers necessary but the original's AC does not require** → follow-up; do not silently expand the original's scope by calling it a dependency.

If applying the rule is genuinely ambiguous, default to **follow-up** and flag the ambiguity in the comment on the original — a follow-up can always be reclassified, an incorrect dependency reshapes a sprint.

---

## Ticket content

The new ticket's description must be self-contained. `Context`, `Goal`, and `Acceptance Criteria` are mandatory. This should be agnostic of the implementation details.

For dependencies, list the original ticket key under `Dependencies` in the new ticket and note in `Scope → Out` what stays in the original. For follow-ups, note in `Context` that this was carved out of the original and what the original already covers.

Apply **refine-jira-ticket** skill to append the implementation details.

---

## Rules

- Never modify the original ticket's description, AC, or fields beyond adding the link and the single comment.
- Never duplicate work that already exists as a sibling subtask or linked issue — search first, link to the existing one if found, and note that in the chat summary instead of creating a duplicate.
- One comment on the original per run, max.
- Omit the `story-builder-assisted` label when created via this skill.
- Omit story points