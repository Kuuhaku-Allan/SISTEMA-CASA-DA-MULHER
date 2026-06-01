const API_BASE_URL = window.API_BASE_URL || "http://localhost:5001";
const PERFIS_LABEL = {
    adm: "Coordenacao / ADM",
    recepcao: "Recepcao",
    professor: "Professor",
    as_social: "Assistente Social",
    juridico: "Juridico"
};

function setMessage(element, text, type) {
    if (!element) {
        return;
    }

    element.textContent = text;
    element.className = `message ${type || ""}`.trim();
}

async function readApiMessage(response) {
    try {
        const data = await response.json();

        if (data.mensagem) {
            return data.mensagem;
        }

        if (Array.isArray(data.erros) && data.erros.length > 0) {
            return data.erros.join(" ");
        }

        if (data.errors) {
            return Object.values(data.errors).flat().join(" ");
        }
    } catch {
        return "Nao foi possivel ler a resposta da API.";
    }

    return "Nao foi possivel concluir a operacao.";
}

function disableSubmit(form, disabled) {
    const button = form?.querySelector("button[type='submit']");

    if (button) {
        button.disabled = disabled;
    }
}

function getAuthHeaders(includeJson) {
    return CasaMulherAuth.getAuthHeaders(includeJson);
}

function clearSession() {
    CasaMulherAuth.limparSessao();
}

function storeAuthResult(resultado) {
    CasaMulherAuth.salvarSessao(resultado);
}

function redirectAfterLogin(resultado) {
    if (resultado.deveTrocarSenha) {
        window.location.href = "trocar-senha.html";
        return;
    }

    window.location.href = "painel.html";
}

function bindLogoutButton(id) {
    document.getElementById(id)?.addEventListener("click", function () {
        CasaMulherAuth.logout();
    });
}

function formatDate(value) {
    if (!value) {
        return "-";
    }

    return new Date(value).toLocaleDateString("pt-BR");
}

function formatDateTime(value) {
    if (!value) {
        return "-";
    }

    return new Date(value).toLocaleString("pt-BR");
}

function formatPerfil(perfil) {
    return PERFIS_LABEL[perfil] || perfil || "-";
}

function formatAcaoAuditoria(acao) {
    const acoes = {
        CONVITE_CRIADO: "Convite criado",
        CONVITE_CANCELADO: "Convite cancelado",
        FUNCIONARIO_DESATIVADO: "Acesso desativado",
        FUNCIONARIO_REATIVADO: "Acesso reativado",
        PERFIL_ALTERADO: "Perfil alterado",
        SENHA_RESETADA: "Senha redefinida",
        DOIS_FATORES_RESETADO: "Autenticador redefinido",
        SENHA_TROCADA: "Senha trocada"
    };

    return acoes[acao] || acao || "-";
}

function formatDescricaoAuditoria(descricao) {
    return String(descricao || "-")
        .replaceAll("2FA", "autenticador");
}

function escapeHtml(value) {
    return String(value || "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

async function copyText(text, messageElement) {
    try {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
        } else {
            const textarea = document.createElement("textarea");
            textarea.value = text;
            textarea.setAttribute("readonly", "");
            textarea.style.position = "fixed";
            textarea.style.left = "-9999px";
            document.body.appendChild(textarea);
            textarea.select();
            document.execCommand("copy");
            document.body.removeChild(textarea);
        }

        setMessage(messageElement, "Copiado.", "success");
    } catch {
        setMessage(messageElement, "Nao foi possivel copiar automaticamente.", "error");
    }
}

function setupCadastro() {
    const form = document.getElementById("formCadastroFuncionario");
    const mensagem = document.getElementById("mensagemCadastro");

    if (!form) {
        return;
    }

    const params = new URLSearchParams(window.location.search);
    const emailParam = params.get("email");
    const codigoParam = params.get("codigo");
    const emailInput = document.getElementById("email");
    const codigoInput = document.getElementById("codigoCadastro");
    const avisoConviteReconhecido = document.getElementById("avisoConviteReconhecido");
    const ajudaConviteAutomatico = document.getElementById("ajudaConviteAutomatico");

    if (emailParam) {
        emailInput.value = emailParam;
        emailInput.readOnly = true;
    }

    if (codigoParam) {
        codigoInput.value = codigoParam;
        codigoInput.readOnly = true;
    }

    if (emailParam && codigoParam) {
        avisoConviteReconhecido?.classList.remove("hidden");
        ajudaConviteAutomatico?.classList.remove("hidden");
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form.reportValidity()) {
            return;
        }

        setMessage(mensagem, "Criando acesso...", "info");
        disableSubmit(form, true);

        const dados = {
            nomeCompleto: document.getElementById("nomeCompleto").value.trim(),
            email: document.getElementById("email").value.trim(),
            senha: document.getElementById("senha").value,
            confirmarSenha: document.getElementById("confirmarSenha").value,
            codigoCadastro: document.getElementById("codigoCadastro").value.trim()
        };

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/register-funcionario`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(dados)
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            const resultado = await response.json();
            const identificador = resultado.identificadorFuncionario;
            const textoSucesso = identificador
                ? `${resultado.mensagem || "Cadastro realizado com sucesso."} Seu ID: ${identificador}`
                : resultado.mensagem || "Cadastro realizado com sucesso.";

            if (identificador) {
                sessionStorage.setItem("ultimoIdentificadorFuncionario", identificador);
            }

            setMessage(mensagem, textoSucesso, "success");
            form.reset();

            setTimeout(function () {
                window.location.href = "index.html";
            }, 3500);
        } catch {
            setMessage(mensagem, "Nao foi possivel conectar a API.", "error");
        } finally {
            disableSubmit(form, false);
        }
    });
}

function setupLogin() {
    const form = document.getElementById("formLogin");
    const mensagem = document.getElementById("mensagemLogin");
    const form2fa = document.getElementById("formLogin2fa");
    const mensagem2fa = document.getElementById("mensagemLogin2fa");

    if (!form) {
        return;
    }

    const mensagemSessao = sessionStorage.getItem("mensagemLogin");

    if (mensagemSessao) {
        setMessage(mensagem, mensagemSessao, "info");
        sessionStorage.removeItem("mensagemLogin");
    }

    const ultimoIdentificador = sessionStorage.getItem("ultimoIdentificadorFuncionario");

    if (ultimoIdentificador) {
        document.getElementById("identificador").value = ultimoIdentificador;
        sessionStorage.removeItem("ultimoIdentificadorFuncionario");
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form.reportValidity()) {
            return;
        }

        setMessage(mensagem, "Entrando...", "info");
        disableSubmit(form, true);

        const dados = {
            identificador: document.getElementById("identificador").value.trim(),
            senha: document.getElementById("senha").value
        };

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(dados)
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            const resultado = await response.json();

            if (resultado.requerDoisFatores) {
                sessionStorage.setItem("loginTemporario2fa", resultado.loginTemporario);
                form.classList.add("hidden");
                form2fa.classList.remove("hidden");
                setMessage(mensagem2fa, "Informe o codigo de seguranca do seu aplicativo autenticador.", "info");
                return;
            }

            storeAuthResult(resultado);

            setMessage(mensagem, "Login realizado com sucesso.", "success");

            setTimeout(function () {
                redirectAfterLogin(resultado);
            }, 600);
        } catch {
            setMessage(mensagem, "Nao foi possivel conectar a API.", "error");
        } finally {
            disableSubmit(form, false);
        }
    });

    form2fa.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form2fa.reportValidity()) {
            return;
        }

        setMessage(mensagem2fa, "Validando codigo...", "info");
        disableSubmit(form2fa, true);

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/login-2fa`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    loginTemporario: sessionStorage.getItem("loginTemporario2fa"),
                    codigo: document.getElementById("codigo2fa").value.trim()
                })
            });

            if (!response.ok) {
                setMessage(mensagem2fa, await readApiMessage(response), "error");
                return;
            }

            const resultado = await response.json();
            sessionStorage.removeItem("loginTemporario2fa");
            storeAuthResult(resultado);

            setMessage(mensagem2fa, "Login realizado com sucesso.", "success");

            setTimeout(function () {
                redirectAfterLogin(resultado);
            }, 600);
        } catch {
            setMessage(mensagem2fa, "Nao foi possivel conectar a API.", "error");
        } finally {
            disableSubmit(form2fa, false);
        }
    });
}

async function setupPainel() {
    const painelNome = document.getElementById("painelNome");

    if (!painelNome) {
        return;
    }

    const usuario = await CasaMulherAuth.protegerPagina();

    if (!usuario) {
        return;
    }

    document.getElementById("painelNome").textContent = usuario.nomeCompleto || "-";
    document.getElementById("painelIdentificador").textContent = usuario.identificadorFuncionario || "-";
    document.getElementById("painelEmail").textContent = usuario.email || "-";
    document.getElementById("painelPerfil").textContent = formatPerfil(usuario.perfil);

    CasaMulherAuth.salvarUsuario(usuario);

    if (CasaMulherAuth.podeAcessar("convites")) {
        document.getElementById("linkConvites")?.classList.remove("hidden");
    }

    if (CasaMulherAuth.podeAcessar("funcionarios")) {
        document.getElementById("linkFuncionarios")?.classList.remove("hidden");
    }

    if (CasaMulherAuth.podeAcessar("auditoria")) {
        document.getElementById("linkAuditoria")?.classList.remove("hidden");
    }

    bindLogoutButton("btnSair");
}

async function setupConvites() {
    const page = document.getElementById("convitesPage");

    if (!page) {
        return;
    }

    const conteudo = document.getElementById("convitesConteudo");
    const restrito = document.getElementById("convitesRestrito");
    const mensagem = document.getElementById("mensagemConvite");

    bindLogoutButton("btnSairConvites");

    const usuario = await CasaMulherAuth.protegerPerfil("adm", {
        conteudoElement: conteudo,
        restritoElement: restrito,
        mensagemElement: mensagem
    });

    if (!usuario) {
        return;
    }

    const form = document.getElementById("formConvite");
    const resultPanel = document.getElementById("conviteGerado");
    let ultimoCodigo = "";
    let ultimoLink = "";

    async function carregarConvites() {
        const lista = document.getElementById("listaConvites");
        lista.innerHTML = "<tr><td colspan=\"6\">Carregando...</td></tr>";

        try {
            const response = await CasaMulherAuth.apiFetch("/api/convites-funcionarios", {
                mensagemElement: mensagem
            });

            if (response.status === 401) {
                return;
            }

            if (response.status === 403) {
                conteudo.classList.add("hidden");
                restrito.classList.remove("hidden");
                return;
            }

            if (!response.ok) {
                lista.innerHTML = "<tr><td colspan=\"6\">Nao foi possivel carregar os convites.</td></tr>";
                return;
            }

            const convites = await response.json();

            if (convites.length === 0) {
                lista.innerHTML = "<tr><td colspan=\"6\">Nenhum convite cadastrado.</td></tr>";
                return;
            }

            lista.innerHTML = convites.map(function (convite) {
                const podeCancelar = convite.status === "Pendente";
                const cancelar = podeCancelar
                    ? `<button type="button" class="btn-link-danger" data-cancelar="${convite.id}">Cancelar convite</button>`
                    : "-";

                return `
                    <tr>
                        <td>${escapeHtml(convite.nomeCompleto)}</td>
                        <td>${escapeHtml(convite.email)}</td>
                        <td>${escapeHtml(formatPerfil(convite.perfil))}</td>
                        <td><span class="status-badge status-${convite.status.toLowerCase()}">${escapeHtml(convite.status)}</span></td>
                        <td>${formatDate(convite.expiraEm)}</td>
                        <td>${cancelar}</td>
                    </tr>
                `;
            }).join("");
        } catch {
            lista.innerHTML = "<tr><td colspan=\"6\">Nao foi possivel conectar a API.</td></tr>";
        }
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form.reportValidity()) {
            return;
        }

        setMessage(mensagem, "Gerando convite...", "info");
        disableSubmit(form, true);

        const dados = {
            nomeCompleto: document.getElementById("conviteNome").value.trim(),
            email: document.getElementById("conviteEmail").value.trim(),
            perfil: document.getElementById("convitePerfil").value,
            diasParaExpirar: Number(document.getElementById("conviteDias").value)
        };

        try {
            const response = await CasaMulherAuth.apiFetch("/api/convites-funcionarios", {
                method: "POST",
                headers: getAuthHeaders(true),
                body: JSON.stringify(dados),
                mensagemElement: mensagem
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            const resultado = await response.json();
            ultimoCodigo = resultado.codigoCadastro;
            ultimoLink = resultado.linkCadastro;

            document.getElementById("codigoGerado").textContent = ultimoCodigo;
            document.getElementById("linkGerado").textContent = ultimoLink;
            resultPanel.classList.remove("hidden");
            setMessage(mensagem, "Convite criado com sucesso. Envie o link para o funcionario criar a conta.", "success");
            form.reset();
            document.getElementById("conviteDias").value = "7";
            await carregarConvites();
        } catch {
            setMessage(mensagem, "Nao foi possivel conectar a API.", "error");
        } finally {
            disableSubmit(form, false);
        }
    });

    document.getElementById("btnCopiarCodigo").addEventListener("click", function () {
        copyText(ultimoCodigo, mensagem);
    });

    document.getElementById("btnCopiarLink").addEventListener("click", function () {
        copyText(ultimoLink, mensagem);
    });

    document.getElementById("btnAtualizarConvites").addEventListener("click", carregarConvites);

    document.getElementById("listaConvites").addEventListener("click", async function (event) {
        const button = event.target.closest("[data-cancelar]");

        if (!button) {
            return;
        }

        button.disabled = true;
        setMessage(mensagem, "Cancelando convite...", "info");

        try {
            const response = await CasaMulherAuth.apiFetch(`/api/convites-funcionarios/${button.dataset.cancelar}/cancelar`, {
                method: "PATCH",
                headers: getAuthHeaders(false),
                mensagemElement: mensagem
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            setMessage(mensagem, "Convite cancelado.", "success");
            await carregarConvites();
        } catch {
            setMessage(mensagem, "Nao foi possivel conectar a API.", "error");
        } finally {
            button.disabled = false;
        }
    });

    carregarConvites();
}

async function carregarUsuarioAtual() {
    return CasaMulherAuth.carregarUsuarioAtual();
}

async function setupTrocarSenha() {
    const form = document.getElementById("formTrocarSenha");

    if (!form) {
        return;
    }

    const usuario = await CasaMulherAuth.protegerPagina({
        permitirTrocaSenhaPendente: true
    });

    if (!usuario) {
        return;
    }

    const mensagem = document.getElementById("mensagemTrocarSenha");
    bindLogoutButton("btnSairTrocarSenha");

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form.reportValidity()) {
            return;
        }

        setMessage(mensagem, "Salvando nova senha...", "info");
        disableSubmit(form, true);

        try {
            const response = await CasaMulherAuth.apiFetch("/api/auth/trocar-senha-obrigatoria", {
                method: "POST",
                headers: getAuthHeaders(true),
                body: {
                    senhaAtual: document.getElementById("senhaAtual").value,
                    novaSenha: document.getElementById("novaSenha").value,
                    confirmarNovaSenha: document.getElementById("confirmarNovaSenha").value
                },
                mensagemElement: mensagem
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            CasaMulherAuth.salvarUsuario(Object.assign(CasaMulherAuth.getUsuario(), {
                deveTrocarSenha: false
            }));
            setMessage(mensagem, "Senha trocada com sucesso.", "success");

            setTimeout(function () {
                window.location.href = "painel.html";
            }, 700);
        } catch {
            setMessage(mensagem, "Nao foi possivel conectar a API.", "error");
        } finally {
            disableSubmit(form, false);
        }
    });
}

async function setupFuncionarios() {
    const page = document.getElementById("funcionariosPage");

    if (!page) {
        return;
    }

    const conteudo = document.getElementById("funcionariosConteudo");
    const restrito = document.getElementById("funcionariosRestrito");
    const mensagem = document.getElementById("mensagemFuncionarios");
    let senhaTemporariaAtual = "";
    bindLogoutButton("btnSairFuncionarios");

    const usuario = await CasaMulherAuth.protegerPerfil("adm", {
        conteudoElement: conteudo,
        restritoElement: restrito,
        mensagemElement: mensagem
    });

    if (!usuario) {
        return;
    }

    async function carregarFuncionarios() {
        const lista = document.getElementById("listaFuncionarios");
        lista.innerHTML = "<tr><td colspan=\"6\">Carregando...</td></tr>";

        try {
            const response = await CasaMulherAuth.apiFetch("/api/funcionarios", {
                mensagemElement: mensagem
            });

            if (response.status === 401) {
                return;
            }

            if (response.status === 403) {
                conteudo.classList.add("hidden");
                restrito.classList.remove("hidden");
                return;
            }

            if (!response.ok) {
                lista.innerHTML = "<tr><td colspan=\"6\">Nao foi possivel carregar funcionarios.</td></tr>";
                return;
            }

            const funcionarios = await response.json();

            lista.innerHTML = funcionarios.map(function (funcionario) {
                const status = funcionario.ativo ? "Ativo" : "Acesso desativado";
                const codigoSeguranca = funcionario.doisFatoresAtivo
                    ? "Ativo"
                    : funcionario.doisFatoresObrigatorio
                        ? "Obrigatorio, pendente"
                        : "Opcional";
                const ativar = funcionario.ativo
                    ? `<button type="button" class="btn-link-danger" data-action="desativar" data-id="${funcionario.id}">Desativar acesso</button>`
                    : `<button type="button" class="btn-link" data-action="reativar" data-id="${funcionario.id}">Reativar acesso</button>`;

                return `
                    <tr>
                        <td>${escapeHtml(funcionario.identificadorFuncionario)}</td>
                        <td>${escapeHtml(funcionario.nomeCompleto)}<br><small>${escapeHtml(funcionario.email)}</small></td>
                        <td>
                            <select data-action="perfil" data-id="${funcionario.id}">
                                ${Object.keys(PERFIS_LABEL).map(function (perfil) {
                                    return `<option value="${perfil}" ${perfil === funcionario.perfil ? "selected" : ""}>${PERFIS_LABEL[perfil]}</option>`;
                                }).join("")}
                            </select>
                        </td>
                        <td>${status}${funcionario.deveTrocarSenha ? "<br><small>Troca de senha pendente</small>" : ""}</td>
                        <td>${codigoSeguranca}</td>
                        <td class="actions-cell">
                            ${ativar}
                            <button type="button" class="btn-link" data-action="resetar-senha" data-id="${funcionario.id}">Redefinir senha</button>
                            <button type="button" class="btn-link" data-action="resetar-2fa" data-id="${funcionario.id}">Redefinir autenticador</button>
                        </td>
                    </tr>
                `;
            }).join("");
        } catch {
            lista.innerHTML = "<tr><td colspan=\"6\">Nao foi possivel conectar a API.</td></tr>";
        }
    }

    document.getElementById("btnAtualizarFuncionarios").addEventListener("click", carregarFuncionarios);

    document.getElementById("btnCopiarSenhaTemporaria").addEventListener("click", function () {
        copyText(senhaTemporariaAtual, mensagem);
    });

    document.getElementById("listaFuncionarios").addEventListener("change", async function (event) {
        const select = event.target.closest("[data-action='perfil']");

        if (!select) {
            return;
        }

        setMessage(mensagem, "Alterando perfil de acesso...", "info");

        let response;

        try {
            response = await CasaMulherAuth.apiFetch(`/api/funcionarios/${select.dataset.id}/alterar-perfil`, {
                method: "PATCH",
                headers: getAuthHeaders(true),
                body: { perfil: select.value },
                mensagemElement: mensagem
            });
        } catch {
            setMessage(mensagem, "Nao foi possivel conectar a API.", "error");
            await carregarFuncionarios();
            return;
        }

        if (!response.ok) {
            setMessage(mensagem, await readApiMessage(response), "error");
            await carregarFuncionarios();
            return;
        }

        setMessage(mensagem, "Perfil de acesso alterado.", "success");
        await carregarFuncionarios();
    });

    document.getElementById("listaFuncionarios").addEventListener("click", async function (event) {
        const button = event.target.closest("[data-action]");

        if (!button || button.dataset.action === "perfil") {
            return;
        }

        const action = button.dataset.action;
        let method = "PATCH";
        let url = `/api/funcionarios/${button.dataset.id}/${action}`;

        if (action === "resetar-senha" || action === "resetar-2fa") {
            method = "POST";
        }

        setMessage(mensagem, "Processando solicitacao...", "info");
        button.disabled = true;

        try {
            const response = await CasaMulherAuth.apiFetch(url, {
                method,
                headers: getAuthHeaders(false),
                mensagemElement: mensagem
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            const resultado = await response.json();

            if (action === "resetar-senha") {
                senhaTemporariaAtual = resultado.senhaTemporaria;
                document.getElementById("senhaTemporariaValor").textContent = senhaTemporariaAtual;
                document.getElementById("senhaTemporariaPanel").classList.remove("hidden");
            }

            let mensagemSucesso = "Acao realizada com sucesso.";

            if (action === "resetar-senha") {
                mensagemSucesso = "Senha temporaria gerada. Entregue ao funcionario e oriente a troca no proximo acesso.";
            }

            if (action === "resetar-2fa") {
                mensagemSucesso = "Aplicativo autenticador redefinido com sucesso.";
            }

            setMessage(mensagem, mensagemSucesso, "success");
            await carregarFuncionarios();
        } catch {
            setMessage(mensagem, "Nao foi possivel conectar a API.", "error");
        } finally {
            button.disabled = false;
        }
    });

    carregarFuncionarios();
}

async function setupAuditoria() {
    const page = document.getElementById("auditoriaPage");

    if (!page) {
        return;
    }

    const conteudo = document.getElementById("auditoriaConteudo");
    const restrito = document.getElementById("auditoriaRestrito");
    const mensagem = document.getElementById("mensagemAuditoria");
    bindLogoutButton("btnSairAuditoria");

    const usuario = await CasaMulherAuth.protegerPerfil("adm", {
        conteudoElement: conteudo,
        restritoElement: restrito,
        mensagemElement: mensagem
    });

    if (!usuario) {
        return;
    }

    async function carregarAuditoria() {
        const lista = document.getElementById("listaAuditoria");
        lista.innerHTML = "<tr><td colspan=\"5\">Carregando...</td></tr>";

        try {
            const response = await CasaMulherAuth.apiFetch("/api/auditoria", {
                mensagemElement: mensagem
            });

            if (response.status === 401) {
                return;
            }

            if (response.status === 403) {
                conteudo.classList.add("hidden");
                restrito.classList.remove("hidden");
                return;
            }

            if (!response.ok) {
                lista.innerHTML = "<tr><td colspan=\"5\">Nao foi possivel carregar auditoria.</td></tr>";
                return;
            }

            const eventos = await response.json();

            if (eventos.length === 0) {
                lista.innerHTML = "<tr><td colspan=\"5\">Nenhum evento registrado.</td></tr>";
                return;
            }

            lista.innerHTML = eventos.map(function (evento) {
                const funcionario = evento.identificadorFuncionario
                    ? `${escapeHtml(evento.identificadorFuncionario)}<br><small>${escapeHtml(evento.nomeFuncionario)}</small>`
                    : "-";

                return `
                    <tr>
                        <td>${formatDateTime(evento.criadoEm)}</td>
                        <td>${funcionario}</td>
                        <td>${escapeHtml(formatAcaoAuditoria(evento.acao))}</td>
                        <td>${escapeHtml(formatDescricaoAuditoria(evento.descricao))}</td>
                        <td>${escapeHtml(evento.ipOrigem || "-")}</td>
                    </tr>
                `;
            }).join("");

            setMessage(mensagem, "Historico atualizado.", "success");
        } catch {
            lista.innerHTML = "<tr><td colspan=\"5\">Nao foi possivel conectar a API.</td></tr>";
        }
    }

    document.getElementById("btnAtualizarAuditoria").addEventListener("click", carregarAuditoria);
    carregarAuditoria();
}

async function setupSeguranca() {
    const page = document.getElementById("segurancaPage");

    if (!page) {
        return;
    }

    const mensagem = document.getElementById("mensagemSeguranca");
    const panel = document.getElementById("configuracao2fa");
    let chaveManualAtual = "";
    const usuarioInicial = await CasaMulherAuth.protegerPagina({
        mensagemElement: mensagem
    });

    if (!usuarioInicial) {
        return;
    }

    bindLogoutButton("btnSairSeguranca");

    async function atualizarStatus() {
        const usuario = await carregarUsuarioAtual();

        if (!usuario) {
            setMessage(mensagem, "Nao foi possivel carregar os dados de seguranca.", "error");
            return;
        }

        document.getElementById("segurancaIdentificador").textContent = usuario.identificadorFuncionario;
        document.getElementById("segurancaStatus").textContent = usuario.doisFatoresAtivado
            ? "Ativado"
            : usuario.doisFatoresObrigatorio
                ? "Obrigatorio, ainda nao configurado"
                : "Opcional";
    }

    document.getElementById("btnIniciar2fa").addEventListener("click", async function () {
        setMessage(mensagem, "Gerando chave do aplicativo...", "info");

        try {
            const response = await CasaMulherAuth.apiFetch("/api/auth/2fa/iniciar-configuracao", {
                method: "POST",
                headers: getAuthHeaders(false),
                mensagemElement: mensagem
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            const resultado = await response.json();
            const authenticatorUri = resultado.authenticatorUri || resultado.qrCodeData;
            chaveManualAtual = resultado.chaveManual || "";

            document.getElementById("chaveManual2fa").textContent = chaveManualAtual || "-";
            document.getElementById("qrCodeAutenticador").innerHTML = "";

            if (authenticatorUri && window.QRCode) {
                new QRCode(document.getElementById("qrCodeAutenticador"), {
                    text: authenticatorUri,
                    width: 196,
                    height: 196
                });
            }

            panel.classList.remove("hidden");
            setMessage(mensagem, resultado.mensagem || "Configuracao iniciada. Escaneie o QR Code e confirme o codigo gerado pelo aplicativo.", "success");
        } catch {
            setMessage(mensagem, "Nao foi possivel conectar a API.", "error");
        }
    });

    document.getElementById("btnCopiarChaveManual").addEventListener("click", function () {
        copyText(chaveManualAtual, mensagem);
    });

    document.getElementById("formConfirmar2fa").addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!event.currentTarget.reportValidity()) {
            return;
        }

        setMessage(mensagem, "Confirmando codigo...", "info");

        try {
            const response = await CasaMulherAuth.apiFetch("/api/auth/2fa/confirmar", {
                method: "POST",
                headers: getAuthHeaders(true),
                body: {
                    codigo: document.getElementById("codigoConfirmar2fa").value.trim()
                },
                mensagemElement: mensagem
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            setMessage(mensagem, "Codigo de seguranca ativado.", "success");
            panel.classList.add("hidden");
            await atualizarStatus();
        } catch {
            setMessage(mensagem, "Nao foi possivel conectar a API.", "error");
        }
    });

    document.getElementById("btnDesativar2fa").addEventListener("click", async function () {
        setMessage(mensagem, "Desativando codigo de seguranca...", "info");

        try {
            const response = await CasaMulherAuth.apiFetch("/api/auth/2fa/desativar", {
                method: "POST",
                headers: getAuthHeaders(false),
                mensagemElement: mensagem
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            setMessage(mensagem, "Codigo de seguranca desativado.", "success");
            await atualizarStatus();
        } catch {
            setMessage(mensagem, "Nao foi possivel conectar a API.", "error");
        }
    });

    atualizarStatus();
}

setupCadastro();
setupLogin();
setupPainel();
setupConvites();
setupSeguranca();
setupTrocarSenha();
setupFuncionarios();
setupAuditoria();
