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
| [`/refine-jira-ticket`](./commands/refine-jira-ticket.md) | Senior-engineer technical refinement agent. Takes a Refinement subtask key, analyzes the codebase, and creates/updates `Development` (and optionally `Refactoring`) sibling subtasks under the parent. Jira-only: never edits source code. |

## Prerequisites

- `npx` available on `PATH` (ships with Node.js).
- For `/migrate-ado-repo-to-github`: `git` and the GitHub CLI (`gh`) authenticated via `gh auth status`.
- Authenticated access to the Suitsupply Azure DevOps organization and the configured Azure tenant for the MCP servers to return data.

## License

MIT
