#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
API_DIR="${REPO_ROOT}/CasaMulher.Api"

cd "${API_DIR}"

echo "==> Atualizando banco de desenvolvimento"
dotnet tool restore
ASPNETCORE_ENVIRONMENT=Development dotnet tool run dotnet-ef database update

echo "==> Convites demo sao criados automaticamente quando a API inicia em Development."
echo "==> Exemplo de cadastro demo: recepcao@casamulher.local / REC-2026 / Senha@123"
