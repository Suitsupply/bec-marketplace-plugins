# Backend Chapter

General-purpose Cursor plugin for the Suitsupply Backend Chapter. Bundles the MCP servers and commands engineers need day-to-day so a single install gives every backend engineer the same baseline.

## What's included

### MCP servers (`mcp.json`)

Auto-discovered by Cursor when the plugin is installed.

| Server | Purpose |
|--------|---------|
| `ado` | Azure DevOps MCP (`@azure-devops/mcp`) scoped to the **Suitsupply** organization. Powers repo lookups, work item queries, and the repo-context behaviour referenced in user rules (e.g. resolving `Suitsupply.*` namespaces back to their source repo). |
| `Azure MCP Server` | Microsoft's Azure MCP (`@azure/mcp`) running in `--read-only` mode against the `fbe43f29-18b2-46ca-a741-bcc4672ba19c` tenant. Used to inspect Azure resources without mutation risk. |

Both servers run via `npx` on demand, so no global install is required.

### Commands

| Command | Description |
|---------|-------------|
| [`/migrate-ado-repo-to-github`](./commands/migrate-ado-repo-to-github.md) | One-shot migration of an Azure DevOps repository to a freshly created, **empty** GitHub repository. Mirrors history, rewrites Azure Pipelines triggers to GitHub conventions, and prints the manual ADO follow-ups (disable repo, repoint pipelines). |
| [`/develop-jira-ticket`](./commands/develop-jira-ticket.md) | Non-interactive development agent. Takes a Jira ticket key, transitions it to In Progress, implements the described changes, updates the Impact Analysis field, posts a single completion comment, and transitions to Review. |

### Skills

Loaded by the agent when explicitly invoked by name (all skills below set `disable-model-invocation: true` — no ambient auto-invocation).

| Skill | Description |
|-------|-------------|
| [`split-jira-ticket`](./skills/split-jira-ticket/SKILL.md) | Carve a piece of work out of an existing Jira ticket into a new ticket — same epic, linked as dependency (`is blocked by`) or follow-up (`relates to`) per a single AC-anchored rule, same-sprint when a dependency lands in an active sprint, labelled `story-builder-assisted`, repo URL copied when applicable. |
| [`refine-jira-ticket`](./skills/refine-jira-ticket/SKILL.md) | Interactive tech-refinement agent. Layers a technical brief onto a business-refined Jira ticket by splicing a `## Tech Refinement` section into the description below a marker; reporter content above the marker is never touched. May ask a clarifying question in chat; does not change ticket status. |
| [`refine-jira-ticket-cloud`](./skills/refine-jira-ticket-cloud/SKILL.md) | Non-interactive cloud counterpart to `refine-jira-ticket`. Same splice behavior, but transitions the ticket through `REFINING` and on to `SPEC REVIEW` and reports the outcome to a single Jira comment. All human communication lives on the ticket. |

Both refinement skills share their analysis logic, guards, splice rules, and brief template via [`skills/refine-jira-ticket-core/REFERENCE.md`](./skills/refine-jira-ticket-core/REFERENCE.md); the two `SKILL.md` files only encode their surface-specific overrides. That folder contains no `SKILL.md`, so it is not discovered as a skill itself.

## Prerequisites

- `npx` available on `PATH` (ships with Node.js).
- For `/migrate-ado-repo-to-github`: `git` and the GitHub CLI (`gh`) authenticated via `gh auth status`.
- Authenticated access to the Suitsupply Azure DevOps organization and the configured Azure tenant for the MCP servers to return data.

## License

MIT
