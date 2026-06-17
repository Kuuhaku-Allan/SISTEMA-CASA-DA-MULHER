# Comece aqui - Equipe do Projeto

Este e o ponto de entrada para quem vai ajudar no Sistema Casa da Mulher.

O projeto tem dois mundos separados:

- Sistema institucional: funcionarias reais, recepcao, coordenacao, historico e logs.
- Equipe do projeto: contas `EQP`, testes, Codespaces, prototipos, Pull Requests e organizacao do desenvolvimento.

Contas `EQP` e ADMs pareados da equipe nao representam funcionarias reais da Casa da Mulher.

## Link central da equipe

O ponto fixo da equipe e o repositorio privado:

```text
https://github.com/Sistema-Casa-da-Mulher/ACESSO-EQUIPE
```

Quando o portal do Render estiver publicado, o link fixo ficara no README desse repositorio privado.

O portal central serve para:

1. Entrar com GitHub.
2. Ativar um EQP disponivel ou reservado.
3. Receber o ADM pareado automaticamente.
4. Redefinir a propria senha.
5. Gerar um registro versionado em `ACESSO-EQUIPE/data/equipe-db.json`.

## Depois de ativar

Depois que seu EQP for ativado no portal, abra seu ambiente local/Codespaces e sincronize.

Windows:

```powershell
.\casa_da_mulher.cmd equipe sync
```

Codespaces:

```text
Casa da Mulher: sincronizar equipe
```

Depois da sincronizacao, o login normal do sistema aceita tanto o EQP quanto o ADM pareado com a senha criada no portal.

## Entrada local

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
Casa da Mulher: abrir area da equipe
```

Se a porta nao abrir sozinha, abra a aba `Ports/Portas`, clique na porta `5500` e acesse `/equipe.html`.

## Allan ou mantenedor

Fluxo recomendado:

1. Abra o projeto na sua IDE.
2. Use o portal central para ativar `EQP-000001`.
3. Esse EQP fica pareado com `ADM-000003`.
4. No ambiente local, execute `.\casa_da_mulher.cmd equipe sync`.
5. Faca login com `EQP-000001` ou `ADM-000003`.

O membro `EQP-000001` e tratado como owner da equipe:

- nao precisa de fork;
- nao precisa de Codespaces;
- usa fluxo `local_owner`;
- pode gerenciar convites, membros e logs da equipe.

## Colaboradora que ja tem fork

1. Abra seu fork no GitHub.
2. Abra Codespaces no seu fork.
3. Rode `Casa da Mulher: iniciar sistema`.
4. Rode `Casa da Mulher: abrir area da equipe`.
5. Ative seu EQP pelo portal central do Render.
6. Rode `Casa da Mulher: sincronizar equipe`.
7. Faca login com seu EQP ou ADM pareado.
8. Trabalhe em `prototipos/`.
9. Envie Pull Request.

O Pull Request deve ir para:

```text
Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER
```

## Colaboradora que ainda nao tem fork

1. Entre no repositorio principal:
   `https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER`
2. Clique em `Fork`.
3. Crie o fork na sua conta.
4. Abra Codespaces no seu fork.
5. Rode `Casa da Mulher: iniciar sistema`.
6. Rode `Casa da Mulher: abrir area da equipe`.
7. Ative seu EQP pelo portal central do Render.
8. Rode `Casa da Mulher: sincronizar equipe`.
9. Crie prototipos e envie PR.

## Criar prototipo

Este e o fluxo recomendado para colaboradoras:

1. Crie um fork.
2. Abra Codespaces no fork.
3. Rode `Casa da Mulher: iniciar sistema`.
4. Rode `Casa da Mulher: criar novo prototipo`.
5. Edite apenas a pasta criada em `prototipos/colaboradores/seu-usuario/`.
6. Abra `prototipos/index.html`.
7. Rode `Casa da Mulher: enviar Pull Request`.
8. Mande o link do PR.

PRs vindos de fork sao bloqueados automaticamente se alterarem arquivos fora de `prototipos/`.

## Guias uteis

- Contas e convites EQP: `docs/EQUIPE-E-CONVITES.md`
- Render do portal EQP: `docs/HOMOLOGACAO-PORTAL-EQP-RENDER.md`
- Codespaces: `docs/CODESPACES-PARA-COLABORADORAS.md`
- Pull Request: `docs/GUIA-RAPIDO-PR.md`
- Matriz de permissoes: `docs/MATRIZ-PERMISSOES-EQP-ADM.md`
- Repositorio privado da equipe: `docs/ACESSO-EQUIPE-PRIVADO.md`

## Deu erro

Verifique nesta ordem:

1. Voce entrou com GitHub no portal central?
2. Seu GitHub esta na organizacao ou na allowlist do `equipe-db.json`?
3. Voce rodou `equipe sync` depois de ativar/redefinir senha?
4. A API abriu na porta `5001`?
5. O front abriu na porta `5500`?
6. O arquivo alterado fica dentro de `prototipos/`, se o PR veio de fork?
7. Nenhum token, senha, hash, banco local ou dado real foi enviado?

Se ainda falhar, copie a mensagem de erro e mande junto com o link do Codespace ou Pull Request.
