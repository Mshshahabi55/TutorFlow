# ADR-0009: Frontend Repository Topology

**Status:** Accepted

## Context

The product brief specifies a frontend covering Public website, Student app, Tutor app, and Admin. The current repository (`TutorFlow`) contains only the .NET backend — no frontend code or frontend tooling exists yet.

## Decision

**Monorepo.** The frontend lives in the same Git repository as the .NET backend, sharing one git history and one PR flow.

Target conceptual structure:

```
TutorFlow/
  apps/
    web/
  src/
    Modules/
      ...
  docs/
  tests/
```

## Rejected options

- **Separate repository** — frontend as its own repo, consuming the backend's OpenAPI contract as its integration boundary, deployed independently.
- **One repo per app** (Public/Student/Tutor/Admin as separate projects) — premature given they share a design system and much of the same data; only relevant if these are expected to deploy/scale fully independently, which is not a current requirement.

## Consequences

- Tooling decisions that are easier to set up once than migrate later (OpenAPI-generated TypeScript client, shared CI) can now proceed against a known topology.
- No frontend scaffolding happens as part of accepting this ADR — only the repository topology decision is encoded. Frontend implementation remains a future task.
- Resolves Architect question A16 (`docs/OPEN_QUESTIONS.md`).
