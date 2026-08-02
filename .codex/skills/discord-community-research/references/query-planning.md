# Query Planning

Build a small query family instead of relying on one broad keyword. Persist each
query with an id, exact content, purpose, and optional criteria.

## Families

- Exact names: project, product, repository, package, model, or abbreviation.
- Variants: spelling, punctuation, capitalization, plural, translated, and common misspellings.
- Problem language: the question a user would ask when blocked.
- Technical language: install, setup, export, workflow, API, format, error, limit, and version terms.
- Validation language: demo, result, example, benchmark, used, production, limitation, and comparison.

Use discovery queries for recall and validation queries for practical detail. Pair
ambiguous terms with a domain term or filter them after retrieval. Do not treat a
server name or a single high-frequency word as proof of relevance.

## Two Passes

1. Mark exact names, aliases, and broad problem terms as purpose: discovery.
2. Run discovery queries across all joined servers with a small
   options.discoveryMaxPages (usually 1).
3. Extract names, URLs, package identifiers, and repeated vocabulary from strong
   discovery hits. The runner automatically restricts validation queries to
   servers with at least one discovery hit.
4. Mark install, workflow, limitation, demo, and usage terms as
   purpose: validation; run them with the normal options.maxPages.

The single-command runner requires at least one discovery query. Use the
lower-level run-search-plan.mjs command for an intentional validation-only or
unrestricted single pass.

Keep the original query attached to each hit. When two queries find the same
message, merge the message and retain all matched queries.

For ambiguous topics, use filters.requireAll or the --require-all term-a,term-b
flag. The filter applies to message body text after message deduplication, so
coverage statistics can show how many messages were removed without confusing
a partial match with a co-occurrence.

## Ambiguity

Record competing meanings in `ambiguities`. Require co-occurring terms when a
term has common unrelated meanings. If ambiguity remains, report the uncertainty
and do not silently convert a partial match into a finding.
