#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
API_DIR="${REPO_ROOT}/CasaMulher.Api"

echo "==> Preparando ambiente do Sistema Casa da Mulher"
echo "==> Nenhum segredo real sera solicitado."

cd "${API_DIR}"

echo "==> Restaurando pacotes .NET"
dotnet restore

if [ -f ".config/dotnet-tools.json" ]; then
  echo "==> Restaurando ferramentas .NET"
  dotnet tool restore
else
  echo "==> Nenhum dotnet-tools.json encontrado. Pulando ferramentas locais."
fi

echo "==> Aplicando migrations do banco de desenvolvimento"
if ASPNETCORE_ENVIRONMENT=Development dotnet tool run dotnet-ef database update; then
  echo "==> Banco de desenvolvimento atualizado."
else
  echo "AVISO: Nao foi possivel aplicar migrations automaticamente."
  echo "AVISO: Confira a saida acima. O ambiente ainda pode iniciar se a API aplicar migrations no primeiro start."
fi

echo "==> Detectando fluxo GitHub/Codespaces"
bash "${SCRIPT_DIR}/detectar-fluxo.sh" || true

echo "==> Ambiente preparado."
echo "==> Para iniciar, use a tarefa: Casa da Mulher: iniciar sistema"
echo "==> Depois, use a tarefa: Casa da Mulher: abrir area da equipe"
