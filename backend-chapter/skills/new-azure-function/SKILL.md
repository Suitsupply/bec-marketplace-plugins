---
name: new-azure-function
description: >-
  Scaffold a new Azure Functions service from the Backend Chapter template by
  fetching the template from GitHub and renaming "Template" to the new project
  name across projects, devops, bicep, and docs. Use when the user says "I want
  a new Azure function", asks to create/bootstrap a new function app or service,
  or wants to clone the chapter template into a new project.
---

# New Azure Function

Scaffold a brand-new Azure Functions service from the Backend Chapter template. The procedure is: ask four questions, fetch the template from GitHub into a target folder, then perform a full `Template` -> project-name rename plus devops/bicep/Confluence value substitution.

Works on Cursor and Claude Code.

## Template source

The template lives in the `bec-marketplace-plugins` repository on GitHub under `backend-chapter/template`:

- Repository: `https://github.com/Suitsupply/bec-marketplace-plugins`
- Path within repo: `backend-chapter/template`
- Branch: use `main`. Until the coding-standards work is merged, the template only exists on `feature/BEC-319-coding-standards` — fall back to that branch if `backend-chapter/template` is not found on `main`.

## Workflow checklist

Copy this checklist and track progress:

```
- [ ] Step 1: Collect answers (project name, readme page id, bicep params, csproj description)
- [ ] Step 2: Derive naming tokens and confirm the resource slug
- [ ] Step 3: Fetch the template from GitHub into the target folder (then verify .cs count)
- [ ] Step 4: Apply the rename — move folders, rename project files, replace text IN PLACE (non-destructive); see reference/replacement-map.md
- [ ] Step 5: Set bicep params + Confluence page id/titles
- [ ] Step 6: Verify (build + unit/component tests, scan for leftovers)
```

## Step 1: Collect answers

Use the AskQuestion tool (one form, four questions). Do not start fetching until all answers are in.

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

## Step 3: Fetch the template from GitHub

- Confirm the target directory (the developer's new/empty repo, or a folder they name).
- Pull only `backend-chapter/template` from GitHub using a shallow sparse checkout into a temp directory, then copy its contents into the target. Use `main`; if the template path is missing (pre-merge), retry with `--branch feature/BEC-319-coding-standards`.

```bash
git clone --depth 1 --filter=blob:none --sparse \
  https://github.com/Suitsupply/bec-marketplace-plugins.git <temp-dir>
cd <temp-dir>
git sparse-checkout set backend-chapter/template
```

- Copy the **entire** `<temp-dir>/backend-chapter/template/` tree into the target **recursively and verbatim** — every file and folder (`.cursorignore`, `.editorconfig`, `.gitignore`, `README.md`, `Template.slnx`, and the full `devops/`, `docs/`, `src/`, `test/` trees including all nested `.cs` files such as `src/Template.Api/Program.cs`). Use a recursive directory copy (`cp -r`, `robocopy /E`, or `Copy-Item -Recurse`); do **not** enumerate or cherry-pick files. `bin/`, `obj/`, and `TestResults/` are gitignored so they will not be present.
- **Verify the copy before continuing.** Record the source `.cs` count and confirm the target matches:

```bash
find "<temp-dir>/backend-chapter/template" -name '*.cs' | wc -l   # SOURCE_CS_COUNT
find "<target>" -name '*.cs' | wc -l                              # must equal SOURCE_CS_COUNT
```

  If the counts differ, the copy failed — fix it before proceeding. Then remove the temp directory.

## Step 4: Apply the rename

> **The rename is non-destructive. Never delete files, never recreate folders, and never copy only "the files that matter."** A folder rename must MOVE the existing directory with all of its contents intact; content edits happen IN PLACE. The most common failure is recreating each renamed project folder and moving only its `.csproj` / `local.settings.json`, which silently drops every subfolder and `.cs` file (including `Program.cs`). Do not do that.

Work against the **target** tree (the temp clone is already deleted). Do the three sub-steps strictly in order. The complete, file-by-file map is in [reference/replacement-map.md](reference/replacement-map.md).

**4a. Rename the eight project directories** (move the whole directory; contents come with it):

```bash
mv src/Template.Api          src/<Name>.Api
mv src/Template.Api.Models   src/<Name>.Api.Models
mv src/Template.App          src/<Name>.App
mv src/Template.App.Models   src/<Name>.App.Models
mv src/Template.Infra        src/<Name>.Infra
mv test/Template.UnitTests        test/<Name>.UnitTests
mv test/Template.ComponentTests   test/<Name>.ComponentTests
mv test/Template.IntegrationTests test/<Name>.IntegrationTests
```

**4b. Rename the nine project files** (the `.slnx` and each `.csproj`), e.g. `mv Template.slnx <Name>.slnx`, `mv src/<Name>.Api/Template.Api.csproj src/<Name>.Api/<Name>.Api.csproj`, etc. Rename the file only — do not touch its folder's other contents.

**4c. Replace text IN PLACE across every remaining file** (recursive find + replace; do not move or delete anything, and skip any `.git/` directory):

- `Template` token -> `<Name>`: C# `namespace`/`using` and type references, `.slnx` project paths, `<Product>`, `<PackageId>` (`Suitsupply.Template.Api.Models` -> `Suitsupply.<Name>.Api.Models`), `PackageTags`, `InternalsVisibleTo`, pipeline project/solution paths, `webApiPackage`, `sonarProjectKey`/`sonarProjectName`.
- Set every `.csproj` `<Description>` to the Step 1 answer.
- `ServiceSettings` service name `Template.Api` -> `<Name>.Api`, and the component-test in-memory value `"Template"` -> `<Name>`.
- `template` resource slug -> `<slug>` across the pipeline and both bicep parameter files (resource names + storage account names).

**4d. Verify nothing was lost.** The `.cs` count must equal `SOURCE_CS_COUNT` from Step 3, and the key nested files/folders must still exist:

```bash
find . -name '*.cs' | wc -l            # must equal SOURCE_CS_COUNT
ls src/<Name>.Api/Program.cs           # must exist
ls -d src/<Name>.Api/Example test/<Name>.UnitTests/Example   # must exist
```

  If anything is missing, the rename was destructive — stop and restore from a fresh fetch.

## Step 5: Bicep params + Confluence

- In both bicep parameter files, set `logAnalyticsWorkspaceId`, `contributorPrincipalIds`, and `teamNameTag` to the Step 1 answers. Leave `serviceBusNamespaceName` (`placeholder-bus`) and `teamKeyVault` as derived/placeholder values and add a `TODO` note for the developer.
- In the pipeline, set the readme Confluence step's `confluencePageId` to the Step 1 answer and rename its title to `<Name> - Readme`. For the other two publish steps (Technical Overview, Example Feature), keep them but set `confluencePageId` to `TODO-CONFLUENCE-PAGE-ID` and rename the titles. Also fix the existing path mismatch: those steps reference `docs/00-technical-overview.md` / `docs/00-example-feature.md`, but the files are `docs/01-technical-overview.md` / `docs/02-example-feature.md`.

## Step 6: Verify

- Run `dotnet build` on the new solution.
- Run `dotnet test` on the UnitTests project, then the ComponentTests project **sequentially** (running both in parallel can trigger a file-lock on the WebJobs DLL in the Api worker-extensions output).
- Scan the new tree for leftover `Template`/`template` occurrences and report any to the developer for manual review.
