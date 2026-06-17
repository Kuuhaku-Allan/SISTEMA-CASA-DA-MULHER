# Comece aqui - Equipe do Projeto

Este é o ponto de entrada para quem vai ajudar no Sistema Casa da Mulher.

O projeto tem dois mundos separados:

- Sistema institucional: funcionárias reais, recepção, coordenação, histórico e logs.
- Equipe do projeto: contas `EQP`, testes, Codespaces, protótipos, Pull Requests e organização do desenvolvimento.

Contas `EQP` não representam funcionárias reais da Casa da Mulher.

## Entrada única

Use sempre a Área da Equipe.

No ambiente local do mantenedor:

```powershell
.\casa_da_mulher.cmd equipe
```

Esse comando inicia API, banco e front, depois abre:

```text
http://localhost:5500/equipe.html
```

No Codespaces, rode a task:

```text
Casa da Mulher: abrir área da equipe
```

Se a porta não abrir sozinha, abra a aba `Ports/Portas`, clique na porta `5500` e acesse `/equipe.html`.

## Allan ou mantenedor

Fluxo recomendado:

1. Abra o projeto na sua IDE.
2. Execute `.\casa_da_mulher.cmd equipe`.
3. A Área da Equipe abrirá automaticamente.
4. Se ainda não tiver EQP, execute `.\casa_da_mulher.cmd equipe bootstrap`.
5. Use o `EQP-000001` e o código gerado para ativar sua conta.

O membro `EQP-000001` é tratado como owner da equipe:

- não precisa de fork;
- não precisa de Codespaces;
- usa fluxo `local_owner`;
- pode gerenciar convites, membros e logs da equipe.

A conta `ADM-000003` continua sendo super admin institucional/de desenvolvimento.

## Colaboradora que já tem fork

1. Abra seu fork no GitHub.
2. Abra Codespaces no seu fork.
3. Rode `Casa da Mulher: iniciar sistema`.
4. Rode `Casa da Mulher: abrir área da equipe`.
5. Clique em `Ativar meu EQP`.
6. Use o ID e o código enviados pelo mantenedor.
7. Trabalhe em `prototipos/`.
8. Envie Pull Request.

O Pull Request deve ir para:

```text
Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER
```

## Colaboradora que ainda não tem fork

1. Entre no repositório principal:
   `https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER`
2. Clique em `Fork`.
3. Crie o fork na sua conta.
4. Abra Codespaces no seu fork.
5. Rode `Casa da Mulher: iniciar sistema`.
6. Rode `Casa da Mulher: abrir área da equipe`.
7. Clique em `Ativar meu EQP`.
8. Crie protótipos e envie PR.

Se o painel mostrar `Precisa criar fork`, volte para este passo.

## Como ativar meu EQP

Quem gerencia a equipe envia individualmente:

- ID `EQP-000000`;
- código de ativação;
- orientação de fork/Codespaces, quando necessário.

Depois:

1. Abra a Área da Equipe.
2. Clique em `Ativar meu EQP`.
3. Informe ID, código, nome e senha.
4. Faça login com o ID `EQP` e a senha criada.

Nunca compartilhe seu código de ativação.

## Criar protótipo

Este é o fluxo recomendado para colaboradoras:

1. Crie um fork.
2. Abra Codespaces no fork.
3. Rode `Casa da Mulher: iniciar sistema`.
4. Rode `Casa da Mulher: criar novo protótipo`.
5. Edite apenas a pasta criada em `prototipos/colaboradores/seu-usuario/`.
6. Abra `prototipos/index.html`.
7. Rode `Casa da Mulher: enviar Pull Request`.
8. Mande o link do PR.

PRs vindos de fork são bloqueados automaticamente se alterarem arquivos fora de `prototipos/`.

## Guias úteis

- Contas e convites EQP: `docs/EQUIPE-E-CONVITES.md`
- Codespaces: `docs/CODESPACES-PARA-COLABORADORAS.md`
- Pull Request: `docs/GUIA-RAPIDO-PR.md`
- Matriz de permissões: `docs/MATRIZ-PERMISSOES-EQP-ADM.md`
- Repositório privado da equipe: `docs/ACESSO-EQUIPE-PRIVADO.md`

## Banco local e URL permanente

Sem API central hospedada, não existe uma URL única permanente para a tela real de ativação.

Cada ambiente local/Codespaces tem:

- sua própria URL encaminhada;
- seu próprio banco local;
- sua própria API rodando.

Por isso, o link permanente da equipe é o repositório privado `ACESSO-EQUIPE`, e a Área da Equipe abre a tela correta dentro do ambiente de cada pessoa.

## Deu erro

Verifique nesta ordem:

1. Você está no seu fork, se for colaboradora?
2. A task `Casa da Mulher: detectar fluxo` roda sem erro?
3. A API abriu na porta `5001`?
4. O front abriu na porta `5500`?
5. Você acessou `/equipe.html`?
6. O arquivo alterado fica dentro de `prototipos/`, se o PR veio de fork?
7. Nenhum token, senha, banco local ou dado real foi enviado?

Se ainda falhar, copie a mensagem de erro e mande junto com o link do Codespace ou Pull Request.
