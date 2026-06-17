# Como mexer no projeto pelo Codespaces

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

Depois que abrir, voce pode rodar:

```text
Casa da Mulher: detectar fluxo
```

## 3. Iniciar o sistema

No VS Code Web:

1. Abra `Terminal`.
2. Clique em `Run Task`.
3. Escolha `Casa da Mulher: iniciar sistema`.
4. Aguarde API e front subirem.

O terminal mostrara links parecidos com:

```text
Area da Equipe: https://SEU-CODESPACE-5500.app.github.dev/equipe.html
Ativar EQP:     https://SEU-CODESPACE-5500.app.github.dev/projetocasadamulher/telas/equipe-ativar.html
Login:          https://SEU-CODESPACE-5500.app.github.dev/projetocasadamulher/telas/index.html
Prototipos:     https://SEU-CODESPACE-5500.app.github.dev/prototipos/index.html
```

## 4. Ativar EQP pelo portal central

1. Abra o link central do portal Render que esta no `ACESSO-EQUIPE`.
2. Entre com GitHub.
3. Escolha seu EQP.
4. Informe nome e senha criada somente para este projeto.

Nao use senha pessoal. Nao compartilhe senha.

## 5. Sincronizar equipe no Codespaces

Depois de ativar ou redefinir senha no portal, rode:

```text
Casa da Mulher: sincronizar equipe
```

Essa task le `ACESSO-EQUIPE/data/equipe-db.json` usando seu login do `gh` no Codespaces e importa sua conta para o banco local.

Depois da sincronizacao, faca login com:

```text
EQP-000000
```

ou com o ADM pareado:

```text
ADM-000000
```

## 6. Abrir a Area da Equipe

Rode a task:

```text
Casa da Mulher: abrir area da equipe
```

Ela monta a URL correta da porta `5500` para o seu Codespace e tenta abrir `/equipe.html`.

Se nao abrir sozinha:

1. Abra a aba `Ports/Portas`.
2. Clique no link da porta `5500`.
3. Acesse `/equipe.html`.

## 7. Criar prototipo

Para colaboradoras, o lugar seguro para trabalhar e:

```text
prototipos/colaboradores/seu-github/nome-da-tela/
```

Use a task:

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

## 8. Enviar para revisao

Quando terminar:

1. Abra `Terminal`.
2. Clique em `Run Task`.
3. Escolha `Casa da Mulher: enviar Pull Request`.
4. Escreva uma mensagem curta, por exemplo:
   `Adiciona prototipo de agendamento`
5. Copie o link do Pull Request.
6. Mande o link no grupo.

A task de Pull Request:

- bloqueia envio direto para `main`;
- bloqueia colaboradoras em fork quando houver alteracao fora de `prototipos/`;
- cria uma branch se voce estiver na `main`;
- faz commit e push para o seu fork;
- tenta abrir PR contra `Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER`;
- se nao conseguir abrir automaticamente, mostra instrucoes para criar manualmente.

O GitHub tambem possui um workflow que falha PR de fork quando algum arquivo fora de `prototipos/` for alterado.

## Banco local

Cada Codespace tem seu proprio banco local.

Isso significa:

- cadastro demo feito no seu Codespace nao aparece no Codespace de outra pessoa;
- o EQP ativado no portal so entra no Codespace depois de sincronizar;
- isso e normal para desenvolvimento e testes;
- a fonte central da equipe fica em `ACESSO-EQUIPE/data/equipe-db.json`.

## Seguranca

Nao envie:

- senha;
- token;
- hash;
- banco local;
- arquivo `appsettings` real;
- print com e-mail real ou dado sensivel;
- dado real de atendimento.

Use dados ficticios nos testes.
