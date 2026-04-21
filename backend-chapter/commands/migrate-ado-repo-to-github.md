# Migrate Azure DevOps Repo to GitHub

One-shot migration of an Azure DevOps repository to a freshly created, **empty** GitHub repository. Mirrors all branches, tags, and history; updates Azure Pipelines triggers to GitHub conventions on files that already enable CI; and surfaces the manual ADO follow-ups (disable repo, repoint pipelines).

## Inputs

- `<github-url>` (required) — full HTTPS URL of the target GitHub repo, e.g. `https://github.com/Suitsupply/mdm-productexport`.

## Hard preconditions (abort on any failure)

1. **No uncommitted changes** — `git status --porcelain` must return empty.
2. **Local is in sync with ADO default** — resolve the ADO default branch via `git remote set-head origin -a` then `git symbolic-ref --short refs/remotes/origin/HEAD` (call this `<ado-default>`). After `git fetch origin`, `git rev-list --left-right --count origin/<ado-default>...HEAD` must return `0	0`, and the currently checked-out branch must be `<ado-default>`.
3. **Target GitHub repo is empty**:
   - `gh api repos/<owner>/<repo>` succeeds (repo exists).
   - `gh api repos/<owner>/<repo>/branches --jq 'length'` returns `0`.
   - If not empty, abort with: *"Target repo is not empty (found N branches). This command requires an empty GitHub repository — recreate `<owner>/<repo>` without auto-init."*
4. **Tooling available** — `gh auth status` succeeds and the Azure DevOps MCP is reachable.

If any precondition fails, report which one and stop. Do not attempt to fix automatically.

## Steps

1. **Establish repository context**
   - Identify the repo in the current workspace.
   - In a multi-repo workspace, only process the repo explicitly targeted; if ambiguous, ask the user to clarify.

2. **Run all preconditions above.** Stop on first failure.

3. **Capture Azure DevOps context** (needed for the manual follow-ups after the remote is rewired)
   - Parse the current `origin` URL to extract `organization`, `project`, and `repository` name.
   - Resolve the ADO repo `id` via the Azure DevOps MCP tool `repo_get_repo_by_name_or_id`.
   - Store these in memory for step 8.

4. **Rename local default branch to `main` (if needed)**
   - If `<ado-default>` from step 2 is not `main`, run `git branch -m <ado-default> main`.
   - Note this in the final report so the user knows the GitHub default differs from the ADO default.
   - From this point on, all references to the default branch use `main`.

5. **Update Azure Pipelines YAML triggers** *(only files that already enable CI)*
   - Detect candidate pipeline files at runtime:
     - Glob: `**/*.yml` and `**/*.yaml`.
     - Exclude paths: `**/bin/**`, `**/obj/**`, `**/.git/**`.
     - Content filter (must match **both**):
       1. At least one line matches the regex `^(trigger|pr|stages|jobs|steps|extends|pool|parameters|variables):` (top-level Azure Pipelines key at column 0). This avoids touching docker-compose, GitHub Actions, OpenAPI specs, app configs, etc.
       2. The file contains an existing **CI-enabling** top-level `trigger:` block — i.e. a `trigger:` key at column 0 whose value is a branch list, branch globs, or a `branches:` mapping. Files with **no** `trigger:` block, or with `trigger: none` (CI explicitly disabled), are **skipped** — this command never adds CI to pipelines that didn't have it before.
   - For each qualifying candidate, show the current top-level `trigger:` and `pr:` blocks.
   - Ask the user which files to update (default: all qualifying). If none qualify, skip this step entirely and note it in the final report.
   - For each selected file, replace the top-level `trigger:` block (and the `pr:` block if present) with exactly:
     ```yaml
     pr:
       - main

     trigger:
       - main
     ```
     - Replace any existing `trigger:` block (branch list, globs, `branches:` mapping, etc.).
     - Replace any existing `pr:` block (including `none`); if `pr:` was absent, insert it immediately above the new `trigger:` block.
     - Do **not** add `trigger:` / `pr:` to files that didn't already have a CI-enabling `trigger:` block.
   - Stage and commit the changes locally on `main` with message: `chore: migrate CI triggers to GitHub conventions`.
   - This intentionally puts the local default 1 commit ahead of `origin/main` (ADO). The new commit will be pushed as part of the mirror in step 7.

6. **Rewire `origin` to GitHub**
   - `git remote set-url origin <github-url>`
   - `git fetch origin` against the new remote (sanity check connectivity; expect zero refs returned for an empty repo).

7. **Mirror push to GitHub**
   - `git push --mirror origin`
   - Verify exit code is 0.
   - Verify with `gh api repos/<owner>/<repo>/branches --jq '.[].name'` that `main` now exists.

8. **Manual Azure DevOps follow-ups** (cannot be automated)
   - Neither the Azure DevOps MCP nor project policy permits disabling the repo or rewiring pipeline sources from this command. Print a clear instruction block to the user using the captured ADO context from step 3:
     ```
     The following Azure DevOps changes must be performed manually:

       Organization: <org>
       Project:      <project>
       Repository:   <repository> (id: <repoId>)
       ADO URL:      https://dev.azure.com/<org>/<project>/_git/<repository>
       GitHub URL:   <github-url>

     1. Repoint every Azure Pipeline that built from this ADO repo to GitHub:
          - Pipelines → select the pipeline → Edit → ⋯ → Settings (or "Triggers" → "YAML" source).
          - Change the source from "Azure Repos Git" to "GitHub" and select <github-url>.
          - Update the YAML path / default branch to `main` if it changed.
          - Save. Repeat for every pipeline that targeted this ADO repo.
          - Verify a manual run succeeds end-to-end before relying on CI.

     2. Disable the Azure DevOps repository:
          - Project Settings → Repositories → <repository>.
          - Click the "⋯" menu and choose "Disable Repository".
          - Confirm.
     ```
   - Do not block on these steps; continue to the report.

9. **Report results**
   - GitHub repo URL.
   - Default branch rename (if applied): `<ado-default>` → `main`.
   - Branches and tags pushed (counts).
   - YAML files modified (list) — and explicitly call out any candidate YAML files that were **skipped** because they had no CI-enabling `trigger:` block.
   - Manual ADO follow-ups (always required — repeat the link and the two steps from step 8: repoint pipelines, then disable the repo).
   - Reminder checklist:
     - Configure GitHub branch protection rules on `main` (required reviewers, required status checks).
     - Update any external references (NuGet feeds, deployment pipelines, docs) pointing at the old ADO URL.
     - Remove or archive any ADO build/release pipelines that are no longer needed once the GitHub-sourced pipelines are green.

## Usage

```
/migrate-to-github https://github.com/Suitsupply/mdm-productexport
```

## Notes

- `git push --mirror` is used deliberately to preserve exact commit hashes, all branches, all tags, and notes.
- This command never force-pushes to a non-empty branch and never bypasses branch protection — the empty-repo precondition is what allows the first push to `main` to succeed before policies have any branch to enforce on.
- The trigger rewrite is conservative on purpose: it only touches pipelines that were already wired for CI on ADO. Pipelines with `trigger: none` or no `trigger:` block are left untouched so the user can decide intent explicitly.
- After this command completes, normal workflow resumes: any further changes (including additional YAML tweaks) follow the standard `feature/<JiraTicket>` + PR flow.
