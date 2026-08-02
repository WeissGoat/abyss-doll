#!/usr/bin/env node
import { createDiscordClient } from './lib/api.mjs';
import { createRunDirectory, loadToken, sanitizeError, writeJsonAtomic } from './lib/io.mjs';

export async function inventoryGuilds({ client, outputPath = null, now = new Date().toISOString() } = {}) {
  if (!client?.inventoryGuilds || !client?.inventoryChannels) throw new TypeError('client inventory methods are required');
  const rawGuilds = await client.inventoryGuilds();
  const guilds = [];

  for (const guild of Array.isArray(rawGuilds) ? rawGuilds : []) {
    const record = {
      id: guild.id,
      name: guild.name ?? null,
      icon: guild.icon ?? null,
      owner: guild.owner ?? null,
      channels: [],
      channelError: null,
    };
    try {
      const channels = await client.inventoryChannels(guild.id);
      record.channels = (Array.isArray(channels) ? channels : []).map((channel) => ({
        id: channel.id,
        name: channel.name ?? null,
        type: channel.type ?? null,
        parentId: channel.parent_id ?? channel.parentId ?? null,
        position: channel.position ?? null,
        threadMetadata: channel.thread_metadata ?? null,
      }));
    } catch (error) {
      record.channelError = sanitizeError(error);
    }
    guilds.push(record);
  }

  const result = {
    generatedAt: now,
    guilds,
    totals: {
      joinedGuilds: guilds.length,
      accessibleChannelInventories: guilds.filter((guild) => !guild.channelError).length,
      inaccessibleChannelInventories: guilds.filter((guild) => guild.channelError).length,
    },
  };
  if (outputPath) writeJsonAtomic(outputPath, result);
  return result;
}
function getArg(name, fallback = null) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] ?? fallback : fallback;
}

if (process.argv[1]?.endsWith('inventory-guilds.mjs')) {
  const runDir = createRunDirectory({ outputRoot: getArg('--output-root') ?? undefined, runId: getArg('--run-id') ?? undefined });
  const token = loadToken({ variable: getArg('--token-env') ?? 'VITE_DISCORD_TOKEN' });
  const client = createDiscordClient({ token, baseUrl: getArg('--base-url') ?? undefined });
  const result = await inventoryGuilds({ client, outputPath: getArg('--output') ?? runDir.guilds });
  console.log(JSON.stringify(result.totals, null, 2));
}
