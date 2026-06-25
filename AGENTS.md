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

## Checklist when changing a plugin

1. Make the code/skill/hook change.
2. Decide the bump level (patch / minor / major).
3. Update the version in all three locations (§1).
4. Update the table above.
5. Validate every touched JSON file parses.

## Cursor Cloud specific instructions

This repo is a plugin marketplace (JSON manifests + Markdown skills/agents/commands).
There is **no application server** and **no `package.json`/lockfile** — the only
executable code is the two TypeScript stop-hook scripts, which run under **`bun`**.

- `bun` is the runtime for the hooks. The startup update script installs it to
  `~/.bun/bin`. If `bun` is not on your `PATH` in a fresh shell, add it:
  `export PATH="$HOME/.bun/bin:$PATH"`.
- There is no dependency install step (no `package.json`); do **not** run `bun install`.
- "Lint/test" here = (a) confirm every JSON file parses, and (b) type/build-check the
  hooks:
  - JSON: `find . -name '*.json' -not -path './.git/*' -print0 | xargs -0 -n1 jq empty`
  - Hooks build: `bun build continuous-documentation/hooks/continuous-documentation-trigger.ts --target=bun >/dev/null`
    (and the same for `email-agent-learning/hooks/email-agent-learning-stop.ts`).
- Run a hook end-to-end (it reads a JSON event on stdin and prints a JSON result):
  `echo '{"status":"completed"}' | bun run continuous-documentation/hooks/continuous-documentation-trigger.ts cursor`
- `continuous-documentation` only emits a `followup_message`/`block` when `HEAD` has
  moved **past** the last commit that touched any `README.md`. In a repo whose README
  is already current it correctly returns `{}` — to see the active path, test in a temp
  git repo where the latest commit does not touch a README.
- `email-agent-learning` thresholds are tunable via env vars
  `EMAIL_AGENT_LEARNING_MIN_TURNS` (default 10) and `EMAIL_AGENT_LEARNING_MIN_MINUTES`
  (default 1); it also needs a `transcript_path` whose mtime has advanced to trigger.
  It writes workspace-local state under `.cursor/hooks/state/` (gitignored).
