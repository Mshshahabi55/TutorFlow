# UNUSED SCAFFOLDING — not the supported dev setup path. See the banner
# comment at the top of docker-compose.yml in this same directory for why:
# this pins .NET 8.0 while the project targets .NET 9 (backend/global.json),
# and pulls from Docker Hub, which ADR-018 flags as potentially unreachable
# from Iran. The supported local setup is a natively-installed PostgreSQL 17
# instance — see README.md's "Database setup" section.
FROM mcr.microsoft.com/devcontainers/dotnet:2-8.0

# [Optional] Uncomment this section to install additional OS packages.
# RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
#     && apt-get -y install --no-install-recommends <your-package-list-here>

# [Optional] Uncomment this line to install global node packages.
# RUN su vscode -c "source /usr/local/share/nvm/nvm.sh && npm install -g <your-package-here>" 2>&1