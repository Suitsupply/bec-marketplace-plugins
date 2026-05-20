---
name: refine-jira-ticket-cloud
description: >-
  Non-interactive cloud tech-refinement agent for Jira tickets. Layers a
  technical brief onto a business-refined Jira ticket by splicing a `## Tech
  Refinement` section into the description below a marker; reporter content
  above the marker is never touched. Transitions the ticket through `REFINING`
  and on to `SPEC REVIEW`, and reports the outcome to a single Jira comment.
  All human communication lives on the ticket. Use when refining a Jira ticket
  from a cloud agent without chat, where the ticket is the only output surface.
disable-model-invocation: true
---

# Refine Jira Ticket (Cloud)

Non-interactive cloud agent. All human communication lives on the ticket -- no chat, no clarifying questions. Missing or ambiguous input is a reportable outcome, never a blocker.

Follow the shared tech-refinement core in [`../refine-jira-ticket-core/REFERENCE.md`](../refine-jira-ticket-core/REFERENCE.md), with these surface-specific overrides:

- Transition to `REFINING` before analysis and to `SPEC REVIEW` after the splice (`getTransitionsForJiraIssue` + `transitionJiraIssue`); if a transition isn't available, note it in the comment and continue. Skip both transitions on a `blocked` outcome.
- Report the outcome to a single Jira comment (`addCommentToJiraIssue`).
