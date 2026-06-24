# `.csproj` PropertyGroup conventions

> Reference **5** — Standard `PropertyGroup` blocks for every `src/` and `test/` project.

Every `src/` and `test/` project starts with the same three `PropertyGroup` blocks. Customize `Product`, `Description`, `PackageTags`, and host-specific properties per project.

Replace `{ServiceName}`, `{Year}`, and descriptions with values for the repo.

---

## Shared blocks (all projects)

Add these **before** `ItemGroup` sections in every `.csproj`.

### 1. Build settings

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

**Test projects** — add `<IsPackable>false</IsPackable>` inside this group.

### 2. Deterministic / CI builds

```xml
<PropertyGroup>
  <Deterministic>true</Deterministic>
  <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
</PropertyGroup>
```

### 3. Package metadata

```xml
<PropertyGroup>
  <Authors>Suitsupply B.V.</Authors>
  <Company>Suitsupply B.V.</Company>
  <Product>{ServiceName}</Product>
  <Description>{One-line description of this project's role}</Description>
  <Copyright>Copyright © Suitsupply B.V. {Year}</Copyright>
  <PackageTags>{semicolon-separated tags}</PackageTags>
  <RepositoryType>git</RepositoryType>
</PropertyGroup>
```

Test projects may omit `PackageTags` when not publishing.

---

## Per-project customizations

| Project | Sdk | Extra properties in group 1 | Metadata notes |
|---------|-----|------------------------------|----------------|
| `{ServiceName}.Api` (Functions) | `Microsoft.NET.Sdk` | `AzureFunctionsVersion` v4, `OutputType` Exe | Tags e.g. `azure-functions;webapi` |
| `{ServiceName}.Api` (Web App) | `Microsoft.NET.Sdk.Web` | — | Tags e.g. `aspnetcore;webapi` |
| `{ServiceName}.App` | `Microsoft.NET.Sdk` | — | Tags e.g. `business-logic;services` |
| `{ServiceName}.App.Models` | `Microsoft.NET.Sdk` | — | Tags e.g. `domain;models`; assembly-level `[ExcludeFromCodeCoverage]` when DTO-only |
| `{ServiceName}.Infra` | `Microsoft.NET.Sdk` | — | Tags e.g. `infrastructure;clients` |
| `{ServiceName}.Api.Models` | `Microsoft.NET.Sdk` | — | Add `PackageId` (NuGet id) |
| `*.UnitTests` | `Microsoft.NET.Sdk` | `IsPackable` false | Coverlet group below |
| `*.ComponentTests` | `Microsoft.NET.Sdk` | `IsPackable` false | Coverlet group below |
| `*.IntegrationTests` | `Microsoft.NET.Sdk` | `IsPackable` false | No coverlet |

**`{ServiceName}.App.Models`** — when the project contains only DTOs with no logic, add assembly-level exclusion (see **dotnet-best-practices** code coverage exclusions):

```xml
<ItemGroup>
  <AssemblyAttribute Include="System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute" />
</ItemGroup>
```

---

## `{ServiceName}.Api` — Azure Functions

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AzureFunctionsVersion>v4</AzureFunctionsVersion>
    <OutputType>Exe</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>

  <PropertyGroup>
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>

  <PropertyGroup>
    <Authors>Suitsupply B.V.</Authors>
    <Company>Suitsupply B.V.</Company>
    <Product>{ServiceName}</Product>
    <Description>Azure Function App entry point for {ServiceName}, handling HTTP triggers and dependency injection setup.</Description>
    <Copyright>Copyright © Suitsupply B.V. {Year}</Copyright>
    <PackageTags>{service-slug};azure-functions;webapi</PackageTags>
    <RepositoryType>git</RepositoryType>
  </PropertyGroup>

  <!-- ItemGroup: host.json, local.settings.json, packages, project references -->
</Project>
```

---

## `{ServiceName}.Api` — ASP.NET Web App

Same as Functions **without** `AzureFunctionsVersion` and `OutputType`. Use `Sdk="Microsoft.NET.Sdk.Web"`.

```xml
<Description>ASP.NET Web App entry point for {ServiceName}, handling HTTP endpoints and dependency injection setup.</Description>
<PackageTags>{service-slug};aspnetcore;webapi</PackageTags>
```

---

## `{ServiceName}.Api.Models` (published NuGet)

Add `PackageId` to the metadata group:

```xml
<PackageId>Suitsupply.{ServiceName}.Api.Models</PackageId>
<Description>API contracts layer containing public request/response models for {ServiceName}.</Description>
<PackageTags>{service-slug};api;contracts</PackageTags>
```

---

## Unit / component test projects — coverlet

After the deterministic group, add:

```xml
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>cobertura</CoverletOutputFormat>
  <CoverletOutput>$(MSBuildProjectDirectory)/TestResults/</CoverletOutput>
  <ExcludeByAttribute>GeneratedCodeAttribute</ExcludeByAttribute>
  <ExcludeByFile>**/Program.cs</ExcludeByFile>
</PropertyGroup>
```

---

## Checklist

- [ ] All three shared `PropertyGroup` blocks present in every `src/` and `test/` `.csproj`
- [ ] Functions Api includes `AzureFunctionsVersion` v4 and `OutputType` Exe
- [ ] `Product` matches solution/service name across all projects in the repo
- [ ] `Description` is unique per project and describes that layer's role
- [ ] `Api.Models` has `PackageId` when the package is published
- [ ] Test projects set `IsPackable` false; unit/component include coverlet group
