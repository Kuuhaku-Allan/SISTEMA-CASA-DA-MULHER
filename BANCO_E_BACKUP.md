# Banco Remoto e Backup

Este projeto usa SQLite apenas para desenvolvimento local. Para homologacao e testes com mais de uma pessoa, o caminho recomendado e usar PostgreSQL remoto, com segredos fora do repositorio e uma rotina simples de backup.

## Por que nao usar SQLite local no uso real

SQLite e pratico para desenvolver porque nao exige servidor. Mesmo assim, ele nao e a melhor base para uso real com equipe:

- cada computador pode acabar com uma copia diferente do banco;
- perder o arquivo local pode significar perder dados;
- backup e restauracao dependem de disciplina manual;
- varios usuarios acessando ao mesmo tempo ficam mais dificeis de controlar;
- auditoria e recuperacao ficam presas a uma maquina.

Por isso:

- `Development`: SQLite local, seed demo e testes de programacao.
- `Staging`: PostgreSQL remoto, sem seed demo automatico, simulando uso real.

## Escolha gratuita inicial

A opcao principal para homologacao gratuita e o Neon PostgreSQL Free.

Em 2 de junho de 2026, a pagina de precos do Neon informa plano Free sem cartao, 0,5 GB por projeto, 100 CU-hours mensais por projeto e janela curta de time travel/restores. Esses limites podem mudar; confira a pagina oficial antes de assumir uso institucional:

- https://neon.com/pricing

O Neon Free ajuda a tirar o banco de um computador local, mas nao substitui uma politica profissional de backup. Para MVP academico, combine:

- PostgreSQL remoto no Neon;
- `pg_dump` diario;
- artifact privado/temporario no GitHub Actions;
- documentacao de restore.

## Estrategias possiveis

Existem tres caminhos principais para sair do SQLite local sem criar varios bancos divergentes.

### 1. Banco remoto gratuito

Este e o caminho recomendado para homologacao inicial.

```text
CasaMulher.Api local ou hospedada
        -> Neon PostgreSQL Free
        -> backup logico com pg_dump
```

Vantagens:

- todos usam o mesmo banco central;
- nao depende do arquivo de banco de um notebook;
- permite testar com mais de um computador;
- continua sem custo inicial;
- combina bem com ASP.NET Core, EF Core, Identity e migrations.

Cuidados:

- depende de internet;
- plano gratuito tem limites;
- backup por artifact e temporario;
- dados sensiveis exigem segredo bem guardado.

### 2. Servidor local central da Casa da Mulher

Este e o melhor caminho gratuito sem nuvem.

```text
PC-SERVIDOR-CASA
  - PostgreSQL
  - CasaMulher.Api
  - rotina de backup

PC-RECEPCAO
  - navegador acessando http://servidor-casa-mulher:5001

PC-COORDENACAO
  - navegador acessando http://servidor-casa-mulher:5001
```

Nesse modelo, o armazenamento fica no PC servidor, mas os outros computadores nao instalam nem editam banco. Eles acessam somente a API pelo navegador.

Vantagens:

- nao depende de nuvem;
- todos veem os mesmos dados;
- software continua gratuito;
- o banco nao fica espalhado em cada PC;
- usuarios comuns nao acessam o banco diretamente.

Cuidados:

- o PC servidor precisa ficar ligado;
- precisa de backup para outro dispositivo;
- precisa de manutencao, energia, rede local e controle de acesso;
- se o servidor parar, o sistema para.

Esse modelo pode virar uma fase propria depois da homologacao remota.

### 3. Banco distribuido/local-first

Este e o caminho que parece um "Google Docs do banco", com copia local editavel em cada maquina e sincronizacao posterior. Ele existe tecnicamente, mas nao e recomendado agora.

Ferramentas que chegam perto:

- CouchDB, com replicacao e conflitos de documentos;
- rqlite, com SQLite distribuido por Raft;
- LiteFS, com replicacao de SQLite e no primario para escrita;
- replicacao PostgreSQL, mais voltada a operacao de infraestrutura.

O problema e que cada uma aumenta muito a complexidade para este projeto. A documentacao do CouchDB, por exemplo, mostra que duas versoes conflitantes podem existir e que a aplicacao precisa resolver conflitos. O rqlite mantem um log Raft autoritativo para deixar os nos iguais, mas nao e PostgreSQL normal nem o caminho natural do EF Core Identity. O LiteFS usa um no primario para escrita e replicacao assincrona, com trade-offs de durabilidade.

Para um sistema com dados sensiveis, funcionarios, prontuarios, permissao e auditoria, espalhar copias editaveis do banco em varios computadores aumenta risco de:

- conflito de dados;
- vazamento;
- manutencao dificil;
- restauracao confusa;
- comportamento diferente entre maquinas.

Conclusao: se quiser usar armazenamento local, prefira servidor local central. Se quiser multiusuario simples agora, prefira PostgreSQL remoto.

## Opcao futura: Fase 17B - Servidor Local Central

Uma fase futura pode preparar o projeto para rodar dentro da Casa da Mulher sem nuvem.

Entregas sugeridas:

- `docker-compose.yml` com PostgreSQL e CasaMulher.Api;
- script `instalar-servidor.ps1`;
- script `backup-local.ps1`;
- script `restore-local.ps1`;
- documento `SERVIDOR_LOCAL.md`;
- orientacao de IP/nome do servidor na rede local;
- rotina de backup automatico no servidor;
- copia de backup para outro dispositivo ou pasta segura;
- restore manual com confirmacao explicita.

Regras importantes:

- usuarios comuns acessam apenas o sistema pelo navegador;
- usuarios comuns nao acessam PostgreSQL, arquivos do banco ou backups;
- o banco deve aceitar conexao da API, nao dos computadores dos funcionarios;
- backup local deve ficar fora da pasta publica do sistema;
- se houver copia externa, ela deve ser privada e, idealmente, criptografada.

Essa fase e uma alternativa real ao Neon, nao uma substituicao do fluxo de backup. Mesmo com servidor local, backup e restore continuam obrigatorios.

## Concorrencia e backup em tempo real

Para evitar o problema de duas pessoas salvarem por cima uma da outra, o caminho correto nao e cada PC ter um banco proprio. O melhor e todos acessarem a mesma API, e a API usar controle de concorrencia quando houver telas de edicao sensiveis.

Exemplo futuro:

```text
Maria abre um cadastro as 10:00.
Ana abre o mesmo cadastro as 10:01.
Maria salva as 10:03.
Ana tenta salvar as 10:04.
Sistema avisa que o cadastro foi alterado e pede para atualizar antes de salvar.
```

Para "voltar no tempo" com mais precisao, existem tres niveis:

- backup diario com `pg_dump`, que cobre perda geral e volta para o ultimo backup;
- backup horario, que reduz a janela de perda;
- PITR com WAL, que e mais avancado e permite restaurar para um ponto especifico desde um backup base.

A documentacao do PostgreSQL explica que o WAL registra mudancas no banco e que, combinando backup base com arquivos WAL arquivados, e possivel fazer point-in-time recovery. Isso fica documentado como evolucao futura, nao como requisito da Fase 17.

## Criar banco no Neon

1. Crie uma conta em https://neon.com.
2. Crie um projeto PostgreSQL.
3. Copie a connection string do banco.
4. Use a connection string apenas como variavel de ambiente ou GitHub Secret.
5. Nunca cole a connection string real em `appsettings.json`, README, issue, print publico ou commit.

Para scripts de backup, prefira a URL PostgreSQL:

```text
postgresql://USUARIO:SENHA@HOST.neon.tech/NOME_DO_BANCO?sslmode=require
```

Para a API ASP.NET Core, continue usando a chave:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=HOST;Port=5432;Database=CASA_MULHER;Username=USUARIO;Password=SENHA;SSL Mode=Require;Trust Server Certificate=true"
```

## Configurar Staging localmente

Antes de rodar migrations no banco remoto, configure:

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

Aplicar migrations:

```powershell
cd "C:\Users\Defal\Documents\Projetos\SISTEMA CASA DA MULHER\CasaMulher.Api"
dotnet tool run dotnet-ef database update
```

Sem connection string, JWT key e secret de convites, a aplicacao deve falhar de proposito. Isso evita subir ambiente incompleto.

## Backup local com script

O script le a connection string de `STAGING_DATABASE_URL` ou, como alternativa, de `ConnectionStrings__DefaultConnection`.

```powershell
$env:STAGING_DATABASE_URL="postgresql://USUARIO:SENHA@HOST.neon.tech/CASA_MULHER?sslmode=require"
.\scripts\backup-postgres.ps1
```

Se o Windows bloquear a execucao direta de `.ps1`, rode:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\backup-postgres.ps1
```

Ele gera um arquivo em `backups/`, por exemplo:

```text
backups/casamulher-postgres-20260602-063000.zip
```

O arquivo contem um dump SQL gerado por `pg_dump`, compactado em `.zip`. A pasta `backups/` esta no `.gitignore`.

Requisitos locais:

- `pg_dump` instalado e disponivel no `PATH`;
- connection string configurada;
- permissao de leitura no banco remoto.

## Restore local com script

Restauracao deve ser feita com cuidado. O ideal e restaurar em um banco vazio ou em um novo branch/projeto de teste antes de mexer no banco principal.

```powershell
$env:STAGING_DATABASE_URL="postgresql://USUARIO:SENHA@HOST.neon.tech/CASA_MULHER_RESTORE?sslmode=require"
.\scripts\restore-postgres.ps1 -BackupPath .\backups\casamulher-postgres-20260602-063000.zip
```

Se o Windows bloquear a execucao direta de `.ps1`, rode:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\restore-postgres.ps1 -BackupPath .\backups\casamulher-postgres-20260602-063000.zip
```

O script pede confirmacao digitando `RESTAURAR`. Para automacao controlada, existe `-Force`, mas evite usar sem ter certeza do destino.

Requisitos locais:

- `psql` instalado e disponivel no `PATH`;
- backup `.zip`, `.sql.gz` ou `.sql`;
- banco de destino conferido.

## Backup pelo GitHub Actions

O workflow fica em:

```text
.github/workflows/backup-postgres.yml
```

Ele roda:

- manualmente por `workflow_dispatch`;
- diariamente as 03:30 no horario de Brasilia, configurado como `06:30 UTC`.

Configure este secret no GitHub:

```text
STAGING_DATABASE_URL
```

No repositorio:

```text
Settings -> Secrets and variables -> Actions -> New repository secret
```

O workflow:

- instala o cliente PostgreSQL;
- roda `pg_dump`;
- compacta o `.sql` em `.gz`;
- envia como artifact com retencao curta.

O GitHub Actions aceita workflows agendados e `upload-artifact` permite configurar `retention-days`, conforme a documentacao oficial:

- https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax
- https://docs.github.com/en/actions/tutorials/store-and-share-data

## Limites e cuidados

- Backups em artifact sao temporarios, nao cofre permanente.
- Nunca commite dump puro do banco.
- Se precisar guardar backup por mais tempo, use armazenamento privado e criptografia.
- `pg_dump` e bom para backup logico, mas nao substitui uma estrategia profissional para producao critica.
- Teste restore periodicamente. Backup que nunca foi restaurado ainda e uma promessa.
- A auditoria interna ajuda a entender quem fez o que, mas nao substitui restore de banco.

## Fontes uteis

- Neon pricing: https://neon.com/pricing
- PostgreSQL `pg_dump`: https://www.postgresql.org/docs/current/app-pgdump.html
- PostgreSQL continuous archiving/PITR: https://www.postgresql.org/docs/current/continuous-archiving.html
- CouchDB conflitos de replicacao: https://docs.couchdb.org/en/stable/replication/conflicts.html
- rqlite design/Raft: https://rqlite.io/docs/design/
- LiteFS funcionamento: https://fly.io/docs/litefs/how-it-works/
- GitHub Actions workflow syntax: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax
- GitHub Actions artifacts: https://docs.github.com/en/actions/tutorials/store-and-share-data
