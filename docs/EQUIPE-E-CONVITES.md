# Equipe do projeto e convites EQP

Este documento descreve o fluxo de contas da equipe do projeto/faculdade no Sistema Casa da Mulher.

## O que e o ID EQP

IDs no formato `EQP-000001`, `EQP-000002` e assim por diante identificam integrantes da equipe de desenvolvimento/teste.

Eles nao representam funcionarias reais da Casa da Mulher. A conta EQP existe para apoiar desenvolvimento, homologacao, revisao de telas, testes de fluxo e organizacao do projeto.

Antes da entrega final, o banco de teste deve ser limpo. No futuro, esse modulo pode virar uma area de suporte, erros e atualizacoes.

## Permissao ampla em teste

Nesta fase, contas `equipe` podem acessar areas amplas em ambiente de desenvolvimento/homologacao, incluindo recepcao e telas administrativas.

Essa permissao ampla nao e regra de producao. No `Program.cs`, o perfil `equipe` so entra nas politicas institucionais quando o ambiente e `Development` ou `Staging`.

## Criar convite individual

Entre com uma conta `adm` e abra:

```text
projetocasadamulher/telas/equipe-convites.html
```

Preencha:

- papel na equipe: `contributor`, `maintainer` ou `owner`;
- se a pessoa precisa usar fork no GitHub;
- se a pessoa pode criar convites da equipe;
- observacao opcional.

Ao criar, o sistema mostra:

- ID EQP;
- codigo de ativacao em texto puro;
- texto pronto para copiar.

O codigo em texto puro aparece apenas nessa criacao ou quando for regenerado.

## Criar lote de convites

Na mesma tela, use a secao `Criar lote`.

Exemplo: quantidade `5`, papel `contributor`, `Precisa usar fork` marcado.

Cada convite recebe seu proprio ID EQP e codigo de ativacao.

## Ativar conta EQP

A integrante abre:

```text
projetocasadamulher/telas/equipe-ativar.html
```

Ela informa:

- ID EQP;
- codigo de ativacao;
- nome completo;
- senha;
- confirmacao da senha.

Depois da ativacao, o convite vira `Usado` e nao pode ser reutilizado.

O login passa a ser feito na tela normal usando o ID EQP e a senha criada. Apos login, o sistema redireciona para:

```text
projetocasadamulher/telas/equipe-painel.html
```

## Revogar convite

Convites com status `Disponivel` podem ser revogados em `equipe-convites.html`.

Convite revogado nao pode ser ativado.

## Regenerar codigo

Se o codigo em texto puro foi perdido, use `Regenerar codigo` em um convite ainda `Disponivel`.

O codigo anterior deixa de funcionar. O novo codigo aparece apenas no momento da regeneracao.

## Primeiro owner da equipe

Use uma conta `adm` existente para criar o primeiro convite EQP com:

```text
Papel: owner
Pode criar convites da equipe: marcado
```

Depois ative esse convite normalmente em `equipe-ativar.html`.

Nao ha senha real hardcodada, conta `ADM-DEMO` ou segredo no repositorio.

O convite `EQP-000001` recebe tratamento especial quando for criado:

- `PapelEquipe = owner`;
- `PrecisaFork = false`;
- `UsaCodespaces = false`;
- `FluxoTrabalho = local_owner`;
- `PodeCriarConvitesEquipe = true`.

A conta `ADM-000003` continua sendo super admin institucional/de desenvolvimento.

## Area propria da equipe

A equipe usa telas separadas das areas institucionais:

```text
projetocasadamulher/telas/equipe-painel.html
projetocasadamulher/telas/equipe-membros.html
projetocasadamulher/telas/equipe-convites.html
projetocasadamulher/telas/equipe-atividade.html
projetocasadamulher/telas/equipe-redefinir-senha.html
```

`equipe-membros.html` lista membros EQP, papel, GitHub, fluxo de trabalho, fork, Codespaces e status.

`equipe-atividade.html` mostra Pull Requests do GitHub e logs internos da equipe.

Membro comum ve a area da equipe, mas nao recebe acoes perigosas. Owner/super admin gerenciam membros, papeis e redefinicao de senha.

A matriz completa de permissoes fica em:

```text
docs/MATRIZ-PERMISSOES-EQP-ADM.md
```

Colaboradoras devem criar telas novas primeiro em:

```text
prototipos/
```

PRs vindos de fork sao bloqueados se alterarem arquivos fora de `prototipos/`.

## Separacao do institucional

EQP nao deve aparecer como funcionario real.

O back-end filtra contas, convites e logs de equipe nestas areas:

- Gerenciar Funcionarios;
- Convites de funcionarios;
- Historico/Auditoria institucional;
- Logs de e-mail institucionais.

Eventos de equipe ficam em logs proprios, acessados pela area da equipe.

## Redefinicao de senha EQP sem e-mail

Convite EQP nao usa e-mail nesta fase. Para reset de senha:

1. Owner/super admin abre `equipe-membros.html`.
2. Clica em `Gerar redefinicao`.
3. O sistema gera um codigo temporario de uso unico.
4. O codigo fica salvo apenas como hash.
5. A integrante abre `equipe-redefinir-senha.html`.
6. Informa ID EQP, codigo e nova senha.

Regras:

- codigo expira;
- codigo usado nao funciona de novo;
- codigo revogado/expirado e recusado;
- codigo esta vinculado ao ID EQP correto;
- auditoria de equipe registra geracao e uso.

## GitHub, OAuth e atividade do projeto

Estar logado no GitHub para abrir Codespaces nao identifica automaticamente o usuario dentro da aplicacao.

Login automatico real com GitHub precisa de OAuth/GitHub App. Nesta etapa, o login normal por ID/senha continua funcionando e a estrutura para GitHub fica preparada.

Campos previstos em `EquipeMembro`:

- `GitHubUsername`;
- `GitHubId`;
- `GitHubVinculadoEm`;
- `FluxoTrabalho`;
- `ForkUrl`;
- `UltimaVerificacaoGitHubEm`.

Configuracoes opcionais:

```text
GitHub:ClientId
GitHub:ClientSecret
GitHub:Organization
GitHub:Repository
GitHub:OwnerUsername
GitHub:ReadToken
GITHUB_READ_TOKEN
```

Nao commitar `ClientSecret`, PAT, senha ou token.

`GitHub:ReadToken` ou `GITHUB_READ_TOKEN` deve ser usado apenas no back-end para leitura da API do GitHub. O front-end nunca deve receber esse token.

Sem token ou sem OAuth configurado, a aplicacao continua com login por ID/senha e tenta chamadas publicas/fallback para atividade do GitHub.

## Banco local em Codespaces

Cada Codespace/fork tem seu proprio banco local de desenvolvimento.

Isso significa:

- ativar um `EQP` no Codespace de uma pessoa nao ativa automaticamente em outro Codespace;
- esse comportamento e esperado para desenvolvimento;
- a fonte central de atividade do projeto e o GitHub;
- Pull Requests, commits e revisoes devem ser consultados no GitHub;
- para um controle central de `EQP`, sera necessario um ambiente de homologacao com API e banco compartilhados.

Antes da entrega final para a Casa da Mulher, o banco de teste deve ser limpo e recriado com dados corretos.

## Seguranca implementada

O fluxo atual inclui:

- codigo de ativacao salvo como hash HMAC;
- convite de uso unico;
- bloqueio de convite revogado;
- codigo em texto puro exibido apenas na criacao/regeneracao;
- codigo de redefinicao de senha EQP salvo como hash;
- redefinicao de senha EQP com validade e uso unico;
- validacao no back-end;
- rate limit basico na ativacao publica;
- auditoria para criacao, lote, revogacao, regeneracao, ativacao, criacao de membro, reset de senha e alteracao de papel/fluxo.

## Endpoints principais

```text
GET  /api/equipe/convites
POST /api/equipe/convites
POST /api/equipe/convites/lote
POST /api/equipe/convites/{id}/revogar
POST /api/equipe/convites/{id}/regenerar-codigo
POST /api/equipe/ativar
GET  /api/equipe/membros
PATCH /api/equipe/membros/{id}
POST /api/equipe/membros/{id}/gerar-redefinicao-senha
POST /api/equipe/redefinir-senha
GET  /api/equipe/logs
GET  /api/equipe/github/status
GET  /api/equipe/github/atividade
```

Convites de equipe nao usam e-mail. GitHub OAuth fica preparado para uma etapa futura e nao substitui o login por ID/senha nesta fase.
