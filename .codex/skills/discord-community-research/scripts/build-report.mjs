#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import { loadJson } from './lib/io.mjs';

function values(object) {
  return Object.values(object ?? {});
}

function safe(value, fallback = '-') {
  return value == null || value === '' ? fallback : String(value);
}

function inline(value) {
  return safe(value).replace(/[\r\n]+/g, ' ').replace(/\|/g, '\\|').trim();
}

function dateRange(plan = {}) {
  const range = plan.dateRange ?? {};
  if (!range.minDate && !range.maxDate) return 'bounded range recorded in the search plan';
  return `${safe(range.minDate, 'start not set')} to ${safe(range.maxDate, 'end not set')}`;
}

function taskStatusCounts(state = {}) {
  const counts = {};
  for (const task of values(state.tasks)) counts[task.status ?? 'unknown'] = (counts[task.status ?? 'unknown'] ?? 0) + 1;
  return counts;
}

function coverageComplete(state = {}, inventory = {}, plan = {}) {
  const tasks = values(state.tasks);
  const selectedGuildIds = plan.searchAllJoined === false
    ? new Set((plan.guilds ?? []).map((guild) => guild.id))
    : null;
  const inventoryErrors = (inventory.guilds ?? [])
    .filter((guild) => guild.channelError && (!selectedGuildIds || selectedGuildIds.has(guild.id)))
    .length;
  const searchedGuildIds = new Set(tasks.map((task) => task.guildId).filter(Boolean));
  const unsearchedJoinedGuild = plan.searchAllJoined !== false
    && (inventory.guilds ?? []).some((guild) => !guild.channelError && !searchedGuildIds.has(guild.id));
  return tasks.length > 0
    && tasks.every((task) => task.status === 'success')
    && inventoryErrors === 0
    && !unsearchedJoinedGuild;
}

function serverStatus(guild, tasks) {
  if (guild.channelError) return 'inaccessible';
  if (!tasks.length) return 'inspected-not-searched';
  if (tasks.some((task) => task.status === 'failed' || task.status === 'retryable')) return 'searched-incomplete';
  if (tasks.some((task) => task.status === 'truncated')) return 'searched-truncated';
  return 'searched';
}

function renderInventory(inventory = {}, state = {}) {
  const tasks = values(state.tasks);
  const rows = (inventory.guilds ?? []).map((guild) => {
    const guildTasks = tasks.filter((task) => task.guildId === guild.id);
    return `| ${inline(guild.name)} | ${serverStatus(guild, guildTasks)} | ${guildTasks.length} | ${guild.channelError ? inline(guild.channelError.message ?? `HTTP ${guild.channelError.status ?? 'error'}`) : `${guild.channels?.length ?? 0} channels`} |`;
  });
  if (!rows.length) return '- No guild inventory was recorded.';
  return ['| Server | Coverage | Tasks | Access |', '| --- | --- | ---: | --- |', ...rows].join('\n');
}

function renderTasks(state = {}) {
  const tasks = values(state.tasks);
  if (!tasks.length) return '- No search tasks were recorded.';
  const rows = tasks.map((task) => `| ${inline(task.query)} | ${inline(task.guildName ?? task.guildId)} | ${inline(task.status)} | ${task.pages ?? 0} | ${task.fetchedCount ?? 0}/${task.reportedTotal ?? 0} | ${task.attempts ?? 0} |`);
  return ['| Query | Server | Status | Pages | Fetched / reported | Attempts |', '| --- | --- | --- | ---: | ---: | ---: |', ...rows].join('\n');
}

function renderFindings(evidence = {}) {
  const candidates = (evidence.candidates ?? []).slice(0, 20);
  if (!candidates.length) return null;
  return candidates.map((candidate, index) => {
    const title = inline(candidate.content || candidate.messageId || 'Untitled message').slice(0, 240);
    const location = [candidate.guildName, candidate.channelName].filter(Boolean).map(inline).join(' / ') || 'Discord';
    const queries = candidate.matchedQueries?.length ? `; queries: ${candidate.matchedQueries.map(inline).join(', ')}` : '';
    const citation = candidate.jumpUrl ? `[open message](${candidate.jumpUrl})` : 'jump URL unavailable';
    const evidenceStatus = candidate.evidenceStatus ? `; status: ${inline(candidate.evidenceStatus)}` : '';
    return `${index + 1}. **${title}** (${location}) - ${candidate.grade ?? 'E0'}: ${inline(candidate.gradeReason)}${evidenceStatus}; score ${safe(candidate.score, '0')}${queries}; ${citation}.`;
  }).join('\n');
}

function renderSignals(stats = {}, coverage = {}) {
  return [
    `- Unique messages: ${stats.uniqueMessages ?? 0} (raw: ${stats.rawMessages ?? 0}; duplicates merged: ${stats.duplicateMessages ?? 0})`,
    `- Messages removed by required-term filters: ${stats.filteredOutMessages ?? 0}`,
    `- Required body terms: ${(coverage.requireAll ?? []).map(inline).join(', ') || 'none'}`,
    `- Distinct authors: ${stats.distinctAuthors ?? 0}; servers: ${stats.distinctGuilds ?? 0}; channels: ${stats.distinctChannels ?? 0}`,
    `- Evidence grades: ${Object.entries(stats.grades ?? {}).map(([grade, count]) => `${grade}=${count}`).join(', ') || 'none'}`,
    `- Evidence statuses: ${Object.entries(stats.evidenceStatuses ?? {}).map(([status, count]) => `${status}=${count}`).join(', ') || 'none'}`,
    `- Link-only messages penalized: ${stats.linkOnlyMessages ?? 0}; repeated-content messages penalized: ${stats.repeatedContentMessages ?? 0}`,
  ].join('\n');
}

function renderErrors(state = {}, inventory = {}, run = {}) {
  const taskErrors = values(state.tasks).filter((task) => task.status === 'failed' || task.status === 'retryable');
  const inventoryErrors = (inventory.guilds ?? []).filter((guild) => guild.channelError);
  const lines = [];
  for (const entry of state.errors ?? []) {
    const error = entry.error ?? {};
    const status = error.status == null ? '' : ` HTTP ${error.status}`;
    lines.push(`- Historical task \`${inline(entry.task)}\`: ${inline(error.message ?? `HTTP ${error.status ?? 'error'}`)}${status} (attempts: ${error.attempts ?? 'unknown'})`);
  }
  for (const task of taskErrors) lines.push(`- Task \`${inline(task.key ?? task.query)}\`: ${inline(task.status)}${task.lastError ? ` - ${inline(task.lastError.message ?? `HTTP ${task.lastError.status ?? 'error'}`)}` : ''}`);
  for (const guild of inventoryErrors) lines.push(`- Server \`${inline(guild.name)}\`: channel inventory failed (${inline(guild.channelError.message ?? `HTTP ${guild.channelError.status ?? 'error'}`)})`);
  for (const entry of Array.isArray(run.enrichment?.errors) ? run.enrichment.errors : []) {
    lines.push(`- Context enrichment for message \`${inline(entry.messageId)}\`: ${inline(entry.error?.message ?? 'unknown error')}`);
  }
  return lines.length ? lines.join('\n') : '- None recorded.';
}

export function buildReport({ run = {}, inventory = {}, plan = {}, evidence = {}, state = {} } = {}) {
  const counts = taskStatusCounts(state);
  const complete = coverageComplete(state, inventory, plan);
  const findings = renderFindings(evidence);
  const externalValidation = plan.externalValidation === true || run.externalValidation === true;
  const resultLead = findings
    ? 'The findings below are ranked Discord evidence candidates, not independent validation.'
    : complete
      ? 'No relevant results were observed in the completed Discord coverage.'
      : 'Relevant results were not observed within the completed coverage; the run is incomplete.';

  const lines = [
    `# Discord Community Research Report`,
    '',
    `- Objective: ${inline(run.objective ?? plan.objective)}`,
    `- Date range: ${inline(dateRange(plan))}`,
    `- Generated: ${inline(run.generatedAt ?? evidence.generatedAt)}`,
    `- Evidence boundary: Discord-only by default. External validation: ${externalValidation ? 'enabled' : 'disabled'}.`,
    '',
    '## Coverage',
    '',
    `- Tasks: ${Object.keys(state.tasks ?? {}).length}; ${Object.entries(counts).map(([status, count]) => `${status}=${count}`).join(', ') || 'no task status recorded'}`,
    `- Coverage sufficient for absence claims: ${complete ? 'yes' : 'no'}`,
    '',
    renderInventory(inventory, state),
    '',
    '## Query Execution',
    '',
    renderTasks(state),
    '',
    '## Findings',
    '',
    resultLead,
    findings ?? '- No ranked evidence candidates.',
    '',
    '## Community Signals',
    '',
    renderSignals(evidence.stats, evidence.coverage),
    '',
    '## Errors And Limits',
    '',
    `- Truncated tasks: ${values(state.tasks).filter((task) => task.status === 'truncated' || task.truncated).map((task) => `${inline(task.key ?? task.query)} (${task.fetchedCount ?? 0}/${task.reportedTotal ?? 0})`).join(', ') || 'none'}`,
    renderErrors(state, inventory, run),
    '',
    '## Ambiguities And Unresolved Questions',
    '',
    (plan.ambiguities ?? []).length ? plan.ambiguities.map((item) => `- ${inline(typeof item === 'string' ? item : item.description ?? item.term)}`).join('\n') : '- None recorded in the search plan.',
    '',
    '## Validation Status',
    '',
    externalValidation
      ? '- External validation was enabled by the plan and must be interpreted separately from Discord evidence.'
      : '- External validation was disabled. This report makes no claim about repository activity, releases, documentation, or correctness outside Discord.',
    '',
  ];
  return lines.join('\n');
}

export function writeReport({ outputPath, input } = {}) {
  if (!outputPath) throw new TypeError('outputPath is required');
  const report = buildReport(input);
  const tempPath = `${outputPath}.${process.pid}.tmp`;
  fs.mkdirSync(path.dirname(outputPath), { recursive: true });
  fs.writeFileSync(tempPath, report, 'utf8');
  fs.renameSync(tempPath, outputPath);
  return report;
}

function getArg(name, fallback = null) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] ?? fallback : fallback;
}

if (process.argv[1]?.endsWith('build-report.mjs')) {
  const outputPath = getArg('--output');
  const runPath = getArg('--run');
  const inventoryPath = getArg('--inventory');
  const planPath = getArg('--plan');
  const evidencePath = getArg('--evidence');
  const statePath = getArg('--state');
  if (!outputPath || !runPath || !inventoryPath || !planPath || !evidencePath || !statePath) {
    throw new Error('--output, --run, --inventory, --plan, --evidence, and --state are required');
  }
  const report = buildReport({
    run: loadJson(runPath, {}),
    inventory: loadJson(inventoryPath, {}),
    plan: loadJson(planPath, {}),
    evidence: loadJson(evidencePath, {}),
    state: loadJson(statePath, {}),
  });
  fs.mkdirSync(path.dirname(outputPath), { recursive: true });
  fs.writeFileSync(outputPath, report, 'utf8');
  console.log(JSON.stringify({ output: outputPath }, null, 2));
}
