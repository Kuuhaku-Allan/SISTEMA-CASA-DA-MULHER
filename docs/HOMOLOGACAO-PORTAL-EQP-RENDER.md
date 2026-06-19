# Homologacao do Portal EQP no Render

Este ambiente e somente para equipe de desenvolvimento/homologacao. O mesmo Web Service hospeda a API ASP.NET Core e todo o front em `projetocasadamulher/telas`. Ele nao usa Neon/PostgreSQL: o banco SQLite e temporario (filesystem efemero do Render Free) e a equipe e reconstruida a partir do repositorio privado `Sistema-Casa-da-Mulher/ACESSO-EQUIPE`.

> **Render Free usa filesystem efemero.** O SQLite em `/tmp` e apagado em redeploy/restart. Use o snapshot criptografado para persistir 2FA, passkeys e dados de homologacao (veja secao Persistencia abaixo).

## O que o Render hospeda

- API ASP.NET Core em modo `Staging`.
- Todas as telas do sistema, incluindo `/equipe.html` e `/index.html`.
- Todos os endpoints `/api/*` no mesmo dominio.
- SQLite temporario de homologacao.
- GitHub Gate antes das telas e APIs sensiveis.

O portal usa OAuth GitHub para identificar a pessoa e usa a API Contents do GitHub para ler/gravar o JSON privado. O token de escrita fica somente em variavel de ambiente do Render.

O GitHub Gate nao substitui o login normal. Primeiro a pessoa comprova que pertence a equipe pelo GitHub; depois entra no sistema com EQP ou ADM.

EQP e ADM pareado sao aliases do mesmo usuario e compartilham seguranca. O sync preserva e-mail/recuperacao reais, 2FA, passkeys, telefone e bloqueio existentes. `@equipe.local` e somente fallback tecnico para conta nova sem e-mail e nunca substitui um endereco real.

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
ENABLE_RENDER_GITHUB_GATE=true
PORTAL_EQP_BASE_URL=https://SEU-SERVICE.onrender.com
SQLITE_DB_PATH=/tmp/casa_mulher_hml.db

GITHUB_OAUTH_CLIENT_ID=...
GITHUB_OAUTH_CLIENT_SECRET=...
GITHUB_ORG=Sistema-Casa-da-Mulher
GITHUB_OWNER_LOGIN=Kuuhaku-Allan

GITHUB_EQP_DB_REPO_OWNER=Sistema-Casa-da-Mulher
GITHUB_EQP_DB_REPO=ACESSO-EQUIPE
GITHUB_EQP_DB_PATH=data/equipe-db.json
GITHUB_EQP_EVENTS_PATH=data/equipe-events.ndjson
GITHUB_EQP_WRITE_TOKEN=...

Jwt__Key=CHAVE_FORTE_E_EXCLUSIVA
Convites__HashSecret=CHAVE_FORTE_E_EXCLUSIVA
```

Opcional, se quiser permitir leitura com token separado:

```text
GITHUB_EQP_READ_TOKEN=...
```

### Variaveis adicionais para Passkeys e Persistencia

As variaveis abaixo ja tem valores padrao corretos para o dominio `casa-mulher-eqp.onrender.com`, mas podem ser sobrescritas:

```text
# WebAuthn / Passkeys — RP ID deve ser exatamente o dominio, sem https:// nem porta
WEBAUTHN_RP_ID=casa-mulher-eqp.onrender.com
WEBAUTHN_RP_NAME=Sistema Casa da Mulher - Homologacao
WEBAUTHN_ORIGINS=https://casa-mulher-eqp.onrender.com

# Snapshot criptografado do banco (opcional, gratuito, necessario para persistir 2FA/passkeys)
HML_DB_SNAPSHOT_ENABLED=true
HML_DB_SNAPSHOT_KEY=<base64 de 32 bytes gerado fora do repositorio>
HML_DB_SNAPSHOT_REPO_OWNER=Sistema-Casa-da-Mulher
HML_DB_SNAPSHOT_REPO=ACESSO-EQUIPE
HML_DB_SNAPSHOT_PATH=data/render-hml-db/latest.sqlite.gz.enc
```

Nao configure connection string de producao. O `Staging` aplica migrations automaticamente no SQLite temporario e inicia a sincronizacao da equipe. Se o disco efemero for apagado, o banco e recriado no proximo startup.

Os nomes OAuth documentados sao exatamente `GITHUB_OAUTH_CLIENT_ID` e `GITHUB_OAUTH_CLIENT_SECRET`.

## GitHub Gate

Sem sessao GitHub autorizada:

- `/equipe.html`, `/equipe-ativar.html` e os endpoints de OAuth permanecem publicos;
- paginas sensiveis redirecionam para a Area da Equipe;
- APIs sensiveis retornam `401` em JSON antes da autenticacao Identity.

A autorizacao aceita owner, `allowlistGitHub`, membro ativo ou membership confirmada da organizacao. Falha ao consultar a organizacao nunca libera acesso. O cookie e `HttpOnly`, `Secure` em HTTPS e `SameSite=Lax`; ele contem somente um ID aleatorio protegido. O token OAuth permanece na memoria do servidor.

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
node scripts/validar-p1-final.js
node --no-warnings scripts/validar-preservacao-seguranca-eqp.mjs
node scripts/validar-render-gate.mjs
git diff --check
```

Em Windows, use o Git Bash explicitamente para validar scripts:

```powershell
& "C:\Program Files\Git\bin\bash.exe" -n scripts/codespaces/sincronizar-equipe.sh
& "C:\Program Files\Git\bin\bash.exe" -n scripts/codespaces/start.sh
& "C:\Program Files\Git\bin\bash.exe" -n scripts/codespaces/abrir-equipe.sh
```

## Passkeys e WebAuthn por dominio

Passkeys sao vinculadas ao dominio (RP ID) em que foram criadas. O RP ID no Render e `casa-mulher-eqp.onrender.com`. Passkeys criadas em `localhost` **nao funcionam** no Render e vice-versa. Isso e comportamento definido pelo padrao WebAuthn (W3C), nao um bug.

- O RP ID nunca deve conter `https://` nem porta.
- Para registrar uma passkey no Render, entre primeiro com ID e senha e vá em Seguranca da Conta.
- Cada ambiente (local, Codespaces, Render) tem sua propria lista de passkeys.
- A mensagem de erro `The relying party ID is not a registrable domain suffix` indica passkey de outro ambiente.

## Persistencia do banco no Render

O Render Free usa filesystem efemero: dados em `/tmp` sao perdidos em redeploys e reinicializacoes. Para manter 2FA, passkeys e dados entre reinicializacoes, use o snapshot criptografado:

1. Configure `HML_DB_SNAPSHOT_ENABLED=true` e `HML_DB_SNAPSHOT_KEY` no Render.
2. A chave deve ser Base64 de exatamente 32 bytes gerado com `openssl rand -base64 32` ou equivalente.
3. **Nunca commitar a chave.** Ela fica somente em variavel de ambiente.
4. Apos configurar 2FA/passkeys, clique em "Gerar snapshot seguro agora" na tela Seguranca.
5. No proximo startup, a API restaura o banco automaticamente antes das migrations.

A tela Seguranca mostra aviso se o banco for temporario. O botao de snapshot aparece somente para owner quando snapshot esta configurado.

## Historico institucional x logs da equipe

Eventos de equipe (EQP, sync, portal-eqp) ficam somente nos logs da equipe. O Historico institucional mostra apenas eventos de funcionarios institucionais reais. A separacao e automatica: qualquer evento com identificador `EQP-`, rota `/api/portal-eqp/` ou `/api/equipe/` e classificado como escopo Equipe.

## Dados de homologacao

O banco do Render nao tem dados ficticios automaticamente no primeiro acesso. Para popular as telas:

1. Configure o seed no ACESSO-EQUIPE em `data/homologacao-seed.json` (veja formato abaixo).
2. Na primeira inicializacao sem snapshot, a API aplica o seed se `HML_SEED_ENABLED=true` (padrao em Staging).
3. Para exportar dados locais sanitizados: `node scripts/exportar-homologacao-seed.mjs`

O seed nao exporta: senhas, hashes, SecurityStamp, tokens, passkeys, 2FA nem dados reais.

## Regras de seguranca

- Nao salvar senha em texto puro.
- Nao expor `passwordHash` no front-end.
- Nao expor `GITHUB_EQP_WRITE_TOKEN`.
- Nao expor `GITHUB_OAUTH_CLIENT_SECRET`.
- Nao guardar o token OAuth no cookie do navegador.
- Nao deixar APIs institucionais acessiveis antes do GitHub Gate em Staging.
- Cada GitHub pode ativar apenas um EQP.
- Pessoa comum so redefine a propria senha.
- Convite usado nao volta a disponivel sem acao do owner.
- Nao usar dados reais da Casa da Mulher.
- Nao commitar `HML_DB_SNAPSHOT_KEY` nem banco SQLite puro no repositorio.
- Nao salvar segredo TOTP puro em JSON.

Referencias:

- W3C WebAuthn spec: https://www.w3.org/TR/webauthn-3/
- Render Persistent Disks: https://render.com/docs/disks
- GitHub Contents API: https://docs.github.com/en/rest/repos/contents
- Render Web Services: https://render.com/docs/web-services
- Render Environment Variables: https://render.com/docs/configure-environment-variables
