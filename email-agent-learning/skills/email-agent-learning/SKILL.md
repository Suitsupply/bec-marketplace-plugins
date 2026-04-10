---
name: email-agent-learning
description: >-
  Rules for extracting email domain knowledge from agent transcripts and writing
  it into EMAIL-AGENT.md. Use when running the update-email-agent command or
  when the stop hook triggers the sync workflow.
---

# Email Agent Learning

Rules for what belongs in `EMAIL-AGENT.md` and how to write it. **Transcripts** are the sole source — no git, no code scanning. Each run reads new transcripts since the last index, extracts learnings, and merges them into the document.

`EMAIL-AGENT.md` is a workspace-local file. It is never committed. It is the agent's growing memory of this email workspace.

---

## What EMAIL-AGENT.md Is

A living knowledge document that the agent reads as context. It captures:

- What exists in this workspace and what each thing does
- How work gets done (workflows, processes, conventions)
- Why things were built or decided a certain way
- Current status of ongoing work
- Patterns discovered across sessions

It is **not** a changelog, a task list, or a README. It does not log what happened in a session — it distills what was learned into a persistent reference.

---

## Document Structure

Maintain these sections in order. Add subsections freely within each. Never delete a section even if currently empty.

### 1. Infrastructure

Systems and services this workspace integrates with. For each: what it is, its role in the email pipeline, known quirks, retention limits, or constraints.

Covers: Customer.io (CIO), SendAPI, Azure, Parcel.io, email environments (PRD/TST), delivery tracking, API endpoints used.

### 2. Email Projects

Known email projects and their current state. For each project: folder location, purpose, languages supported, active/archived status, and any notable context from recent work.

Covers: campaigns (mid-interest, bestseller, welcome, etc.), transactionals (orders, returns, alterations, SBL, appointments), templates (master-ss, tx-ss), and NL/international variants.

### 3. Tools & Apps

What each tool, script, or app in the workspace does and when to use it. Include: folder path, primary function, and how it's invoked.

Covers: `tools/` agent skill scripts, `apps/` standalone tools (dashboards, compare tool, master-email-code), and any external scripts referenced in sessions.

### 4. Workflows

How work gets done. Each workflow entry describes a repeatable process: the trigger, the steps, the output, and any gotchas.

Covers: deploy/release flow, QA process, export from CIO, email backtracking, data collection, file size analysis, and any other repeatable task pattern observed in transcripts.

### 5. Patterns & Conventions

Recurring code patterns, naming rules, structural decisions that apply across email files.

Covers: Liquid templating patterns, HTML structure conventions, language/locale handling, subject line format, CSS inlining approach, snippet usage, conditional rendering patterns.

### 6. Decisions

Design choices that would not be obvious from reading the code. Each entry: the decision, the reason, and alternatives that were rejected or considered.

Only document decisions that carry forward — not session-specific workarounds.

### 7. Recent Context

A rolling summary of the last few sessions: what was being worked on, what state it was left in, and any open threads. Replace the previous entry when a newer one covers the same project.

This section is a pointer for the agent: "here is what is currently active and what you last knew about it."

---

## Extraction Rules

When scanning a transcript, extract a learning if it meets **all** of these:

1. It reveals something about the workspace that the agent would not know from reading the code alone.
2. It is specific (names a file, tool, system, decision, or pattern — not a vague observation).
3. It is durable — still true after the session ends.

Skip:
- Conversational back-and-forth with no informational content
- Debugging steps that did not produce a conclusion
- Questions the user asked but that were not answered
- Anything that only applied to a single session and has no forward relevance

---

## Writing Rules

Every sentence must earn its place. Apply the slop filter before writing.

### Slop Filter

Remove or rewrite any sentence containing:

- "comprehensive", "robust", "seamless", "powerful", "flexible"
- "leverage", "utilize" (use "use")
- "facilitate", "streamline", "ensure"
- "This allows", "This provides", "This ensures"
- "It's worth noting", "It should be noted"
- "In order to" (use "to")
- "Going forward"
- "Key", "critical", "crucial" used as filler adjectives
- Marketing language of any kind

Structural slop to delete:
- Bullet points that restate the heading
- Introductory sentences that say "this section covers X" — just cover X
- Summaries that repeat what was already said above

### Tone

Dense and factual. Write like internal documentation for someone who already works here — no onboarding preamble. State facts directly.

Good: `SendAPI retains full email payloads for 90 days. After that, only CIO activity logs remain.`

Bad: `It's worth noting that SendAPI has a retention policy of 90 days, which is important to be aware of when investigating older issues.`

---

## Merge Rules

When updating an existing section:

- **Add** new facts that are not already covered
- **Refine** existing entries if the transcript adds nuance or corrects something
- **Replace** Recent Context entries when a newer session covers the same area
- **Never delete** a section, even if it is temporarily empty
- **Never duplicate** — if the fact is already captured accurately, skip it

When in doubt whether something is new: check if the existing text already implies it. If yes, skip.

---

## Incremental Index Format

```json
{
  "version": 1,
  "transcripts": {
    "/abs/path/to/file.jsonl": {
      "mtimeMs": 1730000000000,
      "lastProcessedAt": "2026-03-19T12:00:00.000Z"
    }
  }
}
```

---

## Exclusions

Never write into EMAIL-AGENT.md:

- API keys, tokens, credentials, or connection strings
- Customer PII (email addresses, order IDs, names from production data)
- Anything marked confidential in the transcript
- Internal ticket numbers or branch names
- Temporary workarounds the user explicitly said were short-term
