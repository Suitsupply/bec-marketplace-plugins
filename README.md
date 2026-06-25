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
| [backend-chapter](./backend-chapter/) | General-purpose plugin for the Suitsupply Backend Chapter; bundles the Azure DevOps and Azure (read-only) MCP servers plus the **/migrate-ado-repo-to-github** command. |
| [out-of-process-tests](https://github.com/Suitsupply/bec-template-tests-outofprocess) | External-repo plugin. Ships the **create-out-of-process-tests** skill that scaffolds contract / e2e / data-validation tests into a repo by copying the template's sample and wiring the shared daily pipeline. |

## Adding a new plugin

1. Create a new directory at the root named after your plugin (kebab-case).
2. Add a `.cursor-plugin/plugin.json` manifest inside it.
3. Add your skills, hooks, rules, or other components.
4. Register the plugin in `.cursor-plugin/marketplace.json` under the `plugins` array.

## License

MIT
