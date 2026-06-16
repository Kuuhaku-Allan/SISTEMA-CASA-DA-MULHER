# Como mexer no projeto sem instalar nada

Este guia e para quem vai ajudar criando telas, ajustando HTML/CSS/JavaScript ou melhorando prototipos do Sistema Casa da Mulher.

Voce nao precisa instalar .NET, banco, Git ou VS Code no computador. O trabalho pode ser feito pelo navegador usando GitHub Codespaces.

## 1. Fazer fork

1. Entre no repositorio principal:
   `https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER`
2. Clique em `Fork`.
3. Crie o fork na sua propria conta.
4. Depois do fork, voce tera uma copia do projeto no seu GitHub.

Nao mexa direto no repositorio principal. A sua alteracao vai voltar por Pull Request.

## 2. Abrir Codespace

1. Entre no seu fork.
2. Clique em `Code`.
3. Clique em `Codespaces`.
4. Clique em `Create codespace`.
5. Espere abrir o VS Code no navegador.

Na primeira abertura, o Codespaces prepara o ambiente automaticamente. Pode demorar alguns minutos.

## 3. Iniciar o sistema

1. No VS Code Web, abra `Terminal`.
2. Clique em `Run Task`.
3. Escolha `Casa da Mulher: iniciar sistema`.
4. Quando o GitHub mostrar a porta `5500`, abra essa porta no navegador.

Se o ambiente for novo, crie um acesso demo primeiro:

1. Abra `cadastro.html`.
2. Use:
   - e-mail: `recepcao@casamulher.local`
   - codigo: `REC-2026`
   - senha sugerida: `Senha@123`
3. Depois do cadastro, use o ID gerado para fazer login.

## 4. Editar tela

As telas ficam em:

```text
projetocasadamulher/telas/
```

Voce pode mexer em arquivos `.html`, `.css` e `.js` combinados com o mantenedor.

Evite mexer nestas pastas sem combinar antes:

- `CasaMulher.Api/`
- `scripts/`
- `.github/`
- `.devcontainer/`

## 5. Enviar para revisao

Quando terminar:

1. Abra `Terminal`.
2. Clique em `Run Task`.
3. Escolha `Casa da Mulher: enviar Pull Request`.
4. Escreva uma mensagem curta, por exemplo:
   `Adiciona tela de agendamento`
5. Copie o link do Pull Request.
6. Mande o link no grupo.

O mantenedor vai revisar, pedir ajuste se precisar e depois aprovar.

## 6. Regras de seguranca

Nao envie:

- senha;
- token;
- banco local;
- arquivo `appsettings` real;
- print com e-mail real ou dado sensivel;
- dado real de atendimento.

Use dados ficticios nos testes.
