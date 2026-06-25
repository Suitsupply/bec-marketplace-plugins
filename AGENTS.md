# AGENTS.md

Guidance for agents working in this repository. This repo is a **dual-host plugin
marketplace**: every plugin is published to both **Claude Code** (`.claude-plugin/`)
and **Cursor** (`.cursor-plugin/`).

## Repository layout

```
.claude-plugin/marketplace.json     # Claude marketplace index (no per-plugin versions)
.cursor-plugin/marketplace.json     # Cursor marketplace index (carries per-plugin versions)
<plugin>/
  .claude-plugin/plugin.json        # Claude manifest  (has "version")
  .cursor-plugin/plugin.json        # Cursor manifest  (has "version")
  hooks/
    hooks.claude.json               # Claude hook notation
    hooks.cursor.json               # Cursor hook notation
  skills/ agents/ commands/ ...
```

### Host notation differences

The two hosts are **not** interchangeable. Keep host-specific files separate and
referenced from their own manifest (`"hooks": "./hooks/hooks.<host>.json"`):

| | Claude | Cursor |
|---|---|---|
| Hook event name | PascalCase (`Stop`) | lowercase (`stop`) |
| Hook entry shape | `{ "hooks": [ { "type": "command", "command": … } ] }` | `{ "command": … }` |
| Plugin-root var | `${CLAUDE_PLUGIN_ROOT}` | `${CURSOR_PLUGIN_ROOT}` |
| Top-level `version` key | absent | `1` (schema version, **not** the plugin version) |

> Do **not** rely on Claude's auto-discovery of `hooks/hooks.json`. Name hook files
> per host and point each manifest at its own file, so the two notations never collide.

## Versioning policy

### 1. One shared version per plugin

A plugin has a **single version** that is kept identical in all three places:

1. `<plugin>/.claude-plugin/plugin.json` → `"version"`
2. `<plugin>/.cursor-plugin/plugin.json` → `"version"`
3. `.cursor-plugin/marketplace.json` → that plugin's entry → `"version"`

(`.claude-plugin/marketplace.json` has no per-plugin version field — nothing to bump there.)

If you change any of the three, change all three in the same commit.

### 2. Bump whenever a plugin's shipped content changes

Bump a plugin's version when you modify what it ships — skills, agents, commands,
hooks, or its manifest. Use [SemVer](https://semver.org/):

- **patch** (`x.y.Z`) — internal fix or refactor, no user-facing behavior change
  (e.g. splitting hooks into host-specific files, fixing a typo in frontmatter).
- **minor** (`x.Y.0`) — new capability or a structural change users can observe.
- **major** (`X.0.0`) — breaking change (removed/renamed skill, incompatible hook).

Repo-only files (`README.md`, `AGENTS.md`, CI) do **not** require a version bump.

### 3. Never reuse a published version number

When a plugin's versions have diverged across hosts, **adopt the highest already-published
number and bump up from there** — never reset to a lower line that was already shipped.
Example: `continuous-documentation` shipped `2.1.0` on Cursor, so its shared line is `2.x`;
a patch on top becomes `2.1.1`, not `1.x.x`.

### Current versions (keep this table current)

| Plugin | Version |
|---|---|
| backend-chapter | 1.1.0 |
| continuous-documentation | 2.1.1 |
| email-agent-learning | 1.0.1 |
| out-of-process-tests | 0.1.0 |

> `out-of-process-tests` is an **external-repo** plugin: its manifests and skill live in
> [`bec-template-tests-outofprocess`](https://github.com/Suitsupply/bec-template-tests-outofprocess), so its version
> source-of-truth is that repo (Claude refs it via a `github` source; Cursor via a remote-URL `source`). Only the
> Cursor `marketplace.json` entry carries a mirrored `version` here — keep it in step with the external repo.

## Checklist when changing a plugin

1. Make the code/skill/hook change.
2. Decide the bump level (patch / minor / major).
3. Update the version in all three locations (§1).
4. Update the table above.
5. Validate every touched JSON file parses.
