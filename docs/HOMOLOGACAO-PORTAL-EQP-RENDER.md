# Homologacao do Portal EQP no Render

Este portal e somente para equipe de desenvolvimento/homologacao. Ele nao hospeda o sistema inteiro e nao usa Neon/PostgreSQL como fonte da equipe. O estado dos convites e membros fica no repositorio privado `Sistema-Casa-da-Mulher/ACESSO-EQUIPE`, em `data/equipe-db.json`.

## O que o Render hospeda

- API ASP.NET Core em modo `Staging`.
- Tela `/equipe.html`.
- Tela `/equipe-ativar.html`.
- Endpoints `/api/portal-eqp/*`.

O portal usa OAuth GitHub para identificar a pessoa e usa a API Contents do GitHub para ler/gravar o JSON privado. O token de escrita fica somente em variavel de ambiente do Render.

## Criar o Web Service com Docker

O repositorio possui um `Dockerfile` na raiz. No Render, use Docker e deixe:

```text
Root Directory:
(vazio)

Dockerfile Path:
Dockerfile
```

O Dockerfile publica:

```text
CasaMulher.Api/CasaMulher.Api.csproj
```

e inicia:

```text
dotnet CasaMulher.Api.dll --urls http://0.0.0.0:${PORT}
```

O nome do projeto e do `.dll` batem com o projeto real: `CasaMulher.Api.csproj` gera `CasaMulher.Api.dll`.

## Alternativa sem Docker

1. No Render, crie `New > Web Service`.
2. Conecte o repositorio `Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER`.
3. Use a branch desejada, normalmente `main` depois do merge.
4. Se optar por runtime nativo em vez de Docker, configure:

```bash
Build Command:
dotnet publish CasaMulher.Api/CasaMulher.Api.csproj -c Release -o out

Start Command:
dotnet out/CasaMulher.Api.dll --urls http://0.0.0.0:$PORT
```

O Render exige que o servico escute em `0.0.0.0` e recomenda usar a variavel `PORT`.

## Variaveis de ambiente

Configure no Render:

```text
ASPNETCORE_ENVIRONMENT=Staging
DOTNET_ENVIRONMENT=Staging
PORTAL_EQP_BASE_URL=https://SEU-SERVICE.onrender.com

GITHUB_OAUTH_CLIENT_ID=...
GITHUB_OAUTH_CLIENT_SECRET=...
GITHUB_ORG=Sistema-Casa-da-Mulher
GITHUB_OWNER_LOGIN=Kuuhaku-Allan

GITHUB_EQP_DB_REPO_OWNER=Sistema-Casa-da-Mulher
GITHUB_EQP_DB_REPO=ACESSO-EQUIPE
GITHUB_EQP_DB_PATH=data/equipe-db.json
GITHUB_EQP_EVENTS_PATH=data/equipe-events.ndjson
GITHUB_EQP_WRITE_TOKEN=...
```

Opcional, se quiser permitir leitura com token separado:

```text
GITHUB_EQP_READ_TOKEN=...
```

Nao configure connection string de producao. O `Staging` usa SQLite local apenas para a API subir e para importacoes de homologacao; a fonte da equipe continua sendo o JSON privado.

## OAuth GitHub

Crie um OAuth App no GitHub e configure:

```text
Homepage URL:
https://SEU-SERVICE.onrender.com

Authorization callback URL:
https://SEU-SERVICE.onrender.com/api/portal-eqp/github/callback
```

O `Client Secret` nunca deve aparecer no front-end nem em commits.

## Token do ACESSO-EQUIPE

Use um token fine-grained com acesso somente ao repositorio privado `ACESSO-EQUIPE` e permissao:

```text
Contents: Read and write
```

Esse token e usado pelo back-end para atualizar `data/equipe-db.json` e `data/equipe-events.ndjson`.

## Fluxo de uso

1. Pessoa abre o link do Render.
2. Entra com GitHub.
3. Escolhe um EQP disponivel ou reservado para ela.
4. Informa nome e senha do projeto.
5. Portal grava o hash compatvel com ASP.NET Identity no JSON privado.
6. Pessoa abre o ambiente local ou Codespaces.
7. A API sincroniza automaticamente na inicialização e a cada minuto.

Para forçar uma atualização imediata no Windows:

```powershell
.\casa_da_mulher.cmd equipe sync
```

No Codespaces, use a task manual:

```text
Casa da Mulher: sincronizar equipe
```

Depois disso, o login normal aceita tanto o EQP quanto o ADM pareado.

## Validacao rapida

```bash
dotnet build CasaMulher.Api/CasaMulher.Api.csproj --configuration Release --no-restore
node --check projetocasadamulher/telas/portal-eqp.js
node scripts/validar-html-inline.mjs
git diff --check
```

Em Windows, use o Git Bash explicitamente para validar scripts:

```powershell
& "C:\Program Files\Git\bin\bash.exe" -n scripts/codespaces/sincronizar-equipe.sh
& "C:\Program Files\Git\bin\bash.exe" -n scripts/codespaces/start.sh
& "C:\Program Files\Git\bin\bash.exe" -n scripts/codespaces/abrir-equipe.sh
```

## Regras de seguranca

- Nao salvar senha em texto puro.
- Nao expor `passwordHash` no front-end.
- Nao expor `GITHUB_EQP_WRITE_TOKEN`.
- Nao expor `GITHUB_OAUTH_CLIENT_SECRET`.
- Cada GitHub pode ativar apenas um EQP.
- Pessoa comum so redefine a propria senha.
- Convite usado nao volta a disponivel sem acao do owner.
- Nao usar dados reais da Casa da Mulher.

Referencias:

- GitHub Contents API: https://docs.github.com/en/rest/repos/contents
- Render Web Services: https://render.com/docs/web-services
- Render Environment Variables: https://render.com/docs/configure-environment-variables
