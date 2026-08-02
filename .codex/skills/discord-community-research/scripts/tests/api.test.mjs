import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  DiscordApiError,
  createDiscordClient,
  iterateSearch,
} from '../lib/api.mjs';
import { normalizeMessage } from '../lib/records.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const fixture = (name) => JSON.parse(fs.readFileSync(path.join(HERE, 'fixtures', name), 'utf8'));

function response(status, body, headers = {}) {
  return {
    ok: status >= 200 && status < 300,
    status,
    headers: new Headers(headers),
    async text() {
      return body == null ? '' : JSON.stringify(body);
    },
  };
}

test('retries 429 using retry_after and returns the eventual JSON', async () => {
  const calls = [];
  const waits = [];
  const fetchImpl = async (url) => {
    calls.push(url);
    if (calls.length === 1) return response(429, { retry_after: 0.01, global: false });
    return response(200, [{ id: 'guild-1', name: 'Guild One' }]);
  };
  const client = createDiscordClient({
    token: 'secret-token-value',
    fetchImpl,
    sleep: async (ms) => waits.push(ms),
    maxRetries: 2,
  });

  const data = await client.inventoryGuilds();

  assert.equal(data[0].id, 'guild-1');
  assert.equal(calls.length, 2);
  assert.deepEqual(waits, [10]);
});

test('retries 202 and transient 503 without treating either as success', async () => {
  let attempt = 0;
  const statuses = [];
  const waits = [];
  const client = createDiscordClient({
    token: 'secret-token-value',
    fetchImpl: async () => {
      attempt += 1;
      statuses.push(attempt);
      if (attempt === 1) return response(202, { retry_after: 0.02 });
      if (attempt === 2) return response(503, { message: 'temporary' });
      return response(200, { ok: true });
    },
    sleep: async (ms) => waits.push(ms),
    maxRetries: 3,
  });

  const data = await client.getJson('/health');

  assert.deepEqual(data, { ok: true });
  assert.deepEqual(statuses, [1, 2, 3]);
  assert.equal(waits[0], 20);
  assert.equal(waits[1], 2000);
});

test('throws a status-bearing retryable error after bounded retries', async () => {
  const client = createDiscordClient({
    token: 'secret-token-value',
    fetchImpl: async () => response(429, { retry_after: 0 }),
    sleep: async () => {},
    maxRetries: 1,
  });

  await assert.rejects(
    client.getJson('/limited'),
    (error) => {
      assert.ok(error instanceof DiscordApiError);
      assert.equal(error.status, 429);
      assert.equal(error.retryable, true);
      assert.doesNotMatch(error.message, /secret-token-value/);
      return true;
    },
  );
});

test('flattens nested search groups, deduplicates overlap, and yields both pages', async () => {
  const pages = [fixture('search-page.json'), fixture('search-page-2.json')];
  const requested = [];
  const client = {
    async searchPage(_guildId, criteria, offset) {
      requested.push({ criteria, offset });
      return pages[requested.length - 1] ?? { messages: [], total_results: 2 };
    },
  };
  const yielded = [];
  const callbackPages = [];

  for await (const page of iterateSearch({
    client,
    guildId: '4001',
    criteria: { content: 'tool', minDate: '2026-07-01T00:00:00.000Z' },
    pageSize: 2,
    maxPages: 2,
    onPage: async (page) => callbackPages.push(page),
  })) {
    yielded.push(page);
  }

  const ids = yielded.flatMap((page) => page.messages.map((message) => message.id));
  assert.deepEqual(ids, ['1001', '1002']);
  assert.deepEqual(requested.map((call) => call.offset), [0, 2]);
  assert.equal(callbackPages.length, 2);
  assert.equal(yielded.at(-1).fetchedCount, 2);
});

test('stops after the first empty page when Discord reports zero results', async () => {
  let calls = 0;
  const client = {
    async searchPage() {
      calls += 1;
      return { messages: [], total_results: 0 };
    },
  };

  const pages = [];
  for await (const page of iterateSearch({
    client,
    guildId: '4001',
    criteria: { content: 'missing project' },
    maxPages: 2,
  })) {
    pages.push(page);
  }

  assert.equal(calls, 1);
  assert.equal(pages.length, 1);
  assert.equal(pages[0].truncated, false);
});

test('normalizes URLs, replies, attachments, and jump links without credentials', () => {
  const message = normalizeMessage(
    {
      id: '1002',
      type: 19,
      content: 'See https://example.test/tool.',
      channel_id: '2001',
      timestamp: '2026-07-01T00:00:00.000Z',
      author: { id: '3002', username: 'user-b', global_name: 'User B', bot: false },
      attachments: [{ filename: 'demo.png', content_type: 'image/png', size: 12, width: 4, height: 3, url: 'https://cdn.test/demo.png' }],
      embeds: [{ url: 'https://example.test/embed', title: 'Demo' }],
      message_reference: { message_id: '1001', channel_id: '2001', guild_id: '4001' },
    },
    { guildId: '4001', guildName: 'Guild One', channelName: 'tools', query: 'tool' },
  );

  assert.deepEqual(message.urls, ['https://example.test/tool', 'https://example.test/embed']);
  assert.equal(message.replyReference.messageId, '1001');
  assert.equal(message.attachments[0].filename, 'demo.png');
  assert.equal(message.jumpUrl, 'https://discord.com/channels/4001/2001/1002');
  assert.doesNotMatch(JSON.stringify(message), /secret-token-value/);
});
