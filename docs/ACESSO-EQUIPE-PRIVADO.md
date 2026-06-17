# Ponto de entrada privado da equipe

O repositorio principal pode ser publico, mas o acesso real da equipe deve ficar em um repositorio privado da organizacao.

Repositorio privado definido:

```text
Sistema-Casa-da-Mulher/ACESSO-EQUIPE
```

Esse repositorio privado deve ser o link permanente da equipe. Ele nao substitui a tela `equipe-ativar.html`; ele explica como cada pessoa abre a tela no ambiente correto.

## Por que nao usar um link unico para equipe-ativar.html

A tela `projetocasadamulher/telas/equipe-ativar.html` depende da API e do banco rodando.

Em Codespaces, cada pessoa tera:

- uma URL temporaria propria;
- um banco local proprio;
- um ambiente criado no fork ou no repositorio que ela abriu.

Sem uma API/banco central sempre ligado, nao existe uma URL unica permanente para ativacao EQP.

Por isso, o link permanente deve apontar para o guia privado `ACESSO-EQUIPE`, e a tela real deve ser aberta dentro do Codespaces/local de cada pessoa.

## O que deve ficar no repositorio privado

- Link do repositorio principal.
- Guia "Comece Aqui" da equipe.
- Guia de ativacao EQP.
- Guia de fork e Codespaces.
- Guia de Pull Request.
- Orientacao para area `prototipos/`.
- Instrucao para abrir `equipe-ativar.html` no ambiente local/Codespaces.
- Aviso sobre como pedir o codigo EQP.

## O que nao deve ficar publico

Codigos EQP sao individuais e nao devem ser publicados no repositorio principal.

Tambem nao publicar:

- senhas;
- tokens;
- `ClientSecret`;
- PAT GitHub;
- `appsettings` real;
- banco local;
- dados reais de atendimento.

Codigos EQP devem ser entregues individualmente ou registrados apenas em local privado da equipe.
