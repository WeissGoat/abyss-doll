---
name: p3-release-validation
description: Read-only aggregate completed Project P3 ProgramRunID and ArtRunID evidence into a release decision. Use for vertical-slice or T0 release gates; never run Unity tests, mutate Unity, or substitute for required art review.
---
# P3 Release Validation

Accept only `ProgramRunID`, `ArtRunID`, and a release Profile. Run `Merge-P3ReleaseEvidence.ps1`, verify fingerprints and input summaries, then report `Failed`, `Blocked`, `Limited`, `ReviewRequired`, or `Passed` according to [release-profiles.md](references/release-profiles.md). Never execute tests or Unity mutations.
