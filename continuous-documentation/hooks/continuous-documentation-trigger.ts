/// <reference types="bun-types-no-globals/lib/index.d.ts" />

import { execFileSync } from "node:child_process";
import {
  appendFileSync,
  existsSync,
  mkdirSync,
  readFileSync,
  writeFileSync,
} from "node:fs";
import { resolve } from "node:path";
import { stdin } from "bun";

type Host = "cursor" | "claude";

const MARKER_DIR = resolve(".continuous-documentation");
const MARKER_PATH = resolve(MARKER_DIR, "last-documented-sha");
const GITIGNORE_PATH = resolve(".gitignore");
const GITIGNORE_ENTRY = ".continuous-documentation/";
const ROOT_README = resolve("README.md");

interface TriggerDecision {
  message: string | null;
  head: string | null;
  seedOnly: boolean;
}

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

function readMarker(): string | null {
  if (!existsSync(MARKER_PATH)) {
    return null;
  }
  try {
    const value = readFileSync(MARKER_PATH, "utf-8").trim();
    return value.length > 0 ? value : null;
  } catch {
    return null;
  }
}

function writeMarker(sha: string): void {
  if (!existsSync(MARKER_DIR)) {
    mkdirSync(MARKER_DIR, { recursive: true });
  }
  writeFileSync(MARKER_PATH, `${sha}\n`, "utf-8");
}

function ensureGitignore(): void {
  if (!existsSync(resolve(".git"))) {
    return;
  }
  try {
    const content = existsSync(GITIGNORE_PATH)
      ? readFileSync(GITIGNORE_PATH, "utf-8")
      : "";
    if (content.split(/\r?\n/).some((line) => line.trim() === GITIGNORE_ENTRY)) {
      return;
    }
    const separator = content.length > 0 && !content.endsWith("\n") ? "\n" : "";
    appendFileSync(GITIGNORE_PATH, `${separator}${GITIGNORE_ENTRY}\n`, "utf-8");
  } catch {
    // best-effort; never break the hook over a gitignore update
  }
}

function buildInstruction(base: string | null, head: string): string {
  const scope =
    base === null
      ? `No README.md exists at the repository root — treat this as a first run and read the full source tree for the "what".`
      : `Document the new commits in \`${base}..${head}\` — use \`git log --stat ${base}..${head}\` and \`git diff ${base}..${head}\` for the "what".`;
  return [
    "The repository has committed changes that the documentation may not reflect yet.",
    '1. Distill a short "why" summary from the current conversation — the design decisions, constraints, and rejected alternatives behind these changes. A few sentences at most. Do not invent intent that was not discussed.',
    `2. Launch the \`continuous-documentation\` subagent with the Task tool (run_in_background: true). Pass it your "why" summary and this instruction: ${scope}`,
    "The subagent applies the `documentation-standards` skill and updates the appropriate README.md (root or project level). Do not commit the README changes as part of this sync — leave committing to the user.",
  ].join("\n");
}

// Decide whether documentation is owed, and advance the marker so the same
// change is never reported twice. The marker is the single source of state.
function evaluate(): TriggerDecision {
  if (!isGitRepo()) {
    return { message: null, head: null, seedOnly: false };
  }
  const head = headSha();
  if (head === null) {
    return { message: null, head: null, seedOnly: false };
  }
  const marker = readMarker();
  if (marker === head) {
    return { message: null, head, seedOnly: false };
  }

  const rootReadmeExists = existsSync(ROOT_README);

  // Existing repo with a README but no marker yet (fresh install): adopt the
  // current commit as the baseline silently, so history is not re-documented.
  if (marker === null && rootReadmeExists) {
    return { message: null, head, seedOnly: true };
  }

  const base = rootReadmeExists ? marker : null;
  return { message: buildInstruction(base, head), head, seedOnly: false };
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
    ensureGitignore();
    const decision = evaluate();
    if (decision.head !== null && (decision.seedOnly || decision.message !== null)) {
      writeMarker(decision.head);
    }
    emit(host, decision.seedOnly ? null : decision.message);
  } catch (error) {
    console.error("[continuous-documentation-trigger] failed", error);
    emit(host, null);
  }
}

await main();
process.exit(0);
