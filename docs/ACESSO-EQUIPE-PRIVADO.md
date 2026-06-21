# Ponto de entrada privado da equipe

O repositorio principal pode ser publico, mas o controle real da equipe deve ficar no repositorio privado da organizacao.

Repositorio privado definido:

```text
Sistema-Casa-da-Mulher/ACESSO-EQUIPE
```

Esse repositorio privado e o ponto fixo da equipe e tambem guarda a fonte versionada dos convites/membros EQP.

## O que fica no ACESSO-EQUIPE

```text
data/equipe-db.json
data/equipe-events.ndjson
data/access-requests.json
data/equipe-db.example.json
data/README.md
README.md
```

- `data/equipe-db.json`: fonte de verdade dos convites EQP, ADMs pareados e membros ativados.
- `data/equipe-events.ndjson`: log de eventos do portal.
- `data/access-requests.json`: solicitações GitHub pendentes e decisões do owner.
- `README.md`: instrucoes para a equipe e link do portal Render quando estiver publicado.

## Portal central

O portal central roda no Render e hospeda somente a ativacao EQP:

- `/equipe.html`
- `/equipe-ativar.html`
- `/api/portal-eqp/*`

Ele nao hospeda o sistema inteiro e nao usa Neon/PostgreSQL como fonte da equipe. O estado fica no JSON privado do `ACESSO-EQUIPE`.

## Fluxo correto

1. Pessoa abre o link do portal Render.
2. Entra com GitHub.
3. Ativa um EQP disponivel ou reservado.
4. Recebe o ADM pareado automaticamente.
5. Abre o ambiente local ou Codespaces.
6. A API sincroniza a equipe automaticamente.
7. Faz login com EQP ou ADM pareado.

Para forçar a sincronização no Windows:

```powershell
.\casa_da_mulher.cmd equipe sync
```

Para baixar ou publicar especificamente a base privada (isso não faz parte de `hml push`):

```powershell
.\casa_da_mulher.cmd equipe pull
.\casa_da_mulher.cmd equipe push
```

O `equipe push` mescla a allowlist local no arquivo remoto e preserva membros, convites e configurações existentes.

Para forçar a sincronização no Codespaces:

```text
Casa da Mulher: sincronizar equipe
```

## Seguranca

Nao publicar no repositorio principal:

- senhas;
- tokens;
- hashes reais;
- `ClientSecret`;
- PAT GitHub;
- `appsettings` real;
- banco local;
- dados reais de atendimento.

O token `GITHUB_EQP_WRITE_TOKEN` deve existir somente no back-end do portal Render.
