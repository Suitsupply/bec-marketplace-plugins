# EditorConfig — Annotated Reference

Canonical file: [editorconfig](editorconfig). Copy to your repo root as `.editorconfig`.

## Apply before commit

**Always** format code before opening a PR or pushing — do not rely on reviewers or CI to fix style.

| Tool | When | How |
|------|------|-----|
| **`dotnet format`** | **Every commit** — all IDEs, local and CI | From solution directory: `dotnet format` |
| **Code Cleanup** (Visual Studio) | **Every commit** when using VS | **Analyze → Code Cleanup → Run Code Cleanup** (default profile), or **Ctrl+K**, **Ctrl+E** |

**Workflow:**

1. Finish your changes.
2. In Visual Studio: run **Code Cleanup** on touched files or the whole solution.
3. Run `dotnet format` from the repo/solution root (applies `.editorconfig` rules; same command CI can use).
4. Build and test.

```bash
# Solution root — format all projects
dotnet format

# CI gate — fail if formatting would change files
dotnet format --verify-no-changes
```

`EnforceCodeStyleInBuild` catches many style violations at build time, but **whitespace, import order, and fixable refactorings** still require `dotnet format` and/or Code Cleanup. Commit only the formatted result.

**Cursor / VS Code / Rider:** use `dotnet format` (and the IDE’s format-on-save if enabled). Code Cleanup is a Visual Studio feature — not a substitute for `dotnet format` in other editors.

---

## Core formatting

| Setting | Value | Notes |
|---------|-------|-------|
| `indent_size` | 4 | Spaces, not tabs |
| `indent_style` | space | |
| `root` | true | Do not inherit parent editorconfig |

## Enforced as error (build fails)

| Rule | Rationale |
|------|-----------|
| `csharp_style_namespace_declarations = file_scoped` | Consistent namespace style |
| `IDE0160`, `IDE0161` | Enforce file-scoped namespaces |
| `csharp_style_prefer_primary_constructors = true` | C# 12 primary constructors |
| `IDE0290` | Primary constructor diagnostic |
| `IDE0090` / `csharp_style_implicit_object_creation_when_type_is_apparent` | Target-typed `new(...)` |
| `dotnet_style_namespace_match_folder = true` | Namespace must match folder |
| `IDE0130` | Namespace/folder mismatch diagnostic |
| `csharp_style_expression_bodied_accessors = true` | Expression-bodied accessors |
| `csharp_style_expression_bodied_indexers = true` | Expression-bodied indexers |
| `csharp_style_expression_bodied_lambdas = true` | Expression-bodied lambdas |
| `csharp_style_expression_bodied_properties = true` | Expression-bodied properties |
| `csharp_style_expression_bodied_constructors = false` | Classic constructors for subclassing framework types |
| `csharp_style_expression_bodied_operators = false` | Block-bodied operators |

## Suppressed or relaxed (chapter defaults)

| Rule | Severity | Rationale |
|------|----------|-----------|
| CA2007 (ConfigureAwait) | none | Not required in ASP.NET/Functions host |
| CS8618 (non-nullable property) | none | Use `required`/`init` + guards instead |
| CA1031 (catch Exception) | suggestion | **Api `Functions/` boundaries only** — log then rethrow / HTTP 500 / retry scheduler |
| CA1873 (logging interpolation) | none | Pragmatic; prefer structured logging in new code |
| S107 (too many parameters) | none | Primary constructors can have many deps |
| CA1034 (nested types visible) | none | Test base/derived pattern uses nested classes |

## Suggestions (not build-breaking)

| Rule | Guidance |
|------|----------|
| CA1859 | Prefer concrete types when possible for performance |
| CA1860 | Avoid `Enumerable.Any()` when cheaper check exists |
| CA1861 | Avoid constant array allocations as arguments |
| `csharp_style_var_when_type_is_apparent` | `var` when type is obvious |
| `dotnet_style_prefer_collection_expression` | Collection expressions where clear |

## Naming (suggestion severity)

- Interfaces: `I` prefix (`begins_with_i`)
- Types and members: PascalCase

## Formatting conventions (not EditorConfig)

| Rule | Detail |
|------|--------|
| Method signatures | **Single line** — classes, constructors, and methods on one line unless longer than **160 characters** (positional `record` parameters excepted) |
| Blank line before final `return` | One empty line before the method's **last** `return`. **Exempt** only between `Log*` and `return` — log may sit directly above `return`, but a blank line is still required before the log. Early/guard `return` statements mid-method are unaffected. |

## Project settings (csproj, not EditorConfig)

Pair `.editorconfig` with the standard `PropertyGroup` blocks on **every** `src/` and `test/` project. See [csproj.md](csproj.md) for full templates.

Minimum build settings (group 1 — also include deterministic/CI and metadata groups):

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);CS1591</NoWarn>
</PropertyGroup>
```

Functions Api also requires `AzureFunctionsVersion` v4 and `OutputType` Exe in group 1.
