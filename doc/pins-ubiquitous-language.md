## Pins Domain Ubiquitous Language

Purpose: Shared vocabulary for the Pins domain (managing starred Google Maps places), derived from the code in src/Pinventory.Pins. This document standardizes terminology for clarity.



### Core Concepts
- Pin: Canonical record of a saved Google Place with domain metadata. Key fields: GooglePlaceId, Name, Address, Location, ExistsStatus, ExistsCheckedAt, UpdatedAt, Tags, Version.
- Tag: String label assigned to Pins for categorization and fast filtering/search.
- Tag Catalog: Curated, finite set of allowed Tags (aka categories). Can be global or user-owned.
- Tagging: The process of assigning Tags to Pins, manually or AI-assisted.

- Verification Job: Process that checks whether a Pin’s Place still exists or is closed; results stored on Pin.ExistsStatus.
- Import Job: Process that ingests starred places from Google Data Portability.
- Exists Status: Pin existence status: Unknown, Open, or Closed.


### Aggregates and Entities
- Pin (Aggregate Root)
  - Responsibilities
    - Hold Place identity (GooglePlaceId) and snapshot data (Name, Address, Location).
    - Track verification (ExistsStatus, ExistsCheckedAt) and last update (UpdatedAt).
    - Manage child Tags (PinTag), enforcing normalization and deduplication.
    - Emit domain events on import/update, tag assignment, and verification updates.
  - Invariants / Rules
    - GooglePlaceId is required and unique (DB unique index) to avoid duplicate Pins for the same Place.
    - Name is required and trimmed.
    - Location coordinates validated: latitude [-90, 90], longitude [-180, 180].
    - AssignTags fully replaces the tag set with distinct, trimmed values; raises PinTagsAssigned.
    - UpdateVerification sets ExistsStatus/ExistsCheckedAt; raises PinVerificationUpdated.
    - Version is an optimistic concurrency token (EF-managed).
  - Child entity: PinTag { PinId, Tag } (Tag required, trimmed).

- ImportJob (Aggregate Root)
  - Responsibilities
    - Represent a single import job for a user/source (e.g., Google Data Portability of starred places).
    - Track counters (Processed/Created/Updated/Failed/Conflicts), state, timestamps, optional cursor.
    - Emit lifecycle and progress events.
  - States: InProgress → Complete | Failed (enum also includes Unspecified, Cancelled).
  - Invariants / Rules
    - Only one InProgress ImportJob per user (domain policy IImportConcurrencyPolicy + DB unique index on (UserId, State='InProgress')).
    - AppendBatch requires non-negative counters and state InProgress.
    - Complete/Fail only valid when InProgress.

- TaggingJob (Aggregate Root)
  - Responsibilities
    - Represent a tagging job (often AI-assisted) over a Scope using a ModelVersion.
    - Track state and timestamps; emit started/suggestion/completed/failed events.
  - States: Started, Completed, Failed.

- VerificationJob (Aggregate Root)
  - Responsibilities
    - Represent a verification job over a Scope.
    - Track state and timestamps; emit started/completed/failed events.
  - States: Started, Completed, Failed.

- TagCatalog (Aggregate Root)
  - Responsibilities
    - Maintain the allowed set of Tags (global or per OwnerUserId).
    - Define/replace full set (DefineTags); add/remove single tags.
  - Rules
    - Tags trimmed; case-insensitive distinctness enforced in catalog.
    - Add is idempotent (no change if tag already exists case-insensitively).
  - Child entity: TagItem { CatalogId, Tag } (Tag required, trimmed).

### Value Objects and Enums
- GooglePlaceId: Opaque identifier for a Google Place; non-empty, trimmed; equality semantics; implicit conversions to/from string.
- Address: Record with optional address line.
- Location: Latitude/Longitude pair with range validation; factory method Location.Of.
- ExistsStatus (enum): Unknown | Open | Closed.

### Domain Events
- Pin events (namespace Pinventory.Pins.Events)
  - PinImported(PinId, GooglePlaceId)
  - PinUpdated(PinId)
  - PinTagsAssigned(PinId, Tags[], Reason)
  - PinVerificationUpdated(PinId, ExistsStatus, CheckedAt, Source)

- Import job events (namespace Pinventory.Pins.Import.Events)
  - ImportStarted(ImportJobId, UserId, Source)
  - ImportBatchProcessed(ImportJobId, Processed, Created, Updated, Failed, Conflicts)
  - ImportCompleted(ImportJobId)
  - ImportFailed(ImportJobId, Error)

- Tagging job events
  - TaggingStarted(JobId, Scope, ModelVersion)
  - TaggingSuggestionProduced(JobId, PinId, Tags[], Confidence)
  - TaggingCompleted(JobId)
  - TaggingFailed(JobId, Error)

- Verification job events
  - VerificationStarted(JobId, Scope)
  - VerificationCompleted(JobId)
  - VerificationFailed(JobId, Error)

### Commands (application messages touching the domain)
- ImportOrUpdatePin(GooglePlaceId, Name, Address, Lat, Lng, StarredAt, IdempotencyKey)
- AssignTags(PinId, Tags, Reason, ExpectedVersion)
- UpdateVerificationStatus(PinId, ExistsStatus, CheckedAt, Source, ExpectedVersion)
- StartImport(UserId, Source, Cursor, IdempotencyKey); AppendBatchStats; CompleteImport; FailImport
- StartTagging(Scope, ModelVersion); ApplySuggestion; StartVerification(Scope)

These are application-layer contracts; included here to align vocabulary with domain operations.

### Business Rules and Invariants
- No duplicate Pins for the same GooglePlaceId (unique index ensures one Pin per Place identity).
- Only one active ImportJob per user (policy + DB uniqueness).
- Tagging semantics
  - Pin.AssignTags is full replacement and deduplicated; tags are trimmed; emits event with Reason.
  - TagCatalog governs the finite allowed set of Tags; case-insensitive uniqueness.
- Validation
  - Location must be valid lat/lng; GooglePlaceId must be non-empty and within reasonable length.
- Concurrency
  - Pin.Version is used for optimistic concurrency; relevant commands carry ExpectedVersion.
- Verification
  - UpdateVerification records ExistsStatus transitions with timestamp and source via events for auditability.

### Quick Glossary (preferred terms)
- Pin: Aggregate representing a saved Google Place and its metadata.
- GooglePlaceId: Value object pointing to the external Google Place.
- Address: Value object for address line.
- Location: Value object for lat/lng coordinates.
- ExistsStatus: Unknown | Open | Closed.
- Tag: String label applied to a Pin.
- Tagging: The act of assigning Tags to Pins.
- PinTag: Child entity representing a Tag on a Pin.
- TagCatalog: Aggregate holding allowed Tags (category list).
- TagItem: Catalog entry for a Tag.
- ImportJob: Aggregate for an import job.
- TaggingJob: Aggregate for tagging (manual or AI-assisted).
- VerificationJob: Aggregate for verification.
