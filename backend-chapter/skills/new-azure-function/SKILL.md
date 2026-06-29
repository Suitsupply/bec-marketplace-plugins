---
name: new-azure-function
description: >-
  Scaffold a new Azure Functions service from the Backend Chapter template by
  copying backend-chapter/template and renaming "Template" to the new project
  name across projects, devops, bicep, and docs. Use when the user says "I want
  a new Azure function", asks to create/bootstrap a new function app or service,
  or wants to clone the chapter template into a new project.
---

# New Azure Function

Scaffold a brand-new Azure Functions service from the Backend Chapter [`template/`](../../template) project. The procedure is: ask four questions, copy the template into a target folder, then perform a full `Template` -> project-name rename plus devops/bicep/Confluence value substitution.

Works on Cursor and Claude Code.

## Workflow checklist

Copy this checklist and track progress:

```
- [ ] Step 1: Collect answers (project name, readme page id, bicep params, csproj description)
- [ ] Step 2: Derive naming tokens and confirm the resource slug
- [ ] Step 3: Copy the template into the target folder (exclude bin/obj/TestResults)
- [ ] Step 4: Apply the rename (folders, files, contents) — see reference/replacement-map.md
- [ ] Step 5: Set bicep params + Confluence page id/titles
- [ ] Step 6: Verify (build + unit/component tests, scan for leftovers)
```

## Step 1: Collect answers

Use the AskQuestion tool (one form, four questions). Do not start copying until all answers are in.

1. **Project name** — PascalCase, dotted-segment friendly (e.g. `ShopifyIntegration`). This replaces `Template`.
2. **Confluence page id** — the page id where the first document (readme) is published.
3. **Bicep parameters** — collected for both `azuredeploy.parameters.tst.json` and `azuredeploy.parameters.prd.json`:
   - `logAnalyticsWorkspaceId` (full resource id string)
   - `contributorPrincipalIds` (array of Azure AD group object ids)
   - `teamNameTag`
4. **csproj `<Description>`** — short description used in the `<Description>` element of every `.csproj`.

## Step 2: Derive naming tokens

From the project name, derive two replacement tokens and confirm the slug with the user before proceeding:

- **`Template`** (PascalCase) -> the project name. Used for namespaces, folder/file names, `.slnx`/`.csproj` names, `<Product>`, `<PackageId>`, `ServiceSettings` value, Confluence titles, and docs.
- **`template`** (lowercase resource slug) -> project name lowercased with all non-alphanumeric characters removed. Used for Azure resource names. Storage account names must stay lowercase, alphanumeric, 3-24 chars — warn the user if the slug breaks that rule.

Example: project name `ShopifyIntegration` -> PascalCase token `ShopifyIntegration`, slug `shopifyintegration` (resources become `shopifyintegration-tst-af`, storage `shopifyintegrationtstsa`, etc.).

## Step 3: Copy the template

- Confirm the target directory (the developer's new/empty repo, or a folder they name).
- Copy everything under `backend-chapter/template/` into the target, **excluding** `bin/`, `obj/`, and `TestResults/` anywhere in the tree.

## Step 4: Apply the rename

Perform folder/file renames first, then content replacements. The complete, file-by-file map is in [reference/replacement-map.md](reference/replacement-map.md). In summary:

- Rename the eight project folders + their `.csproj`, and `Template.slnx`, from `Template.*` to `<Name>.*`.
- Replace the `Template` token in file contents across the whole tree: C# `namespace`/`using`, `.slnx` project paths, `<Product>`, `<PackageId>` (`Suitsupply.Template.Api.Models` -> `Suitsupply.<Name>.Api.Models`), `PackageTags`, `InternalsVisibleTo`, pipeline project/solution paths, `webApiPackage`, `sonarProjectKey`/`sonarProjectName`.
- Set every `.csproj` `<Description>` to the Step 1 answer.
- Replace the `ServiceSettings` service name (`Template.Api` -> `<Name>.Api`) and the component-test in-memory value (`"Template"` -> `<Name>`).
- Replace the `template` resource slug across the pipeline and both bicep parameter files (resource names + storage account names).

## Step 5: Bicep params + Confluence

- In both bicep parameter files, set `logAnalyticsWorkspaceId`, `contributorPrincipalIds`, and `teamNameTag` to the Step 1 answers. Leave `serviceBusNamespaceName` (`placeholder-bus`) and `teamKeyVault` as derived/placeholder values and add a `TODO` note for the developer.
- In the pipeline, set the readme Confluence step's `confluencePageId` to the Step 1 answer and rename its title to `<Name> - Readme`. For the other two publish steps (Technical Overview, Example Feature), keep them but set `confluencePageId` to `TODO-CONFLUENCE-PAGE-ID` and rename the titles. Also fix the existing path mismatch: those steps reference `docs/00-technical-overview.md` / `docs/00-example-feature.md`, but the files are `docs/01-technical-overview.md` / `docs/02-example-feature.md`.

## Step 6: Verify

- Run `dotnet build` on the new solution.
- Run `dotnet test` on the UnitTests project, then the ComponentTests project **sequentially** (running both in parallel can trigger a file-lock on the WebJobs DLL in the Api worker-extensions output).
- Scan the new tree for leftover `Template`/`template` occurrences and report any to the developer for manual review.
