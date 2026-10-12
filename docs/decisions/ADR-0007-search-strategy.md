# ADR-0007: Search Strategy — PostgreSQL Now, OpenSearch-Ready Later

**Status:** Accepted

## Context

Tutor discovery/search is one of the most important product surfaces (`docs/domain/DOMAIN_MODEL.md`). The brief allows starting on PostgreSQL but requires the architecture not block a future move to OpenSearch/Elasticsearch.

## Decision

`Search` owns a denormalized read model (`TutorSearchDocument`) built from events consumed from `Tutors`, `Subjects`, `Reviews`, `Availability` — never a live cross-module query. Initial implementation queries this read model via PostgreSQL (full-text/trigram). `Search` must never perform cross-module database joins. Because the read model is already event-sourced and module-owned, swapping the query engine later (index the same read model into OpenSearch instead of/alongside Postgres) does not require redesigning `Search`'s boundary or its producers' event contracts — only `Search`'s internal query implementation changes.

## Consequences

- Other modules (`Tutors`, `Subjects`, `Reviews`, `Availability`) must publish integration events carrying whatever `Search` needs to stay current; the exact event payload contracts are designed when each producing module's first relevant feature is scaffolded, not pre-built speculatively.
- `Search`'s PostgreSQL implementation (full-text/trigram indexes) is an implementation detail inside the module; an eventual OpenSearch migration only touches `Search` internals.

## Status

Accepted — resolves Architect question A14 (`docs/OPEN_QUESTIONS.md`). The event contracts this depends on (what exactly `Tutors`/`Subjects`/`Reviews`/`Availability` publish) are still designed per-producer at scaffold time, not pre-decided here.
