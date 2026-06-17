# Ponto de entrada privado da equipe

O repositório principal pode ser público, mas o acesso real da equipe deve ficar em um repositório privado da organização.

Repositório privado definido:

```text
Sistema-Casa-da-Mulher/ACESSO-EQUIPE
```

Esse repositório privado é o link permanente da equipe. Ele não substitui a aplicação rodando; ele orienta cada pessoa a abrir a Área da Equipe no ambiente correto.

## Por que não usar uma URL única da tela real

A Área da Equipe depende de API e banco rodando.

Em Codespaces, cada pessoa tem:

- uma URL temporária própria;
- um banco local próprio;
- um ambiente criado no fork ou no repositório que ela abriu.

Sem uma API/banco central sempre ligado, não existe uma URL única permanente para ativação EQP.

Por isso, o link permanente deve apontar para o `ACESSO-EQUIPE`, e a tela real deve ser aberta dentro do Codespaces/local de cada pessoa.

## O que deve ficar no repositório privado

- Link do repositório principal.
- Guia `Comece Aqui` da equipe.
- Guia de contas e convites EQP.
- Guia de fork e Codespaces.
- Guia de Pull Request.
- Orientação para `prototipos/`.
- Comandos para abrir a Área da Equipe.
- Aviso sobre como pedir o código EQP.

## Fluxo correto

Para Allan/mantenedor:

```powershell
.\casa_da_mulher.cmd equipe
.\casa_da_mulher.cmd equipe bootstrap
```

Para colaboradoras no Codespaces:

```text
Casa da Mulher: iniciar sistema
Casa da Mulher: abrir área da equipe
```

Depois, a pessoa clica em `Ativar meu EQP`.

## O que não deve ficar público

Códigos EQP são individuais e não devem ser publicados no repositório principal.

Também não publicar:

- senhas;
- tokens;
- `ClientSecret`;
- PAT GitHub;
- `appsettings` real;
- banco local;
- dados reais de atendimento.

Códigos EQP devem ser entregues individualmente ou registrados apenas em local privado da equipe.
