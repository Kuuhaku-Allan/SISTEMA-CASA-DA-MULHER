#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
API_DIR="${REPO_ROOT}/CasaMulher.Api"
FRONT_DIR="${REPO_ROOT}"

API_PID=""
FRONT_PID=""

cleanup() {
  echo
  echo "==> Encerrando processos do ambiente..."
  if [ -n "${API_PID}" ] && kill -0 "${API_PID}" 2>/dev/null; then
    kill "${API_PID}" 2>/dev/null || true
  fi
  if [ -n "${FRONT_PID}" ] && kill -0 "${FRONT_PID}" 2>/dev/null; then
    kill "${FRONT_PID}" 2>/dev/null || true
  fi
}

trap cleanup EXIT INT TERM

echo "==> Detectando fluxo GitHub/Codespaces"
bash "${SCRIPT_DIR}/detectar-fluxo.sh" || true

echo "==> Iniciando API em http://0.0.0.0:5001"
cd "${API_DIR}"
ASPNETCORE_ENVIRONMENT=Development \
DOTNET_ENVIRONMENT=Development \
dotnet run --urls "http://0.0.0.0:5001" &
API_PID=$!

echo "==> Iniciando telas em http://0.0.0.0:5500"
cd "${FRONT_DIR}"
python3 -m http.server 5500 --bind 0.0.0.0 &
FRONT_PID=$!

cat <<'INFO'

Sistema iniciado.

Portas:
- API:   5001
- Telas:      5500/projetocasadamulher/telas/
- Equipe:     5500/equipe.html
- Prototipos: 5500/prototipos/

Para testar a recepcao em ambiente novo:
1. Abra projetocasadamulher/telas/cadastro.html usando o convite demo recepcao@casamulher.local / REC-2026.
2. Crie a senha Senha@123.
3. Entre com o ID gerado na tela e a senha criada.

Para parar, pressione Ctrl+C neste terminal.

INFO

bash "${SCRIPT_DIR}/abrir-equipe.sh" --print-only

wait -n "${API_PID}" "${FRONT_PID}"
