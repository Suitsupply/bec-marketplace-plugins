/// <reference types="bun-types-no-globals/lib/index.d.ts" />

import { execFileSync } from "node:child_process";
import { stdin } from "bun";

type Host = "cursor" | "claude";

// Pathspecs that match every README.md in the repo — root and nested. The
// `:(glob)` magic lets `**/` match zero or more leading directories, so the
// root README is covered too.
const README_PATHSPECS = [":(glob)**/README.md", "README.md"];

function parseHost(): Host {
  return process.argv[2] === "claude" ? "claude" : "cursor";
}

function git(args: string[]): string | null {
  try {
    return execFileSync("git", args, {
      encoding: "utf-8",
      stdio: ["ignore", "pipe", "ignore"],
    }).trim();
  } catch {
    return null;
  }
}

function isGitRepo(): boolean {
  return git(["rev-parse", "--is-inside-work-tree"]) === "true";
}

function headSha(): string | null {
  const sha = git(["rev-parse", "HEAD"]);
  return sha && sha.length > 0 ? sha : null;
}

// The baseline is derived, not stored: the last commit that touched any README.
// Because it comes from git history, every clone computes the same value, so the
// baseline is shared across the team with no workspace-local file to drift.
function lastDocumentedSha(): string | null {
  const sha = git(["log", "-1", "--format=%H", "--", ...README_PATHSPECS]);
  return sha && sha.length > 0 ? sha : null;
}

// An uncommitted README edit means a documentation pass is already staged and
// waiting for the user to commit it. Stay quiet so the hook does not re-prompt
// on every turn while those edits sit in the working tree.
function hasUncommittedReadme(): boolean {
  const status = git(["status", "--porcelain", "--", ...README_PATHSPECS]);
  return status !== null && status.length > 0;
}

function buildInstruction(base: string | null, head: string): string {
  const scope =
    base === null
      ? `No README.md exists yet — treat this as a first run and read the full source tree for the "what".`
      : `Document the new commits in \`${base}..${head}\` — use \`git log --stat ${base}..${head}\` and \`git diff ${base}..${head}\` for the "what".`;
  return [
    "The repository has committed changes that the documentation may not reflect yet.",
    '1. Distill a short "why" summary from the current conversation — the design decisions, constraints, and rejected alternatives behind these changes. A few sentences at most. Do not invent intent that was not discussed.',
    `2. Launch the \`continuous-documentation\` subagent with the Task tool (run_in_background: true). Pass it your "why" summary and this instruction: ${scope}`,
    "The subagent applies the `documentation-standards` skill and updates the appropriate README.md (root or project level). Do not commit the README changes as part of this sync — leave committing to the user.",
  ].join("\n");
}

// Documentation is owed when HEAD has moved past the last commit that touched a
// README and no doc edits are already pending. Committing the README is what
// advances the baseline, so there is no marker to write back.
function evaluate(): string | null {
  if (!isGitRepo()) {
    return null;
  }
  const head = headSha();
  if (head === null) {
    return null;
  }
  const base = lastDocumentedSha();
  if (base === head) {
    return null;
  }
  if (hasUncommittedReadme()) {
    return null;
  }
  return buildInstruction(base, head);
}

function emit(host: Host, message: string | null): void {
  if (message === null) {
    console.log(JSON.stringify({}));
    return;
  }
  if (host === "claude") {
    console.log(JSON.stringify({ decision: "block", reason: message }));
    return;
  }
  console.log(JSON.stringify({ followup_message: message }));
}

async function main(): Promise<void> {
  const host = parseHost();

  let status = "completed";
  let claudeLoopActive = false;
  try {
    const raw = await stdin.text();
    const input = raw.trim().length > 0 ? JSON.parse(raw) : {};
    if (typeof input.status === "string") {
      status = input.status;
    }
    claudeLoopActive = input.stop_hook_active === true;
  } catch {
    // proceed with defaults
  }

  // Cursor only consumes followup_message on completed turns; Claude guards
  // against re-entrancy with stop_hook_active.
  const canDeliver = host === "claude" ? !claudeLoopActive : status === "completed";
  if (!canDeliver) {
    emit(host, null);
    return;
  }

  try {
    emit(host, evaluate());
  } catch (error) {
    console.error("[continuous-documentation-trigger] failed", error);
    emit(host, null);
  }
}

await main();
process.exit(0);
