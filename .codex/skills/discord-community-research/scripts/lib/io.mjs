import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';

function replaceSecret(value, secret) {
  if (!secret || value == null) return value;
  return String(value).split(secret).join('[REDACTED]');
}

export function redactSecrets(value, secrets = []) {
  const values = (Array.isArray(secrets) ? secrets : [secrets]).filter(Boolean).map(String);
  if (typeof value === 'string') return values.reduce((result, secret) => replaceSecret(result, secret), value);
  if (Array.isArray(value)) return value.map((item) => redactSecrets(item, values));
  if (value && typeof value === 'object') {
    return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, redactSecrets(item, values)]));
  }
  return value;
}

export function sanitizeError(error, ...secrets) {
  const safe = redactSecrets({
    name: error?.name ?? 'Error',
    message: error?.message ?? String(error),
    status: error?.status ?? null,
    retryable: error?.retryable ?? false,
    attempts: error?.attempts ?? null,
    body: error?.body ?? null,
  }, secrets.flat());
  return safe;
}

export function writeJsonAtomic(filePath, value) {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  const tempPath = `${filePath}.${process.pid}.tmp`;
  fs.writeFileSync(tempPath, `${JSON.stringify(value, null, 2)}\n`, 'utf8');
  fs.renameSync(tempPath, filePath);
}

export function appendNdjson(filePath, value) {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.appendFileSync(filePath, `${JSON.stringify(value)}\n`, 'utf8');
}

export function readNdjson(filePath, fallback = []) {
  if (!fs.existsSync(filePath)) return fallback;
  return fs.readFileSync(filePath, 'utf8')
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean)
    .map((line) => JSON.parse(line));
}

export function writeNdjsonAtomic(filePath, values = []) {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  const tempPath = `${filePath}.${process.pid}.tmp`;
  const content = values.length ? `${values.map((value) => JSON.stringify(value)).join('\n')}\n` : '';
  fs.writeFileSync(tempPath, content, 'utf8');
  fs.renameSync(tempPath, filePath);
}

export function loadJson(filePath, fallback = null) {
  if (!fs.existsSync(filePath)) return fallback;
  return JSON.parse(fs.readFileSync(filePath, 'utf8'));
}

export function createRunDirectory({
  outputRoot = process.env.DISCORD_RESEARCH_OUTPUT || path.join(os.homedir(), '.codex', 'discord-research'),
  runId = new Date().toISOString().replace(/[:.]/g, '-'),
} = {}) {
  const root = path.resolve(outputRoot);
  const directory = path.join(root, runId);
  fs.mkdirSync(directory, { recursive: true });
  return {
    root,
    directory,
    run: path.join(directory, 'run.json'),
    guilds: path.join(directory, 'guilds.json'),
    searchPlan: path.join(directory, 'search-plan.json'),
    messages: path.join(directory, 'messages.ndjson'),
    contexts: path.join(directory, 'contexts.ndjson'),
    evidence: path.join(directory, 'evidence.json'),
    state: path.join(directory, 'state.json'),
    report: path.join(directory, 'report.md'),
  };
}

function parseEnvValue(value) {
  const trimmed = value.trim();
  if ((trimmed.startsWith('"') && trimmed.endsWith('"')) || (trimmed.startsWith("'") && trimmed.endsWith("'"))) {
    return trimmed.slice(1, -1);
  }
  return trimmed;
}

export function loadToken({
  env = process.env,
  cwd = process.cwd(),
  variable = 'VITE_DISCORD_TOKEN',
} = {}) {
  const direct = env[variable] || env.DISCORD_TOKEN;
  if (direct) return direct;

  const envPath = path.join(cwd, '.env');
  if (fs.existsSync(envPath)) {
    const pattern = new RegExp(`^\\s*${variable.replace(/[.*+?^${}()|[\\]\\\\]/g, '\\$&')}\\s*=\\s*(.*?)\\s*$`, 'm');
    const match = fs.readFileSync(envPath, 'utf8').match(pattern);
    if (match?.[1]) return parseEnvValue(match[1]);
  }

  throw new Error(`${variable} is required in the environment or .env`);
}
