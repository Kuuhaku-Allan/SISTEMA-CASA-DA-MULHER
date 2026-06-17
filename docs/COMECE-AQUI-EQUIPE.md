# Comece aqui - Equipe do Projeto

Este e o ponto de entrada para quem vai ajudar no Sistema Casa da Mulher.

## Link permanente da equipe

Quando o repositorio privado abaixo estiver disponivel, ele sera o ponto fixo de entrada da equipe:

```text
https://github.com/Sistema-Casa-da-Mulher/ACESSO-EQUIPE
```

A tela `equipe-ativar.html` deve ser aberta dentro do ambiente local ou Codespaces de cada pessoa. Sem API/banco central sempre ligado, ela nao tem uma URL unica permanente.

Se a pessoa nao sabe por onde comecar, deve abrir o repositorio privado `ACESSO-EQUIPE` e a issue fixada `COMECE AQUI - Acesso da equipe`.

O projeto tem dois mundos separados:

- Sistema institucional: funcionarios reais, recepcao, coordenacao, historico e logs de e-mail.
- Equipe do projeto: contas `EQP`, testes, Codespaces, Pull Requests e organizacao do desenvolvimento.

Contas `EQP` nao representam funcionarias reais da Casa da Mulher.

## Sou Allan ou mantenedor e uso IDE local

Use o repositorio principal normalmente na sua IDE local.

Fluxo esperado:

1. Trabalhe em uma branch propria.
2. Rode a API localmente.
3. Rode as telas locais.
4. Faca commit e abra Pull Request quando quiser revisao.

O membro `EQP-000001` e tratado como owner da equipe:

- nao precisa de fork;
- nao precisa de Codespaces;
- usa fluxo `local_owner`;
- pode gerenciar convites, membros e logs da equipe.

A conta `ADM-000003` continua sendo super admin institucional/de desenvolvimento.

## Ja tenho fork

1. Abra o seu fork no GitHub.
2. Clique em `Code`.
3. Clique em `Codespaces`.
4. Crie ou abra o Codespace no seu fork.
5. Rode a tarefa `Casa da Mulher: iniciar sistema`.
6. Acesse a porta `5500`.
7. Trabalhe em uma branch.
8. Rode a tarefa `Casa da Mulher: enviar Pull Request`.

O Pull Request deve ir para:

```text
Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER
```

## Ainda nao tenho fork

1. Entre no repositorio principal:
   `https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER`
2. Clique em `Fork`.
3. Crie o fork na sua conta.
4. Depois abra o Codespace no seu fork, nao no repositorio principal.

Se o painel mostrar `Precisa criar fork`, volte para este passo.

## Quero ativar meu EQP

Quem gerencia a equipe vai te enviar:

- ID `EQP-000000`;
- codigo de ativacao;
- link da tela de ativacao.

Abra:

```text
projetocasadamulher/telas/equipe-ativar.html
```

Informe o ID, o codigo, seu nome e crie uma senha.

Depois disso, entre pela tela normal de login usando o ID `EQP` e a senha criada.

## Quero abrir Codespaces

Veja o guia completo:

```text
docs/CODESPACES-PARA-COLABORADORAS.md
```

Resumo:

1. Abra seu fork.
2. Crie um Codespace.
3. Rode `Casa da Mulher: preparar ambiente` se for a primeira vez.
4. Rode `Casa da Mulher: iniciar sistema`.
5. Abra a porta `5500`.

## Quero mandar Pull Request

Veja:

```text
docs/GUIA-RAPIDO-PR.md
```

No Codespaces, a forma mais simples e rodar a tarefa:

```text
Casa da Mulher: enviar Pull Request
```

Ela cria branch quando necessario, faz commit, envia para o seu fork e tenta abrir o PR contra o repositorio principal.

## Quero criar um prototipo

Este e o fluxo recomendado para colaboradoras:

1. Crie um fork.
2. Abra Codespaces no fork.
3. Rode `Casa da Mulher: iniciar sistema`, se precisar testar.
4. Rode `Casa da Mulher: criar novo prototipo`.
5. Edite apenas a pasta criada em `prototipos/colaboradores/seu-usuario/`.
6. Abra `prototipos/index.html`.
7. Rode `Casa da Mulher: enviar Pull Request`.
8. Mande o link do PR.

PRs vindos de fork sao bloqueados automaticamente se alterarem arquivos fora de `prototipos/`.

O mantenedor revisa o prototipo e decide depois se alguma parte sera integrada em `projetocasadamulher/telas/`.

## Quero validar permissoes EQP/ADM

Consulte:

```text
docs/MATRIZ-PERMISSOES-EQP-ADM.md
```

Com a API local rodando, use a tarefa:

```text
Casa da Mulher: validar regras EQP/ADM
```

## Banco local e atividade central

Em Codespaces, cada pessoa tem seu proprio banco local de desenvolvimento.

Isso significa:

- ativar um `EQP` no seu Codespace vale para aquele ambiente;
- outro Codespace nao ve automaticamente esse mesmo banco;
- isso e normal para desenvolvimento;
- a atividade central do projeto vem do GitHub: Pull Requests, branches, commits e revisoes.

Para ter `EQP` centralizado para todos no futuro, sera necessario um ambiente de homologacao com API e banco compartilhados.

Antes da entrega final para a Casa da Mulher, o banco de teste deve ser limpo e recriado com dados corretos.

## Deu erro, o que fazer?

Verifique nesta ordem:

1. Voce esta no seu fork?
2. Voce esta logada no GitHub?
3. A tarefa `Casa da Mulher: detectar fluxo` roda sem erro?
4. A API abriu na porta `5001`?
5. As telas abriram na porta `5500`?
6. O arquivo alterado fica dentro de `projetocasadamulher/telas/`?
7. Nao foi enviado token, senha, banco local ou arquivo sensivel?

Se ainda falhar, copie a mensagem de erro e mande junto com o link do Codespace ou Pull Request.

## GitHub e login

Estar logada no GitHub para abrir Codespaces nao faz a aplicacao saber automaticamente quem voce e.

Login automatico real com GitHub precisa de OAuth/GitHub App. Nesta etapa, o sistema esta preparado com campos e configuracoes para esse futuro, mas o login por ID/senha continua sendo o fluxo principal e seguro.
