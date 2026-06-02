# Ambientes

Este projeto esta preparado para dois modos principais:

- `Development`: ambiente local, com SQLite e seed demo.
- `Staging`: ambiente de homologacao, preparado para PostgreSQL e sem seed demo automatico.

Nao commite senhas, connection strings reais, chaves JWT reais ou segredos de convite. Configure esses valores por variaveis de ambiente, user-secrets ou painel do provedor de hospedagem.

## Development local

O ambiente local usa:

- `CasaMulher.Api/appsettings.Development.json`
- banco SQLite em `CasaMulher.Api/casamulher.db`
- seed demo habilitado

Rodar API local:

```powershell
cd "C:\Users\Defal\Documents\Projetos\SISTEMA CASA DA MULHER\CasaMulher.Api"
dotnet run --environment Development --urls http://localhost:5001
```

Aplicar migrations no SQLite local:

```powershell
cd "C:\Users\Defal\Documents\Projetos\SISTEMA CASA DA MULHER\CasaMulher.Api"
dotnet tool run dotnet-ef database update
```

## Staging/homologacao

O ambiente de homologacao usa:

- `CasaMulher.Api/appsettings.Staging.json`
- provider `PostgreSQL`
- seed demo desativado por padrao
- connection string e segredos vindos de variaveis de ambiente

Variaveis recomendadas:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Staging"
$env:Database__Provider="PostgreSQL"
$env:ConnectionStrings__DefaultConnection="Host=HOST;Port=5432;Database=CASA_MULHER;Username=USUARIO;Password=SENHA;SSL Mode=Require;Trust Server Certificate=true"
$env:Jwt__Key="CHAVE_FORTE_COM_PELO_MENOS_32_CARACTERES"
$env:Jwt__Issuer="CasaMulher.Api"
$env:Jwt__Audience="CasaMulher.Api"
$env:Convites__HashSecret="CHAVE_FORTE_PARA_HASH_DE_CONVITES"
$env:Seed__RunDemoData="false"
```

Aplicar migrations no banco de homologacao:

```powershell
cd "C:\Users\Defal\Documents\Projetos\SISTEMA CASA DA MULHER\CasaMulher.Api"
$env:ASPNETCORE_ENVIRONMENT="Staging"
dotnet tool run dotnet-ef database update
```

Esse comando precisa da connection string real configurada antes de rodar. Sem `ConnectionStrings__DefaultConnection`, `Jwt__Key` e `Convites__HashSecret`, a aplicacao falha de proposito para evitar uso de configuracao incompleta.

Rodar API em Staging:

```powershell
cd "C:\Users\Defal\Documents\Projetos\SISTEMA CASA DA MULHER\CasaMulher.Api"
$env:ASPNETCORE_ENVIRONMENT="Staging"
dotnet run --urls http://localhost:5001
```

## Provedores sugeridos para PostgreSQL

- Supabase
- Neon
- Railway
- Render

Use sempre o painel do provedor para guardar a connection string e segredos. O repositorio deve manter somente exemplos e configuracoes sem senha.

## Seed demo

O seed demo cria roles e convites locais para teste. Ele fica habilitado apenas em `Development`.

Para habilitar explicitamente em outro ambiente, configure:

```powershell
$env:Seed__RunDemoData="true"
```

Use isso apenas em banco descartavel de teste. Nao habilite seed demo em producao.
