# Evidence and claims

The merger owns `AutomationStatus` and `ClaimCeiling`.

Required-step precedence is `Failed > Blocked > Limited > Cancelled > Passed`.

Claim meanings:

- `evidence_collected`: evidence exists, but no pass claim is allowed.
- `automation_passed`: registered machine checks passed.
- `owner_validated`: the implementation Owner validated the player path.
- `externally_reviewed`: all required art/director/function reviews passed.

An ArtAcceptance or T0 machine pass never means visual seal, complete T0, or production approval while external review is `Required`.

Do not promote `OwnerValidation` or `ExternalReview` from free text, a machine status, or the existence of screenshots. Promote them only from explicit review evidence and retain its path in the task handoff.

Every artifact must retain destination path, source path, SHA-256, size, capture time, and MIME type. Evidence from `latest` is valid only when its timestamp is after the RunID job start and the adapter copies it into that RunID root.
