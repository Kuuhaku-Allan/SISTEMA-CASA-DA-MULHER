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

Portal central da equipe:

```text
https://casa-mulher-eqp.onrender.com/equipe.html
```

O portal central serve para:

1. Entrar com GitHub.
2. Ativar um EQP disponivel ou reservado.
3. Receber o ADM pareado automaticamente.
4. Redefinir a propria senha.
5. Gerar um registro versionado em `ACESSO-EQUIPE/data/equipe-db.json`.

## Depois de ativar

Depois que seu EQP for ativado no portal, abra seu ambiente local ou Codespaces. A API sincroniza a equipe automaticamente na inicialização e repete a atualização a cada minuto.

Para forçar uma atualização imediata no Windows:

```powershell
.\casa_da_mulher.cmd equipe sync
```

No Codespaces, a task manual continua disponível:

```text
Casa da Mulher: sincronizar equipe
```

Depois da sincronização, o login normal do sistema aceita tanto o EQP quanto o ADM pareado com a senha criada no portal.

EQP e ADM pareado sao aliases do mesmo usuario. Eles compartilham senha, e-mail de recuperacao, 2FA, passkeys e demais configuracoes de seguranca. A sincronizacao atualiza aliases e senha versionada, mas nunca troca um e-mail real por `@equipe.local` nem apaga 2FA/passkeys.

Para contas sincronizadas, a senha e redefinida somente no portal EQP. As telas normais de troca ou recuperacao do sistema ficam bloqueadas para evitar divergencia com o banco privado da equipe.

Para auditar contas EQP/ADM sem alterar dados:

```powershell
.\casa_da_mulher.cmd equipe reparar-seguranca
```

No ambiente central do Render, acesse `/equipe.html`, entre primeiro com GitHub e depois use o login normal do sistema com EQP ou ADM. O GitHub Gate apenas bloqueia pessoas externas; ele nao substitui o login do sistema.

Os IDs abrem contextos diferentes, mesmo quando pertencem à mesma pessoa:

- entrar com `EQP-...` abre o Painel da Equipe;
- entrar com `ADM-...` abre o painel administrativo institucional;
- sair e entrar novamente troca o contexto da sessão com segurança.

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
4. Inicie o ambiente local; a sincronização acontece automaticamente.
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
6. Aguarde a sincronização automática da API.
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
8. Aguarde a sincronização automática da API.
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

## Passkeys no Render

Passkeys sao vinculadas ao dominio em que foram criadas. Uma passkey de `localhost` nao funciona no Render e vice-versa. Isso e padrao WebAuthn, nao e um bug.

- Para usar passkey no Render: entre primeiro com ID e senha, va em Seguranca da Conta e registre uma nova passkey.
- Cada ambiente tem sua propria lista de passkeys.

O banco do Render Free e apagado em redeploys. Para persistir 2FA e passkeys, o owner precisa configurar o snapshot criptografado (veja `docs/HOMOLOGACAO-PORTAL-EQP-RENDER.md`). Se nao configurado, a tela Seguranca mostra aviso.

O Historico institucional mostra apenas eventos de funcionarios reais. Falhas de login com EQP aparecem somente nos logs da equipe.

## Deu erro

Verifique nesta ordem:

1. Voce entrou com GitHub no portal central?
2. Seu GitHub esta na organizacao ou na allowlist do `equipe-db.json`?
3. A API ficou ligada por até um minuto depois de ativar ou redefinir a senha? Se necessário, force com `equipe sync`.
4. A API abriu na porta `5001`?
5. O front abriu na porta `5500`?
6. O arquivo alterado fica dentro de `prototipos/`, se o PR veio de fork?
7. Nenhum token, senha, hash, banco local ou dado real foi enviado?

Se ainda falhar, copie a mensagem de erro e mande junto com o link do Codespace ou Pull Request.
