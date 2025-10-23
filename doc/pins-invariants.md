## Pins Domain: Invariants and Rules by Aggregate/Entity

This document distills enforceable business rules from doc/pins-ubiquitous-language.md, grouped by aggregate/entity, aligned with the current code.


### Pin
- Exactly one Pin exists per PlaceId (GooglePlaceId) — unique index ensures no duplicates for the same Place.
- Location must be a valid geographic coordinate (latitude in [-90, 90], longitude in [-180, 180]).
- Tag assignment
  - Input tags are filtered by an ITagVerifier (allowed set), trimmed, normalized to lowercase, and deduplicated (case‑insensitive).
  - AssignTags replaces the entire tag set; raises PinTagsAssigned only if the resulting set is non‑empty.
- Status transitions
  - Open() allowed from Unknown or TemporaryClosed; Close(isTemporary) allowed from Open or TemporaryClosed.
  - Transitions update StatusUpdatedAt and raise PinOpened/PinClosed with previous status.
  - Calling Open/Close when already in target state is a no‑op (success without event).

### TagCatalog
- Represents a finite, curated set of allowed tags (global or per owner) as normalized Tag value objects.
- Tags are case‑insensitive unique within the catalog.
- DefineTags replaces the entire set; raises TagCatalogTagsDefined with the normalized list.
- AddTag adds when missing; duplicate add fails; raises TagCatalogTagAdded only on add.
- RemoveTag removes by normalized match; missing is a success but raises no event; raises TagCatalogTagRemoved only when removed.

### ImportJob
- At most one ImportJob may be active (InProgress) per user at a time — enforced by IImportConcurrencyPolicy in application/infrastructure.
- Progress counters (Processed, Created, Updated, Failed, Conflicts) are non‑negative and accumulate during InProgress.
- AppendBatch is only valid while InProgress.
- Complete and Fail are only valid from InProgress and are terminal; no further batches after terminal.

### TaggingJob
- Valid workflow: Started → Completed | Failed.
- Suggestions can be produced only while Started; no suggestions after terminal states.

### VerificationJob
- Valid workflow: Started → Completed | Failed.

### Cross‑cutting business policies
- Tags are owned value objects on both Pin and TagCatalog and are normalized (trimmed, lowercased).
- One Pin per GooglePlaceId across the system.
- Pin and TagCatalog use Version as optimistic concurrency tokens (EF‑managed).
- Domain events are raised for auditability on state changes and tag operations.

