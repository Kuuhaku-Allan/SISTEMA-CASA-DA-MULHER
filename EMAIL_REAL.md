# Envio Real de E-mail

O sistema tem dois modos de e-mail:

- `Fake`: usado em desenvolvimento para simular envio e registrar `EmailEvento` com status `Simulado`.
- `Smtp`: usado para envio real por um provedor SMTP, como Brevo.

O modo `Fake` continua sendo o padrao de `Development` para evitar gasto de limite, dependencia de internet e envio acidental durante testes.

## Regras de seguranca

- Nunca commite chave SMTP, senha, connection string real ou token.
- Nunca coloque credenciais reais em `appsettings.json` commitado.
- Use `dotnet user-secrets` no ambiente local.
- Use variaveis de ambiente ou painel da hospedagem em Staging/Producao.
- O `FromEmail` precisa ser um remetente verificado no provedor.
- Se uma chave SMTP foi colada em chat, issue, print ou documento, gere uma nova no provedor e revogue a antiga.

O projeto ja possui `UserSecretsId`, entao `dotnet user-secrets` pode ser usado diretamente na pasta da API.

## Configurar Brevo SMTP localmente

Na pasta da API:

```powershell
cd "C:\Users\Defal\Documents\Projetos\SISTEMA CASA DA MULHER\CasaMulher.Api"
```

Configure os valores:

```powershell
dotnet user-secrets set "Email:Provider" "Smtp"
dotnet user-secrets set "Email:Smtp:Host" "smtp-relay.brevo.com"
dotnet user-secrets set "Email:Smtp:Port" "587"
dotnet user-secrets set "Email:Smtp:EnableSsl" "true"
dotnet user-secrets set "Email:Smtp:User" "SEU_LOGIN_SMTP_BREVO"
dotnet user-secrets set "Email:Smtp:Password" "SUA_CHAVE_SMTP_BREVO"
dotnet user-secrets set "Email:Smtp:FromEmail" "SEU_REMETENTE_VERIFICADO"
dotnet user-secrets set "Email:Smtp:FromName" "Casa da Mulher"
dotnet user-secrets set "Frontend:BaseUrl" "http://localhost:5500"
```

Use em `Email:Smtp:User` o login SMTP exibido pelo Brevo. Use em `Email:Smtp:Password` a chave SMTP gerada pelo Brevo. O valor de `Email:Smtp:FromEmail` deve ser um remetente verificado no Brevo, não necessariamente o login SMTP.

Para conferir sem exibir senha:

```powershell
dotnet user-secrets list
```

Se precisar remover a configuração real e voltar ao fake:

```powershell
dotnet user-secrets remove "Email:Provider"
dotnet user-secrets remove "Email:Smtp:Host"
dotnet user-secrets remove "Email:Smtp:Port"
dotnet user-secrets remove "Email:Smtp:EnableSsl"
dotnet user-secrets remove "Email:Smtp:User"
dotnet user-secrets remove "Email:Smtp:Password"
dotnet user-secrets remove "Email:Smtp:FromEmail"
dotnet user-secrets remove "Email:Smtp:FromName"
```

## Rodar para teste local

Primeiro, sirva as telas por HTTP:

```powershell
cd "C:\Users\Defal\Documents\Projetos\SISTEMA CASA DA MULHER\projetocasadamulher\telas"
python -m http.server 5500
```

Depois, rode a API:

```powershell
cd "C:\Users\Defal\Documents\Projetos\SISTEMA CASA DA MULHER\CasaMulher.Api"
dotnet run --environment Development --urls http://localhost:5001
```

Acesse:

```text
http://localhost:5500/convites.html
```

Crie um convite com a opção `Enviar convite por e-mail` marcada.

Resultado esperado com Brevo SMTP configurado:

- a resposta da tela mostra `E-mail enviado`;
- a tabela de e-mails mostra `Tipo = Convite de funcionário`;
- o status do evento fica `Enviado`;
- o e-mail chega na caixa do destinatário.

Se a configuração SMTP estiver incorreta:

- o convite continua criado;
- o status do evento fica `Falhou`;
- a tela mostra aviso de falha;
- o link manual continua disponivel como alternativa.

## Link localhost

Em desenvolvimento, `Frontend:BaseUrl` usa:

```text
http://localhost:5500
```

Esse link funciona apenas no proprio computador onde as telas estao rodando. Para enviar convites para outra pessoa, use uma destas opcoes:

- front hospedado;
- servidor local na rede da Casa da Mulher;
- tunel temporario apenas para demonstracao.

## Alternativa Gmail com senha de app

Use apenas para teste. Para Gmail, a conta precisa ter verificacao em duas etapas e senha de app habilitada.

```powershell
dotnet user-secrets set "Email:Provider" "Smtp"
dotnet user-secrets set "Email:Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Email:Smtp:Port" "587"
dotnet user-secrets set "Email:Smtp:EnableSsl" "true"
dotnet user-secrets set "Email:Smtp:User" "seuemail@gmail.com"
dotnet user-secrets set "Email:Smtp:Password" "SENHA_DE_APP_DO_GOOGLE"
dotnet user-secrets set "Email:Smtp:FromEmail" "seuemail@gmail.com"
dotnet user-secrets set "Email:Smtp:FromName" "Casa da Mulher"
```

Brevo ou outro provedor transacional e mais adequado que conta pessoal para uso continuo.
