/**
 * IDE da Equipe - Fase 1
 * Script responsável pelo editor local, preview isolado e exportação de tela.
 */

document.addEventListener('DOMContentLoaded', async () => {
    // 1. Proteção de Rota (apenas Equipe e Adm)
    const usuario = CasaMulherAuth.getUsuario();
    
    if (!usuario) {
        window.location.href = 'index.html';
        return;
    }

    if (usuario.perfil !== 'equipe' && usuario.perfil !== 'adm') {
        window.location.href = CasaMulherAuth.getPainelUrl(usuario);
        return;
    }

    // Se passou, inicializa a tela
    document.getElementById('equipeIdePage').style.display = 'flex';
    
    // Atualiza cabeçalho de sessão compacto
    const headerNome = document.getElementById('headerNome');
    const headerPerfil = document.getElementById('headerPerfil');
    const headerAvatar = document.getElementById('headerAvatar');
    const dropdownEmail = document.getElementById('dropdownEmail');
    const dropdownId = document.getElementById('dropdownId');
    
    if (usuario) {
        const userNome = usuario.nome || usuario.nomeCompleto || 'Equipe';
        if (headerNome) headerNome.textContent = userNome;
        if (headerPerfil) headerPerfil.textContent = (usuario.perfil || 'equipe').toUpperCase();
        if (headerAvatar) {
            const nomes = userNome.split(' ');
            let iniciais = nomes[0].charAt(0);
            if (nomes.length > 1) iniciais += nomes[nomes.length - 1].charAt(0);
            headerAvatar.textContent = iniciais.toUpperCase();
        }
        if (dropdownEmail) dropdownEmail.textContent = usuario.email || 'Email não informado';
        if (dropdownId) dropdownId.textContent = usuario.identificadorFuncionario || 'ID não informado';
    }

    const btnToggleSession = document.getElementById('btnToggleSession');
    const sessionDropdown = document.getElementById('sessionDropdown');
    const btnSair = document.getElementById('btnSair');

    if (btnToggleSession && sessionDropdown) {
        btnToggleSession.addEventListener('click', (e) => {
            e.stopPropagation();
            sessionDropdown.classList.toggle('hidden');
            const isExpanded = !sessionDropdown.classList.contains('hidden');
            btnToggleSession.setAttribute('aria-expanded', isExpanded);
        });

        document.addEventListener('click', (e) => {
            if (!sessionDropdown.contains(e.target) && !btnToggleSession.contains(e.target)) {
                sessionDropdown.classList.add('hidden');
                btnToggleSession.setAttribute('aria-expanded', 'false');
            }
        });
    }

    if (btnSair) {
        btnSair.addEventListener('click', () => {
            if (typeof CasaMulherAuth.logout === 'function') {
                CasaMulherAuth.logout("Sessão encerrada com sucesso.");
            }
        });
    }

    // 2. Chave de Rascunho por Usuário
    const DRAFT_KEY = "ide_casa_mulher_draft";
    
    // MAPA DO PROJETO - FASE 3
    const MAPA_PROJETO_IDE = [
        {
            id: "login",
            nome: "Login",
            descricao: "Tela de autenticação e portais de entrada.",
            quemUsa: "Todos os usuários.",
            perfil: "Todos",
            status: "estavel",
            arquivosPrincipais: ["index.html", "auth.js", "style.css"],
            arquivosRelacionados: ["recuperar-acesso.html"],
            endpointsRelacionados: ["/api/auth/login"],
            cuidados: ["Não alterar form de login.", "Não expor senhas no front."],
            comoTestar: ["Testar login com credenciais válidas e inválidas."],
            observacoes: ["Área crítica."],
            dependeBackend: true
        },
        {
            id: "painel-adm",
            nome: "Painel ADM",
            descricao: "Área de administração geral do sistema.",
            quemUsa: "Administradores",
            perfil: "ADM",
            status: "estavel",
            arquivosPrincipais: ["painel-adm.html", "painel-adm.js", "style.css"],
            arquivosRelacionados: ["equipe.html", "funcionarios.html"],
            endpointsRelacionados: ["/api/dashboard/adm"],
            cuidados: ["Evitar carregamento pesado de gráficos.", "Proteção de rota obrigatória."],
            comoTestar: ["Acessar via login de ADM."],
            observacoes: ["Nesta fase, o mapa é apenas informativo."],
            dependeBackend: true
        },
        {
            id: "recepcao",
            nome: "Recepção",
            descricao: "Área operacional usada para cadastro, busca e atendimento inicial.",
            quemUsa: "Equipe de recepção e administração",
            perfil: "REC / ADM",
            status: "estavel",
            arquivosPrincipais: [
                "projetocasadamulher/telas/recepcao.html",
                "projetocasadamulher/telas/recepcao.js",
                "projetocasadamulher/telas/recepcao.css"
            ],
            arquivosRelacionados: [
                "projetocasadamulher/telas/recepcao-coordenacao.html",
                "projetocasadamulher/telas/app.js",
                "projetocasadamulher/telas/auth.js"
            ],
            endpointsRelacionados: [
                "/api/recepcao",
                "/api/acolhimentos"
            ],
            cuidados: [
                "Não alterar fluxo de login sem revisar auth.js.",
                "Não remover IDs usados pelo JavaScript.",
                "Evitar tabelas largas e scroll horizontal.",
                "Não usar dados reais em rascunhos."
            ],
            comoTestar: [
                "Entrar com perfil REC ou ADM.",
                "Abrir a tela de recepção.",
                "Testar busca, cadastro e navegação principal."
            ],
            observacoes: [
                "Nesta fase, o mapa é apenas informativo."
            ],
            dependeBackend: true
        },
        {
            id: "coord-recepcao",
            nome: "Coordenação da Recepção",
            descricao: "Área de gerência para a equipe de recepção.",
            quemUsa: "Coordenadores",
            perfil: "COORD_REC / ADM",
            status: "em evolucao",
            arquivosPrincipais: ["recepcao-coordenacao.html"],
            arquivosRelacionados: ["recepcao.html"],
            endpointsRelacionados: [],
            cuidados: ["Dados sensíveis de atendimento."],
            comoTestar: ["Login como Coordenador."],
            observacoes: [],
            dependeBackend: true
        },
        {
            id: "professor",
            nome: "Professor",
            descricao: "Área para lançamento de notas e presenças.",
            quemUsa: "Professores",
            perfil: "PRO / ADM",
            status: "estavel",
            arquivosPrincipais: ["professor.html", "professor.js"],
            arquivosRelacionados: [],
            endpointsRelacionados: ["/api/professor/cursos"],
            cuidados: ["Validar datas de aulas rigorosamente."],
            comoTestar: ["Login como Professor e abrir diário."],
            observacoes: [],
            dependeBackend: true
        },
        {
            id: "equipe",
            nome: "Equipe",
            descricao: "Portal da equipe interna.",
            quemUsa: "Equipe",
            perfil: "EQP / ADM",
            status: "estavel",
            arquivosPrincipais: ["equipe.html", "equipe-painel.html"],
            arquivosRelacionados: ["equipe-ide.html"],
            endpointsRelacionados: [],
            cuidados: ["Não alterar menus de navegação fixos."],
            comoTestar: ["Acessar painel da equipe."],
            observacoes: [],
            dependeBackend: true
        },
        {
            id: "convites",
            nome: "Convites",
            descricao: "Sistema de emissão e gestão de convites para equipe.",
            quemUsa: "Administradores",
            perfil: "ADM",
            status: "sensivel",
            arquivosPrincipais: ["equipe-convites.html"],
            arquivosRelacionados: [],
            endpointsRelacionados: ["/api/equipe/convite"],
            cuidados: ["O link do convite não pode ser vazado no frontend."],
            comoTestar: ["Criar convite e revogar."],
            observacoes: [],
            dependeBackend: true
        },
        {
            id: "funcionarios",
            nome: "Funcionários",
            descricao: "Gestão do quadro de funcionários.",
            quemUsa: "RH / ADM",
            perfil: "ADM",
            status: "estavel",
            arquivosPrincipais: ["funcionarios.html"],
            arquivosRelacionados: [],
            endpointsRelacionados: [],
            cuidados: ["Validar perfis atribuídos."],
            comoTestar: ["Cadastrar e inativar funcionário."],
            observacoes: [],
            dependeBackend: true
        },
        {
            id: "auditoria",
            nome: "Auditoria",
            descricao: "Painel de logs do sistema.",
            quemUsa: "Sysadmin",
            perfil: "SYS / ADM",
            status: "sensivel",
            arquivosPrincipais: ["auditoria.html"],
            arquivosRelacionados: [],
            endpointsRelacionados: ["/api/auditoria"],
            cuidados: ["Pode gerar paginação lenta, cuidado com requisições."],
            comoTestar: ["Visualizar e filtrar logs."],
            observacoes: [],
            dependeBackend: true
        },
        {
            id: "emails",
            nome: "E-mails",
            descricao: "Modelos e envios de comunicação oficial.",
            quemUsa: "Sistema",
            perfil: "SYS",
            status: "estavel",
            arquivosPrincipais: [],
            arquivosRelacionados: [],
            endpointsRelacionados: [],
            cuidados: ["Garantir templates responsivos."],
            comoTestar: ["Testar disparos simulados."],
            observacoes: ["Focado no backend."],
            dependeBackend: true
        },
        {
            id: "seguranca-conta",
            nome: "Segurança da Conta",
            descricao: "Área de reset de senha e Passkeys.",
            quemUsa: "Todos",
            perfil: "Todos",
            status: "sensivel",
            arquivosPrincipais: ["recuperacao-seguranca.html", "recuperar-acesso.html"],
            arquivosRelacionados: ["auth.js"],
            endpointsRelacionados: ["/api/auth/reset-password"],
            cuidados: ["Segurança crítica. Não modificar sem revisão estrita."],
            comoTestar: ["Solicitar reset de senha."],
            observacoes: [],
            dependeBackend: true
        },
        {
            id: "ide-equipe",
            nome: "IDE da Equipe",
            descricao: "Ambiente de desenvolvimento e prototipagem seguro.",
            quemUsa: "Desenvolvedores e Designers da Equipe",
            perfil: "EQP / ADM",
            status: "em evolucao",
            arquivosPrincipais: ["equipe-ide.html", "equipe-ide.js", "equipe-ide.css"],
            arquivosRelacionados: [],
            endpointsRelacionados: ["/api/equipe-ide/github"],
            cuidados: ["Manter restrição ao diretório de rascunhos.", "Não permitir execução irrestrita no servidor."],
            comoTestar: ["Criar um rascunho, gerar um PR e validar."],
            observacoes: ["Área em constante evolução (Fase 3 em progresso)."],
            dependeBackend: true
        }
    ];
    
    // TAREFAS GUIADAS
    const TAREFAS_GUIADAS = [
        {
            id: "criar-prototipo-livre",
            titulo: "Criar protótipo livre",
            tipo: "prototipo",
            modeloSugerido: "html-simples",
            descricao: "Criar uma ideia visual isolada sem alterar telas oficiais.",
            objetivo: "Testar uma ideia de tela ou componente com segurança.",
            arquivosLiberados: ["index.html", "style.css", "script.js"],
            naoFazer: [
                "Não editar telas oficiais nesta fase.",
                "Não adicionar dados reais.",
                "Não depender de backend real."
            ],
            checklist: [
                { id: "preview-testado", texto: "Preview testado localmente" },
                { id: "sem-dados-reais", texto: "Não inclui dados reais ou sensíveis" },
                { id: "escopo-isolado", texto: "Alteração isolada em rascunho" }
            ]
        },
        {
            id: "criar-tela-soft-ui",
            titulo: "Criar tela Soft UI",
            tipo: "prototipo",
            modeloSugerido: "soft-ui",
            descricao: "Criar uma tela visual no padrão Casa da Mulher.",
            objetivo: "Construir uma nova interface seguindo a estética Soft UI.",
            arquivosLiberados: ["index.html", "style.css", "script.js"],
            naoFazer: [
                "Não usar tabela quando cards bastam.",
                "Não criar scroll horizontal.",
                "Não depender de backend real."
            ],
            checklist: [
                { id: "segue-soft-ui", texto: "Segue visual Soft UI" },
                { id: "sem-tabela", texto: "Não usa tabela desnecessária" },
                { id: "sem-scroll-horiz", texto: "Não cria scroll horizontal" }
            ]
        },
        {
            id: "ajustar-visual-tela",
            titulo: "Ajustar visual de tela",
            tipo: "ajuste",
            modeloSugerido: null,
            descricao: "Propor ajuste visual em uma tela existente, ainda como rascunho.",
            objetivo: "Aprimorar o visual sem quebrar funcionalidades.",
            arquivosLiberados: ["index.html", "style.css", "script.js"],
            naoFazer: [
                "Não editar a tela oficial diretamente."
            ],
            checklist: [
                { id: "nao-quebra-layout", texto: "Não quebra o layout em telas menores" },
                { id: "contraste-ok", texto: "Mantém bom contraste de cores" }
            ]
        },
        {
            id: "criar-lista-cards",
            titulo: "Criar lista em cards",
            tipo: "prototipo",
            modeloSugerido: "card-lista",
            descricao: "Criar layout de listagem sem tabela.",
            objetivo: "Exibir itens de forma responsiva.",
            arquivosLiberados: ["index.html", "style.css", "script.js"],
            naoFazer: [
                "Não usar <table>."
            ],
            checklist: [
                { id: "cards-responsivos", texto: "Cards são responsivos" },
                { id: "botoes-claros", texto: "Botões de ação são claros" },
                { id: "estado-vazio", texto: "Estado vazio previsto" }
            ]
        },
        {
            id: "criar-form-simples",
            titulo: "Criar formulário simples",
            tipo: "prototipo",
            modeloSugerido: "html-simples",
            descricao: "Criar formulário visual de teste.",
            objetivo: "Prototipar entrada de dados.",
            arquivosLiberados: ["index.html", "style.css", "script.js"],
            naoFazer: [
                "Não conectar a API."
            ],
            checklist: [
                { id: "labels-claros", texto: "Labels claros" },
                { id: "campos-obrigatorios", texto: "Campos obrigatórios marcados" },
                { id: "botoes-form", texto: "Botões de cancelar/salvar presentes" },
                { id: "msgs-erro", texto: "Mensagens de erro previstas" }
            ]
        }
    ];

    const TAREFA_PADRAO = {
        id: "criar-prototipo-livre",
        titulo: "Criar protótipo livre",
        tipo: "prototipo"
    };
    // 3. Modelos Iniciais
    const TEMPLATES = {
        'html-simples': {
            nome: 'Prototipo HTML simples',
            arquivos: {
                'index.html': `<!DOCTYPE html>
<html lang="pt-br">
<head>
  <meta charset="UTF-8">
  <style>
    /* O CSS do style.css e injetado automaticamente aqui pelo Preview */
  </style>
</head>
<body>
  <main>
    <h1>Ola, Equipe!</h1>
    <p>Comece a prototipar aqui.</p>
  </main>
  <script>
    // O JS do script.js e injetado automaticamente aqui pelo Preview
  </script>
</body>
</html>`,
                'style.css': `body {
  font-family: Arial, sans-serif;
  background: #f1f5f9;
  color: #333;
  padding: 20px;
}

main {
  background: white;
  padding: 20px;
  border-radius: 8px;
  box-shadow: 0 4px 6px rgba(0,0,0,0.1);
}`,
                'script.js': `console.log("Prototipo carregado.");

// Escreva seu JS aqui`
            }
        },
        'soft-ui': {
            nome: 'Tela Soft UI',
            arquivos: {
                'index.html': `<!DOCTYPE html>
<html lang="pt-br">
<head>
  <meta charset="UTF-8">
  <!-- Simulando importacao da fonte oficial -->
  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
</head>
<body class="soft-body">
  <main class="soft-admin-main">
    <header class="soft-header">
      <div>
        <img src="IMAGENS/logo_oficial.png" alt="Logo" class="logo-admin" style="height:40px; margin-right: 15px; border-radius:8px;">
        <div>
          <h1>Nova Tela</h1>
          <p>Exemplo de layout Soft UI</p>
        </div>
      </div>
    </header>
    
    <section class="admin-panel" style="margin-top: 24px;">
      <h2>Conteudo Principal</h2>
      <p class="section-intro">Use botoes arredondados e cores da paleta oficial.</p>
      <br>
      <button class="btn-primary">Acao Principal</button>
      <button class="btn-secondary">Acao Secundaria</button>
    </section>
  </main>
</body>
</html>`,
                'style.css': `/* Variaveis baseadas no Soft UI real */
:root {
  --cor-primaria: #8B5A96;
  --cor-primaria-clara: #F8F4F9;
  --cor-secundaria: #6B4C73;
  --cor-fundo: #FAFAFA;
  --cor-texto: #333333;
  --cor-texto-secundario: #666666;
  --cor-borda: #EAEAEA;
  --raio-borda-card: 16px;
  --raio-borda-input: 12px;
  --sombra-card: 0 4px 12px rgba(139, 90, 150, 0.05);
}

body.soft-body {
  font-family: 'Inter', sans-serif;
  background-color: var(--cor-fundo);
  color: var(--cor-texto);
  margin: 0;
  padding: 0;
}

/* Restante das classes para manter a aparencia sem carregar o style.css externo para evitar conflitos de rotas */
.soft-header {
  display: flex;
  background: #fff;
  padding: 16px 24px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.02);
  border-bottom: 1px solid var(--cor-borda);
}
.soft-header > div {
  display: flex;
  align-items: center;
}
.soft-header h1 { margin:0; font-size: 1.25rem; color: var(--cor-primaria); }
.soft-header p { margin:0; font-size: 0.9rem; color: var(--cor-texto-secundario); }

.admin-panel {
  background: #fff;
  border-radius: var(--raio-borda-card);
  box-shadow: var(--sombra-card);
  padding: 24px;
  max-width: 800px;
  margin-left: auto;
  margin-right: auto;
}
.admin-panel h2 { color: var(--cor-primaria); margin-top:0; }

.btn-primary {
  background: var(--cor-primaria);
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 20px;
  cursor: pointer;
  font-weight: 500;
}
.btn-secondary {
  background: transparent;
  color: var(--cor-secundaria);
  border: 1px solid var(--cor-secundaria);
  padding: 10px 20px;
  border-radius: 20px;
  cursor: pointer;
  font-weight: 500;
}`,
                'script.js': `// Inicializacao do Soft UI
console.log("Tela Soft UI carregada.");`
            }
        },
        'card-lista': {
            nome: 'Lista em cards',
            arquivos: {
                'index.html': `<!DOCTYPE html>
<html lang="pt-br">
<body>
  <div class="card-list-container">
    <div class="data-card">
      <h3>Joao da Silva</h3>
      <p>ID: PRO-0001</p>
    </div>
    <div class="data-card">
      <h3>Maria Sousa</h3>
      <p>ID: PRO-0002</p>
    </div>
  </div>
</body>
</html>`,
                'style.css': `body { font-family: sans-serif; padding: 20px; background: #f9f9f9; }

.card-list-container {
  display: grid;
  gap: 16px;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
}

.data-card {
  background: white;
  padding: 20px;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.05);
  border: 1px solid #eee;
}

.data-card h3 {
  margin: 0 0 8px 0;
  color: #8B5A96;
}

.data-card p {
  margin: 0;
  color: #666;
  font-size: 0.9em;
}`,
                'script.js': `// Logica de lista
console.log("Lista carregada");`
            }
        }
    };

    let rascunhoAtual = {
        versaoWorkspace: 2,
        nome: 'Tela Soft UI',
        arquivos: { ...TEMPLATES['soft-ui'].arquivos },
        arquivosBase: { ...TEMPLATES['soft-ui'].arquivos },
        pastas: [],
        arquivoAtivo: 'index.html',
        abasAbertas: ['index.html', 'style.css', 'script.js'],
        tarefa: TAREFA_PADRAO,
        areaProjeto: null
    };

    let editorInstance = null;
    let isFallback = false;
    let unsavedChanges = false;
    let updateTimeout = null;

    // Elementos da DOM
    const editorTextarea = document.getElementById('ideCodeEditor');
    let iframePreview = document.getElementById('idePreviewFrame');
    const lblCurrentFile = document.getElementById('ideCurrentFileName');
    const statusBarFile = document.getElementById('statusBarFile');
    const statusBarLang = document.getElementById('statusBarLang');
    const btnSave = document.getElementById('btnIdeSave');
    const badgeSave = document.getElementById('statusBarSave');
    const fileButtons = document.querySelectorAll('.ide-file-item, .ide-tab');
    const templateButtons = document.querySelectorAll('.ide-template-item');
    const previewEmpty = document.getElementById('idePreviewEmpty');
    
    // 4. Fallback do Editor
    function inicializarEditor() {
        if (typeof window.CodeMirror !== 'undefined') {
            try {
                editorInstance = CodeMirror.fromTextArea(editorTextarea, {
                    lineNumbers: true,
                    mode: 'htmlmixed',
                    theme: 'dracula',
                    autoCloseTags: true,
                    indentUnit: 4,
                    lineWrapping: true
                });

                editorInstance.on('change', () => {
                    marcarComoNaoSalvo();
                    agendarPreview();
                });
            } catch (e) {
                console.error("Erro ao iniciar CodeMirror", e);
                ativarFallback();
            }
        } else {
            ativarFallback();
        }
    }

    function ativarFallback() {
        isFallback = true;
        document.getElementById('ideEditorFallbackWarning').classList.remove('hidden');
        editorTextarea.addEventListener('input', () => {
            marcarComoNaoSalvo();
            agendarPreview();
        });
    }

    // 5. Configurar modo de linguagem
    function updateEditorMode(filename) {
        if (isFallback || !editorInstance) return;
        
        let mode = 'htmlmixed';
        if (filename.endsWith('.css')) mode = 'css';
        else if (filename.endsWith('.js')) mode = 'javascript';
        
        editorInstance.setOption('mode', mode);
    }

    // 6. Atualizar conteúdo do editor
    function setEditorValue(value) {
        if (isFallback) {
            editorTextarea.value = value;
        } else {
            editorInstance.setValue(value);
        }
    }

    function getEditorValue() {
        if (isFallback) {
            return editorTextarea.value;
        } else {
            return editorInstance.getValue();
        }
    }

    // 7. Salvar e recuperar rascunhos (localStorage)
    function carregarRascunhoSalvo() {
        const salvoStr = localStorage.getItem(DRAFT_KEY);
        if (salvoStr) {
            try {
                let salvo = JSON.parse(salvoStr);
                // MIGRATION SCRIPT TO V2
                if (salvo.versaoWorkspace !== 2) {
                    console.log("Migrando rascunho antigo para Workspace V2...");
                    const arquivosAntigos = salvo.arquivos || {};
                    const html = arquivosAntigos['index.html'] || arquivosAntigos['html'] || '';
                    const css = arquivosAntigos['style.css'] || arquivosAntigos['css'] || '';
                    const js = arquivosAntigos['script.js'] || arquivosAntigos['js'] || '';
                    
                    const novosArquivos = {
                        "index.html": html,
                        "style.css": css,
                        "script.js": js
                    };
                    
                    salvo = {
                        versaoWorkspace: 2,
                        nome: salvo.nome || 'Rascunho Migrado',
                        arquivos: novosArquivos,
                        arquivosBase: JSON.parse(JSON.stringify(novosArquivos)), // Clone deep
                        pastas: [],
                        arquivoAtivo: salvo.arquivoAtivo || "index.html",
                        abasAbertas: ["index.html", "style.css", "script.js"],
                        tarefa: salvo.tarefa || TAREFA_PADRAO,
                        areaProjeto: salvo.areaProjeto || null,
                        atualizadoEm: salvo.atualizadoEm,
                        githubModo: salvo.githubModo,
                        githubToken: salvo.githubToken,
                        githubOwner: salvo.githubOwner,
                        githubRepo: salvo.githubRepo,
                        githubBranch: salvo.githubBranch
                    };
                }
                
                // Prevenção contra rascunhos zumbis
                const isInvalid = !salvo.arquivos || !salvo.arquivos['index.html'] || (!salvo.arquivos['index.html'].trim() && salvo.arquivos['style.css'].trim());
                
                if (isInvalid) {
                    console.warn("Rascunho antigo inválido ou corrompido, carregando vazio.");
                    localStorage.removeItem(DRAFT_KEY);
                } else {
                    rascunhoAtual = salvo;
                    
                    // Fallbacks extras
                    if (!rascunhoAtual.arquivosBase) rascunhoAtual.arquivosBase = JSON.parse(JSON.stringify(rascunhoAtual.arquivos));
                    if (!rascunhoAtual.pastas) rascunhoAtual.pastas = [];
                    if (!rascunhoAtual.abasAbertas) rascunhoAtual.abasAbertas = ["index.html", "style.css", "script.js"];
                    if (!rascunhoAtual.arquivoAtivo) rascunhoAtual.arquivoAtivo = 'index.html';
                    if (!rascunhoAtual.tarefa) rascunhoAtual.tarefa = TAREFA_PADRAO;
                    
                    document.getElementById('ideCurrentFileName').textContent = rascunhoAtual.arquivoAtivo;
                    atualizarStatusTarefa();
                    marcarComoSalvo();
                    console.log(`Rascunho restaurado V2. Atualizado em: ${salvo.atualizadoEm}`);
                    renderizarArvoreArquivos();
                renderizarAbas();
                }
            } catch (e) {
                console.error("Erro ao ler rascunho salvo.", e);
            }
        }
    }

    function salvarRascunhoLocal() {
        // Puxa do editor o valor do arquivo atual para o objeto antes de salvar
        const file = rascunhoAtual.arquivoAtivo || 'index.html';
        if (editorInstance) {
            rascunhoAtual.arquivos[file] = getEditorValue();
        }
        
        // Remove lixo caso exista
        if (rascunhoAtual.arquivos['undefined'] !== undefined) {
            delete rascunhoAtual.arquivos['undefined'];
        }
        if (rascunhoAtual.arquivos['null'] !== undefined) {
            delete rascunhoAtual.arquivos['null'];
        }

        rascunhoAtual.atualizadoEm = new Date().toISOString();
        localStorage.setItem(DRAFT_KEY, JSON.stringify(rascunhoAtual));
        marcarComoSalvo();
    }

    function marcarComoNaoSalvo() {
        if (!unsavedChanges) {
            unsavedChanges = true;
            badgeSave.textContent = 'Não salvo';
            badgeSave.className = 'ide-statusbar-item warning';
        }
    }

    function marcarComoSalvo() {
        unsavedChanges = false;
        const now = new Date();
        const hora = String(now.getHours()).padStart(2, '0');
        const min = String(now.getMinutes()).padStart(2, '0');
        badgeSave.textContent = `Salvo às ${hora}:${min}`;
        badgeSave.className = 'ide-statusbar-item success';
    }

    // 8. Mecanismo de Preview Isolado (sandbox)
    function atualizarPreview() {
        if (!rascunhoAtual || !rascunhoAtual.arquivos) return;
        
        // Atualiza a memoria primeiro
        rascunhoAtual.arquivos[rascunhoAtual.arquivoAtivo] = getEditorValue();
        
        const html = rascunhoAtual.arquivos['index.html'] || '';
        const css = rascunhoAtual.arquivos['style.css'] || '';
        const js = rascunhoAtual.arquivos['script.js'] || '';

        if (!html.trim()) {
            if (previewEmpty) previewEmpty.classList.remove('hidden');
            iframePreview.classList.add('hidden');
            iframePreview.srcdoc = ''; // Força a limpeza para evitar bug do Chromium
            return;
        }

        let finalHtml = html;
        const styleTag = css.trim() ? `\n<style>\n${css}\n</style>\n` : '';
        const scriptTag = js.trim() ? `\n<script>\ntry {\n${js}\n} catch(e) { console.error("Erro no script:", e); }\n<\/script>\n` : '';

        // Tenta injetar no head e body
        if (styleTag) {
            if (finalHtml.includes('</head>')) finalHtml = finalHtml.replace('</head>', `${styleTag}</head>`);
            else finalHtml = styleTag + finalHtml;
        }

        if (scriptTag) {
            if (finalHtml.includes('</body>')) finalHtml = finalHtml.replace('</body>', `${scriptTag}</body>`);
            else finalHtml += scriptTag;
        }

        // Garante que o iframe exibe
        if (previewEmpty) previewEmpty.classList.add('hidden');
        
        // Em vez de só trocar srcdoc, recria o node. Isso resolve os bugs agressivos de cache do Chromium
        // quando um iframe volta de display:none com a mesma string.
        const novoIframe = document.createElement('iframe');
        novoIframe.id = 'idePreviewFrame';
        novoIframe.className = 'ide-preview-frame';
        novoIframe.sandbox = 'allow-scripts allow-same-origin'; // allow-same-origin permite carregar fontes corretamente
        novoIframe.srcdoc = finalHtml;
        
        iframePreview.parentNode.replaceChild(novoIframe, iframePreview);
        iframePreview = novoIframe; // atualiza a referencia
    }

    function agendarPreview() {
        clearTimeout(updateTimeout);
        updateTimeout = setTimeout(() => {
            atualizarPreview();
        }, 800);
    }

    // --- VFS: ARVORE DE ARQUIVOS ---
    function validarCaminhoArquivo(path) {
        if (!path) return { valido: false, erro: "O caminho não pode ser vazio." };
        path = path.replace(/\\/g, '/');
        if (path.includes('../') || path.startsWith('/') || path.match(/^[a-zA-Z]:\//) || path.includes('//')) {
            return { valido: false, erro: "Caminho inválido. Evite barras duplas ou navegação para fora do diretório." };
        }
        
        const partes = path.split('/');
        const nomeArquivo = partes.pop();
        if (nomeArquivo) {
            const extPermitidas = ['.html', '.css', '.js', '.json', '.md', '.txt', '.cs', '.cshtml'];
            const ext = nomeArquivo.substring(nomeArquivo.lastIndexOf('.')).toLowerCase();
            if (!extPermitidas.includes(ext) || nomeArquivo.indexOf('.') === -1) {
                return { valido: false, erro: `Extensão não permitida. Use: ${extPermitidas.join(', ')}` };
            }
        }
        return { valido: true, pathNorm: path, partesPasta: partes };
    }

    function renderizarArvoreArquivos() {
        const container = document.getElementById('ideFileList');
        if (!container) return;
        
        container.innerHTML = '';
        
        const arquivos = Object.keys(rascunhoAtual.arquivos || {}).sort();
        const pastas = (rascunhoAtual.pastas || []).sort();
        
        pastas.forEach(pasta => {
            const divPasta = document.createElement('div');
            divPasta.style.padding = '4px 8px';
            divPasta.style.color = 'var(--ide-text)';
            divPasta.style.opacity = '0.8';
            divPasta.style.display = 'flex';
            divPasta.innerHTML = `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right:6px;"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path></svg> ${pasta}/`;
            container.appendChild(divPasta);
        });

        arquivos.forEach(path => {
            const btn = document.createElement('div');
            btn.className = `ide-file-item ${rascunhoAtual.arquivoAtivo === path ? 'active' : ''}`;
            btn.style.display = 'flex';
            btn.style.justifyContent = 'space-between';
            btn.style.alignItems = 'center';
            btn.style.padding = '6px 8px';
            
            let color = '#ccc';
            if (path.endsWith('.html')) color = '#e34c26';
            else if (path.endsWith('.css')) color = '#264de4';
            else if (path.endsWith('.js')) color = '#f0db4f';
            else if (path.endsWith('.cs')) color = '#178600';
            else if (path.endsWith('.json')) color = '#cb3837';
            else if (path.endsWith('.md')) color = '#fff';
            
            const btnName = document.createElement('span');
            btnName.style.display = 'flex';
            btnName.style.alignItems = 'center';
            btnName.style.gap = '6px';
            btnName.style.cursor = 'pointer';
            btnName.style.flex = '1';
            btnName.style.overflow = 'hidden';
            btnName.style.textOverflow = 'ellipsis';
            btnName.style.whiteSpace = 'nowrap';
            btnName.innerHTML = `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="${color}" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline></svg> <span style="overflow:hidden; text-overflow:ellipsis;">${path}</span>`;
            btnName.onclick = () => abrirArquivo(path);
            
            const btnAcoes = document.createElement('div');
            btnAcoes.style.display = 'flex';
            btnAcoes.style.gap = '4px';
            
            const btnRenomear = document.createElement('button');
            btnRenomear.innerHTML = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>';
            btnRenomear.style = "background:none; border:none; color:var(--ide-text); cursor:pointer; opacity: 0.6; padding:0 4px; display:flex; align-items:center;";
            btnRenomear.onclick = (e) => { e.stopPropagation(); renomearArquivoVFS(path); };
            
            const btnExcluir = document.createElement('button');
            btnExcluir.innerHTML = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>';
            btnExcluir.style = "background:none; border:none; color:var(--ide-text); cursor:pointer; opacity: 0.6; padding:0 4px; display:flex; align-items:center;";
            btnExcluir.onclick = (e) => { e.stopPropagation(); excluirArquivoVFS(path); };
            
            btnAcoes.appendChild(btnRenomear);
            btnAcoes.appendChild(btnExcluir);
            
            btn.appendChild(btnName);
            btn.appendChild(btnAcoes);
            container.appendChild(btn);
        });
    }

    function renomearArquivoVFS(oldPath) {
        const newPathRaw = prompt("Novo nome do arquivo (ex: pasta/arquivo.js):", oldPath);
        if (!newPathRaw || newPathRaw === oldPath) return;
        
        const val = validarCaminhoArquivo(newPathRaw);
        if (!val.valido) return alert(val.erro);
        const newPath = val.pathNorm;
        
        if (rascunhoAtual.arquivos[newPath] !== undefined) return alert("Arquivo já existe com este nome.");
        
        if (rascunhoAtual.arquivoAtivo === oldPath) {
            rascunhoAtual.arquivos[oldPath] = getEditorValue();
        }
        
        rascunhoAtual.arquivos[newPath] = rascunhoAtual.arquivos[oldPath];
        delete rascunhoAtual.arquivos[oldPath];
        
        const abaIndex = rascunhoAtual.abasAbertas.indexOf(oldPath);
        if (abaIndex !== -1) rascunhoAtual.abasAbertas[abaIndex] = newPath;
        
        if (rascunhoAtual.arquivoAtivo === oldPath) {
            abrirArquivo(newPath);
        } else {
            renderizarArvoreArquivos();
                renderizarAbas();
            salvarRascunhoLocal();
        }
    }

    function excluirArquivoVFS(path) {
        if (!confirm(`Deseja excluir o arquivo '${path}'?`)) return;
        
        delete rascunhoAtual.arquivos[path];
        
        fecharAba(path, true); // true indica que estamos excluindo
    }

    function renderizarAbas() {
        const container = document.getElementById('ideEditorTabs');
        if (!container) return;
        
        container.innerHTML = '';
        if (!rascunhoAtual.abasAbertas) rascunhoAtual.abasAbertas = [];
        
        rascunhoAtual.abasAbertas.forEach(path => {
            const btn = document.createElement('div');
            btn.className = `ide-tab ${rascunhoAtual.arquivoAtivo === path ? 'active' : ''}`;
            btn.style.display = 'flex';
            btn.style.alignItems = 'center';
            btn.style.gap = '6px';
            btn.title = path;
            
            let color = '#ccc';
            if (path.endsWith('.html')) color = '#e34c26';
            else if (path.endsWith('.css')) color = '#264de4';
            else if (path.endsWith('.js')) color = '#f0db4f';
            else if (path.endsWith('.cs')) color = '#178600';
            else if (path.endsWith('.json')) color = '#cb3837';
            else if (path.endsWith('.md')) color = '#fff';
            
            const btnName = document.createElement('span');
            btnName.style.cursor = 'pointer';
            btnName.style.display = 'flex';
            btnName.style.alignItems = 'center';
            btnName.style.gap = '6px';
            btnName.innerHTML = `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="${color}" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline></svg> <span style="white-space:nowrap; overflow:hidden; text-overflow:ellipsis; max-width: 150px;">${path}</span>`;
            btnName.onclick = () => abrirArquivo(path);
            
            const btnClose = document.createElement('button');
            btnClose.innerHTML = '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>';
            btnClose.style = "background:none; border:none; color:inherit; cursor:pointer; padding:2px; margin-left:4px; opacity: 0.6; display: flex; align-items: center; justify-content: center;";
            btnClose.onclick = (e) => {
                e.stopPropagation();
                fecharAba(path, false);
            };
            
            btn.appendChild(btnName);
            btn.appendChild(btnClose);
            container.appendChild(btn);
        });
    }

    function fecharAba(path, isExcluindo = false) {
        if (!rascunhoAtual.abasAbertas) rascunhoAtual.abasAbertas = [];
        const abaIndex = rascunhoAtual.abasAbertas.indexOf(path);
        
        if (abaIndex !== -1) {
            rascunhoAtual.abasAbertas.splice(abaIndex, 1);
        }
        
        if (rascunhoAtual.arquivoAtivo === path || isExcluindo) {
            const novaAba = rascunhoAtual.abasAbertas[Math.min(abaIndex, rascunhoAtual.abasAbertas.length - 1)];
            if (novaAba) {
                abrirArquivo(novaAba);
            } else {
                rascunhoAtual.arquivoAtivo = '';
                setEditorValue('');
                lblCurrentFile.textContent = 'Sem arquivo';
                if (statusBarFile) statusBarFile.textContent = 'Sem arquivo';
                if (statusBarLang) statusBarLang.textContent = '-';
                renderizarArvoreArquivos();
                renderizarAbas();
                salvarRascunhoLocal();
            }
        } else {
            renderizarAbas();
            salvarRascunhoLocal();
        }
    }

    // 9. Alternar abas de arquivo
    function abrirArquivo(filename) {
        if (rascunhoAtual.arquivoAtivo && rascunhoAtual.arquivos[rascunhoAtual.arquivoAtivo] !== undefined) {
            rascunhoAtual.arquivos[rascunhoAtual.arquivoAtivo] = getEditorValue();
        }
        
        if (rascunhoAtual.arquivos[filename] === undefined) {
            if (filename === 'index.html') rascunhoAtual.arquivos[filename] = '';
            else return;
        }
        
        if (!rascunhoAtual.abasAbertas) rascunhoAtual.abasAbertas = [];
        if (!rascunhoAtual.abasAbertas.includes(filename)) {
            rascunhoAtual.abasAbertas.push(filename);
        }
        
        rascunhoAtual.arquivoAtivo = filename;
        lblCurrentFile.textContent = filename;
        if (statusBarFile) statusBarFile.textContent = filename;
        if (statusBarLang) {
            if (filename.endsWith('.js')) statusBarLang.textContent = 'JavaScript';
            else if (filename.endsWith('.css')) statusBarLang.textContent = 'CSS';
            else if (filename.endsWith('.cs')) statusBarLang.textContent = 'C#';
            else if (filename.endsWith('.json')) statusBarLang.textContent = 'JSON';
            else statusBarLang.textContent = 'HTML';
        }
        
        setEditorValue(rascunhoAtual.arquivos[filename] || '');
        updateEditorMode(filename);
        
        renderizarArvoreArquivos();
                renderizarAbas();
        setTimeout(() => { if (editorInstance) editorInstance.refresh(); }, 50);
        salvarRascunhoLocal();
    }

    // 10. Ações da Toolbar/Sidebar
    // Substituído por renderizarArvoreArquivos e renderizarAbas

    templateButtons.forEach(btn => {
        btn.addEventListener('click', (e) => {
            if (unsavedChanges || localStorage.getItem(DRAFT_KEY)) {
                if (!confirm("Isso substituirá o rascunho atual. Deseja continuar?")) {
                    return;
                }
            }
            const templateId = e.currentTarget.getAttribute('data-template');
            if (TEMPLATES[templateId]) {
                rascunhoAtual = {
                    nome: TEMPLATES[templateId].nome,
                    arquivos: { ...TEMPLATES[templateId].arquivos },
                    arquivoAtivo: 'index.html',
                    tarefa: TAREFA_PADRAO,
                    areaProjeto: null
                };
                
                // Em vez de chamar abrirArquivo (que salvaria o editor atual no novo rascunho),
                // forçamos a atualização da UI manualmente para o index.html do novo template:
                lblCurrentFile.textContent = 'index.html';
                if (statusBarFile) statusBarFile.textContent = 'index.html';
                if (statusBarLang) statusBarLang.textContent = 'HTML';
                
                setEditorValue(rascunhoAtual.arquivos['index.html']);
                updateEditorMode('index.html');
                
                renderizarArvoreArquivos();
                renderizarAbas();
                
                setTimeout(() => { if (editorInstance) editorInstance.refresh(); }, 50);

                salvarRascunhoLocal();
                atualizarPreview();
                atualizarStatusTarefa();
            }
        });
    });

    // Renderizar Tarefas Guiadas no Menu Lateral
    function renderizarTarefasGuiadas() {
        const list = document.getElementById("ideTasksList");
        if (!list) return;

        list.innerHTML = "";
        TAREFAS_GUIADAS.forEach(tarefa => {
            const btn = document.createElement("button");
            btn.className = "ide-template-item";
            btn.innerHTML = `
                <strong>${tarefa.titulo}</strong>
                <span>${tarefa.descricao}</span>
            `;
            btn.addEventListener("click", () => {
                if (confirm(`Deseja iniciar a tarefa "${tarefa.titulo}"? Isso substituirá seu rascunho atual.`)) {
                    iniciarTarefa(tarefa);
                }
            });
            list.appendChild(btn);
        });
    }

    function iniciarTarefa(tarefa) {
        const modeloParaCarregar = tarefa.modeloSugerido && TEMPLATES[tarefa.modeloSugerido] 
            ? TEMPLATES[tarefa.modeloSugerido] 
            : TEMPLATES['html-simples'];

        rascunhoAtual = {
            nome: modeloParaCarregar.nome,
            arquivos: { ...modeloParaCarregar.arquivos },
            arquivoAtivo: 'index.html',
            tarefa: {
                id: tarefa.id,
                titulo: tarefa.titulo,
                tipo: tarefa.tipo
            }
        };

        lblCurrentFile.textContent = 'index.html';
        if (statusBarFile) statusBarFile.textContent = 'index.html';
        if (statusBarLang) statusBarLang.textContent = 'HTML';
        
        setEditorValue(rascunhoAtual.arquivos['index.html']);
        updateEditorMode('index.html');
        
        renderizarArvoreArquivos();
                renderizarAbas();
        
        setTimeout(() => { if (editorInstance) editorInstance.refresh(); }, 50);

        salvarRascunhoLocal();
        atualizarPreview();
        atualizarStatusTarefa();
    }

    function atualizarStatusTarefa() {
        const badge = document.getElementById("statusBarTask");
        if (badge) {
            const tituloTarefa = rascunhoAtual.tarefa ? rascunhoAtual.tarefa.titulo : "Livre";
            const tituloArea = rascunhoAtual.areaProjeto ? rascunhoAtual.areaProjeto.nome : "Não informada";
            badge.textContent = `Tarefa: ${tituloTarefa} • Área: ${tituloArea}`;
        }
    }

    // ==============================================================================
    // MAPA DO PROJETO E CONTEXTO
    // ==============================================================================

    const btnTabExplorer = document.getElementById("btnTabExplorer");
    const btnTabMap = document.getElementById("btnTabMap");
    const btnTabBackend = document.getElementById("btnTabBackend");
    const panelExplorer = document.getElementById("panelExplorer");
    const panelMap = document.getElementById("panelMap");
    const panelBackend = document.getElementById("panelBackend");
    const mapContextDrawer = document.getElementById("mapContextDrawer");

    let areaEmFoco = null;

    if (btnTabExplorer && btnTabMap) {
        btnTabExplorer.addEventListener("click", () => {
            btnTabExplorer.classList.add("active");
            btnTabMap.classList.remove("active");
            if (btnTabBackend) btnTabBackend.classList.remove("active");
            panelExplorer.classList.remove("hidden");
            panelExplorer.style.display = "";
            panelMap.classList.add("hidden");
            panelMap.style.display = "none";
            if (panelBackend) {
                panelBackend.classList.add("hidden");
                panelBackend.style.display = "none";
            }
        });

        btnTabMap.addEventListener("click", () => {
            btnTabMap.classList.add("active");
            btnTabExplorer.classList.remove("active");
            if (btnTabBackend) btnTabBackend.classList.remove("active");
            panelMap.classList.remove("hidden");
            panelMap.style.display = "flex";
            panelExplorer.classList.add("hidden");
            panelExplorer.style.display = "none";
            if (panelBackend) {
                panelBackend.classList.add("hidden");
                panelBackend.style.display = "none";
            }
            renderizarMapaProjeto();
        });

        if (btnTabBackend) {
            btnTabBackend.addEventListener("click", () => {
                btnTabBackend.classList.add("active");
                btnTabExplorer.classList.remove("active");
                btnTabMap.classList.remove("active");
                panelBackend.classList.remove("hidden");
                panelBackend.style.display = "flex";
                panelExplorer.classList.add("hidden");
                panelExplorer.style.display = "none";
                panelMap.classList.add("hidden");
                panelMap.style.display = "none";
                verificarStatusBackend();
            });
        }
    }

    async function verificarStatusBackend() {
        const statusCard = document.getElementById("backendStatusCard");
        if (!statusCard) return;

        statusCard.innerHTML = `<span style="color: var(--ide-muted);">Consultando API...</span>`;
        
        try {
            const res = await CasaMulherAuth.apiFetch('/api/equipe-ide/ambiente/status', { method: 'GET' });

            if (res.status === 401 || res.status === 403) {
                statusCard.innerHTML = `
                    <div style="color: #e34c26; font-weight:600; display:flex; align-items:center; gap:8px;">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>
                        Acesso Negado
                    </div>
                    <div style="font-size:0.8rem; white-space: normal; line-height: 1.4;">Você não tem permissão para consultar o status da API.</div>
                `;
                return;
            }

            if (!res.ok) throw new Error("Erro de conexão");
            
            const data = await res.json();
            
            statusCard.innerHTML = `
                <div style="color: #2ea043; font-weight:600; display:flex; align-items:center; gap:8px;">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
                    API Online
                </div>
                <div style="font-size:0.8rem; margin-top:8px; white-space: normal; line-height: 1.4;"><strong>Ambiente:</strong> ${data.ambiente}</div>
                <div style="font-size:0.8rem; white-space: normal; line-height: 1.4;"><strong>Usuário:</strong> ${data.usuario.nome} (${data.usuario.perfil})</div>
                <div style="font-size:0.8rem; margin-top:8px; color: var(--ide-accent); white-space: normal; line-height: 1.4;">
                    Runner Full-Stack: ${data.recursos.runnerBackend ? 'Disponível' : 'Desativado nesta fase'}
                </div>
            `;
        } catch (e) {
            console.warn("API Offline ou indisponível", e);
            statusCard.innerHTML = `
                <div style="color: #e34c26; font-weight:600; display:flex; align-items:center; gap:8px;">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>
                    API Offline ou Indisponível
                </div>
                <div style="font-size:0.8rem; margin-top:4px; white-space: normal; line-height: 1.4;">A IDE continua abrindo rascunhos locais, mas recursos que dependem da API podem não funcionar.</div>
            `;
        }
    }

    const btnVerificarBackend = document.getElementById("btnVerificarBackend");
    if (btnVerificarBackend) {
        btnVerificarBackend.addEventListener("click", verificarStatusBackend);
    }

    function renderizarMapaProjeto() {
        const container = document.getElementById("ideMapList");
        if (!container) return;

        container.innerHTML = "";
        MAPA_PROJETO_IDE.forEach(area => {
            const el = document.createElement("div");
            el.className = "ide-map-item";
            
            let classeStatus = "";
            if (area.status === "estavel") classeStatus = "estavel";
            else if (area.status === "sensivel") classeStatus = "sensivel";
            else classeStatus = "evolucao";

            el.innerHTML = `
                <div class="ide-map-item-title">
                    ${area.nome}
                    <span class="ide-badge ${classeStatus}">${area.status}</span>
                </div>
                <div class="ide-map-item-desc">${area.descricao}</div>
                <div style="font-size: 0.75rem; margin-top: 8px; color: var(--ide-accent);">Ver contexto &rarr;</div>
            `;

            el.addEventListener("click", () => abrirContextoArea(area));
            container.appendChild(el);
        });
    }

    function abrirContextoArea(area) {
        areaEmFoco = area;
        document.getElementById("drawerAreaNome").textContent = area.nome;
        
        const content = document.getElementById("drawerAreaContent");
        
        let htmlContexto = `<p style="margin-top: 0; color: var(--ide-muted);">${area.descricao}</p>`;
        
        if (area.dependeBackend) {
            htmlContexto += `
                <div class="ide-pill info" style="margin-top: 12px; font-size: 0.8rem; white-space: normal; line-height: 1.4;">
                    <strong>Depende da API:</strong> Nesta fase, a IDE apenas informa o status da API; testes reais de endpoints ficam para fases futuras.
                </div>
            `;
        }
        
        htmlContexto += `<h4>Acessos</h4>`;
        htmlContexto += `<ul><li><strong>Quem usa:</strong> ${area.quemUsa}</li><li><strong>Perfil:</strong> ${area.perfil}</li></ul>`;

        if (area.arquivosPrincipais && area.arquivosPrincipais.length > 0) {
            htmlContexto += `<h4>Arquivos Principais</h4><ul>`;
            area.arquivosPrincipais.forEach(arq => htmlContexto += `<li>${arq}</li>`);
            htmlContexto += `</ul>`;
        }

        if (area.cuidados && area.cuidados.length > 0) {
            htmlContexto += `<h4>Cuidados e Restrições</h4><ul>`;
            area.cuidados.forEach(c => htmlContexto += `<li>${c}</li>`);
            htmlContexto += `</ul>`;
        }

        if (area.comoTestar && area.comoTestar.length > 0) {
            htmlContexto += `<h4>Como Testar</h4><ul>`;
            area.comoTestar.forEach(c => htmlContexto += `<li>${c}</li>`);
            htmlContexto += `</ul>`;
        }

        content.innerHTML = htmlContexto;
        mapContextDrawer.classList.remove("hidden");
    }

    document.getElementById("btnFecharDrawer")?.addEventListener("click", () => {
        mapContextDrawer.classList.add("hidden");
        areaEmFoco = null;
    });

    document.getElementById("btnAssociarArea")?.addEventListener("click", () => {
        if (!areaEmFoco) return;
        rascunhoAtual.areaProjeto = {
            id: areaEmFoco.id,
            nome: areaEmFoco.nome,
            perfil: areaEmFoco.perfil,
            status: areaEmFoco.status,
            dependeBackend: !!areaEmFoco.dependeBackend
        };
        salvarRascunhoLocal();
        atualizarStatusTarefa();
        alert(`Rascunho atual associado à área: ${areaEmFoco.nome}`);
        mapContextDrawer.classList.add("hidden");
        areaEmFoco = null;
    });

    function atualizarSelectAreaProjeto() {
        const select = document.getElementById("ideReviewArea");
        if (!select) return;
        
        select.innerHTML = '<option value="">(Não informada)</option>';
        MAPA_PROJETO_IDE.forEach(area => {
            const opt = document.createElement("option");
            opt.value = area.id;
            opt.textContent = area.nome;
            select.appendChild(opt);
        });

        if (rascunhoAtual.areaProjeto) {
            select.value = rascunhoAtual.areaProjeto.id;
        }
    }

    // ==============================================================================

    btnSave.addEventListener('click', () => {
        salvarRascunhoLocal();
    });
    
    const btnNewFile = document.getElementById('btnIdeNewFile');
    if (btnNewFile) {
        btnNewFile.addEventListener('click', () => {
            const pathRaw = prompt("Caminho do novo arquivo (ex: js/app.js ou login.html):");
            if (!pathRaw) return;
            const val = validarCaminhoArquivo(pathRaw);
            if (!val.valido) return alert(val.erro);
            const path = val.pathNorm;
            if (rascunhoAtual.arquivos[path] !== undefined) return alert("Arquivo já existe!");
            
            if (rascunhoAtual.arquivoAtivo) {
                rascunhoAtual.arquivos[rascunhoAtual.arquivoAtivo] = getEditorValue();
            }
            
            rascunhoAtual.arquivos[path] = '';
            if (!rascunhoAtual.abasAbertas.includes(path)) {
                rascunhoAtual.abasAbertas.push(path);
            }
            abrirArquivo(path);
        });
    }
    
    const btnNewFolder = document.getElementById('btnIdeNewFolder');
    if (btnNewFolder) {
        btnNewFolder.addEventListener('click', () => {
            let pathRaw = prompt("Nome da nova pasta:");
            if (!pathRaw) return;
            pathRaw = pathRaw.replace(/\\/g, '/');
            if (pathRaw.includes('../') || pathRaw.startsWith('/') || pathRaw.match(/^[a-zA-Z]:\//) || pathRaw.includes('//')) {
                return alert("Caminho de pasta inválido.");
            }
            if (pathRaw.endsWith('/')) pathRaw = pathRaw.slice(0, -1);
            if (rascunhoAtual.pastas.includes(pathRaw)) return alert("Pasta já existe!");
            
            rascunhoAtual.pastas.push(pathRaw);
            renderizarArvoreArquivos();
                renderizarAbas();
            salvarRascunhoLocal();
        });
    }

    document.getElementById('btnIdeUpdatePreview').addEventListener('click', () => {
        atualizarPreview();
    });

    document.getElementById('btnIdeLimpar').addEventListener('click', () => {
        if (confirm("Isso limpará todo o rascunho atual e apagará do cache. Deseja continuar?")) {
            localStorage.removeItem(DRAFT_KEY);
            rascunhoAtual = {
                nome: 'Rascunho Vazio',
                arquivos: {
                    "index.html": "",
                    "style.css": "",
                    "script.js": ""
                },
                tarefa: TAREFA_PADRAO,
                areaProjeto: null,
                arquivoAtivo: 'index.html'
            };
            
            lblCurrentFile.textContent = 'index.html';
            if (statusBarFile) statusBarFile.textContent = 'index.html';
            if (statusBarLang) statusBarLang.textContent = 'HTML';
            
            setEditorValue('');
            updateEditorMode('index.html');
            
            renderizarArvoreArquivos();
                renderizarAbas();
            
            setTimeout(() => { if (editorInstance) editorInstance.refresh(); }, 50);

            salvarRascunhoLocal();
            atualizarPreview();
            atualizarStatusTarefa();
        }
    });

    document.getElementById('btnIdeNovoModelo').addEventListener('click', () => {
        alert("Escolha um modelo na lista inicial para substituí-lo.");
    });

    const btnLoadSoftUiEmpty = document.getElementById('btnLoadSoftUiEmpty');
    if (btnLoadSoftUiEmpty) {
        btnLoadSoftUiEmpty.addEventListener('click', () => {
            rascunhoAtual = {
                nome: TEMPLATES['soft-ui'].nome,
                arquivos: { ...TEMPLATES['soft-ui'].arquivos },
                arquivoAtivo: 'index.html',
                tarefa: TAREFA_PADRAO,
                areaProjeto: null
            };
            
            lblCurrentFile.textContent = 'index.html';
            if (statusBarFile) statusBarFile.textContent = 'index.html';
            if (statusBarLang) statusBarLang.textContent = 'HTML';
            
            setEditorValue(rascunhoAtual.arquivos['index.html']);
            updateEditorMode('index.html');
            
            renderizarArvoreArquivos();
                renderizarAbas();
            
            setTimeout(() => { if (editorInstance) editorInstance.refresh(); }, 50);

            salvarRascunhoLocal();
            atualizarPreview();
            atualizarStatusTarefa();
        });
    }

    window.addEventListener('beforeunload', (e) => {
        if (unsavedChanges) {
            e.preventDefault();
            e.returnValue = '';
        }
    });

    window.addEventListener('resize', () => {
        if (editorInstance) editorInstance.refresh();
    });

    // 11. Modal de Revisão / Exportação
    function abrirModalRevisao() {
        const modal = document.getElementById("ideReviewModal");
        if (!modal) {
            console.warn("[IDE] Modal de revisão não encontrado.");
            return;
        }

        salvarRascunhoLocal();

        modal.classList.remove("hidden");
        modal.classList.add("is-open");
        modal.setAttribute("aria-hidden", "false");
        document.body.classList.add("ide-modal-open");
        
        // Reset do formulário e estados de sucesso/erro
        const btnPR = document.getElementById('btnIdeGitHubPR');
        if (btnPR) {
            btnPR.disabled = false;
            btnPR.style.opacity = '1';
        }
        const loadingDiv = document.getElementById('ideReviewLoading');
        const successDiv = document.getElementById('ideReviewSuccess');
        const erroMsg = document.getElementById('ideReviewGitHubStatus');
        
        if (loadingDiv) loadingDiv.style.display = 'none';
        if (successDiv) successDiv.style.display = 'none';
        if (erroMsg) erroMsg.style.display = 'none';
        
        const descInput = document.getElementById('ideReviewDescription');
        if (descInput) descInput.value = '';
        
        const titleInput = document.getElementById('ideReviewTitle');
        if (titleInput) titleInput.value = '';
        
        const chkPreview = document.getElementById('chkPreview');
        const chkEscopo = document.getElementById('chkEscopo');
        const chkDados = document.getElementById('chkDados');
        
        if (chkPreview) chkPreview.checked = false;
        if (chkEscopo) chkEscopo.checked = false;
        if (chkDados) chkDados.checked = true;
        
        // Preencher detalhes da tarefa no modal
        const taskContainer = document.getElementById("ideReviewTaskChecklistContainer");
        const taskList = document.getElementById("ideReviewTaskChecklist");
        
        if (rascunhoAtual.tarefa) {
            const tarefaCompleta = TAREFAS_GUIADAS.find(t => t.id === rascunhoAtual.tarefa.id);
            if (tarefaCompleta && tarefaCompleta.checklist && tarefaCompleta.checklist.length > 0) {
                taskContainer.style.display = "flex";
                taskList.innerHTML = "";
                tarefaCompleta.checklist.forEach(item => {
                    taskList.innerHTML += `
                        <label style="display: flex; gap: 8px; cursor: pointer;">
                            <input type="checkbox" class="chk-tarefa-item" data-id="${item.id}" data-texto="${item.texto}"> ${item.texto}
                        </label>
                    `;
                });
            } else {
                taskContainer.style.display = "none";
                taskList.innerHTML = "";
            }
        } else {
            taskContainer.style.display = "none";
            taskList.innerHTML = "";
        }

        // Amarrar o evento Validar Agora e os checkboxes
        setTimeout(() => {
            renderizarRelatorioValidacao();
            
            const btnValidarAgora = document.getElementById('btnIdeValidarAgora');
            if (btnValidarAgora && !btnValidarAgora.dataset.bound) {
                btnValidarAgora.dataset.bound = "true";
                btnValidarAgora.addEventListener('click', renderizarRelatorioValidacao);
            }
            
            ['chkPreview', 'chkEscopo', 'chkDados'].forEach(id => {
                const el = document.getElementById(id);
                if (el && !el.dataset.bound) {
                    el.dataset.bound = "true";
                    el.addEventListener('change', renderizarRelatorioValidacao);
                }
            });
            
            document.querySelectorAll('.chk-tarefa-item').forEach(el => {
                if (!el.dataset.bound) {
                    el.dataset.bound = "true";
                    el.addEventListener('change', renderizarRelatorioValidacao);
                }
            });
            
            const selectArea = document.getElementById("ideReviewArea");
            if (selectArea && !selectArea.dataset.bound) {
                selectArea.dataset.bound = "true";
                selectArea.addEventListener('change', (e) => {
                    const selectedId = e.target.value;
                    if (selectedId) {
                        // MAPA_PROJETO_IDE está disponível no contexto global (app.js/equipe-ide.js)
                        const area = MAPA_PROJETO_IDE.find(a => a.id === selectedId);
                        if (area) {
                            rascunhoAtual.areaProjeto = {
                                id: area.id,
                                nome: area.nome,
                                perfil: area.perfil,
                                status: area.status
                            };
                        }
                    } else {
                        rascunhoAtual.areaProjeto = null;
                    }
                    salvarRascunhoLocal();
                    renderizarRelatorioValidacao();
                });
            }
        }, 50);
    }

    function obterChecklistGeral() {
        return {
            previewTestado: document.getElementById('chkPreview')?.checked || false,
            semDadosSensiveis: document.getElementById('chkDados')?.checked || false,
            escopoConfirmado: document.getElementById('chkEscopo')?.checked || false
        };
    }

    function renderizarRelatorioValidacao() {
        if (!window.IdeValidacoes) return null;

        salvarRascunhoLocal();
        
        if (rascunhoAtual.checklistTarefa) {
            const chks = document.querySelectorAll('.chk-tarefa-item');
            chks.forEach(chk => {
                const id = chk.getAttribute('data-id');
                const t = rascunhoAtual.checklistTarefa.find(x => x.id === id);
                if (t) t.marcado = chk.checked;
            });
        }
        
        const relatorio = window.IdeValidacoes.gerarRelatorioValidacao(rascunhoAtual, obterChecklistGeral());
        
        const container = document.getElementById('ideReviewValidationReport');
        const summary = document.getElementById('ideReviewValidationSummary');
        const list = document.getElementById('ideReviewValidationList');
        const btnPR = document.getElementById('btnIdeGitHubPR');
        
        if (!container || !summary || !list) return relatorio;
        
        container.style.display = 'flex';
        
        summary.innerHTML = `
            <div style="color: ${relatorio.bloqueios.length > 0 ? 'var(--ide-error)' : 'inherit'}">Bloqueios: <strong>${relatorio.bloqueios.length}</strong></div>
            <div style="color: ${relatorio.avisos.length > 0 ? 'var(--ide-warning)' : 'inherit'}">Avisos: <strong>${relatorio.avisos.length}</strong></div>
            <div style="color: var(--ide-muted)">Informações: <strong>${relatorio.infos.length}</strong></div>
        `;
        
        list.innerHTML = '';
        const todos = window.IdeValidacoes.achatarRelatorio(relatorio);
        
        todos.forEach(v => {
            const el = document.createElement('div');
            let color = 'var(--ide-muted)';
            let bg = 'transparent';
            if (v.severidade === 'bloqueio') { color = 'var(--ide-error)'; bg = 'rgba(239,68,68,0.1)'; }
            if (v.severidade === 'aviso') { color = 'var(--ide-warning)'; bg = 'rgba(245,158,11,0.1)'; }
            
            el.style.padding = '8px';
            el.style.borderRadius = '4px';
            el.style.background = bg;
            el.style.borderLeft = `3px solid ${color}`;
            
            el.innerHTML = `
                <div style="font-weight: 600; color: ${color}; margin-bottom: 2px;">${v.titulo}</div>
                <div style="color: var(--ide-text-light);">${v.mensagem}</div>
                ${v.arquivo ? `<div style="font-size: 0.75rem; color: var(--ide-muted); margin-top: 4px;">Arquivo: ${v.arquivo}</div>` : ''}
            `;
            list.appendChild(el);
        });
        
        if (relatorio.bloqueios.length > 0) {
            btnPR.disabled = true;
            btnPR.style.opacity = '0.5';
            btnPR.style.cursor = 'not-allowed';
            btnPR.title = "Corrija os bloqueios antes de criar o Pull Request.";
            
            const btnAlert = document.createElement('div');
            btnAlert.id = "ideReviewBtnAlert";
            btnAlert.style.color = "var(--ide-error)";
            btnAlert.style.fontSize = "0.85rem";
            btnAlert.style.marginTop = "8px";
            btnAlert.innerText = "Corrija os bloqueios antes de criar o Pull Request.";
            
            const existingAlert = document.getElementById("ideReviewBtnAlert");
            if (!existingAlert) {
                btnPR.parentNode.appendChild(btnAlert);
            }
        } else {
            btnPR.disabled = false;
            btnPR.style.opacity = '1';
            btnPR.style.cursor = 'pointer';
            btnPR.title = "";
            
            const existingAlert = document.getElementById("ideReviewBtnAlert");
            if (existingAlert) existingAlert.remove();
        }
        
        return relatorio;
    }

    function fecharModalRevisao() {
        const modal = document.getElementById("ideReviewModal");
        if (!modal) return;

        modal.classList.add("hidden");
        modal.classList.remove("is-open");
        modal.setAttribute("aria-hidden", "true");
        document.body.classList.remove("ide-modal-open");
    }
    
    const btnOpenReview = document.getElementById("btnIdeOpenReview");
    if (btnOpenReview) {
        btnOpenReview.addEventListener("click", (event) => {
            event.preventDefault();
            console.log("[IDE] Botão Preparar revisão clicado");
            console.log("[IDE] Modal:", document.getElementById("ideReviewModal"));
            atualizarSelectAreaProjeto();
            abrirModalRevisao();
        });
    } else {
        console.warn("[IDE] Botão btnIdeOpenReview não encontrado.");
    }

    document.getElementById("btnIdeCloseReview")?.addEventListener("click", fecharModalRevisao);

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
            fecharModalRevisao();
        }
    });

    document.getElementById('btnIdeCopyPR').addEventListener('click', async () => {
        const desc = document.getElementById('ideReviewDescription').value || "Não informada";
        
        let markdown = `Título sugerido:\nProtótipo: [${rascunhoAtual.nome}]\n\n`;
        markdown += `Descrição:\nAlterações feitas:\n${desc}\n\n`;
        markdown += `Arquivos:\n- index.html\n- style.css\n- script.js\n\n`;
        markdown += `Checklist:\n- [x] Preview testado\n- [x] Sem dados sensíveis\n- [x] Pronto para revisão\n`;

        try {
            await navigator.clipboard.writeText(markdown);
            alert("Resumo de PR copiado para a área de transferência!");
        } catch (e) {
            alert("Erro ao copiar. Seu navegador pode não suportar ou você não tem permissão.");
        }
    });

    document.getElementById('btnIdeExportFiles').addEventListener('click', async () => {
        const code = rascunhoAtual.arquivos[rascunhoAtual.arquivoAtivo];
        try {
            await navigator.clipboard.writeText(code);
            alert(`Código do ${rascunhoAtual.arquivoAtivo} copiado com sucesso!`);
        } catch (e) {
            alert("Erro ao copiar código.");
        }
    });

    // START IDE
    inicializarEditor();
    carregarRascunhoSalvo();
    abrirArquivo(rascunhoAtual.arquivoAtivo);
    atualizarPreview();
    atualizarStatusTarefa();
    renderizarTarefasGuiadas();

    // 12. Integração GitHub
    let githubStatus = { enabled: false, canCreatePullRequest: false };
    let githubPessoalStatus = { conectado: false, podeConectar: false, login: '' };

    async function checkGitHubStatus() {
        try {
            const resp = await CasaMulherAuth.apiFetch('/api/equipe-ide/github/status', { method: 'GET' });
            if (resp && resp.ok) {
                const status = await resp.json();
                if (status.enabled) {
                    githubStatus = status;
                }
            }
            
            // Check Conexão Pessoal
            const respPessoal = await CasaMulherAuth.apiFetch('/api/equipe-ide/github/conexao/status', { method: 'GET' });
            if (respPessoal && respPessoal.ok) {
                githubPessoalStatus = await respPessoal.json();
            }
        } catch (e) {
            console.error("Erro ao checar status do GitHub IDE", e);
        }
        
        const statusDiv = document.getElementById('ideReviewGitHubStatus');
        const btnPR = document.getElementById('btnIdeGitHubPR');
        const statusBarGithub = document.getElementById('ideStatusBarGithub');
        
        // Elementos Fase 2B
        const connArea = document.getElementById('ideGitHubConnectionArea');
        const notConnDiv = document.getElementById('ideGitHubNotConnected');
        const connDiv = document.getElementById('ideGitHubConnected');
        const modoEnvioArea = document.getElementById('ideModoEnvioArea');
        const lblModoForkPessoal = document.getElementById('lblModoForkPessoal');
        const rdModoForkPessoal = document.querySelector('input[name="ideModoEnvio"][value="forkPessoal"]');
        
        if (githubStatus.enabled && githubStatus.canCreatePullRequest) {
            statusDiv.style.display = 'none';
            btnPR.disabled = false;
            btnPR.style.opacity = '1';
            btnPR.style.cursor = 'pointer';
            if (statusBarGithub) statusBarGithub.innerHTML = 'Preview isolado &bull; GitHub ativo';
            
            // Lógica Fase 2B UI
            if (githubPessoalStatus.podeConectar || githubPessoalStatus.podeCriarFork) {
                connArea.style.display = 'block';
                modoEnvioArea.style.display = 'block';
                
                if (githubPessoalStatus.conectado) {
                    notConnDiv.style.display = 'none';
                    connDiv.style.display = 'flex';
                    document.getElementById('ideGitHubAvatar').src = githubPessoalStatus.avatarUrl || '';
                    document.getElementById('ideGitHubLogin').textContent = '@' + githubPessoalStatus.login;
                    
                    if (githubPessoalStatus.podeCriarFork) {
                        lblModoForkPessoal.style.opacity = '1';
                        rdModoForkPessoal.disabled = false;
                        rdModoForkPessoal.checked = true; // Seleciona modo pessoal por padrão
                    }
                } else {
                    notConnDiv.style.display = 'flex';
                    connDiv.style.display = 'none';
                    lblModoForkPessoal.style.opacity = '0.5';
                    rdModoForkPessoal.disabled = true;
                }
            }
        } else {
            statusDiv.style.display = 'block';
            statusDiv.textContent = "GitHub ainda não configurado neste ambiente. Você ainda pode copiar o resumo ou exportar os arquivos.";
            btnPR.disabled = true;
            btnPR.style.opacity = '0.5';
            btnPR.style.cursor = 'not-allowed';
            if (connArea) connArea.style.display = 'none';
            if (modoEnvioArea) modoEnvioArea.style.display = 'none';
        }
    }

    // Handlers Conexão
    document.getElementById('btnIdeConnectGitHub')?.addEventListener('click', async () => {
        try {
            const resp = await CasaMulherAuth.apiFetch('/api/equipe-ide/github/conectar', { method: 'GET' });
            if (resp && resp.ok) {
                const data = await resp.json();
                window.location.href = data.url;
            } else {
                alert('Erro ao iniciar a conexão com o GitHub.');
            }
        } catch (e) {
            console.error(e);
            alert('Erro ao conectar.');
        }
    });
    
    document.getElementById('btnIdeDisconnectGitHub')?.addEventListener('click', async () => {
        if(confirm('Tem certeza que deseja desconectar sua conta do GitHub?')) {
            try {
                const resp = await CasaMulherAuth.apiFetch('/api/equipe-ide/github/conexao', { method: 'DELETE' });
                if (resp.ok) {
                    window.location.reload();
                } else {
                    alert('Erro ao desconectar.');
                }
            } catch(e) {
                alert('Erro ao desconectar.');
            }
        }
    });

    // Handle OAuth Callback Query Params
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.has('github')) {
        const result = urlParams.get('github');
        if (result === 'conectado') {
            setTimeout(() => alert('GitHub conectado com sucesso!'), 500);
        } else if (result === 'erro') {
            setTimeout(() => alert('Não foi possível conectar ao GitHub. Tente novamente.'), 500);
        }
        window.history.replaceState({}, document.title, window.location.pathname);
    }

    checkGitHubStatus();

    document.getElementById('btnIdeGitHubPR').addEventListener('click', async () => {
        if (!githubStatus.enabled || !githubStatus.canCreatePullRequest) return;
        
        // Validação de checklists obrigatórios
        const modoEl = document.querySelector('input[name="ideModoEnvio"]:checked');
        const modo = modoEl ? modoEl.value : 'modoSeguroEquipe';

        // Recalcular validações rigorosamente antes de enviar
        const relatorioFinal = renderizarRelatorioValidacao();
        if (relatorioFinal && window.IdeValidacoes.temBloqueios(relatorioFinal)) {
            alert("A validação falhou e encontrou bloqueios. Por favor, corrija-os antes de enviar o Pull Request.");
            return;
        }

        const validacoesAchatadas = relatorioFinal ? window.IdeValidacoes.achatarRelatorio(relatorioFinal) : [];

        const btnPR = document.getElementById('btnIdeGitHubPR');
        const loadingDiv = document.getElementById('ideReviewLoading');
        const successDiv = document.getElementById('ideReviewSuccess');
        const prLink = document.getElementById('ideReviewPrLink');
        const erroMsg = document.getElementById('ideReviewGitHubStatus');

        btnPR.disabled = true;
        btnPR.style.opacity = '0.5';
        loadingDiv.style.display = 'flex';
        successDiv.style.display = 'none';
        erroMsg.style.display = 'none';
        erroMsg.style.background = "#fff3cd";
        erroMsg.style.color = "#856404";

        erroMsg.style.color = "#856404";

        const areaSelecionadaId = document.getElementById("ideReviewArea")?.value;
        let areaPayload = null;
        if (areaSelecionadaId) {
            const areaMap = MAPA_PROJETO_IDE.find(a => a.id === areaSelecionadaId);
            if (areaMap) {
                areaPayload = {
                    id: areaMap.id,
                    nome: areaMap.nome,
                    perfil: areaMap.perfil,
                    status: areaMap.status
                };
            }
        } else if (rascunhoAtual.areaProjeto) {
            areaPayload = rascunhoAtual.areaProjeto;
        }

        const payload = {
            modo: modo,
            titulo: document.getElementById('ideReviewTitle')?.value || `Protótipo: ${rascunhoAtual.nome}`,
            descricao: document.getElementById('ideReviewDescription')?.value || "Sem descrição",
            modelo: rascunhoAtual.nome,
            tarefa: rascunhoAtual.tarefa || TAREFA_PADRAO,
            areaProjeto: areaPayload,
            checklistTarefa: rascunhoAtual.checklistTarefa || [],
            arquivos: rascunhoAtual.arquivos,
            checklist: obterChecklistGeral(),
            validacoes: validacoesAchatadas
        };

        try {
            const res = await CasaMulherAuth.apiFetch('/api/equipe-ide/github/preparar-revisao', {
                method: 'POST',
                body: payload // apiFetch stringifies objects automatically
            });

            const resp = await res.json();

            if (res.ok && resp && resp.sucesso) {
                loadingDiv.style.display = 'none';
                successDiv.style.display = 'block';
                prLink.href = resp.pullRequestUrl;
                
                const btnCopy = document.getElementById('btnIdeCopyPrLink');
                if (btnCopy) {
                    btnCopy.onclick = async () => {
                        try {
                            await navigator.clipboard.writeText(resp.pullRequestUrl);
                            btnCopy.textContent = 'Copiado!';
                            setTimeout(() => { btnCopy.textContent = 'Copiar Link do PR'; }, 2000);
                        } catch (err) {
                            console.error('Erro ao copiar', err);
                        }
                    };
                }

                const btnNew = document.getElementById('btnIdeNewDraft');
                if (btnNew) {
                    btnNew.onclick = () => {
                        const btnLimpar = document.getElementById('btnIdeLimpar');
                        if (btnLimpar) btnLimpar.click();
                        document.getElementById('ideReviewModal').classList.add('hidden');
                        document.getElementById('ideReviewModal').classList.remove('is-open');
                    };
                }
                
                const statusBarSave = document.getElementById('statusBarSave');
                if (statusBarSave) {
                    statusBarSave.textContent = "Revisão enviada";
                    statusBarSave.style.color = "var(--ide-primary)";
                }
            } else {
                loadingDiv.style.display = 'none';
                erroMsg.style.display = 'block';
                erroMsg.textContent = resp ? resp.mensagem : "Ocorreu um erro ao enviar para revisão.";
                erroMsg.style.background = "#f8d7da";
                erroMsg.style.color = "#721c24";
                btnPR.disabled = false;
                btnPR.style.opacity = '1';
            }
        } catch (e) {
            loadingDiv.style.display = 'none';
            erroMsg.style.display = 'block';
            erroMsg.textContent = "Erro de conexão ao tentar enviar a revisão.";
            erroMsg.style.background = "#f8d7da";
            erroMsg.style.color = "#721c24";
            btnPR.disabled = false;
            btnPR.style.opacity = '1';
        }
    });

});
