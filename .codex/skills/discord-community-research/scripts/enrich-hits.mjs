#!/usr/bin/env node
import { readNdjson, loadJson, loadToken, sanitizeError, writeJsonAtomic, writeNdjsonAtomic } from './lib/io.mjs';
import { normalizeMessage } from './lib/records.mjs';
import { createDiscordClient } from './lib/api.mjs';

function asArray(payload) {
  if (Array.isArray(payload)) return payload;
  if (Array.isArray(payload?.messages)) return payload.messages.flatMap((group) => Array.isArray(group) ? group : [group]);
  return [];
}

function asDiscordMessage(record = {}) {
  return {
    ...record,
    id: record.id ?? record.messageId,
    guild_id: record.guild_id ?? record.guildId,
    channel_id: record.channel_id ?? record.channelId,
    message_reference: record.message_reference ?? record.replyReference,
    author: record.author && typeof record.author === 'object'
      ? record.author
      : {
          id: record.authorId,
          username: record.authorUsername ?? record.author,
          global_name: record.author,
        },
  };
}

function normalizeContextRecord(record, hit) {
  const normalized = normalizeMessage(asDiscordMessage(record), {
    guildId: record.guildId ?? hit.guildId,
    guildName: record.guildName ?? hit.guildName,
    channelId: record.channelId ?? hit.channelId,
    channelName: record.channelName ?? hit.channelName,
  });
  const merged = { ...normalized };
  for (const field of ['guildId', 'guildName', 'channelId', 'channelName', 'messageId', 'timestamp', 'content', 'authorId', 'author', 'authorUsername', 'jumpUrl']) {
    if ((merged[field] == null || merged[field] === '') && record[field] != null) merged[field] = record[field];
    if ((merged[field] == null || merged[field] === '') && hit[field] != null) merged[field] = hit[field];
  }
  merged.forumTitle = record.forumTitle ?? record.forum_title ?? record.thread_name ?? hit.forumTitle ?? null;
  merged.threadRootId = record.threadRootId ?? record.thread_root_id ?? record.thread_id ?? hit.threadRootId ?? null;
  merged.matchedQueries = [];
  return merged;
}

function uniqueRecords(records) {
  const seen = new Set();
  return records.filter((record) => {
    const key = `${record.guildId ?? ''}/${record.channelId ?? ''}/${record.messageId ?? ''}`;
    if (!record.messageId || seen.has(key)) return false;
    seen.add(key);
    return true;
  });
}

function compareTimestamp(left, right) {
  const leftTime = Date.parse(left?.timestamp ?? '') || 0;
  const rightTime = Date.parse(right?.timestamp ?? '') || 0;
  if (leftTime !== rightTime) return leftTime - rightTime;
  return String(left?.messageId ?? '').localeCompare(String(right?.messageId ?? ''));
}

function mergeTarget(hit, normalized) {
  const target = { ...hit, ...normalized };
  for (const field of ['guildName', 'channelName', 'timestamp', 'content', 'authorId', 'author', 'authorUsername', 'jumpUrl']) {
    if ((target[field] == null || target[field] === '') && hit[field] != null) target[field] = hit[field];
  }
  target.matchedQueries = [...new Set([...(hit.matchedQueries ?? []), ...(normalized.matchedQueries ?? [])].filter(Boolean))];
  target.isTarget = true;
  return target;
}

function buildReplyChain(target, records) {
  const byId = new Map(records.map((record) => [record.messageId, record]));
  const chain = [];
  const visited = new Set([target.messageId]);
  let nextId = target.replyReference?.messageId ?? null;
  while (nextId && !visited.has(nextId)) {
    visited.add(nextId);
    const record = byId.get(nextId);
    if (!record) break;
    chain.push({ ...record, isContext: true, relation: 'reply' });
    nextId = record.replyReference?.messageId ?? null;
  }
  return chain;
}

export async function enrichHit({ client, hit, before = 4, after = 4, now = new Date().toISOString() } = {}) {
  if (!client?.fetchAround) throw new TypeError('client.fetchAround is required');
  if (!hit?.channelId || !hit?.messageId) throw new TypeError('hit.channelId and hit.messageId are required');

  const payload = await client.fetchAround(hit.channelId, hit.messageId, Math.min(100, Math.max(1, before + after + 1)));
  const rawRecords = asArray(payload);
  const normalized = uniqueRecords(rawRecords.map((record) => normalizeContextRecord(record, hit)));
  if (!normalized.some((record) => record.messageId === hit.messageId)) {
    normalized.push(normalizeContextRecord(hit, hit));
  }
  normalized.sort(compareTimestamp);

  const rawTarget = normalized.find((record) => record.messageId === hit.messageId) ?? normalizeContextRecord(hit, hit);
  const target = mergeTarget(hit, rawTarget);
  const targetIndex = normalized.findIndex((record) => record.messageId === hit.messageId);
  const beforeRecords = normalized
    .slice(0, Math.max(targetIndex, 0))
    .slice(-Math.max(0, before))
    .map((record) => ({ ...record, isContext: true, relation: 'before' }));
  const afterRecords = normalized
    .slice(Math.max(targetIndex, 0) + 1, Math.max(targetIndex, 0) + 1 + Math.max(0, after))
    .map((record) => ({ ...record, isContext: true, relation: 'after' }));
  const threadRootId = target.threadRootId ?? null;
  const threadRootRecord = threadRootId
    ? normalized.find((record) => record.messageId === threadRootId) ?? { messageId: threadRootId, isContext: true, relation: 'thread-root' }
    : null;

  return {
    target,
    before: beforeRecords,
    after: afterRecords,
    replyChain: buildReplyChain(target, normalized),
    threadRoot: threadRootRecord,
    forumTitle: hit.forumTitle ?? target.forumTitle ?? null,
    fetchedAt: now,
  };
}

export async function enrichHits({ client, hits = [], before = 4, after = 4, maxHits = 50, now = new Date().toISOString(), onHit } = {}) {
  const contexts = [];
  const errors = [];
  for (const hit of hits.slice(0, Math.max(0, maxHits))) {
    try {
      const context = await enrichHit({ client, hit, before, after, now });
      contexts.push(context);
      await onHit?.(context);
    } catch (error) {
      errors.push({ messageId: hit?.messageId ?? null, error: sanitizeError(error) });
    }
  }
  return { contexts, errors };
}

function getArg(name, fallback = null) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] ?? fallback : fallback;
}

export function loadHits(inputPath) {
  try {
    const json = loadJson(inputPath, null);
    if (Array.isArray(json)) return json;
    if (Array.isArray(json?.candidates)) return json.candidates;
  } catch {
    // Search output is NDJSON, which is not valid as one JSON document.
  }
  return readNdjson(inputPath);
}

if (process.argv[1]?.endsWith('enrich-hits.mjs')) {
  const inputPath = getArg('--input');
  if (!inputPath) throw new Error('--input is required');
  const outputPath = getArg('--output');
  if (!outputPath) throw new Error('--output is required');
  const token = loadToken({ variable: getArg('--token-env') ?? 'VITE_DISCORD_TOKEN' });
  const client = createDiscordClient({ token, baseUrl: getArg('--base-url') ?? undefined });
  const result = await enrichHits({
    client,
    hits: loadHits(inputPath),
    before: Number(getArg('--before', 4)),
    after: Number(getArg('--after', 4)),
    maxHits: Number(getArg('--max-hits', 50)),
  });
  writeNdjsonAtomic(outputPath, result.contexts);
  const errorPath = getArg('--errors');
  if (errorPath) writeJsonAtomic(errorPath, { errors: result.errors });
  console.log(JSON.stringify({ enriched: result.contexts.length, errors: result.errors.length }, null, 2));
}
