const API_BASE_URL = "http://localhost:5001";
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
    const headers = {};

    if (includeJson) {
        headers["Content-Type"] = "application/json";
    }

    headers.Authorization = `Bearer ${localStorage.getItem("token")}`;
    return headers;
}

function clearSession() {
    localStorage.removeItem("token");
    localStorage.removeItem("perfil");
    localStorage.removeItem("email");
    localStorage.removeItem("nomeCompleto");
    localStorage.removeItem("identificadorFuncionario");
    localStorage.removeItem("doisFatoresObrigatorio");
    localStorage.removeItem("doisFatoresAtivado");
    localStorage.removeItem("deveTrocarSenha");
    sessionStorage.removeItem("loginTemporario2fa");
}

function storeAuthResult(resultado) {
    localStorage.setItem("token", resultado.token);
    localStorage.setItem("perfil", resultado.perfil);
    localStorage.setItem("email", resultado.email);
    localStorage.setItem("nomeCompleto", resultado.nomeCompleto);
    localStorage.setItem("identificadorFuncionario", resultado.identificadorFuncionario);
    localStorage.setItem("doisFatoresObrigatorio", String(resultado.doisFatoresObrigatorio));
    localStorage.setItem("doisFatoresAtivado", String(resultado.doisFatoresAtivado));
    localStorage.setItem("deveTrocarSenha", String(resultado.deveTrocarSenha));
}

function redirectAfterLogin(resultado) {
    if (resultado.deveTrocarSenha) {
        window.location.href = "trocar-senha.html";
        return;
    }

    window.location.href = "painel.html";
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

    if (emailParam) {
        emailInput.value = emailParam;
        emailInput.readOnly = true;
    }

    if (codigoParam) {
        codigoInput.value = codigoParam;
        codigoInput.readOnly = true;
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form.reportValidity()) {
            return;
        }

        setMessage(mensagem, "Enviando cadastro...", "info");
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
            setMessage(mensagem, "Erro ao conectar com a API. Verifique se o servidor esta rodando.", "error");
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
                setMessage(mensagem2fa, "Informe o codigo do Authenticator.", "info");
                return;
            }

            storeAuthResult(resultado);

            setMessage(mensagem, "Login realizado com sucesso.", "success");

            setTimeout(function () {
                redirectAfterLogin(resultado);
            }, 600);
        } catch {
            setMessage(mensagem, "Erro ao conectar com a API. Verifique se o servidor esta rodando.", "error");
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
            setMessage(mensagem2fa, "Erro ao conectar com a API.", "error");
        } finally {
            disableSubmit(form2fa, false);
        }
    });
}

function setupPainel() {
    const painelNome = document.getElementById("painelNome");

    if (!painelNome) {
        return;
    }

    const token = localStorage.getItem("token");

    if (!token) {
        window.location.href = "index.html";
        return;
    }

    document.getElementById("painelNome").textContent = localStorage.getItem("nomeCompleto") || "-";
    document.getElementById("painelIdentificador").textContent = localStorage.getItem("identificadorFuncionario") || "-";
    document.getElementById("painelEmail").textContent = localStorage.getItem("email") || "-";
    document.getElementById("painelPerfil").textContent = localStorage.getItem("perfil") || "-";

    const linkConvites = document.getElementById("linkConvites");

    if (localStorage.getItem("perfil") === "adm") {
        linkConvites?.classList.remove("hidden");
        document.getElementById("linkFuncionarios")?.classList.remove("hidden");
        document.getElementById("linkAuditoria")?.classList.remove("hidden");
    }

    document.getElementById("btnSair").addEventListener("click", function () {
        clearSession();
        window.location.href = "index.html";
    });
}

function setupConvites() {
    const page = document.getElementById("convitesPage");

    if (!page) {
        return;
    }

    const token = localStorage.getItem("token");
    const perfil = localStorage.getItem("perfil");
    const conteudo = document.getElementById("convitesConteudo");
    const restrito = document.getElementById("convitesRestrito");
    const mensagem = document.getElementById("mensagemConvite");

    if (!token) {
        window.location.href = "index.html";
        return;
    }

    document.getElementById("convitesUsuario").textContent = localStorage.getItem("nomeCompleto") || "Coordenacao";

    document.getElementById("btnSairConvites").addEventListener("click", function () {
        clearSession();
        window.location.href = "index.html";
    });

    if (perfil !== "adm") {
        conteudo.classList.add("hidden");
        restrito.classList.remove("hidden");
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
            const response = await fetch(`${API_BASE_URL}/api/convites-funcionarios`, {
                headers: getAuthHeaders(false)
            });

            if (response.status === 401) {
                clearSession();
                window.location.href = "index.html";
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
                    ? `<button type="button" class="btn-link-danger" data-cancelar="${convite.id}">Cancelar</button>`
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
            lista.innerHTML = "<tr><td colspan=\"6\">Erro ao conectar com a API.</td></tr>";
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
            const response = await fetch(`${API_BASE_URL}/api/convites-funcionarios`, {
                method: "POST",
                headers: getAuthHeaders(true),
                body: JSON.stringify(dados)
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
            setMessage(mensagem, "Convite criado com sucesso.", "success");
            form.reset();
            document.getElementById("conviteDias").value = "7";
            await carregarConvites();
        } catch {
            setMessage(mensagem, "Erro ao conectar com a API.", "error");
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
            const response = await fetch(`${API_BASE_URL}/api/convites-funcionarios/${button.dataset.cancelar}/cancelar`, {
                method: "PATCH",
                headers: getAuthHeaders(false)
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            setMessage(mensagem, "Convite cancelado.", "success");
            await carregarConvites();
        } catch {
            setMessage(mensagem, "Erro ao conectar com a API.", "error");
        } finally {
            button.disabled = false;
        }
    });

    carregarConvites();
}

async function carregarUsuarioAtual() {
    const response = await fetch(`${API_BASE_URL}/api/auth/me`, {
        headers: getAuthHeaders(false)
    });

    if (response.status === 401) {
        clearSession();
        window.location.href = "index.html";
        return null;
    }

    if (!response.ok) {
        return null;
    }

    const usuario = await response.json();
    localStorage.setItem("perfil", usuario.perfil);
    localStorage.setItem("email", usuario.email);
    localStorage.setItem("nomeCompleto", usuario.nomeCompleto);
    localStorage.setItem("identificadorFuncionario", usuario.identificadorFuncionario);
    localStorage.setItem("doisFatoresObrigatorio", String(usuario.doisFatoresObrigatorio));
    localStorage.setItem("doisFatoresAtivado", String(usuario.doisFatoresAtivado));
    localStorage.setItem("deveTrocarSenha", String(usuario.deveTrocarSenha));
    return usuario;
}

function setupTrocarSenha() {
    const form = document.getElementById("formTrocarSenha");

    if (!form) {
        return;
    }

    if (!localStorage.getItem("token")) {
        window.location.href = "index.html";
        return;
    }

    const mensagem = document.getElementById("mensagemTrocarSenha");

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form.reportValidity()) {
            return;
        }

        setMessage(mensagem, "Alterando senha...", "info");
        disableSubmit(form, true);

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/trocar-senha-obrigatoria`, {
                method: "POST",
                headers: getAuthHeaders(true),
                body: JSON.stringify({
                    senhaAtual: document.getElementById("senhaAtual").value,
                    novaSenha: document.getElementById("novaSenha").value,
                    confirmarNovaSenha: document.getElementById("confirmarNovaSenha").value
                })
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            localStorage.setItem("deveTrocarSenha", "false");
            setMessage(mensagem, "Senha alterada com sucesso.", "success");

            setTimeout(function () {
                window.location.href = "painel.html";
            }, 700);
        } catch {
            setMessage(mensagem, "Erro ao conectar com a API.", "error");
        } finally {
            disableSubmit(form, false);
        }
    });
}

function setupFuncionarios() {
    const page = document.getElementById("funcionariosPage");

    if (!page) {
        return;
    }

    if (!localStorage.getItem("token")) {
        window.location.href = "index.html";
        return;
    }

    const conteudo = document.getElementById("funcionariosConteudo");
    const restrito = document.getElementById("funcionariosRestrito");
    const mensagem = document.getElementById("mensagemFuncionarios");
    let senhaTemporariaAtual = "";

    if (localStorage.getItem("perfil") !== "adm") {
        conteudo.classList.add("hidden");
        restrito.classList.remove("hidden");
        return;
    }

    async function carregarFuncionarios() {
        const lista = document.getElementById("listaFuncionarios");
        lista.innerHTML = "<tr><td colspan=\"6\">Carregando...</td></tr>";

        try {
            const response = await fetch(`${API_BASE_URL}/api/funcionarios`, {
                headers: getAuthHeaders(false)
            });

            if (response.status === 401) {
                clearSession();
                window.location.href = "index.html";
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
                const status = funcionario.ativo ? "Ativo" : "Inativo";
                const doisFatores = funcionario.doisFatoresAtivo ? "Ativo" : funcionario.doisFatoresObrigatorio ? "Obrigatorio" : "Inativo";
                const ativar = funcionario.ativo
                    ? `<button type="button" class="btn-link-danger" data-action="desativar" data-id="${funcionario.id}">Desativar</button>`
                    : `<button type="button" class="btn-link" data-action="reativar" data-id="${funcionario.id}">Reativar</button>`;

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
                        <td>${status}${funcionario.deveTrocarSenha ? "<br><small>Trocar senha</small>" : ""}</td>
                        <td>${doisFatores}</td>
                        <td class="actions-cell">
                            ${ativar}
                            <button type="button" class="btn-link" data-action="resetar-senha" data-id="${funcionario.id}">Resetar senha</button>
                            <button type="button" class="btn-link" data-action="resetar-2fa" data-id="${funcionario.id}">Resetar 2FA</button>
                        </td>
                    </tr>
                `;
            }).join("");
        } catch {
            lista.innerHTML = "<tr><td colspan=\"6\">Erro ao conectar com a API.</td></tr>";
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

        setMessage(mensagem, "Alterando perfil...", "info");

        const response = await fetch(`${API_BASE_URL}/api/funcionarios/${select.dataset.id}/alterar-perfil`, {
            method: "PATCH",
            headers: getAuthHeaders(true),
            body: JSON.stringify({ perfil: select.value })
        });

        if (!response.ok) {
            setMessage(mensagem, await readApiMessage(response), "error");
            await carregarFuncionarios();
            return;
        }

        setMessage(mensagem, "Perfil alterado.", "success");
        await carregarFuncionarios();
    });

    document.getElementById("listaFuncionarios").addEventListener("click", async function (event) {
        const button = event.target.closest("[data-action]");

        if (!button || button.dataset.action === "perfil") {
            return;
        }

        const action = button.dataset.action;
        let method = "PATCH";
        let url = `${API_BASE_URL}/api/funcionarios/${button.dataset.id}/${action}`;

        if (action === "resetar-senha" || action === "resetar-2fa") {
            method = "POST";
        }

        setMessage(mensagem, "Processando...", "info");
        button.disabled = true;

        try {
            const response = await fetch(url, {
                method,
                headers: getAuthHeaders(false)
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

            setMessage(mensagem, resultado.mensagem || "Operacao realizada.", "success");
            await carregarFuncionarios();
        } catch {
            setMessage(mensagem, "Erro ao conectar com a API.", "error");
        } finally {
            button.disabled = false;
        }
    });

    carregarFuncionarios();
}

function setupAuditoria() {
    const page = document.getElementById("auditoriaPage");

    if (!page) {
        return;
    }

    if (!localStorage.getItem("token")) {
        window.location.href = "index.html";
        return;
    }

    const conteudo = document.getElementById("auditoriaConteudo");
    const restrito = document.getElementById("auditoriaRestrito");
    const mensagem = document.getElementById("mensagemAuditoria");

    if (localStorage.getItem("perfil") !== "adm") {
        conteudo.classList.add("hidden");
        restrito.classList.remove("hidden");
        return;
    }

    async function carregarAuditoria() {
        const lista = document.getElementById("listaAuditoria");
        lista.innerHTML = "<tr><td colspan=\"5\">Carregando...</td></tr>";

        try {
            const response = await fetch(`${API_BASE_URL}/api/auditoria`, {
                headers: getAuthHeaders(false)
            });

            if (response.status === 401) {
                clearSession();
                window.location.href = "index.html";
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
                        <td>${escapeHtml(evento.acao)}</td>
                        <td>${escapeHtml(evento.descricao)}</td>
                        <td>${escapeHtml(evento.ipOrigem || "-")}</td>
                    </tr>
                `;
            }).join("");

            setMessage(mensagem, "Auditoria atualizada.", "success");
        } catch {
            lista.innerHTML = "<tr><td colspan=\"5\">Erro ao conectar com a API.</td></tr>";
        }
    }

    document.getElementById("btnAtualizarAuditoria").addEventListener("click", carregarAuditoria);
    carregarAuditoria();
}

function setupSeguranca() {
    const page = document.getElementById("segurancaPage");

    if (!page) {
        return;
    }

    if (!localStorage.getItem("token")) {
        window.location.href = "index.html";
        return;
    }

    const mensagem = document.getElementById("mensagemSeguranca");
    const panel = document.getElementById("configuracao2fa");
    let authenticatorUri = "";

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
        setMessage(mensagem, "Gerando chave...", "info");

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/2fa/iniciar-configuracao`, {
                method: "POST",
                headers: getAuthHeaders(false)
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            const resultado = await response.json();
            authenticatorUri = resultado.authenticatorUri;
            document.getElementById("chaveManual2fa").textContent = resultado.chaveManual;
            document.getElementById("uri2fa").textContent = resultado.qrCodeData;
            panel.classList.remove("hidden");
            setMessage(mensagem, "Chave gerada. Cadastre no app autenticador e confirme o codigo.", "success");
        } catch {
            setMessage(mensagem, "Erro ao conectar com a API.", "error");
        }
    });

    document.getElementById("formConfirmar2fa").addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!event.currentTarget.reportValidity()) {
            return;
        }

        setMessage(mensagem, "Confirmando codigo...", "info");

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/2fa/confirmar`, {
                method: "POST",
                headers: getAuthHeaders(true),
                body: JSON.stringify({
                    codigo: document.getElementById("codigoConfirmar2fa").value.trim()
                })
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            setMessage(mensagem, "Dois fatores ativado.", "success");
            panel.classList.add("hidden");
            await atualizarStatus();
        } catch {
            setMessage(mensagem, "Erro ao conectar com a API.", "error");
        }
    });

    document.getElementById("btnDesativar2fa").addEventListener("click", async function () {
        setMessage(mensagem, "Desativando dois fatores...", "info");

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/2fa/desativar`, {
                method: "POST",
                headers: getAuthHeaders(false)
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            authenticatorUri = "";
            setMessage(mensagem, "Dois fatores desativado.", "success");
            await atualizarStatus();
        } catch {
            setMessage(mensagem, "Erro ao conectar com a API.", "error");
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
