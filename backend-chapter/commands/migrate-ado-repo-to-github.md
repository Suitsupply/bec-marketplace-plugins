---
name: migrate-ado-repo-to-github
description: >-
  One-shot migration of an Azure DevOps repository to a freshly created, empty
  GitHub repository. Mirrors all branches, tags, and history; rewrites Azure
  Pipelines triggers on CI-enabled YAML files; and prints the manual ADO
  follow-ups (repoint pipelines, disable repo).
---

# Migrate Azure DevOps Repo to GitHub

One-shot migration of an Azure DevOps repository to a freshly created, **empty** GitHub repository. Mirrors all branches, tags, and history; updates Azure Pipelines triggers to GitHub conventions on files that already enable CI; and surfaces the manual ADO follow-ups (disable repo, repoint pipelines).

## Inputs

- `<github-url>` (required) — full HTTPS URL of the target GitHub repo, e.g. `https://github.com/Suitsupply/mdm-productexport`.

## Hard preconditions (abort on any failure)

1. **No uncommitted changes** — `git status --porcelain` must return empty.
2. **Local is in sync with ADO default** — run the snippet below. It discovers the ADO default branch dynamically (the repo may use `main`, `master`, `develop`, etc.), refreshes remote refs, and asserts both that the current branch *is* the default and that it has no divergence from `origin`. Any non-zero exit aborts the migration.

   ```bash
   # set-head + symbolic-ref is the only portable way to ask the remote
   # what its default branch is without hardcoding "main" or "master".
   git remote set-head origin -a >/dev/null
   ado_default=$(git symbolic-ref --short refs/remotes/origin/HEAD | sed 's|^origin/||')
   git fetch origin --quiet
   test "$(git rev-parse --abbrev-ref HEAD)" = "$ado_default" \
     && test "$(git rev-list --left-right --count origin/$ado_default...HEAD)" = "0	0"
   ```

   Capture `$ado_default` for use in step 4.
3. **Target GitHub repo is empty** — `git ls-remote <github-url>` must return zero refs. If it returns any refs, abort with: *"Target repo is not empty (found N refs). This command requires an empty GitHub repository — recreate `<owner>/<repo>` without auto-init."* If the command fails outright (auth/network/repo missing), abort and report the error verbatim.

If any precondition fails, report which one and stop. Do not attempt to fix automatically.

## Steps

1. **Establish repository context**
   - Identify the repo in the current workspace.
   - In a multi-repo workspace, only process the repo explicitly targeted; if ambiguous, ask the user to clarify.

2. **Run all preconditions above.** Stop on first failure.

3. **Capture Azure DevOps context** (needed for the manual follow-ups after the remote is rewired)
   - Parse the current `origin` URL to extract `organization`, `project`, and `repository` name.
   - Store these in memory for step 8.

4. **Rename local default branch to `main` (if needed)**
   - If `<ado-default>` from step 2 is not `main`, run `git branch -m <ado-default> main`.
   - Note this in the final report so the user knows the GitHub default differs from the ADO default.
   - From this point on, all references to the default branch use `main`.

5. **Update the pipeline entry-point YAML to trigger CI on `main`**
   - Locate the deployment entry-point pipeline file(s):
      - Glob: `**/*.yml`and `**/*.yaml`.   
   - If multiple candidates are found, list them and ask the user which is the deploy entry point. If none are found, skip this step and note it in the final report.
   - For each selected file, ensure the top-level `pr:` and `trigger:` blocks are exactly:
     ```yaml
     pr:
       - main

     trigger:
       - main
     ```
     Replace any existing `pr:` / `trigger:` blocks (any form: branch list, globs, `branches:` mapping, or `none`); insert them at the top of the file if absent.
   - Stage and commit on `main` with message: `chore: migrate CI triggers to GitHub conventions`.
   - This puts the local default 1 commit ahead of `origin/main` (ADO); it will be pushed as part of the mirror in step 7.

6. **Rewire `origin` to GitHub**
   - `git remote set-url origin <github-url>`
   - `git fetch origin` against the new remote (sanity check connectivity; expect zero refs returned for an empty repo).

7. **Mirror push to GitHub**
   - `git push --mirror origin`
   - Verify exit code is 0.
   - Verify with `git ls-remote --heads origin` that `refs/heads/main` is present.

8. **Manual Azure DevOps follow-ups** (cannot be automated)
   - Disabling the ADO repo and rewiring pipeline sources both require interactive web-UI changes against Azure DevOps. Print a clear instruction block to the user using the captured ADO context from step 3:
     ```
     The following Azure DevOps changes must be performed manually:

       Organization: <org>
       Project:      <project>
       Repository:   <repository>
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
   - Pipeline entry-point YAML file(s) modified (list), or note that none were found.
   - Manual ADO follow-ups (always required — repeat the link and the two steps from step 8: repoint pipelines, then disable the repo).
   - Reminder checklist:
     - Configure GitHub branch protection rules on `main` (required reviewers, required status checks).
     - Update any external references (NuGet feeds, deployment pipelines, docs) pointing at the old ADO URL.
     - Remove or archive any ADO build/release pipelines that are no longer needed once the GitHub-sourced pipelines are green.

## Usage

```
/migrate-ado-repo-to-github https://github.com/Suitsupply/mdm-productexport
```

## Notes

- `git push --mirror` is used deliberately to preserve exact commit hashes, all branches, all tags, and notes.
- This command never force-pushes to a non-empty branch and never bypasses branch protection — the empty-repo precondition is what allows the first push to `main` to succeed before policies have any branch to enforce on.
- The trigger rewrite targets only the deployment entry-point pipeline (`azure-pipelines.yml` at root or under `devops/azure-pipelines/`). Other YAML files in the repo (templates, app configs, GitHub Actions, etc.) are left untouched.
- After this command completes, normal workflow resumes: any further changes (including additional YAML tweaks) follow the standard `feature/<JiraTicket>` + PR flow.
