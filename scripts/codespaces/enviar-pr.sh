#!/usr/bin/env bash
set -euo pipefail

BASE_REPO="Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER"
BASE_BRANCH="main"
BASE_OWNER="Sistema-Casa-da-Mulher"
BASE_NAME="SISTEMA-CASA-DA-MULHER"
OWNER_USERNAME="${GITHUB_OWNER_USERNAME:-Kuuhaku-Allan}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "==> Preparando envio para revisao"

bash "${SCRIPT_DIR}/detectar-fluxo.sh" || true

if ! command -v gh >/dev/null 2>&1; then
  echo "ERRO: GitHub CLI (gh) nao encontrado."
  exit 1
fi

if ! gh auth status >/dev/null 2>&1; then
  echo "ERRO: voce precisa estar autenticada no GitHub CLI."
  echo "No Codespaces, tente rodar: gh auth login"
  exit 1
fi

GITHUB_USER="${GITHUB_USER:-$(gh api user --jq .login)}"

if [ -z "${GITHUB_USER}" ]; then
  echo "ERRO: nao foi possivel identificar seu usuario do GitHub."
  exit 1
fi

IS_MAINTAINER=false
if [ "${GITHUB_USER}" = "${OWNER_USERNAME}" ]; then
  IS_MAINTAINER=true
fi

ORIGIN_URL="$(git remote get-url origin 2>/dev/null || true)"
if [[ "${ORIGIN_URL}" =~ github.com[:/]+${BASE_OWNER}/${BASE_NAME}(\.git)?$ ]] && [ "${IS_MAINTAINER}" != true ]; then
  echo "ERRO: este ambiente esta apontando para o repositorio principal."
  echo "Colaboradoras devem trabalhar no proprio fork e abrir PR para ${BASE_REPO}."
  echo "Crie um fork, abra Codespaces no fork e tente novamente."
  exit 1
fi

CHANGED_FILES="$(
  {
    git diff --name-only
    git diff --cached --name-only
    git ls-files --others --exclude-standard
  } | sort -u
)"

if [ -z "${CHANGED_FILES}" ]; then
  echo "Nada para enviar. Faca uma alteracao antes de abrir Pull Request."
  exit 0
fi

if [ "${IS_MAINTAINER}" != true ]; then
  FORA_DE_PROTOTIPOS="$(printf '%s\n' "${CHANGED_FILES}" | grep -v '^prototipos/' || true)"

  if [ -n "${FORA_DE_PROTOTIPOS}" ]; then
    echo "ERRO: PRs de colaboradoras vindos de fork devem alterar somente prototipos/."
    echo "Arquivos fora de prototipos/:"
    printf '%s\n' "${FORA_DE_PROTOTIPOS}" | sed 's/^/- /'
    echo
    echo "Para alterar arquivos principais, combine com o mantenedor."
    exit 1
  fi
fi

CURRENT_BRANCH="$(git branch --show-current)"

if [ "${CURRENT_BRANCH}" = "${BASE_BRANCH}" ] || [ "${CURRENT_BRANCH}" = "master" ]; then
  TIMESTAMP="$(date +'%Y-%m-%d-%H%M')"
  NEW_BRANCH="tela-${GITHUB_USER}-${TIMESTAMP}"
  echo "==> Voce estava na branch ${CURRENT_BRANCH}. Criando ${NEW_BRANCH}..."
  git checkout -b "${NEW_BRANCH}"
  CURRENT_BRANCH="${NEW_BRANCH}"
fi

if [ "${CURRENT_BRANCH}" = "${BASE_BRANCH}" ]; then
  echo "ERRO: nao envie alteracoes direto para main."
  exit 1
fi

echo
echo "Arquivos alterados:"
git status --short
echo

if [ "${IS_MAINTAINER}" = true ]; then
  read -r -p "Mensagem curta do commit: " COMMIT_MESSAGE
  COMMIT_MESSAGE="${COMMIT_MESSAGE:-Ajusta tela no Codespaces}"
else
  PRIMEIRO_PROTO="$(printf '%s\n' "${CHANGED_FILES}" | awk -F/ '/^prototipos\/colaboradores\/[^/]+\/[^/]+\// { print $4; exit }')"
  PRIMEIRO_PROTO="${PRIMEIRO_PROTO:-novo-prototipo}"
  read -r -p "Nome do prototipo para o PR [${PRIMEIRO_PROTO}]: " PROTOTIPO_NOME
  PROTOTIPO_NOME="${PROTOTIPO_NOME:-${PRIMEIRO_PROTO}}"
  COMMIT_MESSAGE="Prototipo: ${PROTOTIPO_NOME}"
fi

git add .

if git diff --cached --quiet; then
  echo "Nada foi adicionado ao commit."
  exit 0
fi

git commit -m "${COMMIT_MESSAGE}"
git push -u origin "${CURRENT_BRANCH}"

PR_BODY="$(mktemp)"
if [ "${IS_MAINTAINER}" = true ]; then
  AVISO_PROTO=""
  PASSOS_TESTE='1. Abrir o Codespace ou ambiente local.
2. Rodar a tarefa "Casa da Mulher: iniciar sistema", quando aplicavel.
3. Abrir a porta 5500.
4. Conferir a alteracao.'
  CHECKLIST_ESCOPO="- [ ] A alteracao esta dentro do escopo combinado"
else
  AVISO_PROTO="Este PR contem um prototipo e nao deve ser integrado automaticamente ao sistema principal sem revisao do mantenedor."
  PASSOS_TESTE='1. Abrir o Codespace.
2. Rodar a tarefa "Casa da Mulher: iniciar sistema".
3. Abrir a porta 5500.
4. Abrir prototipos/index.html.
5. Conferir o prototipo alterado.'
  CHECKLIST_ESCOPO="- [ ] A alteracao esta em prototipos/"
fi

cat > "${PR_BODY}" <<EOF
## O que foi feito?

${COMMIT_MESSAGE}

${AVISO_PROTO}

## Como testar?

${PASSOS_TESTE}

## Checklist

- [ ] Testei no Codespaces
- [ ] Nao enviei senha/token/appsettings real
${CHECKLIST_ESCOPO}
- [ ] Expliquei como testar
EOF

PR_TITLE="${COMMIT_MESSAGE}"

echo "==> Criando Pull Request para ${BASE_REPO}:${BASE_BRANCH}"
if gh pr create \
  --repo "${BASE_REPO}" \
  --base "${BASE_BRANCH}" \
  --head "${GITHUB_USER}:${CURRENT_BRANCH}" \
  --title "${PR_TITLE}" \
  --body-file "${PR_BODY}"; then
  rm -f "${PR_BODY}"
  echo "==> Pull Request criado."
  exit 0
fi

rm -f "${PR_BODY}"

echo
echo "Nao foi possivel criar o Pull Request automaticamente."
echo "Tentando abrir a tela do GitHub..."
gh pr create \
  --repo "${BASE_REPO}" \
  --base "${BASE_BRANCH}" \
  --head "${GITHUB_USER}:${CURRENT_BRANCH}" \
  --web || true

echo
echo "Se a tela nao abrir, crie manualmente:"
echo "- base repository: ${BASE_REPO}"
echo "- base branch: ${BASE_BRANCH}"
echo "- compare branch: ${GITHUB_USER}:${CURRENT_BRANCH}"
