# Equipe do projeto e convites EQP

Este documento descreve o fluxo de contas da equipe do projeto/faculdade no Sistema Casa da Mulher.

## O que e o ID EQP

IDs no formato `EQP-000001`, `EQP-000002` e assim por diante identificam integrantes da equipe de desenvolvimento/teste.

Eles nao representam funcionarias reais da Casa da Mulher. A conta EQP existe para apoiar desenvolvimento, homologacao, revisao de telas, testes de fluxo e organizacao do projeto.

Cada EQP ativado pelo portal tambem recebe um ADM pareado para o ambiente de desenvolvimento/homologacao.

## Fonte central

A fonte privada versionada fica em:

```text
Sistema-Casa-da-Mulher/ACESSO-EQUIPE
data/equipe-db.json
```

O portal central le e grava esse JSON via GitHub API. O repositorio principal guarda apenas codigo, docs e exemplo seguro.

## IDs iniciais

- `EQP-000001` fica reservado para `Kuuhaku-Allan`.
- `EQP-000001` usa o ADM existente `ADM-000003`.
- Convites iniciais disponiveis:
  - `EQP-000002` + `ADM-000004`
  - `EQP-000003` + `ADM-000005`
  - `EQP-000004` + `ADM-000006`
  - `EQP-000005` + `ADM-000007`
- O proximo lote comeca em `EQP-000006` + `ADM-000008`.

## Ativar conta EQP

A integrante deve:

1. Abrir o portal central do Render.
2. Entrar com GitHub.
3. Escolher um convite disponivel ou reservado para seu GitHub.
4. Informar nome e senha criada somente para este projeto.
5. Confirmar a ativacao.
6. Abrir seu ambiente local ou Codespaces.
7. Aguardar a sincronização automática da API.
8. Fazer login com EQP ou ADM pareado.

Depois da ativacao, o convite vira `usado` e nao pode ser reutilizado.

## Sincronizar no ambiente local

A API sincroniza na inicialização e repete a atualização a cada minuto.

Para forçar a sincronização no Windows:

```powershell
.\casa_da_mulher.cmd equipe sync
```

Para forçar a sincronização no Codespaces:

```text
Casa da Mulher: sincronizar equipe
```

A sincronização:

- le `data/equipe-db.json` do `ACESSO-EQUIPE`;
- cria/atualiza usuarios ASP.NET Identity no banco local;
- vincula EQP e ADM ao mesmo usuario;
- importa `passwordHash`, `securityStamp` e `concurrencyStamp`;
- permite login com EQP ou ADM pareado.

## Redefinir a propria senha

No portal central:

1. Entre com GitHub.
2. Use a opcao `Ja ativei, quero redefinir minha senha`.
3. Informe a nova senha do projeto.
4. Sincronize novamente no ambiente local/Codespaces.

Regras:

- usuario comum so redefine a propria senha;
- owner nao precisa enviar codigo por e-mail;
- senha nunca e salva em texto puro;
- o JSON privado guarda apenas hash compativel com ASP.NET Identity.

## Owner e convites

O owner GitHub configurado pode criar convites pelo portal:

- `POST /api/portal-eqp/admin/criar-convite`
- `POST /api/portal-eqp/admin/criar-lote`
- `GET /api/portal-eqp/admin/db`

O owner tambem pode manter uma allowlist em `equipe-db.json`:

```json
"allowlistGitHub": [
  "Kuuhaku-Allan"
]
```

## Endpoints do portal

```text
GET  /api/portal-eqp/status
GET  /api/portal-eqp/github/login
GET  /api/portal-eqp/github/callback
GET  /api/portal-eqp/me
GET  /api/portal-eqp/convites-disponiveis
POST /api/portal-eqp/ativar
POST /api/portal-eqp/redefinir-minha-senha
POST /api/portal-eqp/admin/criar-convite
POST /api/portal-eqp/admin/criar-lote
GET  /api/portal-eqp/admin/db
POST /api/equipe/sincronizar-github-db
```

## Separacao do institucional

EQP nao deve aparecer como funcionaria real.

O back-end filtra contas, convites e logs de equipe nestas areas:

- Gerenciar Funcionarios;
- Convites de funcionarios;
- Historico/Auditoria institucional;
- Logs de e-mail institucionais.

Eventos de equipe ficam em logs proprios, acessados pela Area da Equipe e pelo `equipe-events.ndjson` privado.

## Seguranca

- Nao use senha pessoal sua ou de outro servico.
- Nao publique senha, token, Client Secret ou hash.
- Nao publique dados reais da Casa da Mulher.
- Nao exponha `GITHUB_EQP_WRITE_TOKEN` no front-end.
- Cada GitHub pode ativar apenas um EQP.
- Convite usado nao volta a disponivel sem acao do owner.
