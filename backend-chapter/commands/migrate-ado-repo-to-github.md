---
name: migrate-ado-repo-to-github
description: >-
  One-shot migration of an Azure DevOps repository to a freshly created, empty
  GitHub repository. Pushes the ADO default branch (renamed to `main`) plus all
  tags and history, then opens a `feature/ci-trigger` PR that updates the
  pipeline entry-point YAML to GitHub conventions, and prints the manual ADO
  follow-ups (repoint pipelines, disable repo).
---

# Migrate Azure DevOps Repo to GitHub

One-shot migration of an Azure DevOps repository to a freshly created, **empty** GitHub repository. Pushes the ADO default branch directly to GitHub as `main` (with full history and all tags), then prepares an optional `feature/ci-trigger` branch that updates the pipeline entry-point YAML to GitHub conventions, and surfaces the manual ADO follow-ups (disable repo, repoint pipelines).

## Inputs

- `<github-url>` (required) — full HTTPS URL of the target GitHub repo, e.g. `https://github.com/Suitsupply/mdm-productexport`.

## Hard preconditions (abort on any failure)

1. **No uncommitted changes** — `git status --porcelain` must return empty. Required because step 6 may run `git reset --hard origin/main` (which would silently discard uncommitted work) and step 7 commits the YAML trigger change (which would otherwise sweep unrelated dirty files into that commit).
2. **ADO default branch is discoverable** — run the snippet below. The push in step 4 reads ADO's state directly from `refs/remotes/origin/<ado-default>`, not from the local working branch, so we don't require local to be on the default branch or in sync with it — only that `origin` is reachable and its default branch is known.

   ```bash
   # set-head + symbolic-ref is the only portable way to ask the remote
   # what its default branch is without hardcoding "main" or "master".
   git remote set-head origin -a >/dev/null
   ado_default=$(git symbolic-ref --short refs/remotes/origin/HEAD | sed 's|^origin/||')
   git fetch origin --quiet
   test -n "$ado_default"
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

4. **Push ADO default branch and tags directly to GitHub** (no local branch rename, no local commits — the rename happens via refspec)

   ```bash
   git push <github-url> "refs/remotes/origin/$ado_default:refs/heads/main"
   git push <github-url> "refs/tags/*:refs/tags/*"
   ```

   - Verify both pushes exit 0.
   - Verify with `git ls-remote --heads <github-url>` that `refs/heads/main` is present.
   - If `$ado_default` was not `main`, note this in the final report so the user knows the GitHub default differs from the ADO default.
   - Old ADO feature branches are intentionally **not** carried over — clean cut.

5. **Rewire local `origin` to GitHub**
   - `git remote set-url origin <github-url>`
   - `git fetch origin` against the new remote (sanity check; should report `refs/heads/main` and any tags pushed in step 4).

6. **Sync local to the new GitHub `main`**
   - If local has a `main` branch already, `git switch main && git reset --hard origin/main`.
   - Otherwise `git switch -c main --track origin/main`.
   - Local `master` (or whatever `<ado-default>` was) can stay around; it's now an orphan local branch and the user can delete it at leisure.

7. **Create `feature/ci-trigger` and update the pipeline entry-point YAML**
   - `git switch -c feature/ci-trigger`
   - Locate the deployment entry-point pipeline file(s):
     - Glob: `**/*.yml` and `**/*.yaml`.
     - Conventional locations: `azure-pipelines.yml` at the repo root, or `devops/azure-pipelines/*.yml`.
   - If multiple candidates are found, list them and ask the user which is the deploy entry point. If none are found, skip the YAML edit, do not create the branch, and note it in the final report.
   - For each selected file, ensure the top-level `pr:` and `trigger:` blocks are exactly:
     ```yaml
     pr:
       - main

     trigger:
       - main
     ```
     Replace any existing `pr:` / `trigger:` blocks (any form: branch list, globs, `branches:` mapping, or `none`); insert them at the top of the file if absent.
   - Stage and commit with message: `chore: migrate CI triggers to GitHub conventions`.
   - `git push -u origin feature/ci-trigger`

8. **Manual follow-ups** (cannot be automated)
   - Print a clear instruction block to the user using the captured ADO context from step 3:
     ```
     The following changes must be performed manually:

       Organization: <org>
       Project:      <project>
       Repository:   <repository>
       ADO URL:      https://dev.azure.com/<org>/<project>/_git/<repository>
       GitHub URL:   <github-url>

     GITHUB:
     1. Open a PR from `feature/ci-trigger` into `main` and merge it
        (skip only if step 7 was skipped because no entry-point pipeline was found).

     AZURE DEVOPS:
     2. Repoint every Azure Pipeline that built from this ADO repo to GitHub:
          - Pipelines → select the pipeline → Edit → ⋯ → Settings (or "Triggers" → "YAML" source).
          - Change the source from "Azure Repos Git" to "GitHub" and select <github-url>.
          - Update the YAML path / default branch to `main` if it changed.
          - Save. Repeat for every pipeline that targeted this ADO repo.
          - Verify a manual run succeeds end-to-end before relying on CI.

     3. Disable the Azure DevOps repository:
          - Project Settings → Repositories → <repository>.
          - Click the "⋯" menu and choose "Disable Repository".
          - Confirm.
     ```
   - Do not block on these steps; continue to the report.

9. **Report results**
   - GitHub repo URL.
   - Default branch rename (if applied): `<ado-default>` → `main`.
   - Tags pushed (count).
   - `feature/ci-trigger` branch status: pushed (with the entry-point YAML file(s) listed), or skipped because no entry-point pipeline was found.
   - Manual follow-ups (always required — repeat the block from step 8: open the `feature/ci-trigger` PR, repoint pipelines, disable the ADO repo).
   - Reminder checklist:
     - Configure GitHub branch protection rules on `main` (required reviewers, required status checks) — do this **after** the `feature/ci-trigger` PR is merged so the first commit doesn't get blocked.
     - Update any external references (NuGet feeds, deployment pipelines, docs) pointing at the old ADO URL.
     - Remove or archive any ADO build/release pipelines that are no longer needed once the GitHub-sourced pipelines are green.

## Usage

```
/migrate-ado-repo-to-github https://github.com/Suitsupply/mdm-productexport
```

## Notes

- The migration push uses explicit refspecs (`refs/remotes/origin/<ado-default>:refs/heads/main` plus `refs/tags/*`) instead of `git push --mirror`. This (a) avoids polluting GitHub with `refs/remotes/*` and other non-branch/non-tag refs that `--mirror` would carry over, (b) performs the `<ado-default>` → `main` rename atomically at push time without touching the local working tree, and (c) leaves stale ADO feature branches behind by design.
- This command never force-pushes to a non-empty branch and never bypasses branch protection — the empty-repo precondition is what allows the first push to `main` to succeed before policies have any branch to enforce on.
- The trigger rewrite targets only the deployment entry-point pipeline (`azure-pipelines.yml` at root or under `devops/azure-pipelines/`). Other YAML files in the repo (templates, app configs, GitHub Actions, etc.) are left untouched.
- The trigger rewrite ships as a `feature/ci-trigger` PR rather than a direct commit on `main`, so even the first post-migration change goes through the standard review flow. The branch name intentionally deviates from the `feature/<JiraTicket>` convention because this is a tooling commit owned by the migration, not a Jira-tracked feature.
- After this command completes, normal workflow resumes: any further changes follow the standard `feature/<JiraTicket>` + PR flow.
