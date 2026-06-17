# Sistema Casa da Mulher

Projeto em desenvolvimento para o Sistema Casa da Mulher Itaquaquecetuba.

## Comece aqui - Equipe do Projeto

Se voce faz parte da equipe de desenvolvimento/teste, comece por estes guias:

- Guia central da equipe: `docs/COMECE-AQUI-EQUIPE.md`
- Contas e convites EQP: `docs/EQUIPE-E-CONVITES.md`
- Codespaces para colaboradoras: `docs/CODESPACES-PARA-COLABORADORAS.md`
- Enviar Pull Request: `docs/GUIA-RAPIDO-PR.md`
- Matriz de permissoes EQP/ADM: `docs/MATRIZ-PERMISSOES-EQP-ADM.md`
- Area segura de prototipos: `prototipos/index.html`
- Atividade do projeto: `projetocasadamulher/telas/equipe-atividade.html`
- Ativar EQP: `projetocasadamulher/telas/equipe-ativar.html`

O GitHub concentra a atividade central do projeto. O banco de cada Codespace e local e serve para desenvolvimento.

## Equipe do projeto

Este projeto possui um fluxo separado para integrantes da equipe usando IDs `EQP`.

Os guias ficam em `docs/COMECE-AQUI-EQUIPE.md`.

Codigos de ativacao EQP sao individuais e nao devem ser publicados neste repositorio.

## Estrutura

- `CasaMulher.Api/`: API ASP.NET Core com autenticação de funcionários por convite.
- `projetocasadamulher/telas/`: telas HTML/CSS/JavaScript para cadastro, login e painel simples.
- Documentos e materiais de apoio do projeto na raiz.

## Rodar a API

```powershell
cd CasaMulher.Api
dotnet restore
dotnet tool restore
dotnet tool run dotnet-ef database update
dotnet run --urls http://localhost:5001 --environment Development
```

Swagger:

```text
http://localhost:5001/swagger
```

## Modo fácil para colaboradoras

Quem for contribuir com telas pode usar o fluxo gratuito de Fork + GitHub Codespaces + Pull Request, sem instalar .NET, Git ou VS Code no computador.

Resumo:

1. A colaboradora faz fork do repositório.
2. Abre um Codespace no próprio fork.
3. Roda a tarefa `Casa da Mulher: iniciar sistema`.
4. Edita os arquivos em `projetocasadamulher/telas/`.
5. Roda a tarefa `Casa da Mulher: enviar Pull Request`.
6. O Pull Request vai para revisão na branch `main`.

Guia completo:

```text
docs/CODESPACES-PARA-COLABORADORAS.md
```

Guia das contas da equipe do projeto:

```text
docs/EQUIPE-E-CONVITES.md
```

A `main` é a versão oficial do projeto. Alterações de outras pessoas devem entrar por Pull Request e passar por revisão.

## Fluxo de autenticação

O cadastro de funcionário é restrito por convite. A tela envia apenas:

- nome completo
- e-mail
- senha
- confirmação de senha
- código de cadastro

O perfil vem do convite registrado no banco.

Depois do cadastro, o funcionário recebe um ID para login diário. O ID usa prefixo por perfil original de cadastro:

```text
ADM-000001
REC-000001
PRO-000001
SOC-000001
JUR-000001
EQP-000001
```

Esse prefixo é apenas uma identificação amigável. As permissões continuam vindo das roles do ASP.NET Identity.

IDs `EQP` sao usados apenas pela equipe de desenvolvimento/teste e nao representam funcionarios reais da Casa da Mulher.

## Convites demo

```text
coord@casamulher.local / ADM-2026 / adm
recepcao@casamulher.local / REC-2026 / recepcao
professora@casamulher.local / PROF-2026 / professor
social@casamulher.local / SOCIAL-2026 / as_social
juridico@casamulher.local / JUR-2026 / juridico
```

As chaves de `appsettings.json` são somente para desenvolvimento e devem ser trocadas antes de produção.

## Como contribuir

O repositório principal deve manter a branch `main` como a versão oficial e aprovada do projeto. Ninguém deve trabalhar direto na `main`; toda alteração precisa entrar por Pull Request para revisão.

Fluxo recomendado:

1. Faça um fork do repositório.
2. Clone o fork no seu computador.
3. Crie uma branch com o nome da alteração.
4. Faça suas alterações.
5. Envie a branch para o seu fork.
6. Abra um Pull Request para a branch `main` deste repositório.
7. Aguarde a revisão antes de a alteração entrar no projeto principal.

Exemplo de branch:

```bash
git checkout -b tela-agendamento
```

Exemplo de envio:

```bash
git status
git add .
git commit -m "Adiciona tela de agendamento"
git push origin tela-agendamento
```

Ao abrir o Pull Request, deixe marcada a opção `Allow edits by maintainers`, para que o mantenedor possa ajustar pequenos pontos na própria branch do PR.

## Organização das pastas

- `CasaMulher.Api/`: API do sistema. Alterações aqui precisam de revisão cuidadosa.
- `projetocasadamulher/telas/`: telas HTML/CSS/JavaScript. Colegas podem enviar telas e ajustes visuais por Pull Request.
- `docs/`: documentação do projeto, quando existir.
- `scripts/`: scripts de apoio. Alterações aqui precisam de revisão cuidadosa.
- `.github/`: configurações do repositório, incluindo modelo de Pull Request e responsáveis por revisão.

Não envie:

- senhas;
- tokens;
- arquivos `appsettings` reais de produção;
- banco de dados local;
- prints com e-mails reais ou dados sensíveis;
- arquivos temporários.
