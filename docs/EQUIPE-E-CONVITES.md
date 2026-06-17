# Equipe do projeto e convites EQP

Este documento descreve o fluxo de contas da equipe do projeto/faculdade no Sistema Casa da Mulher.

## O que é o ID EQP

IDs no formato `EQP-000001`, `EQP-000002` e assim por diante identificam integrantes da equipe de desenvolvimento/teste.

Eles não representam funcionárias reais da Casa da Mulher. A conta EQP existe para apoiar desenvolvimento, homologação, revisão de telas, testes de fluxo e organização do projeto.

## Entrada correta

Não peça para a pessoa procurar arquivos manualmente.

Use sempre:

```text
Área da Equipe -> Ativar meu EQP
```

No Windows local:

```powershell
.\casa_da_mulher.cmd equipe
```

No Codespaces:

```text
Casa da Mulher: abrir área da equipe
```

## Gerar convites iniciais

Para preparar o primeiro acesso do mantenedor e os primeiros convites de colaboradoras:

```powershell
.\casa_da_mulher.cmd equipe bootstrap
```

Esse comando:

- inicia API, banco e front;
- aplica migrations;
- abre a Área da Equipe;
- cria ou atualiza os convites `EQP-000001` a `EQP-000006`;
- reserva `EQP-000001` para Allan/mantenedor;
- imprime os códigos recém-criados ou regenerados no console.

Guarde esses códigos em local privado. Não faça commit deles.

Regras do bootstrap:

- não recria convite que já existe;
- não sobrescreve conta ativada;
- se `EQP-000001` já estiver ativado, mostra isso no console;
- código em texto puro só aparece na criação ou regeneração;
- código cru não é salvo no banco, apenas hash.

## Criar convite pela tela

Abra a Área da Equipe, faça login como owner/super admin e entre em:

```text
Convites da equipe
```

Você pode:

- criar convite individual;
- criar lote;
- criar os convites iniciais;
- regenerar código de convite disponível;
- revogar convite disponível;
- copiar instrução de ativação.

Ao criar ou regenerar, o sistema mostra:

- ID EQP;
- código de ativação em texto puro;
- texto pronto para copiar.

O código em texto puro aparece apenas nesse momento.

## Ativar conta EQP

A integrante deve:

1. Abrir a Área da Equipe.
2. Clicar em `Ativar meu EQP`.
3. Informar ID EQP, código de ativação, nome e senha.
4. Fazer login normalmente com o ID EQP e a senha criada.

Depois da ativação, o convite vira `Usado` e não pode ser reutilizado.

## Primeiro owner da equipe

O convite `EQP-000001` recebe tratamento especial:

- `PapelEquipe = owner`;
- `PrecisaFork = false`;
- `UsaCodespaces = false`;
- `FluxoTrabalho = local_owner`;
- `PodeCriarConvitesEquipe = true`.

Use esse ID para o mantenedor.

## Área autenticada da equipe

A Área da Equipe aponta para:

- Painel da equipe;
- Convites da equipe;
- Membros da equipe;
- Atividade e logs;
- Protótipos;
- Guias da equipe.

Membro comum vê a área da equipe, mas não recebe ações perigosas. Owner/super admin gerenciam membros, papéis, convites e redefinições de senha.

## Protótipos e Pull Requests

Colaboradoras devem criar telas novas primeiro em:

```text
prototipos/
```

PRs vindos de fork são bloqueados se alterarem arquivos fora de `prototipos/`.

## Separação do institucional

EQP não deve aparecer como funcionária real.

O back-end filtra contas, convites e logs de equipe nestas áreas:

- Gerenciar Funcionários;
- Convites de funcionários;
- Histórico/Auditoria institucional;
- Logs de e-mail institucionais.

Eventos de equipe ficam em logs próprios, acessados pela Área da Equipe.

## Redefinição de senha EQP sem e-mail

Convite EQP não usa e-mail nesta fase. Para reset de senha:

1. Owner/super admin abre a Área da Equipe.
2. Entra em `Membros da equipe`.
3. Clica em `Gerar redefinição`.
4. O sistema gera um código temporário de uso único.
5. A integrante abre a Área da Equipe.
6. Clica em `Redefinir senha EQP`.
7. Informa ID EQP, código e nova senha.

Regras:

- código expira;
- código usado não funciona de novo;
- código revogado/expirado é recusado;
- código fica salvo apenas como hash;
- auditoria registra geração e uso.

## GitHub, OAuth e atividade

Estar logado no GitHub para abrir Codespaces não identifica automaticamente o usuário dentro da aplicação.

Login automático real com GitHub precisa de OAuth/GitHub App. Nesta etapa, o login normal por ID/senha continua funcionando e a estrutura para GitHub fica preparada.

Não commitar `ClientSecret`, PAT, senha ou token.

## Banco local em Codespaces

Cada Codespace/fork tem seu próprio banco local de desenvolvimento.

Isso significa:

- ativar um `EQP` no Codespace de uma pessoa não ativa automaticamente em outro Codespace;
- esse comportamento é esperado para desenvolvimento;
- a fonte central de atividade do projeto é o GitHub;
- para um controle central de `EQP`, será necessário ambiente de homologação com API e banco compartilhados.

## Endpoints principais

```text
GET  /api/equipe/convites
POST /api/equipe/convites
POST /api/equipe/convites/lote
POST /api/equipe/convites/bootstrap
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

O endpoint de bootstrap só responde em `Development` ou `Staging`.
