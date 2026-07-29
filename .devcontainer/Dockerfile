# Dev/CI-only devcontainer (docs/decisions/PENDING-OWNER-DECISIONS.md
# Section 5, resolved 2026-07-29: dev/CI Docker is approved; production
# containerization is deferred, and the ADR-018 Iran-reachability
# constraint applies only to a real production deployment, not to this
# tooling). Version-corrected to match the project (.NET 9 —
# backend/global.json; PostgreSQL 17 — docs/adr/ADR-013-persistence-
# technology.md addendum, see docker-compose.yml). Not build-verified in
# every environment — if `docker compose up` fails, the natively-installed
# PostgreSQL 17 setup in README.md's "Database setup" section remains the
# always-supported fallback.
FROM mcr.microsoft.com/devcontainers/dotnet:2-9.0

# [Optional] Uncomment this section to install additional OS packages.
# RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
#     && apt-get -y install --no-install-recommends <your-package-list-here>

# [Optional] Uncomment this line to install global node packages.
# RUN su vscode -c "source /usr/local/share/nvm/nvm.sh && npm install -g <your-package-here>" 2>&1