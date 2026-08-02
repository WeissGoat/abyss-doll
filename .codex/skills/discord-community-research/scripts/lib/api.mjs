const DEFAULT_BASE_URL = 'https://discord.com/api/v10';
const DEFAULT_MAX_RETRIES = 5;
const DEFAULT_MAX_OFFSET = 5000;
const EMPTY_PAGE_THRESHOLD = 2;
const TRANSIENT_STATUSES = new Set([202, 429, 500, 502, 503, 504]);

export class DiscordApiError extends Error {
  constructor(message, { status = null, retryable = false, body = null, attempts = 0, cause = null } = {}) {
    super(message, cause ? { cause } : undefined);
    this.name = 'DiscordApiError';
    this.status = status;
    this.retryable = retryable;
    this.body = body;
    this.attempts = attempts;
  }
}

export function snowflakeFromDate(value) {
  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.valueOf())) throw new TypeError(`Invalid date: ${String(value)}`);
  return ((BigInt(date.valueOf()) - 1420070400000n) << 22n).toString();
}

function dateFromSnowflake(value) {
  const timestamp = Number((BigInt(value) >> 22n) + 1420070400000n);
  return new Date(timestamp);
}

function redact(value, token) {
  if (!token || value == null) return value;
  if (typeof value === 'string') return value.split(token).join('[REDACTED]');
  if (Array.isArray(value)) return value.map((item) => redact(item, token));
  if (typeof value === 'object') {
    return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, redact(item, token)]));
  }
  return value;
}

async function readBody(response) {
  const text = await response.text();
  if (!text) return null;
  try {
    return JSON.parse(text);
  } catch {
    return { raw: text.slice(0, 1000) };
  }
}

function retryAfterMs(body, headers, attempt, status) {
  const rawBodySeconds = body?.retry_after;
  const bodySeconds = rawBodySeconds == null ? Number.NaN : Number(rawBodySeconds);
  if (Number.isFinite(bodySeconds) && bodySeconds >= 0) return Math.ceil(bodySeconds * 1000);
  const rawHeaderSeconds = headers?.get?.('retry-after') ?? headers?.get?.('x-ratelimit-reset-after');
  const headerSeconds = rawHeaderSeconds == null ? Number.NaN : Number(rawHeaderSeconds);
  if (Number.isFinite(headerSeconds) && headerSeconds >= 0) return Math.ceil(headerSeconds * 1000);
  if (status === 202) return 1000;
  return Math.min(30000, 1000 * (2 ** Math.max(attempt - 1, 0)));
}

function buildSearchPath(guildId, criteria = {}, offset = 0) {
  const params = new URLSearchParams();
  if (criteria.content) params.set('content', criteria.content);
  if (criteria.channelId) params.set('channel_id', criteria.channelId);
  if (criteria.includeNsfw !== false) params.set('include_nsfw', 'true');
  if (criteria.minId) params.set('min_id', criteria.minId);
  if (criteria.maxId) params.set('max_id', criteria.maxId);
  if (criteria.minDate) params.set('min_id', snowflakeFromDate(criteria.minDate));
  if (criteria.maxDate) params.set('max_id', snowflakeFromDate(criteria.maxDate));
  if (criteria.sortBy) params.set('sort_by', criteria.sortBy);
  if (criteria.sortOrder) params.set('sort_order', criteria.sortOrder);
  if (offset > 0) params.set('offset', String(offset));
  return `/guilds/${encodeURIComponent(guildId)}/messages/search?${params.toString()}`;
}

function flattenMessages(messages) {
  if (!Array.isArray(messages)) return [];
  return Array.isArray(messages[0]) ? messages.flat() : messages;
}

function uniqueMessages(messages) {
  const seen = new Set();
  return messages.filter((message) => {
    const id = message?.id;
    if (!id || seen.has(id)) return false;
    seen.add(id);
    return true;
  });
}

export function createDiscordClient({
  token,
  fetchImpl = globalThis.fetch,
  sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms)),
  maxRetries = DEFAULT_MAX_RETRIES,
  baseUrl = DEFAULT_BASE_URL,
} = {}) {
  if (!token) throw new Error('Discord token is required');
  if (typeof fetchImpl !== 'function') throw new TypeError('fetchImpl must be a function');

  async function getJson(path) {
    let attempt = 0;
    while (true) {
      let response;
      let body = null;
      try {
        response = await fetchImpl(`${baseUrl}${path}`, {
          method: 'GET',
          headers: { authorization: token, 'content-type': 'application/json' },
        });
        body = await readBody(response);
      } catch (cause) {
        if (attempt >= maxRetries) {
          throw new DiscordApiError('Discord request failed after retries', {
            retryable: true,
            attempts: attempt + 1,
            cause,
          });
        }
        attempt += 1;
        await sleep(retryAfterMs(null, null, attempt, null));
        continue;
      }

      if (response.ok && response.status !== 202) return body;

      const retryable = TRANSIENT_STATUSES.has(response.status);
      if (!retryable || attempt >= maxRetries) {
        const safeBody = redact(body, token);
        throw new DiscordApiError(
          `Discord request failed with HTTP ${response.status}`,
          { status: response.status, retryable, body: safeBody, attempts: attempt + 1 },
        );
      }

      attempt += 1;
      await sleep(retryAfterMs(body, response.headers, attempt, response.status));
    }
  }

  return {
    getJson,
    inventoryGuilds: () => getJson('/users/@me/guilds'),
    inventoryChannels: (guildId) => getJson(`/guilds/${encodeURIComponent(guildId)}/channels`),
    searchPage: (guildId, criteria, offset = 0) => getJson(buildSearchPath(guildId, criteria, offset)),
    fetchAround: (channelId, aroundId, limit = 11) => {
      const params = new URLSearchParams({ around: aroundId, limit: String(Math.min(Math.max(limit, 1), 100)) });
      return getJson(`/channels/${encodeURIComponent(channelId)}/messages?${params.toString()}`);
    },
  };
}

export async function* iterateSearch({
  client,
  guildId,
  criteria = {},
  pageSize = 25,
  maxPages = Infinity,
  maxMessages = Infinity,
  maxOffset = DEFAULT_MAX_OFFSET,
  onPage,
} = {}) {
  if (!client?.searchPage) throw new TypeError('client.searchPage is required');
  let currentCriteria = { ...criteria };
  let offset = 0;
  let pageIndex = 0;
  let emptyPages = 0;
  let fetchedCount = 0;
  let oldestTimestamp = null;
  let lastWindowUpper = currentCriteria.maxId ?? (currentCriteria.maxDate ? snowflakeFromDate(currentCriteria.maxDate) : null);
  const seen = new Set();
  const lowerBound = currentCriteria.minId ?? (currentCriteria.minDate ? snowflakeFromDate(currentCriteria.minDate) : null);

  while (pageIndex < maxPages) {
    const data = await client.searchPage(guildId, currentCriteria, offset);
    const rawMessages = flattenMessages(data?.messages);
    const pageMessages = uniqueMessages(rawMessages).filter((message) => {
      if (!message?.id || seen.has(message.id)) return false;
      seen.add(message.id);
      return true;
    });

    for (const message of pageMessages) {
      if (message.timestamp && (!oldestTimestamp || new Date(message.timestamp) < new Date(oldestTimestamp))) {
        oldestTimestamp = message.timestamp;
      }
    }

    const remaining = Math.max(0, maxMessages - fetchedCount);
    const accepted = pageMessages.slice(0, remaining);
    fetchedCount += accepted.length;
    emptyPages = rawMessages.length === 0 ? emptyPages + 1 : 0;

    const totalResults = Number(data?.total_results ?? 0);
    const reachedTotal = totalResults > 0 && fetchedCount >= totalResults;
    const reachedLimit = fetchedCount >= maxMessages || pageIndex + 1 >= maxPages;
    const page = {
      pageIndex,
      offset,
      messages: accepted,
      totalResults,
      rawCount: rawMessages.length,
      fetchedCount,
      oldestTimestamp,
      truncated: reachedLimit && !reachedTotal && fetchedCount < totalResults,
      criteria: { ...currentCriteria },
    };
    await onPage?.(page);
    yield page;

    if (reachedLimit || reachedTotal || (totalResults === 0 && rawMessages.length === 0) || emptyPages >= EMPTY_PAGE_THRESHOLD) return;

    if (offset + pageSize < maxOffset) {
      offset += pageSize;
      pageIndex += 1;
      continue;
    }

    if (!oldestTimestamp) return;
    const nextUpper = snowflakeFromDate(oldestTimestamp);
    if (lowerBound && BigInt(nextUpper) <= BigInt(lowerBound)) return;
    if (lastWindowUpper && BigInt(nextUpper) >= BigInt(lastWindowUpper)) return;

    currentCriteria = { ...currentCriteria, maxId: nextUpper, maxDate: undefined };
    lastWindowUpper = nextUpper;
    offset = 0;
    pageIndex += 1;
  }
}
