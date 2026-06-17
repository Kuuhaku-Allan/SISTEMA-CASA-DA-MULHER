#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
TEMPLATE_DIR="${REPO_ROOT}/prototipos/template-tela"
MANIFEST_FILE="${REPO_ROOT}/prototipos/manifest.json"

NOME_TELA=""
AUTOR=""
DRY_RUN=false

while [ "$#" -gt 0 ]; do
  case "$1" in
    --nome)
      NOME_TELA="${2:-}"
      shift 2
      ;;
    --autor)
      AUTOR="${2:-}"
      shift 2
      ;;
    --dry-run)
      DRY_RUN=true
      shift
      ;;
    -h|--help)
      echo "Uso: scripts/codespaces/novo-prototipo.sh [--nome \"Tela\"] [--autor usuario] [--dry-run]"
      exit 0
      ;;
    *)
      echo "Argumento desconhecido: $1"
      exit 1
      ;;
  esac
done

if [ -z "${AUTOR}" ] && command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1; then
  AUTOR="$(gh api user --jq .login 2>/dev/null || true)"
fi

if [ -z "${AUTOR}" ]; then
  read -r -p "Seu nome ou usuario do GitHub: " AUTOR
fi

if [ -z "${NOME_TELA}" ]; then
  read -r -p "Nome da tela/prototipo: " NOME_TELA
fi

if [ -z "${AUTOR}" ] || [ -z "${NOME_TELA}" ]; then
  echo "ERRO: informe autor e nome da tela."
  exit 1
fi

if [ ! -d "${TEMPLATE_DIR}" ]; then
  echo "ERRO: template nao encontrado em prototipos/template-tela."
  exit 1
fi

slugify() {
  node -e "
const input = process.argv[1] || '';
const slug = input
  .normalize('NFD')
  .replace(/[\\u0300-\\u036f]/g, '')
  .toLowerCase()
  .replace(/[^a-z0-9]+/g, '-')
  .replace(/^-+|-+$/g, '')
  .slice(0, 60);
console.log(slug || 'prototipo');
" "$1"
}

AUTOR_SLUG="$(slugify "${AUTOR}")"
TELA_SLUG="$(slugify "${NOME_TELA}")"
DEST_DIR="${REPO_ROOT}/prototipos/colaboradores/${AUTOR_SLUG}/${TELA_SLUG}"
RELATIVE_INDEX="colaboradores/${AUTOR_SLUG}/${TELA_SLUG}/index.html"

if [ -e "${DEST_DIR}" ]; then
  echo "ERRO: ja existe um prototipo nesse caminho:"
  echo "${DEST_DIR}"
  exit 1
fi

if [ "${DRY_RUN}" = true ]; then
  echo "==> Simulacao OK."
  echo "Autor: ${AUTOR_SLUG}"
  echo "Prototipo: ${TELA_SLUG}"
  echo "Destino: ${DEST_DIR}"
  exit 0
fi

mkdir -p "${DEST_DIR}"
cp "${TEMPLATE_DIR}/index.html" "${DEST_DIR}/index.html"
cp "${TEMPLATE_DIR}/style.css" "${DEST_DIR}/style.css"
cp "${TEMPLATE_DIR}/script.js" "${DEST_DIR}/script.js"

node - "${MANIFEST_FILE}" "${NOME_TELA}" "${AUTOR}" "${RELATIVE_INDEX}" <<'NODE'
const fs = require("fs");

const [manifestPath, titulo, autor, pasta] = process.argv.slice(2);
let manifest = { prototipos: [] };

if (fs.existsSync(manifestPath)) {
  manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
}

if (!Array.isArray(manifest.prototipos)) {
  manifest.prototipos = [];
}

const exists = manifest.prototipos.some((item) => item.pasta === pasta);

if (!exists) {
  manifest.prototipos.push({
    titulo,
    autor,
    pasta,
    status: "rascunho"
  });
}

manifest.prototipos.sort((a, b) => String(a.titulo).localeCompare(String(b.titulo), "pt-BR"));
fs.writeFileSync(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`, "utf8");
NODE

echo "==> Prototipo criado:"
echo "    ${DEST_DIR}"
echo
echo "Edite apenas essa pasta e use dados ficticios."
echo "Abra prototipos/index.html para visualizar a lista."
