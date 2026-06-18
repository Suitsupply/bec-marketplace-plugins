---
name: documentation-standards
description: >-
  README content rules: project-type structure, inclusion/exclusion, slop filter,
  and capturing intent from conversation. Use when editing README.md to these
  standards, or when the continuous-documentation agent runs the full sync workflow.
---

# Documentation Standards

Rules for what belongs in the repository `README.md` and how to write it. This skill defines the **content rules** — the `continuous-documentation` agent handles the workflow (reads the commit diff for the "what" and a conversational "why" summary).

## When to use this skill directly

Use when the user asks for README guidance or edits without a full sync. The `continuous-documentation` agent also reads this skill as its rule set.

## Core Principles

### 1. What and Why are equally important

"What" lives in the code — The current state. Data flow, domain logic, data contracts. Read the committed change (`git diff <base>..HEAD`, or the full source on a first run) to find it when syncing.

"Why" lives in the conversation — Design Choices: the user's stated reasoning, rejected alternatives, constraints that shaped the decision. The calling agent distills this from the live conversation and passes it in; describe why it was designed as such.

### 2. What NOT to include is equally important as what to include

Every section, sentence, and word must earn its place. If you cannot justify why a reader needs it, delete it. See the slop filter and exclusion rules below — they carry the same weight as the inclusion rules.

---

## README Structure by Project Type

A `README.md` must exist at the repository root level and contain substantive information about the repository's purpose. Placeholder or auto-generated READMEs are not acceptable.

**Update policy:**
Only update when there are meaningful feature changes (new capabilities, changed behavior, non-trivial design shifts). Do not log routine refactors or obvious details.

For microservices the readme lives at root level. For monolith services the readme lives at project level.

### Service

**Purpose of the Service**
Short executive summary of what the service does.

**Position in the Landscape**
Mermaid sequence diagram showing how the various components in the repository interact.

**Design Choices**
Document only decisions that differ from conventional expectations or introduce significant trade-offs. When documenting a decision, explain why this choice was made over alternatives.

**Business Logic**
Focus on the application/domain layer where logic is more complex than simple CRUD. Highlight business rules, workflows, decision-making processes that involve conditions, transformations, state management, or dependencies. Do not document trivial logic.

### Monolith

**Purpose of the Monolith**
Short executive summary of what the monolith does.

**Position in the Landscape**
Mermaid sequence diagram showing how the various components in the repository interact. References to other `README.md` files in the repository.

### UI

**Purpose & Key Flows**
Short description of what the UI enables and its main user flows.

**Architecture & State Management**
Routing, state management approach, and meaningful cross-cutting concerns (feature flags, offline support, caching).

**Design System & Accessibility**
Reference the design system or component library used and notable accessibility considerations.

**Integration & Data Handling**
Link to backend APIs or contracts. Highlight significant client-side validations or data transformations.

**Error & State Handling**
Document only non-trivial states (empty, loading, offline) and overall error-handling approach.

**Performance & Platform Support**
Only notable performance considerations and non-standard platform/browser support.

### NuGet Package / NPM Package

**Purpose & Fit**
Short summary of what the package does and when to use it.

**Installation & Compatibility**
Installation command, supported frameworks, notable dependencies.

**Public API Overview**
List entry points or abstractions, linking to API docs if available.

**Usage Examples**
One minimal and one advanced example demonstrating meaningful use.

**Configuration & Extensibility**
Configuration options and extension points that change core behavior.

**Versioning & Diagnostics**
Semantic versioning policy, compatibility constraints, logging or diagnostic options if relevant.

**Security & Licensing**
Security considerations and the package license.

### Non-Functional Requirements (ISO 25010)

Document NFRs only when they significantly impact design, operations, or user experience. Focus on measurable qualities that differ from default expectations.

- **Performance Efficiency** — Response time targets, throughput requirements, resource constraints.
- **Reliability** — Uptime/availability targets, RTO/RPO, fault tolerance mechanisms.
- **Security** — Auth approach, encryption, vulnerability management.
- **Maintainability** — Coverage targets (if meaningful), tech debt tracking, deployment/rollback procedures.
- **Scalability** — Horizontal/vertical strategy, load balancing, database scaling.
- **Compatibility** — Backward/forward guarantees, API versioning, platform support matrix.

---

## What Belongs in Documentation

- Purpose: what the service/project does, stated once and plainly.
- How components interact: sequence diagrams, dependency flow.
- Design decisions that would surprise a new reader or that deviate from convention. State the decision and why it was chosen over alternatives.
- Business logic that goes beyond CRUD — rules, workflows, state machines, conditional paths.
- Intent: why something was built this way. What problem prompted it. What constraint shaped it.

## What Does NOT Belong in Documentation

This list carries the same weight as the one above. Enforce it.

- Auto-generated changelogs or version histories. The README is not a changelog.
- Anything that only matters during a single sprint or PR cycle.
- Internal ticket numbers, branch names, or developer names.
- Repeated content

## Slop Filter

Every sentence in the output must pass this filter. Remove or rewrite any sentence that contains:

**Word choice:**
- Prefer the plain word over the inflated one (e.g. "use" not "leverage", "to" not "in order to").
- Cut filler lead-ins that carry no information ("This ensures", "It's worth noting").
- Drop qualifiers and adjectives that aren't backed by a concrete fact or metric ("powerful", "scalable", "key").

**Structural slop:**
- Bullet points that just rephrase the heading.
- Sections with a single obvious sentence and no real content.
- Introductory paragraphs that say "This section describes X" — just describe X.
- Conclusions or summaries that repeat what was already said.
- Multiple sentences that say the same thing with different words.

**Tone slop:**
- Marketing language. The README is for developers, not a sales page.
- Excessive enthusiasm. State facts.
- Hedging without substance ("may potentially help improve").
- Passive voice used to avoid stating who does what.

**Test:** Read each sentence and ask: does this add information a developer cannot get from reading the code or the section heading? If no, delete it.

## Capturing Intent

When the conversation reveals why a change was made, distill it to a sentence or two in the relevant README section — placed next to the description of what changed. Good intent documentation reads like:

- "Uses event sourcing instead of direct DB writes because order state must be auditable across services."
- "Retry policy caps at 3 attempts with exponential backoff — chosen after observing transient Azure Service Bus timeouts under load."
- "Split into two endpoints rather than one polymorphic endpoint to keep OpenAPI schema readable for external consumers."

Bad intent documentation reads like:

- "This was implemented to improve the overall quality of the system."
- "This change ensures better performance and reliability."
- "The decision was made after careful consideration of various factors."

If the conversation does not contain meaningful intent, do not invent it. Silence is better than filler.

## Inclusion Bar

Update the README only when all of these are true:

- The change is user-facing, architecturally significant, or alters documented behavior.
- The change is committed (not speculative or abandoned).
- The existing README does not already accurately describe the current state.

## Exclusions

Never add to the README:

- Secrets, tokens, credentials, connection strings.
- Developer-specific local setup (use a CONTRIBUTING.md for that).
- Temporary workarounds with an expiry date.
