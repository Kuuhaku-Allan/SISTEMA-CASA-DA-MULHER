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

## Convites demo

```text
coord@casamulher.local / ADM-2026 / adm
recepcao@casamulher.local / REC-2026 / recepcao
professora@casamulher.local / PROF-2026 / professor
social@casamulher.local / SOCIAL-2026 / as_social
juridico@casamulher.local / JUR-2026 / juridico
```

As chaves de `appsettings.json` são somente para desenvolvimento e devem ser trocadas antes de produção.
