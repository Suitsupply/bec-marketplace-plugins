/// <reference types="bun-types-no-globals/lib/index.d.ts" />

import { existsSync, mkdirSync, readFileSync, statSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { stdin } from "bun";

const STATE_PATH = resolve(".cursor/hooks/state/email-agent-learning.json");
const INCREMENTAL_INDEX_PATH = resolve(
  ".cursor/hooks/state/email-agent-learning-index.json"
);
const PLUGIN_ROOT = resolve(import.meta.dir, "..");
const SKILL_PATH = resolve(PLUGIN_ROOT, "skills/email-agent-learning/SKILL.md");
const DEFAULT_MIN_TURNS = 10;
const DEFAULT_MIN_MINUTES = 1;

const SUBAGENT_PROMPT = `You are updating the workspace EMAIL-AGENT.md using the email-agent-learning workflow.

Read the full instructions (document structure, extraction rules, merge rules, slop filter, exclusions) from: \`${SKILL_PATH}\`

Then execute:
1. Read the existing EMAIL-AGENT.md at the workspace root (create it from scratch using the structure in the skill if it does not yet exist).
2. Load the incremental index from \`${INCREMENTAL_INDEX_PATH}\` if it exists.
3. List all transcript files in the agent-transcripts folder (check common Cursor paths: \`.cursor/agent-transcripts/\` relative to workspace, or the Cursor project folder for this workspace). Find files with mtime newer than the last indexed mtime.
4. For each new transcript: read it and extract learnings following the extraction rules in the skill. Focus on email infrastructure, projects, tools, workflows, patterns, and decisions.
5. Merge extracted learnings into EMAIL-AGENT.md following the merge rules in the skill. Apply the slop filter to every sentence written.
6. Write back the incremental index with updated transcript mtimes.

If no new transcripts contain extractable learnings, respond exactly: No learnings to add.`;

const FOLLOWUP_MESSAGE =
  `Delegate this to a subagent to preserve context. Use the Task tool with subagent_type "generalPurpose", description "Update EMAIL-AGENT.md", and the following prompt:\n\n${SUBAGENT_PROMPT}`;

interface StopHookInput {
  conversation_id: string;
  generation_id?: string;
  status: "completed" | "aborted" | "error" | string;
  loop_count: number;
  transcript_path?: string | null;
}

interface EmailAgentLearningState {
  version: 1;
  lastRunAtMs: number;
  turnsSinceLastRun: number;
  lastTranscriptMtimeMs: number | null;
  lastProcessedGenerationId: string | null;
}

function parsePositiveInt(value: string | undefined, fallback: number): number {
  if (!value) {
    return fallback;
  }
  const parsed = Number.parseInt(value, 10);
  if (!Number.isFinite(parsed) || parsed <= 0) {
    return fallback;
  }
  return parsed;
}

function loadState(): EmailAgentLearningState {
  const fallback: EmailAgentLearningState = {
    version: 1,
    lastRunAtMs: 0,
    turnsSinceLastRun: 0,
    lastTranscriptMtimeMs: null,
    lastProcessedGenerationId: null,
  };

  if (!existsSync(STATE_PATH)) {
    return fallback;
  }

  try {
    const raw = readFileSync(STATE_PATH, "utf-8");
    const parsed = JSON.parse(raw) as Partial<EmailAgentLearningState>;
    if (parsed.version !== 1) {
      return fallback;
    }
    return {
      version: 1,
      lastRunAtMs:
        typeof parsed.lastRunAtMs === "number" && Number.isFinite(parsed.lastRunAtMs)
          ? parsed.lastRunAtMs
          : 0,
      turnsSinceLastRun:
        typeof parsed.turnsSinceLastRun === "number" &&
        Number.isFinite(parsed.turnsSinceLastRun) &&
        parsed.turnsSinceLastRun >= 0
          ? parsed.turnsSinceLastRun
          : 0,
      lastTranscriptMtimeMs:
        typeof parsed.lastTranscriptMtimeMs === "number" &&
        Number.isFinite(parsed.lastTranscriptMtimeMs)
          ? parsed.lastTranscriptMtimeMs
          : null,
      lastProcessedGenerationId:
        typeof parsed.lastProcessedGenerationId === "string"
          ? parsed.lastProcessedGenerationId
          : null,
    };
  } catch {
    return fallback;
  }
}

function saveState(state: EmailAgentLearningState): void {
  const directory = dirname(STATE_PATH);
  if (!existsSync(directory)) {
    mkdirSync(directory, { recursive: true });
  }
  writeFileSync(STATE_PATH, `${JSON.stringify(state, null, 2)}\n`, "utf-8");
}

function getTranscriptMtimeMs(transcriptPath: string | null | undefined): number | null {
  if (!transcriptPath) {
    return null;
  }

  try {
    return statSync(transcriptPath).mtimeMs;
  } catch {
    return null;
  }
}

function shouldCountTurn(input: StopHookInput): boolean {
  return input.status === "completed" && input.loop_count === 0;
}

async function parseHookInput<T>(): Promise<T> {
  const text = await stdin.text();
  return JSON.parse(text) as T;
}

async function main(): Promise<number> {
  try {
    const input = await parseHookInput<StopHookInput>();
    const state = loadState();

    if (input.generation_id && input.generation_id === state.lastProcessedGenerationId) {
      console.log(JSON.stringify({}));
      return 0;
    }
    state.lastProcessedGenerationId = input.generation_id ?? null;

    const countedTurn = shouldCountTurn(input);
    const turnIncrement = countedTurn ? 1 : 0;
    const turnsSinceLastRun = state.turnsSinceLastRun + turnIncrement;
    const now = Date.now();

    const minTurns = parsePositiveInt(
      process.env.EMAIL_AGENT_LEARNING_MIN_TURNS,
      DEFAULT_MIN_TURNS
    );
    const minMinutes = parsePositiveInt(
      process.env.EMAIL_AGENT_LEARNING_MIN_MINUTES,
      DEFAULT_MIN_MINUTES
    );

    const minutesSinceLastRun =
      state.lastRunAtMs > 0
        ? Math.floor((now - state.lastRunAtMs) / 60000)
        : Number.POSITIVE_INFINITY;

    const transcriptMtimeMs = getTranscriptMtimeMs(input.transcript_path);
    const hasTranscriptAdvanced =
      transcriptMtimeMs !== null &&
      (state.lastTranscriptMtimeMs === null || transcriptMtimeMs > state.lastTranscriptMtimeMs);

    const shouldTrigger =
      countedTurn &&
      turnsSinceLastRun >= minTurns &&
      minutesSinceLastRun >= minMinutes &&
      hasTranscriptAdvanced;

    if (shouldTrigger) {
      state.lastRunAtMs = now;
      state.turnsSinceLastRun = 0;
      state.lastTranscriptMtimeMs = transcriptMtimeMs;
      saveState(state);

      console.log(
        JSON.stringify({
          followup_message: FOLLOWUP_MESSAGE,
        })
      );
      return 0;
    }

    state.turnsSinceLastRun = turnsSinceLastRun;
    saveState(state);
    console.log(JSON.stringify({}));
    return 0;
  } catch (error) {
    console.error("[email-agent-learning-stop] failed", error);
    console.log(JSON.stringify({}));
    return 0;
  }
}

const exitCode = await main();
process.exit(exitCode);
