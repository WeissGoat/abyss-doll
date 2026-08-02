#!/usr/bin/env node
import { buildEvidenceIndex } from './build-evidence-index.mjs';
import { writeReport } from './build-report.mjs';
import { enrichHits } from './enrich-hits.mjs';
import { inventoryGuilds } from './inventory-guilds.mjs';
import { readNdjson, createRunDirectory, loadJson, loadToken, writeJsonAtomic, writeNdjsonAtomic } from './lib/io.mjs';
import { createDiscordClient } from './lib/api.mjs';
import { assertPlanContainsExistingTasks, createProgressReporter, runSearchPlan } from './run-search-plan.mjs';

function hasInventoryClient(client) {
  return typeof client?.inventoryGuilds === 'function' && typeof client?.inventoryChannels === 'function';
}

function mergeGuilds(plan, inventory) {
  const inventoryById = new Map((inventory.guilds ?? []).map((guild) => [guild.id, guild]));
  const requested = Array.isArray(plan.guilds) && plan.guilds.length
    ? plan.guilds
    : inventory.guilds ?? [];
  return requested.map((guild) => {
    const discovered = inventoryById.get(guild.id) ?? {};
    return {
      ...discovered,
      ...guild,
      channels: guild.channels?.length ? guild.channels : discovered.channels ?? [],
    };
  });
}

function phasePlan(plan, guilds, queries, maxPages) {
  return {
    ...plan,
    guilds,
    queries,
    options: {
      ...(plan.options ?? {}),
      maxPages,
    },
  };
}

function queryContent(query) {
  return query.content ?? query.query ?? '';
}

export function selectRelevantGuildIds(messages, discoveryQueries) {
  const terms = discoveryQueries.map(queryContent).map((term) => term.trim().toLocaleLowerCase()).filter(Boolean);
  const relevant = new Set();
  for (const message of messages) {
    const content = String(message.content ?? '').toLocaleLowerCase();
    const matched = (message.matchedQueries ?? []).map((query) => String(query).toLocaleLowerCase());
    if (terms.some((term) => matched.includes(term) || content.includes(term))) {
      if (message.guildId) relevant.add(message.guildId);
    }
  }
  return [...relevant].sort();
}

export async function resolveInventory({ client, plan, runDir, inventory = null } = {}) {
  if (inventory) {
    if (runDir?.guilds) writeJsonAtomic(runDir.guilds, inventory);
    return inventory;
  }
  if (hasInventoryClient(client)) {
    return inventoryGuilds({ client, outputPath: runDir?.guilds ?? null });
  }
  const fallback = {
    generatedAt: new Date().toISOString(),
    guilds: plan.guilds ?? [],
    totals: { joinedGuilds: (plan.guilds ?? []).length, accessibleChannelInventories: 0, inaccessibleChannelInventories: 0 },
  };
  if (runDir?.guilds) writeJsonAtomic(runDir.guilds, fallback);
  return fallback;
}

export async function runCommunityResearch({
  client,
  plan,
  runDir,
  inventory = null,
  enrich = true,
  maxHits = 20,
  onProgress = null,
  now = new Date().toISOString(),
} = {}) {
  if (!client?.searchPage) throw new TypeError('client.searchPage is required');
  if (!plan?.queries) throw new TypeError('plan.queries is required');
  if (!runDir) throw new TypeError('runDir is required');

  const discoveryQueries = plan.queries.filter((query) => (query.purpose ?? 'discovery') !== 'validation');
  const validationQueries = plan.queries.filter((query) => query.purpose === 'validation');
  if (!discoveryQueries.length && validationQueries.length) {
    throw new Error('Validation-only plans are not supported by run-research; use run-search-plan for an explicit single pass');
  }
  if (!discoveryQueries.length) throw new Error('At least one discovery query is required');

  const resolvedInventory = await resolveInventory({ client, plan, runDir, inventory });
  const guilds = mergeGuilds(plan, resolvedInventory);
  assertPlanContainsExistingTasks(loadJson(runDir.state, null), { ...plan, guilds });
  const progress = onProgress;
  let relevantGuildIds = [];
  const phases = [];

  const discoveryPlan = phasePlan(plan, guilds, discoveryQueries, plan.options?.discoveryMaxPages ?? 1);
  await runSearchPlan({ client, plan: discoveryPlan, runDir, onCheckpoint: progress, allowPlanSubset: true });
  phases.push({ name: 'discovery', guilds: guilds.map((guild) => guild.id), queries: discoveryQueries.map(queryContent) });
  relevantGuildIds = selectRelevantGuildIds(readNdjson(runDir.messages), discoveryQueries);
  if (validationQueries.length && relevantGuildIds.length) {
    const relevantGuilds = guilds.filter((guild) => relevantGuildIds.includes(guild.id));
    const validationPlan = phasePlan(plan, relevantGuilds, validationQueries, plan.options?.maxPages ?? Infinity);
    await runSearchPlan({ client, plan: validationPlan, runDir, onCheckpoint: progress, allowPlanSubset: true });
    phases.push({ name: 'validation', guilds: relevantGuildIds, queries: validationQueries.map(queryContent) });
  } else if (validationQueries.length) {
    phases.push({ name: 'validation', guilds: [], queries: validationQueries.map(queryContent), skipped: 'no discovery hits' });
  }

  const inventoryGuildIds = new Set((resolvedInventory.guilds ?? []).map((guild) => guild.id));
  const selectedGuildIds = new Set(guilds.map((guild) => guild.id));
  const searchedAllJoined = inventoryGuildIds.size === selectedGuildIds.size
    && [...inventoryGuildIds].every((guildId) => selectedGuildIds.has(guildId));
  const finalPlan = {
    ...plan,
    guilds,
    phases,
    searchAllJoined: plan.searchAllJoined ?? searchedAllJoined,
    filters: {
      ...(plan.filters ?? {}),
      requireAll: plan.filters?.requireAll ?? plan.requireAll ?? [],
    },
  };
  writeJsonAtomic(runDir.searchPlan, finalPlan);

  const messages = readNdjson(runDir.messages);
  const preliminaryEvidence = buildEvidenceIndex({
    messages,
    queryPlan: finalPlan,
    inventory: resolvedInventory,
    now,
  });
  let contexts = [];
  let enrichmentErrors = [];
  if (enrich && typeof client.fetchAround === 'function') {
    const enriched = await enrichHits({
      client,
      hits: preliminaryEvidence.candidates,
      maxHits,
    });
    contexts = enriched.contexts;
    enrichmentErrors = enriched.errors;
  }
  writeNdjsonAtomic(runDir.contexts, contexts);

  const evidence = buildEvidenceIndex({
    messages,
    contexts,
    queryPlan: finalPlan,
    inventory: resolvedInventory,
    now,
  });
  writeJsonAtomic(runDir.evidence, evidence);

  const state = loadJson(runDir.state, { tasks: {}, errors: [] });
  const run = {
    runId: finalPlan.runId ?? null,
    objective: finalPlan.objective ?? null,
    generatedAt: now,
    evidenceSource: finalPlan.evidenceSource ?? 'discord',
    phases,
    relevantGuildIds,
    enrichment: { enabled: enrich && typeof client.fetchAround === 'function', contexts: contexts.length, errors: enrichmentErrors },
  };
  writeJsonAtomic(runDir.run, run);
  writeReport({
    outputPath: runDir.report,
    input: { run, inventory: resolvedInventory, plan: finalPlan, evidence, state },
  });
  return { runDir, inventory: resolvedInventory, plan: finalPlan, state, evidence, contexts, enrichmentErrors, relevantGuildIds, phases };
}

function getArg(name, fallback = null) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] ?? fallback : fallback;
}

function hasArg(name) {
  return process.argv.includes(name);
}

if (process.argv[1]?.endsWith('run-research.mjs')) {
  const planPath = getArg('--plan');
  if (!planPath) throw new Error('--plan is required');
  const plan = loadJson(planPath);
  const requireAll = getArg('--require-all');
  if (requireAll) {
    plan.filters = {
      ...(plan.filters ?? {}),
      requireAll: requireAll.split(',').map((term) => term.trim()).filter(Boolean),
    };
  }
  const runDir = createRunDirectory({
    outputRoot: getArg('--output-root') ?? undefined,
    runId: plan.runId ?? getArg('--run-id') ?? undefined,
  });
  const token = loadToken({ variable: getArg('--token-env') ?? 'VITE_DISCORD_TOKEN' });
  const client = createDiscordClient({ token, baseUrl: getArg('--base-url') ?? undefined });
  const result = await runCommunityResearch({
    client,
    plan,
    runDir,
    enrich: !hasArg('--no-enrich'),
    maxHits: Number(getArg('--max-hits', 20)),
    onProgress: createProgressReporter(),
  });
  console.log(JSON.stringify({
    runDirectory: result.runDir.directory,
    relevantGuilds: result.relevantGuildIds.length,
    candidates: result.evidence.candidates.length,
    contexts: result.contexts.length,
  }, null, 2));
}
