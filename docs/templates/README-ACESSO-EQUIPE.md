# Comece aqui - Equipe do Projeto

Este repositorio privado e o ponto fixo de entrada da equipe do projeto Sistema Casa da Mulher.

Nao publique senhas, tokens, hashes, Client Secrets ou dados reais aqui.

## Portal central EQP

Portal da equipe:

```text
https://casa-mulher-eqp.onrender.com/equipe.html
```

Fluxo:

1. Entrar com GitHub.
2. Ativar um EQP disponivel ou reservado.
3. Informar e-mail de recuperacao e criar uma senha propria para este projeto.
4. Receber o ADM pareado automaticamente.
5. Abrir ambiente local ou Codespaces.
6. Aguardar a sincronização automática da API.
7. Fazer login com EQP ou ADM pareado.

## Sincronizar depois da ativação

A API sincroniza na inicialização e repete a atualização a cada minuto. Para forçar no Windows:

```powershell
.\casa_da_mulher.cmd equipe sync
```

Para forçar no Codespaces:

```text
Casa da Mulher: sincronizar equipe
```

## Links principais

- Repositorio principal:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER

- Comece Aqui:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER/blob/main/docs/COMECE-AQUI-EQUIPE.md

- Contas e convites EQP:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER/blob/main/docs/EQUIPE-E-CONVITES.md

- Codespaces:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER/blob/main/docs/CODESPACES-PARA-COLABORADORAS.md

- Pull Request:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER/blob/main/docs/GUIA-RAPIDO-PR.md

## Fonte privada da equipe

```text
data/equipe-db.json
data/equipe-events.ndjson
data/equipe-db.example.json
data/README.md
```

O portal grava `equipe-db.json` por commit automatico. Nao edite manualmente sem entender o fluxo.

Os campos privados `email`, `emailRecuperacao` e `emailRecuperacaoConfirmado` permitem reconstruir contas de homologacao sem trocar e-mail real por placeholder. `@equipe.local` e apenas fallback tecnico.

## Seguranca

- Nao use senha pessoal.
- Nao publique token.
- Nao publique hash fora deste repositorio privado.
- Nao publique `appsettings` real.
- Nao publique dados reais.
- Use dados ficticios nos testes e prototipos.
