#!/usr/bin/env node
import { appendNdjson, createRunDirectory, loadJson, loadToken, sanitizeError, writeJsonAtomic } from './lib/io.mjs';
import { createDiscordClient, iterateSearch } from './lib/api.mjs';
import { normalizeMessage } from './lib/records.mjs';

export function taskKey(guildId, queryId) {
  return `${guildId}|${queryId}`;
}

function stableValue(value) {
  if (Array.isArray(value)) return value.map(stableValue);
  if (value && typeof value === 'object') {
    return Object.fromEntries(Object.keys(value).sort().map((key) => [key, stableValue(value[key])]));
  }
  return value;
}

function inputFingerprint(plan, guild, query) {
  return JSON.stringify(stableValue({
    guildId: guild.id,
    queryId: query.id,
    content: query.content ?? query.query ?? '',
    purpose: query.purpose ?? 'discovery',
    criteria: query.criteria ?? {},
    includeNsfw: query.includeNsfw !== false,
    dateRange: plan.dateRange ?? {},
    limits: {
      pageSize: plan.options?.pageSize ?? 25,
      maxPages: plan.options?.maxPages ?? null,
      maxMessages: plan.options?.maxMessages ?? null,
      maxOffset: plan.options?.maxOffset ?? 5000,
    },
  }));
}

function createTask(guild, query, plan) {
  const key = taskKey(guild.id, query.id);
  return {
    key,
    guildId: guild.id,
    guildName: guild.name ?? null,
    queryId: query.id,
    query: query.content ?? query.query ?? '',
    purpose: query.purpose ?? 'discovery',
    status: 'pending',
    attempts: 0,
    pages: 0,
    rawCount: 0,
    fetchedCount: 0,
    reportedTotal: 0,
    truncated: false,
    lastError: null,
    inputFingerprint: inputFingerprint(plan, guild, query),
  };
}

function reconcileTasks(state, plan, previousPlan = null) {
  state.version = 2;
  state.tasks ??= {};
  state.completed ??= [];
  state.errors ??= [];
  for (const guild of plan.guilds ?? []) {
    for (const query of plan.queries ?? []) {
      const key = taskKey(guild.id, query.id);
      const expectedFingerprint = inputFingerprint(plan, guild, query);
      const existing = state.tasks[key];
      if (!existing) {
        state.tasks[key] = createTask(guild, query, plan);
        continue;
      }
      let actualFingerprint = existing.inputFingerprint;
      if (!actualFingerprint && previousPlan) {
        const previousGuild = previousPlan.guilds?.find((item) => item.id === guild.id);
        const previousQuery = previousPlan.queries?.find((item) => item.id === query.id);
        if (previousGuild && previousQuery) actualFingerprint = inputFingerprint(previousPlan, previousGuild, previousQuery);
      }
      if (actualFingerprint && actualFingerprint !== expectedFingerprint) {
        throw new Error('Plan mismatch for task ' + key + '; use a new runId or restore the original query definition');
      }
      if (!actualFingerprint && existing.status !== 'pending') {
        throw new Error('Plan mismatch for legacy task ' + key + '; its completed inputs cannot be verified');
      }
      existing.inputFingerprint = expectedFingerprint;
      existing.guildName = guild.name ?? existing.guildName ?? null;
    }
  }
  return state;
}

export function createInitialState(plan, now = new Date().toISOString()) {
  const state = {
    version: 2,
    runId: plan.runId ?? null,
    startedAt: now,
    updatedAt: now,
    completed: [],
    tasks: {},
    errors: [],
  };
  reconcileTasks(state, plan);
  return state;
}

function terminalForRun(task) {
  return ['success', 'truncated', 'failed', 'retryable'].includes(task.status);
}

function duration(value) {
  if (!Number.isFinite(value)) return 'unknown';
  const seconds = Math.max(0, Math.round(value / 1000));
  if (seconds < 60) return `${seconds}s`;
  const minutes = Math.floor(seconds / 60);
  const remainder = seconds % 60;
  return remainder ? `${minutes}m ${remainder}s` : `${minutes}m`;
}

export function createProgressReporter({
  write = (line) => process.stderr.write(`${line}\n`),
  now = () => Date.now(),
} = {}) {
  const startedAt = now();
  const scheduled = new Set();
  const finished = new Set();
  const reporter = async (state, task, page = null) => {
    const tasks = Object.values(state.tasks ?? {});
    if (scheduled.has(task.key)) {
      if (terminalForRun(task) && !page) finished.add(task.key);
      else finished.delete(task.key);
    }
    const completedCount = scheduled.size ? finished.size : tasks.filter(terminalForRun).length;
    const total = scheduled.size || tasks.length;
    const elapsed = Math.max(0, now() - startedAt);
    const etaMs = completedCount > 0 ? (elapsed / completedCount) * Math.max(0, total - completedCount) : Number.NaN;
    const percent = total ? ((completedCount / total) * 100).toFixed(1) : '100.0';
    const location = task.guildName ?? task.guildId ?? 'unknown server';
    const pageDetail = page ? `; page ${page.pageIndex + 1}; ${page.fetchedCount}/${page.totalResults}` : '';
    write(`[${completedCount}/${total} ${percent}%] ${task.query} @ ${location}: ${task.status}${pageDetail}; ETA ${duration(etaMs)}`);
  };
  reporter.begin = async (_state, tasks = []) => {
    for (const task of tasks) {
      scheduled.add(task.key);
      finished.delete(task.key);
    }
  };
  return reporter;
}

function channelNameFor(guild, channelId) {
  return guild?.channels?.find((channel) => channel.id === channelId)?.name ?? null;
}

async function executeTask({ client, plan, runDir, state, task, onCheckpoint }) {
  const query = plan.queries.find((item) => item.id === task.queryId);
  if (!query) {
    task.status = 'failed';
    task.lastError = { name: 'PlanError', message: `Query ${task.queryId} is missing from the plan`, status: null, retryable: false, attempts: 0, body: null };
    state.updatedAt = new Date().toISOString();
    writeJsonAtomic(runDir.state, { ...state, completed: completedKeys(state) });
    await onCheckpoint?.(state, task);
    return;
  }

  task.status = 'running';
  task.attempts += 1;
  task.pages = 0;
  task.rawCount = 0;
  task.fetchedCount = 0;
  task.reportedTotal = 0;
  task.truncated = false;
  task.lastError = null;
  state.updatedAt = new Date().toISOString();
  writeJsonAtomic(runDir.state, { ...state, completed: completedKeys(state) });
  await onCheckpoint?.(state, task);

  try {
    let truncated = false;
    const guild = plan.guilds.find((item) => item.id === task.guildId);
    const queryCriteria = searchCriteria(plan, query);
    for await (const page of iterateSearch({
      client,
      guildId: task.guildId,
      criteria: queryCriteria,
      pageSize: plan.options?.pageSize ?? 25,
      maxPages: plan.options?.maxPages ?? Infinity,
      maxMessages: plan.options?.maxMessages ?? Infinity,
      maxOffset: plan.options?.maxOffset ?? 5000,
    })) {
      task.pages += 1;
      task.rawCount += page.rawCount;
      task.fetchedCount = page.fetchedCount;
      task.reportedTotal = page.totalResults;
      truncated ||= page.truncated;
      for (const message of page.messages) {
        appendNdjson(runDir.messages, normalizeMessage(message, {
          guildId: task.guildId,
          guildName: guild?.name ?? task.guildName,
          channelId: message.channel_id,
          channelName: channelNameFor(guild, message.channel_id),
          query: task.query,
        }));
      }
      state.updatedAt = new Date().toISOString();
      writeJsonAtomic(runDir.state, { ...state, completed: completedKeys(state) });
      await onCheckpoint?.(state, task, page);
    }
    task.truncated = truncated;
    task.status = truncated ? 'truncated' : 'success';
  } catch (error) {
    task.status = error?.retryable ? 'retryable' : 'failed';
    task.lastError = sanitizeError(error);
    state.errors.push({ task: task.key, error: task.lastError, at: new Date().toISOString() });
  }

  state.updatedAt = new Date().toISOString();
  state.completed = completedKeys(state);
  writeJsonAtomic(runDir.state, state);
  await onCheckpoint?.(state, task);
}

async function runWorkers(tasks, concurrency, worker) {
  let nextIndex = 0;
  async function next() {
    while (nextIndex < tasks.length) {
      const task = tasks[nextIndex];
      nextIndex += 1;
      await worker(task);
    }
  }
  const requested = Number.isFinite(concurrency) && concurrency > 0 ? Math.floor(concurrency) : 1;
  const workerCount = Math.min(tasks.length, requested);
  await Promise.all(Array.from({ length: workerCount }, () => next()));
}

function completedKeys(state) {
  return Object.values(state.tasks)
    .filter((task) => task.status === 'success' || task.status === 'truncated')
    .map((task) => task.key);
}

export function assertPlanContainsExistingTasks(state, plan) {
  if (!state?.tasks) return;
  const guildIds = new Set((plan.guilds ?? []).map((guild) => guild.id));
  const queryIds = new Set((plan.queries ?? []).map((query) => query.id));
  const removed = Object.values(state.tasks).filter((task) => !guildIds.has(task.guildId) || !queryIds.has(task.queryId));
  if (removed.length) {
    throw new Error('Plan mismatch: removed existing tasks ' + removed.map((task) => task.key).join(', ') + '; use a new runId');
  }
}

function searchCriteria(plan, query) {
  return {
    ...(plan.dateRange ?? {}),
    ...(query.criteria ?? {}),
    content: query.content ?? query.query ?? '',
    includeNsfw: query.includeNsfw !== false,
  };
}

export async function runSearchPlan({
  client,
  plan,
  runDir,
  now = new Date().toISOString(),
  onCheckpoint,
  allowPlanSubset = false,
} = {}) {
  if (!client?.searchPage) throw new TypeError('client.searchPage is required');
  if (!plan?.guilds || !plan?.queries) throw new TypeError('plan.guilds and plan.queries are required');
  const previousPlan = loadJson(runDir.searchPlan, null);
  const state = loadJson(runDir.state, null) ?? createInitialState(plan, now);
  if (!allowPlanSubset) assertPlanContainsExistingTasks(state, plan);
  reconcileTasks(state, plan, previousPlan);
  writeJsonAtomic(runDir.searchPlan, plan);
  writeJsonAtomic(runDir.run, {
    runId: plan.runId ?? null,
    objective: plan.objective ?? null,
    generatedAt: now,
    evidenceSource: plan.evidenceSource ?? 'discord',
  });

  const pending = Object.values(state.tasks).filter((task) => {
    const inCurrentPlan = plan.guilds.some((guild) => guild.id === task.guildId)
      && plan.queries.some((query) => query.id === task.queryId);
    return inCurrentPlan && task.status !== 'success' && task.status !== 'truncated';
  });
  await onCheckpoint?.begin?.(state, pending);
  await runWorkers(
    pending,
    Number(plan.options?.concurrency ?? 1),
    (task) => executeTask({ client, plan, runDir, state, task, onCheckpoint }),
  );

  state.completed = completedKeys(state);
  state.updatedAt = new Date().toISOString();
  writeJsonAtomic(runDir.state, state);
  return state;
}

function getArg(name, fallback = null) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] ?? fallback : fallback;
}

if (process.argv[1]?.endsWith('run-search-plan.mjs')) {
  const planPath = getArg('--plan');
  if (!planPath) throw new Error('--plan is required');
  const plan = loadJson(planPath);
  const runDir = createRunDirectory({ outputRoot: getArg('--output-root') ?? undefined, runId: plan.runId ?? getArg('--run-id') ?? undefined });
  const token = loadToken({ variable: getArg('--token-env') ?? 'VITE_DISCORD_TOKEN' });
  const client = createDiscordClient({ token, baseUrl: getArg('--base-url') ?? undefined });
  const state = await runSearchPlan({ client, plan, runDir, onCheckpoint: createProgressReporter() });
  console.log(JSON.stringify({ runDirectory: runDir.directory, completed: state.completed.length, tasks: Object.keys(state.tasks).length }, null, 2));
}
