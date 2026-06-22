document.addEventListener("DOMContentLoaded", setupProfessor);

let professorUsuario = null;
let cursoVinculado = null;
let alunasDoCurso = [];

// Storage keys
const STORAGE_ALUNAS = "casamulher_itaqua";
const STORAGE_CHAMADAS = "casamulher_professor_chamadas";
const STORAGE_PLANOS = "casamulher_professor_planos";
const STORAGE_ATIVIDADES = "casamulher_professor_atividades";

let atividadesSelecionadasPlano = [];
let atividadesSelecionadasChamada = [];
let aulaIdAtualChamada = null;

async function setupProfessor() {
    professorUsuario = await CasaMulherAuth.protegerPagina();
    
    if (!professorUsuario) return;

    if (professorUsuario.perfil !== "professor") {
        window.location.href = "painel.html";
        return;
    }

    inicializarSoftSessionCard(professorUsuario);

    cursoVinculado = professorUsuario.professorCurso || professorUsuario.ProfessorCurso;

    if (!cursoVinculado) {
        document.getElementById("professorEmptyState").classList.remove("hidden");
        return;
    }

    // Populate context card
    document.getElementById("professorCursoDisplay").textContent = cursoVinculado;
    document.getElementById("professorNomeDisplay").textContent = professorUsuario.nomeCompleto || "-";
    document.getElementById("professorIdDisplay").textContent = professorUsuario.identificadorFuncionario || "-";
    
    document.getElementById("professorShell").classList.remove("hidden");

    inicializarAtividades();
    setupChipsEvents();
    setupTabs();
    carregarAlunas();
    setupChamada();
    setupPlanoEnsino();
}

function normalizarTexto(texto) {
    if (!texto) return "";
    return texto.normalize("NFD").replace(/[\u0300-\u036f]/g, "").trim().toLowerCase();
}

function setupTabs() {
    const tabs = document.querySelectorAll(".professor-tab");
    tabs.forEach(tab => {
        tab.addEventListener("click", () => {
            // Remove active from all tabs
            tabs.forEach(t => t.classList.remove("active"));
            document.querySelectorAll(".professor-panel").forEach(p => p.classList.remove("active"));
            
            // Add active to clicked tab
            tab.classList.add("active");
            const tabId = tab.getAttribute("data-tab");
            document.getElementById(`tab-${tabId}`).classList.add("active");
        });
    });
}

function obterNomeCurso(idOuNome) {
    if (typeof CURSOS_RECEPCAO !== 'undefined') {
        const cursoEncontrado = CURSOS_RECEPCAO.find(c => c.id === idOuNome || c.nome === idOuNome);
        if (cursoEncontrado) return cursoEncontrado.nome;
    }
    return idOuNome;
}

/* ==========================================================================
   ATIVIDADES E CHIPS
   ========================================================================== */
function inicializarAtividades() {
    let dados = localStorage.getItem(STORAGE_ATIVIDADES);
    let atividadesMap = dados ? JSON.parse(dados) : {};
    
    if (!atividadesMap["Informática e Inclusão Digital"]) {
        atividadesMap["Informática e Inclusão Digital"] = [
            "Teclado", "Mouse", "Área de trabalho", "Digitação",
            "Navegador", "E-mail", "Pesquisa na internet", "Segurança digital"
        ];
        localStorage.setItem(STORAGE_ATIVIDADES, JSON.stringify(atividadesMap));
    }
    
    renderizarDatalistAtividades();
}

function renderizarDatalistAtividades() {
    const dados = localStorage.getItem(STORAGE_ATIVIDADES);
    const atividadesMap = dados ? JSON.parse(dados) : {};
    const atividades = atividadesMap[cursoVinculado] || [];
    
    const datalist = document.getElementById("atividadesCursoList");
    if(datalist) {
        datalist.innerHTML = "";
        atividades.forEach(a => {
            const option = document.createElement("option");
            option.value = a;
            datalist.appendChild(option);
        });
    }
}

function atividadeJaExiste(lista, nome) {
    const nomeNorm = normalizarTexto(nome);
    return lista.some(a => normalizarTexto(a) === nomeNorm);
}

function adicionarAtividadeGlobal(nomeAtividade) {
    let dados = localStorage.getItem(STORAGE_ATIVIDADES);
    let atividadesMap = dados ? JSON.parse(dados) : {};
    
    if (!atividadesMap[cursoVinculado]) {
        atividadesMap[cursoVinculado] = [];
    }
    
    if (!atividadeJaExiste(atividadesMap[cursoVinculado], nomeAtividade)) {
        atividadesMap[cursoVinculado].push(nomeAtividade.trim());
        localStorage.setItem(STORAGE_ATIVIDADES, JSON.stringify(atividadesMap));
        renderizarDatalistAtividades();
    }
}

function setupChipsEvents() {
    const btnAddPlano = document.getElementById("btnAdicionarAtividadePlano");
    const inputPlano = document.getElementById("novaAtividadePlano");
    const containerPlano = document.getElementById("chipsAtividadesPlano");
    
    if(btnAddPlano) {
        btnAddPlano.addEventListener("click", () => {
            const val = inputPlano.value.trim();
            if (val && !atividadeJaExiste(atividadesSelecionadasPlano, val)) {
                atividadesSelecionadasPlano.push(val);
                adicionarAtividadeGlobal(val);
                inputPlano.value = "";
                renderizarChips(atividadesSelecionadasPlano, containerPlano, 'plano');
            }
        });
        inputPlano.addEventListener("keypress", (e) => {
            if (e.key === "Enter") { e.preventDefault(); btnAddPlano.click(); }
        });
    }
    
    const btnAddChamada = document.getElementById("btnAdicionarAtividadeChamada");
    const inputChamada = document.getElementById("novaAtividadeChamada");
    const containerChamada = document.getElementById("chipsAtividadesChamada");
    
    if(btnAddChamada) {
        btnAddChamada.addEventListener("click", () => {
            const val = inputChamada.value.trim();
            if (val && !atividadeJaExiste(atividadesSelecionadasChamada, val)) {
                atividadesSelecionadasChamada.push(val);
                adicionarAtividadeGlobal(val);
                inputChamada.value = "";
                renderizarChips(atividadesSelecionadasChamada, containerChamada, 'chamada');
            }
        });
        inputChamada.addEventListener("keypress", (e) => {
            if (e.key === "Enter") { e.preventDefault(); btnAddChamada.click(); }
        });
    }
}

window.removerChip = function(index, tipo) {
    if (tipo === 'plano') {
        atividadesSelecionadasPlano.splice(index, 1);
        renderizarChips(atividadesSelecionadasPlano, document.getElementById("chipsAtividadesPlano"), 'plano');
    } else {
        atividadesSelecionadasChamada.splice(index, 1);
        renderizarChips(atividadesSelecionadasChamada, document.getElementById("chipsAtividadesChamada"), 'chamada');
    }
}

function renderizarChips(lista, container, tipo) {
    container.innerHTML = "";
    lista.forEach((atv, i) => {
        const chip = document.createElement("div");
        chip.className = "professor-chip selected";
        chip.innerHTML = `
            ${atv}
            <button type="button" class="professor-chip-remove" onclick="removerChip(${i}, '${tipo}')">&times;</button>
        `;
        container.appendChild(chip);
    });
}

/* ==========================================================================
   ALUNAS
   ========================================================================== */
function carregarAlunas() {
    const dados = localStorage.getItem(STORAGE_ALUNAS);
    let todasAssistidas = [];
    
    if (dados) {
        todasAssistidas = JSON.parse(dados);
    }
    
    alunasDoCurso = todasAssistidas.filter(p => normalizarTexto(obterNomeCurso(p.curso)) === normalizarTexto(cursoVinculado));
    
    renderizarAlunas(alunasDoCurso);

    const inputBusca = document.getElementById("buscaAlunas");
    if (inputBusca) {
        inputBusca.addEventListener("input", (e) => {
            const termo = normalizarTexto(e.target.value);
            const filtradas = alunasDoCurso.filter(a => normalizarTexto(a.nome).includes(termo));
            renderizarAlunas(filtradas);
        });
    }
}

function renderizarAlunas(lista) {
    const grid = document.getElementById("alunasGrid");
    const empty = document.getElementById("alunasEmpty");
    
    grid.innerHTML = "";
    
    if (lista.length === 0) {
        grid.classList.add("hidden");
        empty.classList.remove("hidden");
        return;
    }
    
    grid.classList.remove("hidden");
    empty.classList.add("hidden");
    
    lista.forEach(aluna => {
        const card = document.createElement("div");
        card.className = "professor-aluna-card";
        
        const iconeTelefone = `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#D2AAB9" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="flex-shrink: 0;"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"></path></svg>`;
        const iconeData = `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#D2AAB9" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="flex-shrink: 0;"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>`;

        const telefone = aluna.telefone ? `<div class="professor-aluna-detail">${iconeTelefone} <span style="font-weight: 500;">${aluna.telefone}</span></div>` : "";
        const dataCadastro = aluna.dataAcolhimento ? `<div class="professor-aluna-detail">${iconeData} <span>Cadastrada em: ${new Date(aluna.dataAcolhimento).toLocaleDateString('pt-BR')}</span></div>` : "";
        
        card.innerHTML = `
            <div class="professor-aluna-name">${aluna.nome}</div>
            ${telefone}
            ${dataCadastro}
        `;
        grid.appendChild(card);
    });
}

/* ==========================================================================
   CHAMADA
   ========================================================================== */
function popularDatalistAulas() {
    const list = document.getElementById("aulasPlanejadasList");
    if(!list) return;
    list.innerHTML = "";
    
    const planos = carregarPlanosDoStorage();
    const aulasDoProfessor = planos.filter(p => p.professorId === professorUsuario.identificadorFuncionario && p.curso === cursoVinculado);
    
    aulasDoProfessor.sort((a, b) => a.data.localeCompare(b.data));
    aulasDoProfessor.forEach(aula => {
        const formatData = new Date(aula.data + "T00:00:00").toLocaleDateString('pt-BR');
        const option = document.createElement("option");
        option.value = aula.tema;
        option.textContent = `${formatData} — ${aula.tema}`;
        list.appendChild(option);
    });
}

function setupChamada() {
    const inputData = document.getElementById("dataChamada");
    const inputTema = document.getElementById("temaAulaInput");
    const btnSalvar = document.getElementById("btnSalvarChamada");
    
    popularDatalistAulas();

    const hoje = new Date().toISOString().split('T')[0];
    inputData.value = hoje;
    
    inputData.addEventListener("change", () => carregarChamadaDoDia(inputData.value));
    
    inputTema.addEventListener("change", () => {
        const planos = carregarPlanosDoStorage();
        const aulaEncontrada = planos.find(p => p.professorId === professorUsuario.identificadorFuncionario && p.curso === cursoVinculado && normalizarTexto(p.tema) === normalizarTexto(inputTema.value));
        if (aulaEncontrada) {
            aulaIdAtualChamada = aulaEncontrada.id;
            atividadesSelecionadasChamada = [...(aulaEncontrada.atividades || [])];
            renderizarChips(atividadesSelecionadasChamada, document.getElementById("chipsAtividadesChamada"), 'chamada');
        } else {
            aulaIdAtualChamada = null;
        }
    });

    btnSalvar.addEventListener("click", salvarChamada);
    
    carregarChamadaDoDia(hoje);
}

function carregarChamadasDoStorage() {
    const dados = localStorage.getItem(STORAGE_CHAMADAS);
    return dados ? JSON.parse(dados) : [];
}

function carregarChamadaDoDia(data) {
    const chamadas = carregarChamadasDoStorage();
    const chamadaDoDia = chamadas.find(c => c.data === data && c.curso === cursoVinculado && c.professorId === professorUsuario.identificadorFuncionario);
    
    const inputTema = document.getElementById("temaAulaInput");
    
    if (chamadaDoDia) {
        inputTema.value = chamadaDoDia.tema || "";
        aulaIdAtualChamada = chamadaDoDia.aulaId || null;
        atividadesSelecionadasChamada = [...(chamadaDoDia.atividades || [])];
        renderizarChips(atividadesSelecionadasChamada, document.getElementById("chipsAtividadesChamada"), 'chamada');
        renderizarListaChamada(chamadaDoDia.registros);
    } else {
        const planos = carregarPlanosDoStorage();
        const aulaPlanejada = planos.find(p => p.professorId === professorUsuario.identificadorFuncionario && p.curso === cursoVinculado && p.data === data);
        
        if (aulaPlanejada) {
            inputTema.value = aulaPlanejada.tema;
            aulaIdAtualChamada = aulaPlanejada.id;
            atividadesSelecionadasChamada = [...(aulaPlanejada.atividades || [])];
        } else {
            inputTema.value = "";
            aulaIdAtualChamada = null;
            atividadesSelecionadasChamada = [];
        }
        renderizarChips(atividadesSelecionadasChamada, document.getElementById("chipsAtividadesChamada"), 'chamada');

        const registrosEmBranco = alunasDoCurso.map(a => ({
            alunaId: a.id,
            nome: a.nome,
            status: null,
            observacao: ""
        }));
        renderizarListaChamada(registrosEmBranco);
    }
    
    atualizarAvisoChamada();
}

function renderizarListaChamada(registros) {
    const list = document.getElementById("chamadaList");
    list.innerHTML = "";
    
    if (registros.length === 0) {
        list.innerHTML = `<div style="text-align: center; color: #A26D85; padding: 20px;">Nenhuma aluna matriculada neste curso para fazer chamada.</div>`;
        return;
    }
    
    registros.forEach(reg => {
        const card = document.createElement("div");
        card.className = "professor-chamada-card";
        card.dataset.alunaId = reg.alunaId;
        
        card.innerHTML = `
            <div style="flex: 1; min-width: 200px;">
                <div style="font-weight: 700; color: #8A3D66; font-size: 1.05rem;">${reg.nome}</div>
                <input type="text" class="soft-input obs-input" placeholder="Observação (opcional)" value="${reg.observacao || ''}" style="margin-top: 8px; width: 100%; max-width: 300px; padding: 4px 8px; font-size: 0.85rem;">
            </div>
            <div class="professor-status-group">
                <button type="button" class="professor-status-button ${reg.status === 'presente' ? 'active' : ''}" data-status="presente">Presente</button>
                <button type="button" class="professor-status-button ${reg.status === 'faltou' ? 'active' : ''}" data-status="faltou">Faltou</button>
                <button type="button" class="professor-status-button ${reg.status === 'justificada' ? 'active' : ''}" data-status="justificada">Justificada</button>
            </div>
        `;
        
        const btns = card.querySelectorAll(".professor-status-button");
        btns.forEach(btn => {
            btn.addEventListener("click", () => {
                btns.forEach(b => b.classList.remove("active"));
                btn.classList.add("active");
                atualizarAvisoChamada();
            });
        });
        
        list.appendChild(card);
    });
}

function atualizarAvisoChamada() {
    const aviso = document.getElementById("chamadaAviso");
    const list = document.getElementById("chamadaList");
    const cards = list.querySelectorAll(".professor-chamada-card");
    
    let todosPreenchidos = true;
    cards.forEach(card => {
        const ativo = card.querySelector(".professor-status-button.active");
        if (!ativo) todosPreenchidos = false;
    });
    
    if (cards.length > 0 && !todosPreenchidos) {
        aviso.classList.remove("hidden");
    } else {
        aviso.classList.add("hidden");
    }
}

function salvarChamada() {
    const data = document.getElementById("dataChamada").value;
    const tema = document.getElementById("temaAulaInput").value.trim();
    const feedback = document.getElementById("chamadaFeedback");
    
    if (!data) {
        feedback.textContent = "Selecione a data da aula.";
        feedback.style.color = "#F44336";
        return;
    }
    
    let aulaId = aulaIdAtualChamada;
    
    if (tema) {
        const planos = carregarPlanosDoStorage();
        if (!aulaId) {
            const aulaExist = planos.find(p => p.professorId === professorUsuario.identificadorFuncionario && p.curso === cursoVinculado && normalizarTexto(p.tema) === normalizarTexto(tema));
            if (aulaExist) {
                aulaId = aulaExist.id;
            } else {
                aulaId = Date.now().toString();
                planos.push({
                    id: aulaId,
                    professorId: professorUsuario.identificadorFuncionario,
                    curso: cursoVinculado,
                    data: data,
                    tema: tema,
                    atividades: [...atividadesSelecionadasChamada],
                    criadoEm: new Date().toISOString(),
                    origem: "criado_pela_chamada"
                });
                localStorage.setItem(STORAGE_PLANOS, JSON.stringify(planos));
                popularDatalistAulas();
                renderizarPlanoEnsino();
            }
        }
        
        if (aulaId) {
            const index = planos.findIndex(p => p.id === aulaId);
            if (index >= 0) {
                let planAtvs = planos[index].atividades || [];
                atividadesSelecionadasChamada.forEach(atv => {
                    if (!atividadeJaExiste(planAtvs, atv)) {
                        planAtvs.push(atv);
                    }
                });
                planos[index].atividades = planAtvs;
                localStorage.setItem(STORAGE_PLANOS, JSON.stringify(planos));
                renderizarPlanoEnsino();
            }
        }
    }
    
    const list = document.getElementById("chamadaList");
    const cards = list.querySelectorAll(".professor-chamada-card");
    
    const registros = [];
    cards.forEach(card => {
        const alunaId = card.dataset.alunaId;
        const nome = card.querySelector("div[style*='font-weight: 700']").textContent;
        const obs = card.querySelector(".obs-input").value;
        const btnAtivo = card.querySelector(".professor-status-button.active");
        const status = btnAtivo ? btnAtivo.dataset.status : null;
        
        registros.push({ alunaId, nome, status, observacao: obs });
    });
    
    const chamadas = carregarChamadasDoStorage();
    const indexExistente = chamadas.findIndex(c => c.data === data && c.curso === cursoVinculado && c.professorId === professorUsuario.identificadorFuncionario);
    
    const chamadaParaSalvar = {
        id: `${professorUsuario.identificadorFuncionario}|${cursoVinculado}|${data}`,
        professorId: professorUsuario.identificadorFuncionario,
        professorNome: professorUsuario.nomeCompleto,
        curso: cursoVinculado,
        data: data,
        tema: tema,
        aulaId: aulaId,
        atividades: [...atividadesSelecionadasChamada],
        registros: registros,
        atualizadoEm: new Date().toISOString()
    };
    
    if (indexExistente >= 0) {
        chamadas[indexExistente] = chamadaParaSalvar;
    } else {
        chamadas.push(chamadaParaSalvar);
    }
    
    localStorage.setItem(STORAGE_CHAMADAS, JSON.stringify(chamadas));
    
    aulaIdAtualChamada = aulaId; // Update state after creation
    
    feedback.textContent = "Chamada salva com sucesso!";
    feedback.style.color = "#4CAF50";
    setTimeout(() => { feedback.textContent = ""; }, 3000);
}

/* ==========================================================================
   PLANO DE ENSINO
   ========================================================================== */
let editandoPlanoId = null;

function setupPlanoEnsino() {
    const btnNovaAula = document.getElementById("btnNovaAula");
    const formPlano = document.getElementById("formPlanoEnsino");
    const btnCancelar = document.getElementById("btnCancelarAula");
    
    btnNovaAula.addEventListener("click", () => {
        formPlano.reset();
        editandoPlanoId = null;
        atividadesSelecionadasPlano = [];
        renderizarChips(atividadesSelecionadasPlano, document.getElementById("chipsAtividadesPlano"), 'plano');
        document.getElementById("tituloFormPlano").textContent = "Nova aula";
        formPlano.classList.remove("hidden");
    });
    
    btnCancelar.addEventListener("click", () => {
        formPlano.classList.add("hidden");
    });
    
    formPlano.addEventListener("submit", (e) => {
        e.preventDefault();
        salvarAulaPlano();
    });
    
    renderizarPlanoEnsino();
}

function carregarPlanosDoStorage() {
    const dados = localStorage.getItem(STORAGE_PLANOS);
    return dados ? JSON.parse(dados) : [];
}

function salvarAulaPlano() {
    const data = document.getElementById("aulaData").value;
    const tema = document.getElementById("aulaTema").value;
    const obs = document.getElementById("aulaObservacoes").value;
    
    const planos = carregarPlanosDoStorage();
    
    if (editandoPlanoId) {
        const index = planos.findIndex(p => p.id === editandoPlanoId);
        if (index >= 0) {
            planos[index].data = data;
            planos[index].tema = tema;
            planos[index].atividades = [...atividadesSelecionadasPlano];
            planos[index].observacoes = obs;
            planos[index].atualizadoEm = new Date().toISOString();
        }
    } else {
        planos.push({
            id: Date.now().toString(),
            professorId: professorUsuario.identificadorFuncionario,
            curso: cursoVinculado,
            data: data,
            tema: tema,
            atividades: [...atividadesSelecionadasPlano],
            observacoes: obs,
            origem: "plano",
            criadoEm: new Date().toISOString()
        });
    }
    
    localStorage.setItem(STORAGE_PLANOS, JSON.stringify(planos));
    popularDatalistAulas();
    document.getElementById("formPlanoEnsino").classList.add("hidden");
    renderizarPlanoEnsino();
}

window.excluirAulaPlano = function(id) {
    if (!confirm("Tem certeza que deseja excluir esta aula do plano de ensino?")) return;
    
    let planos = carregarPlanosDoStorage();
    planos = planos.filter(p => p.id !== id);
    localStorage.setItem(STORAGE_PLANOS, JSON.stringify(planos));
    popularDatalistAulas();
    renderizarPlanoEnsino();
}

window.editarAulaPlano = function(id) {
    const planos = carregarPlanosDoStorage();
    const aula = planos.find(p => p.id === id);
    if (!aula) return;
    
    editandoPlanoId = aula.id;
    document.getElementById("tituloFormPlano").textContent = "Editar aula";
    document.getElementById("aulaData").value = aula.data;
    document.getElementById("aulaTema").value = aula.tema;
    document.getElementById("aulaObservacoes").value = aula.observacoes || aula.conteudo || "";
    
    atividadesSelecionadasPlano = [...(aula.atividades || [])];
    renderizarChips(atividadesSelecionadasPlano, document.getElementById("chipsAtividadesPlano"), 'plano');
    
    document.getElementById("formPlanoEnsino").classList.remove("hidden");
}

function renderizarPlanoEnsino() {
    const grid = document.getElementById("planoGrid");
    const empty = document.getElementById("planoEmpty");
    
    const planos = carregarPlanosDoStorage();
    const aulasDoProfessor = planos.filter(p => p.professorId === professorUsuario.identificadorFuncionario && p.curso === cursoVinculado);
    
    aulasDoProfessor.sort((a, b) => a.data.localeCompare(b.data));
    
    grid.innerHTML = "";
    
    if (aulasDoProfessor.length === 0) {
        grid.classList.add("hidden");
        empty.classList.remove("hidden");
        return;
    }
    
    grid.classList.remove("hidden");
    empty.classList.add("hidden");
    
    aulasDoProfessor.forEach(aula => {
        const card = document.createElement("div");
        card.className = "professor-aula-card";
        
        const formatData = new Date(aula.data + "T00:00:00").toLocaleDateString('pt-BR');
        
        let chipsHtml = "";
        if (aula.atividades && aula.atividades.length > 0) {
            chipsHtml = `<div class="professor-chip-list" style="margin-top: 12px;">` + 
                aula.atividades.map(a => `<div class="professor-chip">${a}</div>`).join("") +
            `</div>`;
        }
        
        let obsTexto = aula.observacoes || aula.conteudo || "";
        let obsHtml = obsTexto ? `<div style="font-size: 0.9rem; color: #666; margin-top: 12px;"><strong>Observações:</strong> ${obsTexto}</div>` : '';
        
        let origemHtml = aula.origem === "criado_pela_chamada" ? `<div class="professor-origin-badge">Criada pela chamada</div>` : '';

        card.innerHTML = `
            <div class="professor-aula-header">
                <div>
                    <div class="professor-aula-data">${formatData}</div>
                    <div class="professor-aula-tema">${aula.tema}</div>
                    ${origemHtml}
                </div>
                <div style="display: flex; gap: 8px;">
                    <button type="button" class="professor-action-secondary" style="padding: 4px 12px; font-size: 0.8rem;" onclick="editarAulaPlano('${aula.id}')">Editar</button>
                    <button type="button" class="professor-action-danger" style="padding: 4px 12px; font-size: 0.8rem;" onclick="excluirAulaPlano('${aula.id}')">Excluir</button>
                </div>
            </div>
            ${chipsHtml}
            ${obsHtml}
        `;
        grid.appendChild(card);
    });
}
