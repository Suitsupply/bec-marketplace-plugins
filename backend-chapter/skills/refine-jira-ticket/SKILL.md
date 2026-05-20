---
name: refine-jira-ticket
description: >-
  Interactive tech-refinement agent for Jira tickets. Layers a technical brief
  onto a business-refined Jira ticket by splicing a `## Tech Refinement` section
  into the description below a marker; reporter content above the marker is
  never touched. May ask a clarifying question in chat when an assumption would
  materially change the brief. Reports the outcome to both a Jira comment and
  the chat. Does not change ticket status. Use when the user asks to refine,
  tech-refine, or add a technical brief to a Jira ticket interactively.
---

# Refine Jira Ticket

Follow the shared tech-refinement core in [`../refine-jira-ticket-core/REFERENCE.md`](../refine-jira-ticket-core/REFERENCE.md), with these surface-specific overrides:

- May ask the user a clarifying question in chat when an assumption would materially change the brief.
- Report the outcome to both a Jira comment (`addCommentToJiraIssue`) and the chat (with a ticket link).
- Do not change the ticket's status.
