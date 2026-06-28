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
        nome: 'Tela Soft UI',
        arquivos: { ...TEMPLATES['soft-ui'].arquivos },
        arquivoAtivo: 'index.html',
        tarefa: TAREFA_PADRAO
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
                // Normaliza formato
                if (!salvo.arquivos) salvo.arquivos = {};
                salvo.arquivos['index.html'] = salvo.arquivos['index.html'] || salvo.arquivos['html'] || '';
                salvo.arquivos['style.css'] = salvo.arquivos['style.css'] || salvo.arquivos['css'] || '';
                salvo.arquivos['script.js'] = salvo.arquivos['script.js'] || salvo.arquivos['js'] || '';
                
                // Prevenção contra rascunhos zumbis (onde index.html foi sobrescrito por css ou está vazio)
                const isInvalid = !salvo.arquivos['index.html'].trim() || (!salvo.arquivos['index.html'].includes('<html') && salvo.arquivos['style.css'].trim());
                
                if (isInvalid) {
                    console.warn("Rascunho antigo inválido ou corrompido, carregando vazio.");
                    localStorage.removeItem(DRAFT_KEY);
                } else if (salvo && salvo.arquivos) {
                    rascunhoAtual = salvo;
                    if (!rascunhoAtual.arquivoAtivo) rascunhoAtual.arquivoAtivo = 'index.html';
                    if (!rascunhoAtual.tarefa) rascunhoAtual.tarefa = TAREFA_PADRAO;
                    
                    document.getElementById('ideCurrentFileName').textContent = rascunhoAtual.arquivoAtivo;
                    atualizarStatusTarefa();
                    marcarComoSalvo();
                    console.log(`Rascunho restaurado. Atualizado em: ${salvo.atualizadoEm}`);
                }
            } catch (e) {
                console.error("Erro ao ler rascunho salvo.", e);
            }
        }
    }

    function salvarRascunhoLocal() {
        // Puxa do editor o valor do arquivo atual para o objeto antes de salvar
        rascunhoAtual.arquivos[rascunhoAtual.arquivoAtivo] = getEditorValue();
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

    // 9. Alternar abas de arquivo
    function abrirArquivo(filename) {
        // Salva arquivo antigo
        rascunhoAtual.arquivos[rascunhoAtual.arquivoAtivo] = getEditorValue();
        
        // Define novo arquivo
        rascunhoAtual.arquivoAtivo = filename;
        lblCurrentFile.textContent = filename;
        if (statusBarFile) statusBarFile.textContent = filename;
        if (statusBarLang) {
            if (filename.endsWith('.js')) statusBarLang.textContent = 'JavaScript';
            else if (filename.endsWith('.css')) statusBarLang.textContent = 'CSS';
            else statusBarLang.textContent = 'HTML';
        }
        
        // Carrega conteúdo
        setEditorValue(rascunhoAtual.arquivos[filename] || '');
        updateEditorMode(filename);
        
        // Atualiza UI dos botões
        fileButtons.forEach(btn => {
            if (btn.getAttribute('data-file') === filename) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });
        
        // Refresh CM para recalcular altura após troca de layout ou aba
        setTimeout(() => { if (editorInstance) editorInstance.refresh(); }, 50);
    }

    // 10. Ações da Toolbar/Sidebar
    fileButtons.forEach(btn => {
        btn.addEventListener('click', (e) => {
            const file = e.currentTarget.getAttribute('data-file');
            abrirArquivo(file);
        });
    });

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
                    tarefa: TAREFA_PADRAO
                };
                
                // Em vez de chamar abrirArquivo (que salvaria o editor atual no novo rascunho),
                // forçamos a atualização da UI manualmente para o index.html do novo template:
                lblCurrentFile.textContent = 'index.html';
                if (statusBarFile) statusBarFile.textContent = 'index.html';
                if (statusBarLang) statusBarLang.textContent = 'HTML';
                
                setEditorValue(rascunhoAtual.arquivos['index.html']);
                updateEditorMode('index.html');
                
                fileButtons.forEach(btn => {
                    if (btn.getAttribute('data-file') === 'index.html') btn.classList.add('active');
                    else btn.classList.remove('active');
                });
                
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
        
        fileButtons.forEach(b => {
            if (b.getAttribute('data-file') === 'index.html') b.classList.add('active');
            else b.classList.remove('active');
        });
        
        setTimeout(() => { if (editorInstance) editorInstance.refresh(); }, 50);

        salvarRascunhoLocal();
        atualizarPreview();
        atualizarStatusTarefa();
    }

    function atualizarStatusTarefa() {
        const badge = document.getElementById("statusBarTask");
        if (badge) {
            badge.textContent = `Tarefa: ${rascunhoAtual.tarefa ? rascunhoAtual.tarefa.titulo : "Livre"}`;
        }
    }

    btnSave.addEventListener('click', () => {
        salvarRascunhoLocal();
    });

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
                tarefa: TAREFA_PADRAO
            };
            
            lblCurrentFile.textContent = 'index.html';
            if (statusBarFile) statusBarFile.textContent = 'index.html';
            if (statusBarLang) statusBarLang.textContent = 'HTML';
            
            setEditorValue('');
            updateEditorMode('index.html');
            
            fileButtons.forEach(btn => {
                if (btn.getAttribute('data-file') === 'index.html') btn.classList.add('active');
                else btn.classList.remove('active');
            });
            
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
                tarefa: TAREFA_PADRAO
            };
            
            lblCurrentFile.textContent = 'index.html';
            if (statusBarFile) statusBarFile.textContent = 'index.html';
            if (statusBarLang) statusBarLang.textContent = 'HTML';
            
            setEditorValue(rascunhoAtual.arquivos['index.html']);
            updateEditorMode('index.html');
            
            fileButtons.forEach(btn => {
                if (btn.getAttribute('data-file') === 'index.html') btn.classList.add('active');
                else btn.classList.remove('active');
            });
            
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
        const chkPreview = document.getElementById('chkPreview').checked;
        const chkEscopo = document.getElementById('chkEscopo').checked;
        const chkDados = document.getElementById('chkDados').checked;

        if (!chkPreview || !chkEscopo || !chkDados) {
            alert("Por favor, marque todos os itens do checklist geral antes de enviar.");
            return;
        }

        const taskCheckboxes = document.querySelectorAll('.chk-tarefa-item');
        const checklistTarefa = [];
        let todasMarcadas = true;

        taskCheckboxes.forEach(chk => {
            if (!chk.checked) todasMarcadas = false;
            checklistTarefa.push({
                id: chk.getAttribute('data-id'),
                texto: chk.getAttribute('data-texto'),
                marcado: chk.checked
            });
        });

        if (taskCheckboxes.length > 0 && !todasMarcadas) {
            alert("Por favor, marque todos os itens do checklist da tarefa antes de enviar.");
            return;
        }

        const desc = document.getElementById('ideReviewDescription').value.trim();
        const customTitle = document.getElementById('ideReviewTitle')?.value.trim();
        
        const modoEl = document.querySelector('input[name="ideModoEnvio"]:checked');
        const modo = modoEl ? modoEl.value : 'modoSeguroEquipe';

        if (!chkPreview || !chkEscopo || !chkDados) {
            alert("Por favor, confirme todos os itens do checklist antes de enviar.");
            return;
        }

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

        const payload = {
            modo: modo,
            titulo: customTitle ? customTitle : `Protótipo: ${rascunhoAtual.nome}`,
            descricao: desc,
            modelo: rascunhoAtual.nome,
            tarefa: rascunhoAtual.tarefa || TAREFA_PADRAO,
            checklistTarefa: checklistTarefa,
            arquivos: rascunhoAtual.arquivos,
            checklist: {
                previewTestado: chkPreview,
                semDadosSensiveis: chkDados,
                escopoConfirmado: chkEscopo
            }
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
