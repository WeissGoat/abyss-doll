---
name: p3-program-validation
description: Run Project P3 compile, registered Smoke, T0 functional, P0, configuration, and code acceptance validation. Use for program regressions and automated tests; never use for ArtAcceptance, screenshots, visual seals, or art review claims.
---

# P3 Program Validation

1. Read `agent_status/program.md` and the affected program facts.
2. Select `smoke_focus`, `t0_functional`, or `p0_full` using [profile-routing.md](references/profile-routing.md).
3. Create a ProgramRunID with `tools/agent/p3-validation-core/New-P3ValidationRun.ps1`.
4. Run each registered static step through `Invoke-P3ProgramStaticValidation.ps1`.
5. Pin the intended Unity instance and call `p3_program_run_profile`; never pass raw test method names.
6. Merge with `Merge-P3ValidationEvidence.ps1 -Domain program`.
7. Report only the claim ceiling in [claims.md](references/claims.md).

Do not start `ArtAcceptanceRunner`, require screenshots, mutate visual presentation, or claim art/external review completion.
