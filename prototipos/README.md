# Area segura de prototipos

Esta pasta existe para colaboradoras criarem telas e ideias sem mexer nos arquivos principais do sistema.

Use sempre:

```text
prototipos/colaboradores/SEU-USUARIO/tela-nome/
```

Regras:

- use dados ficticios;
- nao coloque senha, token, documento real ou informacao sensivel;
- nao chame endpoints reais sem combinar com o mantenedor;
- prototipos nao entram automaticamente em `projetocasadamulher/telas/`;
- a integracao com o sistema principal sera feita depois pelo mantenedor.

No Codespaces, rode a tarefa:

```text
Casa da Mulher: criar novo prototipo
```

Depois abra:

```text
prototipos/index.html
```

Pull Requests vindos de fork devem alterar somente arquivos dentro de `prototipos/`.
