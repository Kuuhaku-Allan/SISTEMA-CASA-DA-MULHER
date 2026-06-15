# Sistema Casa da Mulher

Projeto em desenvolvimento para o Sistema Casa da Mulher Itaquaquecetuba.

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
```

Esse prefixo é apenas uma identificação amigável. As permissões continuam vindo das roles do ASP.NET Identity.

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
