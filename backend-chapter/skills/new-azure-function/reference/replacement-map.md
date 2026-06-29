# Replacement map

The exact file/folder renames and content substitutions for scaffolding a new Azure Functions service from the GitHub-hosted `backend-chapter/template` (see SKILL.md "Template source"). Apply these against the fetched target tree (never the original template).

Two tokens are used throughout:

- `<Name>` = the PascalCase project name (replaces `Template`).
- `<slug>` = lowercase, alphanumeric-only resource slug (replaces `template` in resource names).

Work in this order: (1) rename folders, (2) rename files, (3) replace file contents.

## 1. Folder renames (under `src/` and `test/`)

- `src/Template.Api` -> `src/<Name>.Api`
- `src/Template.Api.Models` -> `src/<Name>.Api.Models`
- `src/Template.App` -> `src/<Name>.App`
- `src/Template.App.Models` -> `src/<Name>.App.Models`
- `src/Template.Infra` -> `src/<Name>.Infra`
- `test/Template.UnitTests` -> `test/<Name>.UnitTests`
- `test/Template.ComponentTests` -> `test/<Name>.ComponentTests`
- `test/Template.IntegrationTests` -> `test/<Name>.IntegrationTests`

## 2. File renames

- `Template.slnx` -> `<Name>.slnx`
- Each `Template.*.csproj` -> `<Name>.*.csproj` inside its renamed folder:
  - `src/<Name>.Api/Template.Api.csproj` -> `<Name>.Api.csproj`
  - `src/<Name>.Api.Models/Template.Api.Models.csproj` -> `<Name>.Api.Models.csproj`
  - `src/<Name>.App/Template.App.csproj` -> `<Name>.App.csproj`
  - `src/<Name>.App.Models/Template.App.Models.csproj` -> `<Name>.App.Models.csproj`
  - `src/<Name>.Infra/Template.Infra.csproj` -> `<Name>.Infra.csproj`
  - `test/<Name>.UnitTests/Template.UnitTests.csproj` -> `<Name>.UnitTests.csproj`
  - `test/<Name>.ComponentTests/Template.ComponentTests.csproj` -> `<Name>.ComponentTests.csproj`
  - `test/<Name>.IntegrationTests/Template.IntegrationTests.csproj` -> `<Name>.IntegrationTests.csproj`

## 3. Content replacements

### 3a. C# source + tests (all `.cs`)

- Replace the `Template` token everywhere it appears as a namespace segment: `namespace Template.*`, `using Template.*`, fully-qualified type references, and the `Template` token in test class/namespace names. A whole-token replace of `Template` -> `<Name>` across all `.cs` files is correct (every occurrence is a project/namespace reference).

### 3b. `<Name>.slnx`

- All `<Project Path="src/Template.*/Template.*.csproj" />` and `test/...` paths -> `<Name>.*`.

### 3c. `.csproj` files (all eight)

- `<Product>Template</Product>` -> `<Product><Name></Product>`
- `<Description>...for the Template chapter example.</Description>` -> the Step 1 description answer.
- `<PackageTags>` values containing `template` -> `<slug>` (e.g. `template;azure-functions;webapi`).
- `src/<Name>.Api.Models/<Name>.Api.Models.csproj`: `<PackageId>Suitsupply.Template.Api.Models</PackageId>` -> `<PackageId>Suitsupply.<Name>.Api.Models</PackageId>`.
- `src/<Name>.Api/<Name>.Api.csproj`:
  - `<InternalsVisibleTo Include="Template.UnitTests" />` -> `<Name>.UnitTests`
  - `<InternalsVisibleTo Include="Template.ComponentTests" />` -> `<Name>.ComponentTests`
  - `<ProjectReference Include="..\Template.*\Template.*.csproj" />` -> `<Name>.*` (also covered by the slnx/csproj rename).

### 3d. ServiceSettings service name

- `src/<Name>.Api/local.settings.json` and `local.settings.json.example`: `"ServiceSettings__ServiceName": "Template.Api"` -> `"<Name>.Api"`.
- `devops/bicep/resources/functionappsettings.bicep` (line ~42): `ServiceSettings__ServiceName: 'Template.Api'` -> `'<Name>.Api'`.
- `test/<Name>.ComponentTests/Support/ApplicationFactory.cs` (line ~33): `["ServiceSettings:ServiceName"] = "Template"` -> `"<Name>"`.

### 3e. Pipeline — `devops/azurepipelines/azure-pipeline.yaml`

- `solution: 'Template.slnx'` -> `<Name>.slnx`
- `publishAppProjects: 'src/Template.Api/Template.Api.csproj'` -> `<Name>.Api`
- `unitTestProjects`, `componentTestProjects`, `nugetPackageProjects`, `integrationTestProject`, `integrationTestSettingsDir` paths -> `<Name>.*`
- `webApiPackage: 'Template.Api.zip'` -> `<Name>.Api.zip` (appears twice)
- `sonarProjectKey: 'Template'` and `sonarProjectName: 'Template'` -> `<Name>`
- Resource names: `template-tst-rg`, `template-tst-af`, `template-prd-rg`, `template-prd-af` -> `<slug>-...`
- Variable group `template-tst` -> `<slug>-tst`

### 3f. Bicep parameter files — `azuredeploy.parameters.tst.json` and `...prd.json`

Rename resource-name values using `<slug>` (keep the `-tst`/`-prd` env segment):

- `functionAppName`: `template-{env}-af`
- `managedIdentityName`: `template-{env}-af-id`
- `appServicePlanName`: `template-{env}-af-asp`
- `appInsightsName`: `template-{env}-ai`
- `teamKeyVault`: `template-{env}-kv` (rename to `<slug>`; still a placeholder — add a `TODO` if the real vault differs)
- `storageAccountName`: `templatetstsa` / `templateprdsa` -> `<slug>tstsa` / `<slug>prdsa`

Set from Step 1 answers (both files):

- `logAnalyticsWorkspaceId` -> answer
- `contributorPrincipalIds` -> answer (array)
- `teamNameTag` -> answer (currently `templateteam`)

Leave as placeholders with a `TODO` note for the developer:

- `serviceBusNamespaceName` (`placeholder-bus`)
- `swapiBaseUrl` (example default)

### 3g. Confluence steps (in `azure-pipeline.yaml`)

- Readme step: `confluencePageId: '1111111111'` -> Step 1 answer; `confluencePageTitle: 'Template - Readme'` -> `<Name> - Readme`; `markdownFilePath: 'docs/00-readme.md'` stays (file exists).
- Technical Overview step: `confluencePageId` -> `TODO-CONFLUENCE-PAGE-ID`; title `Template - Technical Overview` -> `<Name> - Technical Overview`; fix `markdownFilePath: 'docs/00-technical-overview.md'` -> `docs/01-technical-overview.md`.
- Example Feature step: `confluencePageId` -> `TODO-CONFLUENCE-PAGE-ID`; title `Template - Example Feature` -> `<Name> - Example Feature`; fix `markdownFilePath: 'docs/00-example-feature.md'` -> `docs/02-example-feature.md`.

### 3h. Docs + README

- `README.md`, `docs/00-readme.md`, `docs/01-technical-overview.md`, `docs/02-example-feature.md`: replace the `Template` token in headings/prose and project references with `<Name>`, and the `template` resource slug where it appears in commands/paths. Update the description text to reflect the new service.

## 4. Post-rename verification

- `dotnet build` the `<Name>.slnx` solution.
- `dotnet test test/<Name>.UnitTests` then `dotnet test test/<Name>.ComponentTests` (sequentially).
- Grep the target tree for `Template` and `template`; the only expected remaining matches are unrelated words (e.g. NuGet package names) — review anything else.
