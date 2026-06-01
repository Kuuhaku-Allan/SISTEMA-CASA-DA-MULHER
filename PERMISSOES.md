# Matriz de Permissoes

Este documento organiza o que cada perfil pode acessar no sistema Casa da Mulher.

Importante: esconder botoes no front-end melhora a usabilidade, mas nao e a seguranca principal. A seguranca real fica no back-end, com policies e autorizacao por perfil.

## Perfis

| Perfil | Uso esperado |
| --- | --- |
| `adm` | Coordenacao/administracao, com acesso total. |
| `recepcao` | Primeiro atendimento, cadastro inicial e triagem sem prontuarios confidenciais. |
| `professor` | Atividades de curso, frequencia e informacoes educacionais. |
| `as_social` | Acompanhamento social/psicologico e prontuario social. |
| `juridico` | Atendimento juridico e orientacao legal. |

## Matriz

| Funcionalidade | ADM | Recepcao | Professor | Social/Psicologia | Juridico |
| --- | --- | --- | --- | --- | --- |
| Criar convites de funcionarios | Sim | Nao | Nao | Nao | Nao |
| Gerenciar funcionarios | Sim | Nao | Nao | Nao | Nao |
| Ver historico de acoes | Sim | Nao | Nao | Nao | Nao |
| Cadastrar usuaria/atendimento inicial | Sim | Sim | Nao | Sim | Sim |
| Ver dados basicos | Sim | Sim | Sim | Sim | Sim |
| Ver prontuario social | Sim | Nao | Nao | Sim | Nao |
| Ver atendimento juridico | Sim | Nao | Nao | Nao | Sim |
| Ver frequencia/curso | Sim | Nao | Sim | Nao | Nao |
| Gerar relatorios gerais | Sim | Nao | Nao | Sim | Sim |

## Policies do back-end

| Policy | Perfis permitidos | Uso |
| --- | --- | --- |
| `SomenteAdm` | `adm` | Administracao, convites, funcionarios e historico de acoes. |
| `AcessoRecepcao` | `adm`, `recepcao` | Cadastro inicial, triagem e recepcao. |
| `AcessoCursos` | `adm`, `professor` | Cursos, frequencia e atividades educacionais. |
| `AcessoProntuarioSocial` | `adm`, `as_social` | Prontuario social e acompanhamento. |
| `AcessoJuridico` | `adm`, `juridico` | Atendimento juridico. |
| `AcessoRelatorios` | `adm`, `as_social`, `juridico` | Relatorios gerais permitidos a coordenacao e areas tecnicas. |

## Areas do front-end

O helper `CasaMulherAuth.podeAcessar(area)` usa as mesmas areas conceituais:

| Area | Perfis permitidos |
| --- | --- |
| `convites` | `adm` |
| `funcionarios` | `adm` |
| `auditoria` | `adm` |
| `recepcao` | `adm`, `recepcao` |
| `cursos` | `adm`, `professor` |
| `social` | `adm`, `as_social` |
| `juridico` | `adm`, `juridico` |
| `relatorios` | `adm`, `as_social`, `juridico` |
