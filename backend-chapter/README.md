# Backend Chapter

General-purpose plugin for the Suitsupply Backend Chapter on **Cursor** and **Claude Code**. Bundles skills, commands, and (on Cursor) MCP servers so every backend engineer gets the same baseline.

Skills live in `skills/` and are shared by both hosts — one source of truth.

## What's included

### MCP servers (`mcp.json`) — Cursor only

Auto-discovered by Cursor when the plugin is installed.

| Server | Purpose |
|--------|---------|
| `ado` | Azure DevOps MCP (`@azure-devops/mcp`) scoped to the **Suitsupply** organization. Powers repo lookups, work item queries, and the repo-context behaviour referenced in user rules (e.g. resolving `Suitsupply.*` namespaces back to their source repo). |
| `Azure MCP Server` | Microsoft's Azure MCP (`@azure/mcp`) running in `--read-only` mode against the configured tenant. Used to inspect Azure resources without mutation risk. |

Both servers run via `npx` on demand, so no global install is required.

### Commands

| Command | Description |
|---------|-------------|
| [`/migrate-ado-repo-to-github`](./commands/migrate-ado-repo-to-github.md) | One-shot migration of an Azure DevOps repository to a freshly created, **empty** GitHub repository. Mirrors history, rewrites Azure Pipelines triggers to GitHub conventions, and prints the manual ADO follow-ups (disable repo, repoint pipelines). |
| [`/develop-jira-ticket`](./commands/develop-jira-ticket.md) | Non-interactive development agent. Takes a Jira ticket key, transitions it to In Progress, implements the described changes, updates the Impact Analysis field, posts a single completion comment, and transitions to Review. |

### Skills

Available on **Cursor and Claude Code** via the shared `skills/` folder.

#### .NET development

| Skill | Description |
|-------|-------------|
| [`dotnet-best-practices`](./skills/dotnet-best-practices/SKILL.md) | Hub — chapter C# standards, EditorConfig, skill map, async/DI/nullability/code review |
| [`write-src-code`](./skills/write-src-code/SKILL.md) | Production code: Azure Functions, services, enrichment, mappers, Infra clients |
| [`write-tests`](./skills/write-tests/SKILL.md) | Testing pyramid and routing to test sub-skills |
| [`write-unit-tests`](./skills/write-unit-tests/SKILL.md) | NUnit unit test conventions |
| [`write-component-tests`](./skills/write-component-tests/SKILL.md) | Reqnroll component test conventions |
| [`write-integration-tests`](./skills/write-integration-tests/SKILL.md) | Live-environment integration and smoke tests |

#### Jira workflows

| Skill | Description |
|-------|-------------|
| [`split-jira-ticket`](./skills/split-jira-ticket/SKILL.md) | Carve a piece of work out of an existing Jira ticket into a new ticket — same epic, linked as dependency or follow-up. |
| [`refine-jira-ticket`](./skills/refine-jira-ticket/SKILL.md) | Non-interactive tech-refinement agent. Layers a technical brief onto a business-refined Jira ticket. Invokes `analyze-test-suite` for test recommendations. |
| [`analyze-test-suite`](./skills/analyze-test-suite/SKILL.md) | Testing analysis skill. Audits the test suite and produces test recommendations plus a suite health verdict. |

## Prerequisites

- `npx` available on `PATH` (ships with Node.js) — Cursor MCP only.
- For `/migrate-ado-repo-to-github`: `git` and the GitHub CLI (`gh`) authenticated via `gh auth status`.
- Authenticated access to the Suitsupply Azure DevOps organization and the configured Azure tenant for the MCP servers to return data.

## License

MIT
