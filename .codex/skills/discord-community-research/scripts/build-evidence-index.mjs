#!/usr/bin/env node
import { readNdjson, loadJson, writeJsonAtomic } from './lib/io.mjs';
import { normalizeMessage } from './lib/records.mjs';

const TECHNICAL_TERMS = /\b(install|setup|configure|configuration|parameter|workflow|prompt|node|api|sdk|script|code|export|import|plugin|package|version|limit|limitation|bug|error|fix|compile|deploy|json|yaml|config)\b/i;
const DEMONSTRATION_TERMS = /\b(demo|demonstrat|showcase|screenshot|video|gif|result|output|example|sample|proof|benchmark|working|works for me|in production)\b/i;
const REPEATED_USE_TERMS = /\b(i use|we use|used it|using it|been using|works for us|multiple times|in production|daily|repeatedly|since)\b/i;
const DISCUSSION_TERMS = /\b(how|why|what|when|where|can someone|does anyone|help|question|recommend|discussion|thoughts|anyone tried)\b|\?/i;
const URL_PATTERN = /https?:\/\/[^\s<>"]+/gi;
const OFFICIAL_CLAIM_TERMS = /\b(official|officially|apparently|reportedly|rumou?r|heard)\b|官方|听说|据说|传闻/i;
const PROJECT_LINK_PATTERN = /https?:\/\/(?:www\.)?(?:github\.com|gitlab\.com|gitee\.com|codeberg\.org|npmjs\.com|pypi\.org|huggingface\.co|civitai\.com)\//i;

function unique(values) {
  return [...new Set((values ?? []).filter(Boolean))];
}

function extractUrls(content = '') {
  return [...String(content).matchAll(URL_PATTERN)].map((match) => match[0].replace(/[)\]}>.,;:!?]+$/, ''));
}

function asCandidateRecord(record = {}) {
  const normalized = normalizeMessage({
    ...record,
    id: record.id ?? record.messageId,
    guild_id: record.guild_id ?? record.guildId,
    channel_id: record.channel_id ?? record.channelId,
    message_reference: record.message_reference ?? record.replyReference,
    author: record.author && typeof record.author === 'object'
      ? record.author
      : { id: record.authorId, username: record.authorUsername ?? record.author, global_name: record.author },
  }, {
    guildId: record.guildId ?? record.guild_id,
    guildName: record.guildName,
    channelId: record.channelId ?? record.channel_id,
    channelName: record.channelName,
  });
  const content = record.content ?? normalized.content;
  return {
    ...normalized,
    ...record,
    guildId: record.guildId ?? record.guild_id ?? normalized.guildId,
    guildName: record.guildName ?? normalized.guildName,
    channelId: record.channelId ?? record.channel_id ?? normalized.channelId,
    channelName: record.channelName ?? normalized.channelName,
    messageId: record.messageId ?? record.id ?? normalized.messageId,
    timestamp: record.timestamp ?? normalized.timestamp,
    content,
    authorId: record.authorId ?? normalized.authorId,
    author: typeof record.author === 'string' ? record.author : normalized.author,
    authorUsername: record.authorUsername ?? normalized.authorUsername,
    urls: unique([...(normalized.urls ?? []), ...(record.urls ?? []), ...extractUrls(content)]),
    attachments: record.attachments ?? normalized.attachments,
    embeds: record.embeds ?? normalized.embeds,
    matchedQueries: unique([...(normalized.matchedQueries ?? []), ...(record.matchedQueries ?? [])]),
    jumpUrl: record.jumpUrl ?? normalized.jumpUrl,
  };
}

function canonicalKey(record) {
  return `${record.guildId ?? ''}/${record.channelId ?? ''}/${record.messageId ?? ''}`;
}

function mergeRecords(existing, incoming) {
  const merged = { ...existing, ...incoming };
  for (const field of ['guildName', 'channelName', 'timestamp', 'content', 'authorId', 'author', 'authorUsername', 'jumpUrl']) {
    if ((merged[field] == null || merged[field] === '') && existing[field] != null) merged[field] = existing[field];
  }
  merged.matchedQueries = unique([...(existing.matchedQueries ?? []), ...(incoming.matchedQueries ?? [])]);
  merged.urls = unique([...(existing.urls ?? []), ...(incoming.urls ?? [])]);
  merged.attachments = incoming.attachments?.length ? incoming.attachments : existing.attachments ?? [];
  merged.embeds = incoming.embeds?.length ? incoming.embeds : existing.embeds ?? [];
  return merged;
}

function contextFor(record, contexts) {
  const key = canonicalKey(record);
  return contexts.find((context) => canonicalKey(context?.target ?? {}) === key) ?? null;
}

function contextSignals(context) {
  if (!context) return { contextMessages: 0, replyCount: 0, hasThreadRoot: false, hasForumTitle: false };
  return {
    contextMessages: (context.before?.length ?? 0) + (context.after?.length ?? 0),
    replyCount: context.replyChain?.length ?? 0,
    hasThreadRoot: Boolean(context.threadRoot),
    hasForumTitle: Boolean(context.forumTitle),
  };
}

function channelIndex(inventory = {}) {
  const index = new Map();
  for (const guild of inventory.guilds ?? []) {
    for (const channel of guild.channels ?? []) {
      index.set(String(guild.id) + '/' + String(channel.id), { guildName: guild.name ?? null, channelName: channel.name ?? null });
    }
  }
  return index;
}

function requiredTerms(queryPlan = {}) {
  const terms = queryPlan.filters?.requireAll ?? queryPlan.requireAll ?? [];
  return (Array.isArray(terms) ? terms : String(terms).split(',')).map((term) => String(term).trim()).filter(Boolean);
}

function matchesRequiredTerms(record, terms) {
  const content = String(record.content ?? '').toLocaleLowerCase();
  return terms.every((term) => content.includes(term.toLocaleLowerCase()));
}

function evidenceStatus(record) {
  if ((record.urls ?? []).some((url) => PROJECT_LINK_PATTERN.test(url))) return 'project-linked';
  if (OFFICIAL_CLAIM_TERMS.test(record.content ?? '')) return 'unverified-claim';
  return 'community-only';
}

export function gradeEvidence(record = {}) {
  const content = String(record.content ?? '').trim();
  const urls = record.urls ?? extractUrls(content);
  const hasAttachment = (record.attachments?.length ?? 0) > 0 || (record.embeds?.length ?? 0) > 0;
  const linkOnly = urls.length > 0 && content.replace(URL_PATTERN, '').replace(/[\s\W_]+/g, '').length < 12;
  if (!content && !hasAttachment) return 'E0';
  if (linkOnly && !record.contextSignals?.contextMessages) return 'E0';
  if (REPEATED_USE_TERMS.test(content) && (TECHNICAL_TERMS.test(content) || DEMONSTRATION_TERMS.test(content))) return 'E4';
  if (DEMONSTRATION_TERMS.test(content) || hasAttachment || record.contextSignals?.hasThreadRoot) return 'E3';
  if (TECHNICAL_TERMS.test(content)) return 'E2';
  if (DISCUSSION_TERMS.test(content) || content.length > 16) return 'E1';
  return 'E0';
}

function gradeReason(grade) {
  return {
    E0: 'name-only or link-only mention',
    E1: 'discussion, question, or general mention',
    E2: 'technical, setup, workflow, or limitation detail',
    E3: 'demonstration, concrete result, attachment, or thread evidence',
    E4: 'repeated-use language plus technical or demonstrated detail',
  }[grade] ?? 'unclassified Discord evidence';
}

function recencySignal(timestamp, now) {
  const time = Date.parse(timestamp ?? '');
  const current = Date.parse(now ?? '');
  if (!Number.isFinite(time) || !Number.isFinite(current)) return 0;
  const ageDays = Math.max(0, (current - time) / 86400000);
  return Math.max(0, 1 - ageDays / 365);
}

function normalizedContent(content) {
  return String(content ?? '').toLowerCase().replace(URL_PATTERN, '').replace(/\s+/g, ' ').trim();
}

function linkOnlyPenalty(record) {
  const content = String(record.content ?? '').trim();
  const urls = record.urls ?? extractUrls(content);
  const remainder = content.replace(URL_PATTERN, '').replace(/[\s\W_]+/g, '');
  return urls.length > 0 && remainder.length < 12 ? 2 : 0;
}

function buildCoverage(queryPlan = {}) {
  const queries = Array.isArray(queryPlan.queries) ? queryPlan.queries : [];
  return {
    plannedQueries: queries.length,
    discoveryQueries: queries.filter((query) => query.purpose === 'discovery').length,
    validationQueries: queries.filter((query) => query.purpose === 'validation').length,
  };
}

export function buildEvidenceIndex({ messages = [], contexts = [], queryPlan = {}, inventory = {}, now = new Date().toISOString() } = {}) {
  const deduped = new Map();
  let duplicateMessages = 0;
  for (const raw of messages) {
    const record = asCandidateRecord(raw);
    const key = canonicalKey(record);
    if (!record.messageId || !record.channelId) continue;
    if (deduped.has(key)) {
      duplicateMessages += 1;
      deduped.set(key, mergeRecords(deduped.get(key), record));
    } else {
      deduped.set(key, record);
    }
  }

  const allRecords = [...deduped.values()];
  const inventoryChannels = channelIndex(inventory);
  for (const record of allRecords) {
    const mapped = inventoryChannels.get(String(record.guildId) + '/' + String(record.channelId));
    if (mapped) {
      record.guildName ??= mapped.guildName;
      record.channelName ??= mapped.channelName;
    }
  }
  const terms = requiredTerms(queryPlan);
  const records = terms.length ? allRecords.filter((record) => matchesRequiredTerms(record, terms)) : allRecords;
  const authors = new Set(records.map((record) => record.authorId).filter(Boolean));
  const guilds = new Set(records.map((record) => record.guildId).filter(Boolean));
  const channels = new Set(records.map((record) => canonicalKey({ guildId: record.guildId, channelId: record.channelId })).filter(Boolean));
  const contentCounts = new Map();
  for (const record of records) {
    const contentKey = normalizedContent(record.content);
    if (contentKey) contentCounts.set(contentKey, (contentCounts.get(contentKey) ?? 0) + 1);
  }

  const candidates = records.map((record) => {
    const context = contextFor(record, contexts);
    const contextSignalsValue = contextSignals(context);
    const grade = gradeEvidence({ ...record, contextSignals: contextSignalsValue });
    const technicalDensity = [TECHNICAL_TERMS, DEMONSTRATION_TERMS, REPEATED_USE_TERMS].filter((pattern) => pattern.test(record.content ?? '')).length;
    const repeatedContent = (contentCounts.get(normalizedContent(record.content)) ?? 0) > 1;
    const penalties = {
      linkOnly: linkOnlyPenalty(record),
      repeatedContent: repeatedContent ? 0.75 : 0,
    };
    const signals = {
      distinctAuthors: authors.size,
      distinctGuilds: guilds.size,
      distinctChannels: channels.size,
      recency: recencySignal(record.timestamp, now),
      technicalDensity,
      contextMessages: contextSignalsValue.contextMessages,
      replyCount: contextSignalsValue.replyCount,
    };
    const score = Number((
      technicalDensity * 2
      + signals.recency
      + Math.min(1, signals.contextMessages / 4)
      + Math.min(1, signals.replyCount / 2)
      - penalties.linkOnly
      - penalties.repeatedContent
    ).toFixed(3));
    return {
      ...record,
      matchedQueries: [...record.matchedQueries].sort(),
      grade,
      gradeReason: gradeReason(grade),
      evidenceStatus: evidenceStatus(record),
      score,
      signals,
      penalties,
      repeatedContent,
      contextSummary: context ? {
        before: context.before?.length ?? 0,
        after: context.after?.length ?? 0,
        replyChain: context.replyChain?.length ?? 0,
        forumTitle: context.forumTitle ?? null,
        threadRoot: context.threadRoot?.messageId ?? null,
      } : null,
    };
  }).sort((left, right) => right.score - left.score || String(right.timestamp ?? '').localeCompare(String(left.timestamp ?? '')) || canonicalKey(left).localeCompare(canonicalKey(right)));

  const grades = Object.fromEntries(['E0', 'E1', 'E2', 'E3', 'E4'].map((grade) => [grade, candidates.filter((candidate) => candidate.grade === grade).length]));
  const queryMatches = {};
  for (const candidate of candidates) {
    for (const query of candidate.matchedQueries) queryMatches[query] = (queryMatches[query] ?? 0) + 1;
  }

  return {
    generatedAt: now,
    candidates,
    stats: {
      rawMessages: messages.length,
      uniqueMessages: candidates.length,
      duplicateMessages,
      filteredOutMessages: allRecords.length - records.length,
      distinctAuthors: authors.size,
      distinctGuilds: guilds.size,
      distinctChannels: channels.size,
      matchedQueryCount: Object.keys(queryMatches).length,
      queryMatches,
      grades,
      linkOnlyMessages: candidates.filter((candidate) => candidate.penalties.linkOnly > 0).length,
      repeatedContentMessages: candidates.filter((candidate) => candidate.repeatedContent).length,
      evidenceStatuses: Object.fromEntries(
        ['project-linked', 'unverified-claim', 'community-only']
          .map((status) => [status, candidates.filter((candidate) => candidate.evidenceStatus === status).length]),
      ),
    },
    coverage: { ...buildCoverage(queryPlan), requireAll: terms },
  };
}

function getArg(name, fallback = null) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] ?? fallback : fallback;
}

if (process.argv[1]?.endsWith('build-evidence-index.mjs')) {
  const messagesPath = getArg('--messages');
  const outputPath = getArg('--output');
  if (!messagesPath || !outputPath) throw new Error('--messages and --output are required');
  const contextsPath = getArg('--contexts');
  const planPath = getArg('--plan');
  const inventoryPath = getArg('--inventory');
  const requireAll = getArg('--require-all');
  const queryPlan = planPath ? loadJson(planPath, {}) : {};
  if (requireAll) {
    queryPlan.filters = {
      ...(queryPlan.filters ?? {}),
      requireAll: requireAll.split(',').map((term) => term.trim()).filter(Boolean),
    };
  }
  const evidence = buildEvidenceIndex({
    messages: readNdjson(messagesPath),
    contexts: contextsPath ? readNdjson(contextsPath) : [],
    queryPlan,
    inventory: inventoryPath ? loadJson(inventoryPath, {}) : {},
  });
  writeJsonAtomic(outputPath, evidence);
  console.log(JSON.stringify({ candidates: evidence.candidates.length, uniqueMessages: evidence.stats.uniqueMessages }, null, 2));
}
