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
- servico de e-mail em modo fake, sem envio real
- telas servidas em `http://localhost:5500` para gerar links absolutos de convite

Rodar API local:

```powershell
cd "C:\Users\Defal\Documents\Projetos\SISTEMA CASA DA MULHER\CasaMulher.Api"
dotnet run --environment Development --urls http://localhost:5001
```

Rodar telas HTML por HTTP local:

```powershell
cd "C:\Users\Defal\Documents\Projetos\SISTEMA CASA DA MULHER\projetocasadamulher\telas"
python -m http.server 5500
```

Acesse as telas por:

```text
http://localhost:5500
```

Evite abrir as telas por `file:///C:/...` quando estiver testando convites por e-mail. Links de e-mail precisam de endereco HTTP absoluto, e `localhost` so funciona no proprio computador. Para enviar para outra pessoa, use uma hospedagem, tunel temporario ou servidor na rede local.

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

Para banco remoto gratuito, backup e restore, veja tambem `BANCO_E_BACKUP.md`.

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
$env:Email__Provider="Smtp"
$env:Email__Smtp__Host="smtp.seu-provedor.com"
$env:Email__Smtp__Port="587"
$env:Email__Smtp__User="USUARIO"
$env:Email__Smtp__Password="SENHA"
$env:Email__Smtp__FromName="Casa da Mulher"
$env:Email__Smtp__FromEmail="nao-responda@seudominio.org"
$env:Email__Smtp__EnableSsl="true"
$env:Frontend__BaseUrl="https://sistema-casa-da-mulher.exemplo"
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

- Neon
- Supabase
- Railway
- Render

Use sempre o painel do provedor para guardar a connection string e segredos. O repositorio deve manter somente exemplos e configuracoes sem senha. Para homologacao gratuita inicial, a recomendacao atual do projeto e Neon PostgreSQL Free com backups logicos via `pg_dump`.

## Seed demo

O seed demo cria roles e convites locais para teste. Ele fica habilitado apenas em `Development`.

Para habilitar explicitamente em outro ambiente, configure:

```powershell
$env:Seed__RunDemoData="true"
```

Use isso apenas em banco descartavel de teste. Nao habilite seed demo em producao.

## E-mails

Em `Development`, `Email:Provider` fica como `Fake`. O sistema grava um evento com status `Simulado`, mas nao envia e-mail real.

Em `Staging`, use `Email:Provider=Smtp` apenas quando `Email:Smtp:Host`, `Email:Smtp:FromEmail` e demais dados do provedor estiverem configurados por variaveis de ambiente ou pelo painel da hospedagem.

Configure `Frontend:BaseUrl` em homologacao/producao para que convites enviados por e-mail usem um link absoluto para a tela de cadastro. Se `enviarEmail=true` e `Frontend:BaseUrl` estiver vazio, o convite ainda sera criado, mas o envio por e-mail sera recusado com aviso de configuracao.

Os logs de e-mail guardam destinatario, assunto, tipo, status, erro e data. Nao grave corpo HTML, senhas, tokens, codigos de convite puros ou chaves de autenticador nos logs.

Para configurar envio real local com Brevo SMTP ou outro provedor, veja `EMAIL_REAL.md`. Nao coloque chaves SMTP em arquivos commitados.
