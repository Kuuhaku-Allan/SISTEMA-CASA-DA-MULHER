#!/usr/bin/env bash
set -euo pipefail

BASE_OWNER="Sistema-Casa-da-Mulher"
BASE_REPO="SISTEMA-CASA-DA-MULHER"
OWNER_USERNAME="${GITHUB_OWNER_USERNAME:-Kuuhaku-Allan}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
STATUS_FILE="${REPO_ROOT}/projetocasadamulher/telas/dev-status.json"
PYTHON_BIN=""

if command -v python3 >/dev/null 2>&1; then
  PYTHON_BIN="python3"
elif command -v python >/dev/null 2>&1; then
  PYTHON_BIN="python"
elif command -v py >/dev/null 2>&1; then
  PYTHON_BIN="py"
fi

ambiente="local"
if [ "${CODESPACES:-false}" = "true" ]; then
  ambiente="codespaces"
fi

github_user=""
repo_owner=""
repo_name=""
parent_owner=""
parent_repo=""
branch_atual="$(git -C "${REPO_ROOT}" branch --show-current 2>/dev/null || true)"

if command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1; then
  github_user="$(gh api user --jq .login 2>/dev/null || true)"
  repo_owner="$(gh repo view --json owner --jq '.owner.login' 2>/dev/null || true)"
  repo_name="$(gh repo view --json name --jq '.name' 2>/dev/null || true)"
  parent_owner="$(gh repo view --json parent --jq '.parent.owner.login // ""' 2>/dev/null || true)"
  parent_repo="$(gh repo view --json parent --jq '.parent.name // ""' 2>/dev/null || true)"
fi

if [ -z "${repo_owner}" ]; then
  origin_url="$(git -C "${REPO_ROOT}" remote get-url origin 2>/dev/null || true)"
  if [[ "${origin_url}" =~ github.com[:/]+([^/]+)/([^/.]+)(\.git)?$ ]]; then
    repo_owner="${BASH_REMATCH[1]}"
    repo_name="${BASH_REMATCH[2]}"
  fi
fi

is_fork=false
parent_matches_main=false
needs_fork=false
can_open_pr=false
recomendacao="Nao foi possivel detectar o fluxo automaticamente. Confira os guias da equipe."

if [ -n "${parent_owner}" ] && [ -n "${parent_repo}" ]; then
  is_fork=true
fi

if [ "${parent_owner}" = "${BASE_OWNER}" ] && [ "${parent_repo}" = "${BASE_REPO}" ]; then
  parent_matches_main=true
fi

fluxo="desconhecido"

if [ "${repo_owner}" = "${BASE_OWNER}" ] && { [ "${github_user}" = "${OWNER_USERNAME}" ] || [ "${github_user}" = "${BASE_OWNER}" ]; }; then
  fluxo="local_owner"
  needs_fork=false
  can_open_pr=true
  recomendacao="Mantenedor detectado no repositorio principal. Use sua IDE local ou branch propria; fork/Codespaces nao e obrigatorio."
elif [ "${is_fork}" = true ] && [ "${parent_matches_main}" = true ]; then
  fluxo="fork_ok"
  needs_fork=false
  can_open_pr=true
  recomendacao="Fork detectado corretamente. Voce pode trabalhar no Codespaces e abrir Pull Request para o repositorio principal."
elif [ "${repo_owner}" = "${BASE_OWNER}" ] && [ -z "${github_user}" ]; then
  fluxo="desconhecido"
  needs_fork=false
  can_open_pr=false
  recomendacao="Repositorio principal detectado, mas o GitHub CLI nao identificou o usuario. Se voce for mantenedor, use o fluxo local; se for colaboradora, crie um fork."
elif [ "${repo_owner}" = "${BASE_OWNER}" ]; then
  fluxo="precisa_fork"
  needs_fork=true
  can_open_pr=false
  recomendacao="Voce esta no repositorio principal. Se nao for mantenedor, crie um fork antes de trabalhar."
else
  fluxo="precisa_fork"
  needs_fork=true
  can_open_pr=false
  recomendacao="Nao detectei um fork ligado ao repositorio principal. Confira o guia de fork + Codespaces."
fi

if [ -z "${PYTHON_BIN}" ]; then
  echo "ERRO: Python nao encontrado. Nao foi possivel gerar projetocasadamulher/telas/dev-status.json."
  exit 1
fi

"${PYTHON_BIN}" - "$STATUS_FILE" <<PY
import json
import sys

path = sys.argv[1]
data = {
    "ambiente": "${ambiente}",
    "githubUser": "${github_user}",
    "repoOwner": "${repo_owner}",
    "repoName": "${repo_name}",
    "isFork": "${is_fork}" == "true",
    "parentOwner": "${parent_owner}",
    "parentRepo": "${parent_repo}",
    "parentMatchesMainRepo": "${parent_matches_main}" == "true",
    "needsFork": "${needs_fork}" == "true",
    "canOpenPullRequest": "${can_open_pr}" == "true",
    "branchAtual": "${branch_atual}",
    "recomendacaoFluxo": "${recomendacao}",
    "fluxo": "${fluxo}"
}
with open(path, "w", encoding="utf-8") as f:
    json.dump(data, f, ensure_ascii=False, indent=2)
    f.write("\\n")
PY

echo "==> Status do fluxo salvo em projetocasadamulher/telas/dev-status.json"
echo "==> ${recomendacao}"
