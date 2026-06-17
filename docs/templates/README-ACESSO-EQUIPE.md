# Comece aqui - Equipe do Projeto

Este repositório privado é o ponto fixo de entrada da equipe do projeto Sistema Casa da Mulher.

Não publique códigos EQP, senhas, tokens ou dados reais aqui.

## Links principais

- Repositório principal:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER

- Guia Comece Aqui:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER/blob/main/docs/COMECE-AQUI-EQUIPE.md

- Contas e convites EQP:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER/blob/main/docs/EQUIPE-E-CONVITES.md

- Guia Codespaces:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER/blob/main/docs/CODESPACES-PARA-COLABORADORAS.md

- Guia Pull Request:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER/blob/main/docs/GUIA-RAPIDO-PR.md

- Matriz de permissões:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER/blob/main/docs/MATRIZ-PERMISSOES-EQP-ADM.md

## Se você é Allan/mantenedor

1. Abra o projeto na sua IDE.
2. Execute:

   ```powershell
   .\casa_da_mulher.cmd equipe
   ```

3. A Área da Equipe abrirá automaticamente.
4. Se ainda não tiver EQP, execute:

   ```powershell
   .\casa_da_mulher.cmd equipe bootstrap
   ```

5. Use o `EQP-000001` e o código gerado para ativar sua conta.

## Se você já tem fork

1. Abra seu fork no Codespaces.
2. Rode a task `Casa da Mulher: iniciar sistema`.
3. Rode a task `Casa da Mulher: abrir área da equipe`.
4. Clique em `Ativar meu EQP`.
5. Use o ID e o código enviados pelo mantenedor.
6. Trabalhe em `prototipos/` e envie Pull Request.

## Se você ainda não tem fork

1. Crie seu fork pelo guia.
2. Abra Codespaces no seu fork.
3. Rode `Casa da Mulher: iniciar sistema`.
4. Rode `Casa da Mulher: abrir área da equipe`.
5. Clique em `Ativar meu EQP`.
6. Crie protótipos e envie Pull Request.

## Como ativar meu EQP

1. Peça seu ID EQP e código individual para o mantenedor.
2. Abra a Área da Equipe no seu ambiente local ou Codespaces.
3. Clique em `Ativar meu EQP`.
4. Informe ID, código, nome e senha.
5. Depois faça login normalmente com o ID EQP e a senha criada.

Importante: não existe uma URL única permanente para a tela real sem uma API central. Em Codespaces, cada pessoa tem sua própria URL temporária.

## Área de protótipos

Protótipos ficam no repositório principal, dentro de:

```text
prototipos/
```

Cada pessoa deve trabalhar em:

```text
prototipos/colaboradores/seu-github/nome-da-tela/
```

PRs vindos de fork são bloqueados se alterarem arquivos fora de `prototipos/`.

## Segurança

- Não publicar código de ativação EQP.
- Não publicar senha.
- Não publicar token.
- Não publicar `appsettings` real.
- Não publicar dados reais.
- Usar dados fictícios nos testes e protótipos.
