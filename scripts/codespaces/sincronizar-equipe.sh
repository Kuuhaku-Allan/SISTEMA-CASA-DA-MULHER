#!/usr/bin/env bash
set -euo pipefail

MODE="${1:-}"
API_URL="${CASA_MULHER_API_URL:-http://localhost:5001}"
DB_REPO="${GITHUB_EQP_DB_REPO_FULL:-Sistema-Casa-da-Mulher/ACESSO-EQUIPE}"
DB_PATH="${GITHUB_EQP_DB_PATH:-data/equipe-db.json}"

warn() {
  printf '[Aviso] %s\n' "$1" >&2
}

finish_best_effort() {
  if [ "${MODE}" = "--best-effort" ]; then
    warn "$1"
    exit 0
  fi

  printf '[Erro] %s\n' "$1" >&2
  exit 1
}

wait_for_api() {
  local tries=30

  while [ "${tries}" -gt 0 ]; do
    if curl -fsS "${API_URL}/api/portal-eqp/status" >/dev/null 2>&1 || curl -fsS "${API_URL}/swagger/index.html" >/dev/null 2>&1; then
      return 0
    fi

    tries=$((tries - 1))
    sleep 1
  done

  return 1
}

if ! wait_for_api; then
  finish_best_effort "API nao respondeu em ${API_URL}."
fi

TMP_JSON="$(mktemp)"
cleanup() {
  rm -f "${TMP_JSON}"
}
trap cleanup EXIT

if command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1; then
  printf '==> Lendo %s em %s pelo gh CLI\n' "${DB_PATH}" "${DB_REPO}"

  if ! gh api "repos/${DB_REPO}/contents/${DB_PATH}" --jq '.content' | tr -d '\r\n ' | base64 -d > "${TMP_JSON}"; then
    finish_best_effort "Nao foi possivel ler o JSON privado com gh CLI."
  fi

  printf '==> Importando membros para o banco local\n'
  curl -fsS \
    -X POST \
    -H 'Content-Type: application/json' \
    --data-binary "@${TMP_JSON}" \
    "${API_URL}/api/equipe/sincronizar-github-db"
  printf '\n'
  exit 0
fi

warn "gh CLI nao autenticado. Tentando sincronizar pela API com token de ambiente."
curl -fsS \
  -X POST \
  -H 'Content-Type: application/json' \
  -d '{}' \
  "${API_URL}/api/equipe/sincronizar-github-db" || finish_best_effort "Sincronizacao falhou. Entre com gh auth login ou configure GITHUB_EQP_READ_TOKEN."
printf '\n'
