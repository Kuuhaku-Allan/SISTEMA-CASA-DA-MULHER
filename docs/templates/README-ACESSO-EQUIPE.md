# Comece aqui - Equipe do Projeto

Este repositorio privado e o ponto fixo de entrada da equipe do projeto Sistema Casa da Mulher.

Nao publique codigos EQP, senhas, tokens ou dados reais aqui.

## Links principais

- Repositorio principal:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER

- Guia Comece Aqui:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER/blob/main/docs/COMECE-AQUI-EQUIPE.md

- Guia de ativacao EQP:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER/blob/main/docs/EQUIPE-E-CONVITES.md

- Guia Codespaces:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER/blob/main/docs/CODESPACES-PARA-COLABORADORAS.md

- Guia Pull Request:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER/blob/main/docs/GUIA-RAPIDO-PR.md

- Matriz de permissoes:
  https://github.com/Sistema-Casa-da-Mulher/SISTEMA-CASA-DA-MULHER/blob/main/docs/MATRIZ-PERMISSOES-EQP-ADM.md

## Como ativar meu EQP

1. Peca seu ID EQP e codigo individual para o mantenedor.
2. Abra seu Codespaces ou ambiente local.
3. Inicie o sistema.
4. Abra `projetocasadamulher/telas/equipe-ativar.html`.
5. Informe ID, codigo, nome e senha.
6. Depois faca login normalmente com o ID EQP e a senha criada.

Importante: `equipe-ativar.html` nao tem uma URL unica permanente sem uma API central. Em Codespaces, cada pessoa tera sua propria URL temporaria.

## Qual fluxo devo usar?

### Allan / mantenedor

- usa IDE local;
- nao precisa fork;
- nao precisa Codespaces;
- pode mexer nos arquivos principais;
- revisa Pull Requests;
- integra prototipos quando fizer sentido.

### Colaborador que ja tem fork

- abre Codespaces no proprio fork;
- cria prototipo em `prototipos/`;
- envia Pull Request.

### Colaboradora que ainda nao tem fork

- cria fork do repositorio principal;
- abre Codespaces no fork;
- cria prototipo em `prototipos/`;
- envia Pull Request.

## Area de prototipos

Prototipos ficam no repositorio principal, dentro de:

```text
prototipos/
```

Cada pessoa deve trabalhar em:

```text
prototipos/colaboradores/seu-github/nome-da-tela/
```

PRs vindos de fork sao bloqueados se alterarem arquivos fora de `prototipos/`.

## Seguranca

- Nao publicar codigo de ativacao EQP no repositorio publico.
- Nao publicar senha.
- Nao publicar token.
- Nao publicar `appsettings` real.
- Nao publicar dados reais.
- Usar dados ficticios nos testes e prototipos.
