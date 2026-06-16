#!/usr/bin/env bash
set -euo pipefail

BASE_REPO="Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER"
BASE_BRANCH="main"

echo "==> Preparando envio para revisao"

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

if git diff --quiet && git diff --cached --quiet; then
  echo "Nada para enviar. Faca uma alteracao antes de abrir Pull Request."
  exit 0
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

read -r -p "Mensagem curta do commit: " COMMIT_MESSAGE
COMMIT_MESSAGE="${COMMIT_MESSAGE:-Ajusta tela no Codespaces}"

git add .

if git diff --cached --quiet; then
  echo "Nada foi adicionado ao commit."
  exit 0
fi

git commit -m "${COMMIT_MESSAGE}"
git push -u origin "${CURRENT_BRANCH}"

PR_BODY="$(mktemp)"
cat > "${PR_BODY}" <<EOF
## O que foi feito?

${COMMIT_MESSAGE}

## Como testar?

1. Abrir o Codespace.
2. Rodar a tarefa "Casa da Mulher: iniciar sistema".
3. Abrir a porta 5500.
4. Conferir a tela alterada.

## Checklist

- [ ] Testei no Codespaces
- [ ] Nao enviei senha/token/appsettings real
- [ ] A alteracao e de tela/prototipo
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
