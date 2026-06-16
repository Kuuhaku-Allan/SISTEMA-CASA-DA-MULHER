# Guia rapido de Pull Requests

Este guia e para o mantenedor revisar contribuicoes sem deixar a `main` virar uma zona de testes.

## Revisar PR

1. Abra a aba `Pull requests`.
2. Entre no PR recebido.
3. Leia a descricao.
4. Abra `Files changed`.
5. Confira se a alteracao esta dentro do combinado.
6. Procure arquivos sensiveis ou estranhos.

Pontos de atencao:

- senha, token ou segredo;
- `appsettings` real;
- banco local;
- arquivos temporarios;
- dados reais em prints;
- alteracoes em `CasaMulher.Api/` quando a pessoa deveria mexer apenas em tela.

## Testar PR

Opcoes:

- usar o botao de Codespaces do proprio PR;
- rodar localmente com `gh pr checkout NUMERO_DO_PR`;
- baixar o fork da colaboradora, se necessario.

Para testar no Codespaces:

1. Abra o PR.
2. Use a opcao de abrir Codespace na branch do PR, quando disponivel.
3. Rode a tarefa `Casa da Mulher: iniciar sistema`.
4. Abra a porta `5500`.
5. Confira a tela alterada.

## Pedir alteracoes

Use comentarios objetivos:

- explique o problema;
- diga o arquivo ou trecho;
- sugira o ajuste;
- evite mensagens vagas como "arrumar layout".

Exemplo:

```text
O CPF esta aceitando numeros invalidos. Adicione validacao antes de salvar.
```

## Fazer merge

Faca merge quando:

- a alteracao estiver dentro do combinado;
- o PR estiver testado;
- nao houver segredo;
- a validacao basica do GitHub Actions estiver verde ou o erro estiver entendido;
- conversas importantes estiverem resolvidas.

Depois do merge, apague a branch se ela nao for mais usada.

## Fechar PR ruim sem constrangimento

Se o PR ficou longe demais do combinado:

1. agradeca a tentativa;
2. explique que o caminho sera refeito;
3. feche o PR sem fazer merge;
4. oriente a pessoa a abrir outro com escopo menor.

## Orientar colaboradoras

Para quem nao conhece Git:

- pedir para trabalhar sempre no fork;
- orientar a usar Codespaces;
- pedir alteracoes pequenas;
- revisar com calma;
- transformar erro em checklist para o proximo PR.
