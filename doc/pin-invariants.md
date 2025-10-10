## Pins Domain: Invariants and Rules by Aggregate/Entity

This document distills enforceable business rules from doc/pins-ubiquitous-language.md, grouped by aggregate/entity. Use it as a reference when implementing domain logic and validations.


### Pin
- Exactly one Pin exists per GooglePlaceId (no duplicates for the same Place).
- Location must be a valid geographic coordinate (latitude in [-90, 90], longitude in [-180, 180]).
- Tag assignments are governed by the TagCatalog; only allowed tags may be assigned; tags are deduplicated (case-insensitive) and normalized.
- AssignTags replaces the entire tag set.
- Verification updates may change ExistsStatus; each update must record when the check occurred and its source.

### TagCatalog
- Represents a finite, curated set of allowed tags (global or per owner).
- Tags are case-insensitive unique within the catalog.
- DefineTags replaces the entire set.
- Add is idempotent; Remove deletes by case-insensitive match.

### ImportJob
- Only one ImportJob may be active (InProgress) per user at a time.
- Progress counters (Processed, Created, Updated, Failed, Conflicts) do not decrease during InProgress.
- AppendBatch is only valid while InProgress.
- CompleteImport and FailImport are only valid from InProgress and are terminal; no further batches after terminal.

### TaggingJob
- Tag suggestions must map to tags defined in TagCatalog; suggestions are deduplicated and normalized.
- Valid workflow: Started → Completed | Failed.
- Suggestions can be produced only while Started; no suggestions after terminal states.

### VerificationJob
- Operates over a defined scope of Pins; verification updates must include when the check occurred and its source.
- Valid workflow: Started → Completed | Failed.
- No further state changes after terminal states.

### Cross-cutting business policies
- TagCatalog is the single source of truth for allowed tags; all tag operations (manual or AI) validate membership.
- One Pin per GooglePlaceId across the system.
- Job aggregates (Import, Tagging, Verification) are single-run with terminal states; actions are only permitted in active states.

