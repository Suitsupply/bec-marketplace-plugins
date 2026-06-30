# Cursor Team Plugins

Team marketplace repository for Cursor IDE plugins. Import this repository as a team marketplace on the Cursor Teams or Enterprise plan.

## Setup

1. Go to **Dashboard > Settings > Plugins**.
2. Under **Team Marketplaces**, click **Import**.
3. Paste this repository's URL and continue.
4. Review the listed plugins and assign distribution groups.
5. Set each plugin as **Required** (auto-installed) or **Optional** per group.

## Plugins

| Plugin | Description |
|--------|-------------|
| [continuous-documentation](./continuous-documentation/) | Keeps README in sync as commits land; a stop hook detects when `HEAD` moves and triggers the **continuous-documentation** agent to document the new range plus the conversational "why"; **documentation-standards** skill holds doc rules. |
| [email-agent-learning](./email-agent-learning/) | Mines agent transcripts every 10 turns to build a local **EMAIL-AGENT.md** knowledge file covering email infrastructure, projects, tools, workflows, and decisions. All learnings stay workspace-local and are never committed. |
| [backend-chapter](./backend-chapter/) | Backend Chapter plugin: .NET skill suite, Jira workflows, MCP servers (Cursor). Skills work on Cursor and Claude Code. |

## Adding a new plugin

1. Create a new directory at the root named after your plugin (kebab-case).
2. Add a `.cursor-plugin/plugin.json` manifest inside it.
3. Add your skills, hooks, rules, or other components.
4. Register the plugin in `.cursor-plugin/marketplace.json` under the `plugins` array.

## License

MIT
