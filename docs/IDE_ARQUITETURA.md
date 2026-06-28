# Arquitetura Técnica da IDE da Equipe

Este documento é voltado para os mantenedores do projeto e descreve o fluxo de dados, arquivos e proteções arquiteturais da IDE embarcada na Casa da Mulher.

---

## 1. Arquivos Principais

O fluxo da IDE se divide entre o Frontend estático no navegador e a API segura no Backend:

**Frontend**
- `equipe-ide.html`: O layout principal, contendo o CodeMirror, explorador, painéis laterais (como *Tarefas Guiadas* e *Mapa do Projeto*) e o modal de Revisão.
- `equipe-ide.css`: Estilização e layout responsivo.
- `equipe-ide.js`: Gerenciador de estado local (`rascunhoAtual` em LocalStorage), manipulador do editor e fetch para API de Envio de PRs. Modelos iniciais, definição do catálogo de **Tarefas Guiadas** e estrutura do **Mapa do Projeto (MAPA_PROJETO_IDE)** ficam aqui.

**Backend (API C#)**
- `EquipeIdeGitHubController`: Ponto de entrada de requisições. Expõe as rotas `/api/equipe-ide/github/preparar-revisao`, valida o auth e roteia para os serviços adequados.
- `ManualTokenGitHubIdeService`: Roda quando o usuário envia via "Modo seguro da equipe". Utiliza o GitHub App/Token em servidor.
- `GitHubForkIdeService`: Roda quando o usuário envia via "Fork Pessoal". Utiliza o Octokit com o OAuth token autenticado do usuário para criar branches remotos.
- `IdeContentSanitizer`: Serviço crítico de normalização e segurança.

---

## 2. Tarefas Guiadas, Modelos e Mapa do Projeto

A IDE adota o conceito de "Tarefas Guiadas" e "Mapa do Projeto" como contexto informativo. A UI do frontend (`equipe-ide.js`) dita o tipo de contribuição e limitações cognitivas, mas **nunca serve de barreira de segurança real**.
- Tarefa = O objetivo do trabalho e o checklist associado.
- Modelo = O payload de arquivos iniciais.
- Área do Projeto = Um descritivo sobre o escopo que está sendo afetado (Fase 3). Ajuda o revisor a saber para onde o rascunho se destina, mas não concede permissão de edição aos arquivos oficiais daquela área.
- O Backend apenas armazena esses dados passivamente (DTO) no `README.md` e corpo do PR gerados.

---

## 3. Fluxos de Envio (Pull Request)

### 3.1. Fluxo do Modo Seguro da Equipe
1. O payload é recebido pelo Controller (`GitHubIdeAreaProjetoDto` incluso).
2. É despachado ao `ManualTokenGitHubIdeService`.
3. Os dados de tarefa (`Titulo`, `Tipo`, `Escopo`), a `AreaProjeto` relacionada e o `Checklist` são apensados no README da contribuição e no corpo do Pull Request.
4. Os arquivos entram no GitHub pela conta de serviço, direto na branch isolada.

### 3.2. Fluxo do Fork Pessoal
1. Semelhante ao fluxo acima, mas a API lê a chave de OAuth criptografada do usuário.
2. Um fork é criado na conta do usuário no GitHub (caso não exista).
3. Uma branch é despachada. O PR é criado e assinado com o token pessoal da colaboradora.

---

## 4. Segurança e Sanitização

> [!CAUTION]
> **Tarefa Guiada não é barreira de segurança.** Toda verificação final é ditada pela API.

### 4.1 Por que o Token GitHub nunca fica no Frontend?
O frontend só manda o comando. Todo o fluxo com o Octokit ou as instâncias de GitHub client residem dentro da CasaMulher.Api. Os tokens de integração nunca trafegam em JSONs de resposta. Segredos (OAuth) são preservados no banco com data-protection.

### 4.2 Fluxo de Sanitização (`IdeContentSanitizer`)
Nenhum arquivo ou payload sobe cru.
- **Normalização de quebra de linha**: Todos os `\r` ou `\r\n` viram um `\n` padronizado.
- **Remoção de caracteres de controle e formatação Unicode (Bidi/Hidden)** para evitar vulnerabilidades visuais na review do GitHub.
- **Metadata**: Título e descrições do PR usam uma regex estrita (`SanitizarTextoCurtoIde`) para evitar headers maliciosos e supressão de whitespace indevido.
- **Conversão Base64**: Todo o payload para o GitHub blob entry passa por decodificação limpa em UTF-8 antes do Base64.

### 4.3 Proteção da Pasta Segura
Por design estrutural nos controllers e services, todo envio obrigatoriamente cai sob o namespace `projetocasadamulher/telas/ide-rascunhos/{NOME_DA_TELA}/`.
É impossível (backend não suporta via regex ou model validation) enviar arquivos subindo de diretório (Path Traversal) para reescrever arquivos raiz como `Program.cs`.

---

## 5. Cuidados para manutenção futura

- Qualquer edição nas propriedades obrigatórias de `GitHubIdeRevisaoRequest` causará erro no parse JSON na chamada `apiFetch` do frontend caso não sejam alinhadas.
- O catálogo de tarefas e o `MAPA_PROJETO_IDE` estão *hardcoded* em JavaScript nas fases atuais (Fase 2 e 3) para facilitar a usabilidade. O backend apenas confia e sanitiza (via `SanitizarTextoCurtoIde`) os metadados. Numa futura transição de integração de *GitHub Issues* (Fase 4), será recomendado espelhar IDs de Issues reais e carregar o mapa do projeto dinamicamente a partir do repositório.
- Configurações de acesso (como o webhook e o Secret do OAuth Pessoal) ficam no `.env` (quando local) e no cofre do servidor de implantação.
