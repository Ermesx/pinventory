## Pins Domain Ubiquitous Language Glossary

Purpose: Authoritative glossary of terms and definitions for the Pins domain (managing starred Google Maps places).
Scope: Definitions only; for behavior, rules, and constraints see doc/pin-invariants.md.

### Glossary
- Address: Postal address; value object used on a Pin.
- GooglePlaceId: Identifier for a Google Place; value object.
- ImportJob: An import job for a user and source (e.g., Google Data Portability); see doc/pin-invariants.md for workflow rules.
- Location: Latitude/Longitude coordinate pair; value object.
- Pin: Aggregate representing a saved Google Place with domain metadata (PlaceId, Address, Location, Status, StatusUpdatedAt, Tags).
- PinStatus: Enumeration of a Pin’s operational state: Unknown, Open, Closed, TemporaryClosed.
- Tag: Normalized label applied to Pins; value object also used by TagCatalog.
- TagCatalog: Aggregate holding the allowed set of Tags (global or per owner).
- Tagging: The act of assigning tags to Pins (may be manual or AI‑assisted).
- TaggingJob: Background job that performs tagging over a defined scope.
- VerificationJob: Background job that verifies places over a defined scope.

### Relationships
- Pin owns a collection of Tags to categorize Pins and enable fast filtering/search.
- Pin uses GooglePlaceId, Address, and Location to uniquely identify and locate the place with user‑friendly context.
- TagCatalog defines the allowed set of Tags used across Pins to keep a consistent, finite vocabulary.
- ITagVerifier filters which Tags can be assigned to a Pin to enforce the allowed set and prevent invalid/noisy labels.
- ImportJob creates new Pins and updates existing Pins from external source data to keep the dataset in sync with the user’s starred places.
- TaggingJob assigns Tags to Pins, guided by the TagCatalog to apply consistent categories at scale.
- VerificationJob checks Pins against external sources to confirm their status so the data reflects real‑world changes.

### Detailed rules and constraints

See doc/pin-invariants.md for the authoritative list of business rules, constraints, and invariants.
