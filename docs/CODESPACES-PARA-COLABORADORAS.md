# Como mexer no projeto pelo Codespaces

Este guia é para quem vai ajudar criando telas, ajustando HTML/CSS/JavaScript ou melhorando protótipos do Sistema Casa da Mulher.

Você não precisa instalar .NET, banco, Git ou VS Code no computador. O trabalho pode ser feito pelo navegador usando GitHub Codespaces.

## 1. Fazer fork

1. Entre no repositório principal:
   `https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER`
2. Clique em `Fork`.
3. Crie o fork na sua própria conta.
4. Depois do fork, você terá uma cópia do projeto no seu GitHub.

Não mexa direto no repositório principal. A sua alteração volta por Pull Request.

Fluxos esperados:

- Allan/mantenedor: pode usar IDE local ou repositório principal em branch própria.
- Colaboradora com fork: abre Codespaces no próprio fork.
- Colaboradora sem fork: cria o fork primeiro.

Se você não for mantenedora do repositório principal, sempre abra Codespaces no seu fork.

## 2. Abrir Codespace

1. Entre no seu fork.
2. Clique em `Code`.
3. Clique em `Codespaces`.
4. Clique em `Create codespace`.
5. Espere abrir o VS Code no navegador.

Na primeira abertura, o Codespaces prepara o ambiente automaticamente. Pode demorar alguns minutos.

Depois que abrir, você pode rodar:

```text
Casa da Mulher: detectar fluxo
```

Essa task gera `projetocasadamulher/telas/dev-status.json` e ajuda o Painel da Equipe a mostrar se o ambiente está em fork, precisa criar fork ou é fluxo de mantenedor.

## 3. Iniciar o sistema

No VS Code Web:

1. Abra `Terminal`.
2. Clique em `Run Task`.
3. Escolha `Casa da Mulher: iniciar sistema`.
4. Aguarde API e front subirem.

O terminal mostrará links parecidos com:

```text
Área da Equipe: https://SEU-CODESPACE-5500.app.github.dev/equipe.html
Ativar EQP:     https://SEU-CODESPACE-5500.app.github.dev/projetocasadamulher/telas/equipe-ativar.html
Login:          https://SEU-CODESPACE-5500.app.github.dev/projetocasadamulher/telas/index.html
Protótipos:     https://SEU-CODESPACE-5500.app.github.dev/prototipos/index.html
```

## 4. Abrir a Área da Equipe

Rode a task:

```text
Casa da Mulher: abrir área da equipe
```

Ela monta a URL correta da porta `5500` para o seu Codespace e tenta abrir `/equipe.html`.

Se não abrir sozinha:

1. Abra a aba `Ports/Portas`.
2. Clique no link da porta `5500`.
3. Acesse `/equipe.html`.
4. Clique em `Ativar meu EQP`.

Use o ID e o código enviados pelo mantenedor. Código EQP é individual.

## 5. Criar protótipo

Para colaboradoras, o lugar seguro para trabalhar é:

```text
prototipos/colaboradores/seu-github/nome-da-tela/
```

Use a task:

```text
Casa da Mulher: criar novo protótipo
```

Ela cria a pasta a partir de um template e atualiza `prototipos/manifest.json`.

Abra:

```text
prototipos/index.html
```

Você pode mexer nos arquivos `.html`, `.css` e `.js` dentro da pasta criada.

Evite mexer nestas pastas sem combinar antes:

- `CasaMulher.Api/`
- `projetocasadamulher/telas/`
- `scripts/`
- `.github/`
- `.devcontainer/`

## 6. Enviar para revisão

Quando terminar:

1. Abra `Terminal`.
2. Clique em `Run Task`.
3. Escolha `Casa da Mulher: enviar Pull Request`.
4. Escreva uma mensagem curta, por exemplo:
   `Adiciona protótipo de agendamento`
5. Copie o link do Pull Request.
6. Mande o link no grupo.

A task de Pull Request:

- bloqueia envio direto para `main`;
- bloqueia colaboradoras em fork quando houver alteração fora de `prototipos/`;
- cria uma branch se você estiver na `main`;
- faz commit e push para o seu fork;
- tenta abrir PR contra `Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER`;
- se não conseguir abrir automaticamente, mostra instruções para criar manualmente.

O GitHub também possui um workflow que falha PR de fork quando algum arquivo fora de `prototipos/` for alterado.

## Banco local

Cada Codespace tem seu próprio banco local.

Isso significa:

- cadastro demo feito no seu Codespace não aparece no Codespace de outra pessoa;
- ativação `EQP` feita no seu Codespace também fica naquele ambiente;
- isso é normal para desenvolvimento e testes;
- a atividade central do projeto fica no GitHub, por meio de Pull Requests, commits e revisões.

No futuro, se a equipe precisar de controle `EQP` centralizado, será necessário um ambiente de homologação com banco compartilhado.

## Segurança

Não envie:

- senha;
- token;
- código EQP;
- banco local;
- arquivo `appsettings` real;
- print com e-mail real ou dado sensível;
- dado real de atendimento.

Use dados fictícios nos testes.
