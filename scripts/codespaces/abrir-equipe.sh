#!/usr/bin/env bash
set -euo pipefail

OPEN_MODE="${1:-}"

build_base_url() {
  if [ -n "${CODESPACE_NAME:-}" ] && [ -n "${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN:-}" ]; then
    local domain="${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN#https://}"
    domain="${domain#http://}"
    domain="${domain%.}"
    printf 'https://%s-5500.%s' "${CODESPACE_NAME}" "${domain}"
    return
  fi

  printf 'http://localhost:5500'
}

open_url() {
  local url="$1"

  if [ "${OPEN_MODE}" = "--print-only" ]; then
    return
  fi

  if command -v code >/dev/null 2>&1; then
    code --open-url "${url}" >/dev/null 2>&1 || true
    return
  fi

  if command -v xdg-open >/dev/null 2>&1; then
    xdg-open "${url}" >/dev/null 2>&1 || true
    return
  fi

  if command -v open >/dev/null 2>&1; then
    open "${url}" >/dev/null 2>&1 || true
  fi
}

BASE_URL="$(build_base_url)"
EQUIPE_URL="${BASE_URL}/equipe.html"

printf '\n'
printf 'Área da Equipe: %s\n' "${EQUIPE_URL}"
printf 'Ativar EQP:     %s/projetocasadamulher/telas/equipe-ativar.html\n' "${BASE_URL}"
printf 'Login:          %s/projetocasadamulher/telas/index.html\n' "${BASE_URL}"
printf 'Protótipos:     %s/prototipos/index.html\n' "${BASE_URL}"
printf '\n'
printf 'Se o navegador não abrir sozinho:\n'
printf '1. Abra a aba Ports/Portas.\n'
printf '2. Clique no link da porta 5500.\n'
printf '3. Acesse /equipe.html.\n'
printf '\n'

open_url "${EQUIPE_URL}"
