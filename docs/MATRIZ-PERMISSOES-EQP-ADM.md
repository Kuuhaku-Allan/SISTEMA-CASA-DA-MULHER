# Matriz de permissoes EQP/ADM

Esta matriz descreve a regra implementada no codigo atual.

Contas `EQP` sao da equipe do projeto. Elas nao representam funcionarias reais da Casa da Mulher.

## Masters configurados

Valores padrao em `Seguranca:Master`:

- `SuperAdminIdentificador`: `ADM-000003`
- `EquipeOwnerCodigo`: `EQP-000001`

Esses valores podem ser ajustados por configuracao de ambiente, sem commitar segredo.

## Papeis

- `super_admin/master`: ADM configurado como master, por padrao `ADM-000003`.
- `adm institucional comum`: perfil `adm`, mas nao e o master configurado.
- `equipe_owner`: membro EQP com papel `owner`.
- `equipe_maintainer`: membro EQP com papel `maintainer`.
- `equipe_contributor`: membro EQP com papel `contributor`.
- `recepcao`: perfil institucional de recepcao.

## Matriz

| Acao | super_admin/master | adm institucional comum | equipe_owner | equipe_maintainer | equipe_contributor | recepcao |
| --- | --- | --- | --- | --- | --- | --- |
| Ver funcionarios institucionais | Sim | Sim | Somente `EQP-000001` | Nao | Nao | Nao |
| Alterar funcionario institucional nao-ADM | Sim | Sim | Somente `EQP-000001` | Nao | Nao | Nao |
| Alterar/desativar outro ADM | Sim, exceto remover o master configurado | Nao | Nao | Nao | Nao | Nao |
| Resetar senha institucional | Sim | Sim para nao-ADM | Somente `EQP-000001` para nao-ADM | Nao | Nao | Nao |
| Resetar autenticador institucional | Sim | Sim para nao-ADM | Somente `EQP-000001` para nao-ADM | Nao | Nao | Nao |
| Ver membros EQP | Sim | Sim, pela politica de acesso, sem gerir | Sim | Sim | Sim | Nao |
| Alterar membro EQP | Sim | Nao | Sim, exceto `EQP-000001` | Nao | Nao | Nao |
| Desativar membro EQP | Sim | Nao | Sim, exceto `EQP-000001` e ultimo owner/master | Nao | Nao | Nao |
| Resetar senha EQP sem e-mail | Sim | Nao | Sim, exceto `EQP-000001` | Nao | Nao | Nao |
| Resetar autenticador EQP | Nao implementado nesta etapa | Nao | Nao | Nao | Nao | Nao |
| Ver logs institucionais | Sim | Sim | Somente `EQP-000001` em desenvolvimento/staging | Nao | Nao | Nao |
| Ver logs de equipe | Sim | Sim, se acessar area equipe | Sim | Sim | Apenas proprios | Nao |
| Criar convites institucionais | Sim | Sim | Somente `EQP-000001` | Nao | Nao | Nao |
| Criar convites EQP | Sim | Nao | Sim | Sim, se permitido | Nao | Nao |

## Regras de seguranca

- EQP nao aparece em funcionarios, convites, auditoria ou e-mails institucionais.
- Endpoints institucionais tambem recusam acesso direto a contas EQP.
- Membro comum de EQP nao altera ADM nem outro EQP.
- ADM comum nao altera EQP e nao altera outro ADM.
- Ninguem deve remover/desativar o ultimo owner/super admin ativo.
- Botoes perigosos podem ser escondidos no front, mas a regra real fica no back-end.

## Validacao

Com a API local rodando, use:

```bash
node scripts/validar-regras-eqp.mjs
```

Para validar logins reais, defina variaveis:

```bash
CASA_MULHER_MASTER_ID=ADM-000003
CASA_MULHER_MASTER_SENHA=...
CASA_MULHER_EQP_OWNER_ID=EQP-000001
CASA_MULHER_EQP_OWNER_SENHA=...
CASA_MULHER_EQP_COMUM_ID=EQP-000002
CASA_MULHER_EQP_COMUM_SENHA=...
CASA_MULHER_ADM_COMUM_ID=ADM-000004
CASA_MULHER_ADM_COMUM_SENHA=...
CASA_MULHER_RECEPCAO_ID=REC-000001
CASA_MULHER_RECEPCAO_SENHA=...
```

Se faltar alguma conta, o script imprime `MANUAL` com o que ainda precisa ser conferido.
