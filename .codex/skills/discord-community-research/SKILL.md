---
name: discord-community-research
description: Use when researching recent discussions, tools, projects, workflows, recommendations, or community sentiment across Discord servers the user has joined.
---

# Discord Community Research

## Overview

Run auditable Discord-only research with deterministic local scripts. Inventory
all joined servers, reduce broad query cost with two-stage search, preserve
failures and truncation, and keep every conclusion traceable to Discord links.

## Workflow

1. Clarify the objective, date range, ambiguous terms, and desired evidence. Use [query-planning.md](references/query-planning.md) for query families.
2. Persist a JSON plan containing runId, objective, queries, dateRange, and bounded options. Mark broad recall queries as purpose: discovery and practical follow-ups as purpose: validation.
3. Prefer the single-command runner: node scripts/run-research.mjs --plan <plan.json> --output-root <root> --max-hits 20. It inventories joined servers, runs discovery across them, runs validation only in discovery-hit servers, enriches top candidates, and writes state.json, contexts.ndjson, evidence.json, and report.md.
4. For strict co-occurrence, add --require-all ComfyUI,MCP; this filters message body text after deduplication and reports the filtered count.
5. Use --no-enrich for a fast discovery pass. Use --run-id <id> to resume the same output directory. Progress and ETA are written to stderr; the final summary is JSON on stdout.
6. Keep at least one discovery query in a run-research plan. For an intentional validation-only or unrestricted single pass, use run-search-plan.mjs directly.
7. Use the lower-level inventory-guilds.mjs, run-search-plan.mjs, build-evidence-index.mjs, and build-report.mjs commands only when a phase must be inspected or rerun independently.

## Non-Negotiable Rules

- Discord-only by default; external validation requires explicit user approval.
- A failed or rate-limited query is not complete.
- Reusing a runId requires identical task inputs. If a query, date range, criteria,
  or page limit changes, start a new runId instead of mixing old and new evidence.
- Truncation and inaccessible servers are visible in the final report.
- Do not claim absence without sufficient completed coverage.
- Never print or persist Discord credentials.
- Keep options.concurrency bounded (default 1; use 2-3 only when rate limits allow).
- Treat project-linked as a concrete external project URL mentioned in Discord,
  unverified-claim as an official or official-sounding claim without such a URL,
  and community-only as ordinary community evidence.
- Read generated UTF-8 JSON/NDJSON with Node or an editor that preserves UTF-8;
  do not use a legacy PowerShell pipeline as the parser for Chinese or emoji text.

Evidence grades describe Discord evidence strength only; they do not prove quality,
correctness, safety, popularity, or external project activity. See
[evidence-rubric.md](references/evidence-rubric.md) for grading and uncertainty language.
