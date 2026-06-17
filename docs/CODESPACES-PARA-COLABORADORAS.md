# Como mexer no projeto sem instalar nada

Este guia e para quem vai ajudar criando telas, ajustando HTML/CSS/JavaScript ou melhorando prototipos do Sistema Casa da Mulher.

Voce nao precisa instalar .NET, banco, Git ou VS Code no computador. O trabalho pode ser feito pelo navegador usando GitHub Codespaces.

## 1. Fazer fork

1. Entre no repositorio principal:
   `https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER`
2. Clique em `Fork`.
3. Crie o fork na sua propria conta.
4. Depois do fork, voce tera uma copia do projeto no seu GitHub.

Nao mexa direto no repositorio principal. A sua alteracao volta por Pull Request.

Fluxos esperados:

- Allan/mantenedor: pode usar IDE local ou repositorio principal em branch propria.
- Colaboradora com fork: abre Codespaces no proprio fork.
- Colaboradora sem fork: cria o fork primeiro.

Se voce nao for mantenedora do repositorio principal, sempre abra Codespaces no seu fork.

## 2. Abrir Codespace

1. Entre no seu fork.
2. Clique em `Code`.
3. Clique em `Codespaces`.
4. Clique em `Create codespace`.
5. Espere abrir o VS Code no navegador.

Na primeira abertura, o Codespaces prepara o ambiente automaticamente. Pode demorar alguns minutos.

Depois que abrir, voce pode rodar a tarefa:

```text
Casa da Mulher: detectar fluxo
```

Ela gera:

```text
projetocasadamulher/telas/dev-status.json
```

Esse arquivo ajuda o Painel da Equipe a mostrar se o ambiente esta em fork, se precisa criar fork ou se e fluxo de mantenedor.

## 3. Iniciar o sistema

1. No VS Code Web, abra `Terminal`.
2. Clique em `Run Task`.
3. Escolha `Casa da Mulher: iniciar sistema`.
4. Quando o GitHub mostrar a porta `5500`, abra essa porta no navegador.

Se o ambiente for novo, crie um acesso demo primeiro:

1. Abra `cadastro.html`.
2. Use:
   - e-mail: `recepcao@casamulher.local`
   - codigo: `REC-2026`
   - senha sugerida: `Senha@123`
3. Depois do cadastro, use o ID gerado para fazer login.

Para contas de equipe, use o ID `EQP` e codigo enviados pelo owner em:

```text
projetocasadamulher/telas/equipe-ativar.html
```

## 4. Editar tela

Para colaboradoras, o lugar seguro para trabalhar e:

```text
prototipos/colaboradores/seu-github/nome-da-tela/
```

Use a tarefa:

```text
Casa da Mulher: criar novo prototipo
```

Ela cria a pasta a partir de um template e atualiza `prototipos/manifest.json`.

Abra:

```text
prototipos/index.html
```

Voce pode mexer nos arquivos `.html`, `.css` e `.js` dentro da pasta criada.

Evite mexer nestas pastas sem combinar antes:

- `CasaMulher.Api/`
- `projetocasadamulher/telas/`
- `scripts/`
- `.github/`
- `.devcontainer/`

## 5. Enviar para revisao

Quando terminar:

1. Abra `Terminal`.
2. Clique em `Run Task`.
3. Escolha `Casa da Mulher: enviar Pull Request`.
4. Escreva uma mensagem curta, por exemplo:
   `Adiciona tela de agendamento`
5. Copie o link do Pull Request.
6. Mande o link no grupo.

O mantenedor vai revisar, pedir ajuste se precisar e depois aprovar.

A tarefa de Pull Request:

- bloqueia envio direto para `main`;
- bloqueia colaboradoras em fork quando houver alteracao fora de `prototipos/`;
- cria uma branch se voce estiver na `main`;
- faz commit e push para o seu fork;
- tenta abrir PR contra `Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER`;
- se nao conseguir abrir automaticamente, mostra instrucoes para criar manualmente.

O GitHub tambem possui um workflow que falha PR de fork quando algum arquivo fora de `prototipos/` for alterado.

## Banco local do Codespaces

Cada Codespace tem seu proprio banco local.

Isso significa:

- cadastro demo feito no seu Codespace nao aparece no Codespace de outra pessoa;
- ativacao `EQP` feita no seu Codespace tambem fica naquele ambiente;
- isso e normal para desenvolvimento e testes;
- a atividade central do projeto fica no GitHub, por meio de Pull Requests, commits e revisoes.

No futuro, se a equipe precisar de controle `EQP` centralizado, sera necessario um ambiente de homologacao com banco compartilhado.

## 6. Regras de seguranca

Nao envie:

- senha;
- token;
- banco local;
- arquivo `appsettings` real;
- print com e-mail real ou dado sensivel;
- dado real de atendimento.

Use dados ficticios nos testes.
