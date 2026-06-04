const API_BASE_URL = window.API_BASE_URL || "http://localhost:5001";
const PERFIS_LABEL = {
    adm: "Coordenação / ADM",
    recepcao: "Recepção",
    professor: "Professor",
    as_social: "Assistente Social",
    juridico: "Jurídico"
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
        return "Não foi possível ler a resposta da API.";
    }

    return "Não foi possível concluir a operação.";
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
        CONVITE_PUBLICO_INVALIDO: "Convite público inválido",
        FUNCIONARIO_DESATIVADO: "Acesso desativado",
        FUNCIONARIO_REATIVADO: "Acesso reativado",
        LOGIN_FALHA: "Falha de login",
        LOGIN_BLOQUEADO: "Login bloqueado",
        LOGIN_2FA_FALHA: "Falha no código de segurança",
        PERFIL_ALTERADO: "Perfil alterado",
        SENHA_RESETADA: "Senha redefinida",
        REDEFINICAO_SENHA_SOLICITADA: "Redefinição de senha solicitada",
        REDEFINICAO_SENHA_AUTO_SOLICITADA: "Redefinição de senha solicitada",
        REDEFINICAO_SENHA_ABUSO_BLOQUEADO: "Redefinição bloqueada",
        REDEFINICAO_SENHA_CONCLUIDA: "Redefinição de senha concluída",
        REDEFINICAO_SENHA_FALHA: "Falha na redefinição",
        DOIS_FATORES_RESETADO: "Autenticador redefinido",
        SENHA_TROCADA: "Senha trocada",
        PASSKEY_CRIADA: "Chave de acesso cadastrada",
        PASSKEY_CRIADA_FALHA: "Falha ao cadastrar chave de acesso",
        PASSKEY_REMOVIDA: "Chave de acesso removida",
        PASSKEY_LOGIN_SUCESSO: "Login por chave de acesso",
        PASSKEY_LOGIN_FALHA: "Falha no login por chave de acesso",
        PASSKEY_RECONFIRMACAO_SOLICITADA: "Reconfirmacao de credenciais solicitada",
        PASSKEY_RECONFIRMADA: "Credenciais reconfirmadas",
        PASSKEY_RECONFIRMACAO_FALHA: "Falha na reconfirmacao de credenciais",
        EMAIL_RECUPERACAO_SOLICITADO: "E-mail de recuperação solicitado",
        EMAIL_RECUPERACAO_CONFIRMADO: "E-mail de recuperação confirmado",
        EMAIL_RECUPERACAO_CONFIRMACAO_FALHA: "Falha na confirmação do e-mail de recuperação",
        EMAIL_RECUPERACAO_REMOVIDO: "E-mail de recuperação removido"
    };

    return acoes[acao] || acao || "-";
}

function formatTipoEmail(tipo) {
    const tipos = {
        ConviteFuncionario: "Convite de funcionário",
        ConfirmacaoEmailRecuperacao: "Confirmação de e-mail de recuperação",
        RedefinicaoSenha: "Redefinição de senha",
        TesteSmoke: "Teste de e-mail"
    };

    return tipos[tipo] || tipo || "-";
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

function formatResultadoEmailConvite(resultado) {
    if (!resultado.statusEmail) {
        return "Envio por e-mail não solicitado.";
    }

    if (resultado.statusEmail === "Simulado") {
        return "E-mail simulado em ambiente de desenvolvimento. Nenhuma mensagem foi enviada de verdade.";
    }

    if (resultado.statusEmail === "Enviado") {
        return "E-mail enviado.";
    }

    if (resultado.statusEmail === "NaoConfigurado") {
        return resultado.avisoEmail || "Configuração de e-mail pendente.";
    }

    if (resultado.statusEmail === "Falhou") {
        return resultado.avisoEmail || "Não foi possível enviar o e-mail.";
    }

    return `Status do e-mail: ${resultado.statusEmail}.`;
}

function getAvisoLinkLocal(link) {
    if (!link) {
        return "";
    }

    try {
        const url = new URL(link, window.location.href);
        const hostname = url.hostname.toLowerCase();

        if (hostname === "localhost" || hostname === "127.0.0.1" || hostname === "::1") {
            return " Este link funciona apenas neste computador. Para enviar para outra pessoa, use um endereço hospedado ou servidor na rede.";
        }
    } catch {
        return "";
    }

    return "";
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
        setMessage(messageElement, "Não foi possível copiar automaticamente.", "error");
    }
}

function setupCadastro() {
    const form = document.getElementById("formCadastroFuncionario");
    const mensagem = document.getElementById("mensagemCadastro");
    const avisoConvite = document.getElementById("avisoConvite");

    if (!form) {
        return;
    }

    const params = new URLSearchParams(window.location.search);
    const emailParam = params.get("email");
    const codigoParam = params.get("codigo");
    const emailInput = document.getElementById("email");
    const codigoInput = document.getElementById("codigoCadastro");
    const nomeInput = document.getElementById("nomeCompleto");
    const identificadorInput = document.getElementById("identificadorFuncionario");

    if (!emailParam || !codigoParam) {
        avisoConvite.textContent = "Abra o link do convite enviado pela coordenação para criar sua senha de acesso.";
        avisoConvite.className = "notice";
        form.classList.add("hidden");
        return;
    }

    emailInput.value = emailParam;
    codigoInput.value = codigoParam;

    async function carregarConvite() {
        avisoConvite.textContent = "Verificando convite...";
        avisoConvite.className = "notice";
        form.classList.add("hidden");

        try {
            const url = `${API_BASE_URL}/api/auth/convite-publico?email=${encodeURIComponent(emailParam)}&codigo=${encodeURIComponent(codigoParam)}`;
            const response = await fetch(url);

            if (!response.ok) {
                avisoConvite.textContent = await readApiMessage(response);
                avisoConvite.className = "notice notice-error";
                return;
            }

            const convite = await response.json();
            nomeInput.value = convite.nomeCompleto || "";
            emailInput.value = convite.email || emailParam;
            identificadorInput.value = convite.identificadorFuncionario || "";
            codigoInput.value = codigoParam;

            avisoConvite.textContent = "Convite reconhecido. Confira seus dados e crie sua senha de acesso.";
            avisoConvite.className = "notice notice-success";
            form.classList.remove("hidden");
        } catch {
            avisoConvite.textContent = "Não foi possível conectar à API para validar o convite.";
            avisoConvite.className = "notice notice-error";
        }
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form.reportValidity()) {
            return;
        }

        setMessage(mensagem, "Criando acesso...", "info");
        disableSubmit(form, true);

        const dados = {
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
            setMessage(mensagem, "Não foi possível conectar à API.", "error");
        } finally {
            disableSubmit(form, false);
        }
    });

    carregarConvite();
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
                setMessage(mensagem2fa, "Informe o código de segurança do seu aplicativo autenticador.", "info");
                return;
            }

            storeAuthResult(resultado);

            setMessage(mensagem, "Login realizado com sucesso.", "success");

            setTimeout(function () {
                redirectAfterLogin(resultado);
            }, 600);
        } catch {
            setMessage(mensagem, "Não foi possível conectar à API.", "error");
        } finally {
            disableSubmit(form, false);
        }
    });

    form2fa.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form2fa.reportValidity()) {
            return;
        }

        setMessage(mensagem2fa, "Validando código...", "info");
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
            setMessage(mensagem2fa, "Não foi possível conectar à API.", "error");
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

    if (CasaMulherAuth.podeAcessar("emails")) {
        document.getElementById("linkEmails")?.classList.remove("hidden");
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
    const conviteEmailInput = document.getElementById("conviteEmail");
    const conviteConfirmarEmailInput = document.getElementById("conviteConfirmarEmail");
    const avisoEmailAlias = document.getElementById("avisoEmailAlias");
    let ultimoCodigo = "";
    let ultimoLink = "";

    function emailTemAlias(email) {
        const partes = String(email || "").split("@");
        return partes.length === 2 && partes[0].includes("+");
    }

    function atualizarAvisoEmailAlias() {
        const email = conviteEmailInput.value.trim();

        if (emailTemAlias(email)) {
            avisoEmailAlias.classList.remove("hidden");
            return;
        }

        avisoEmailAlias.classList.add("hidden");
    }

    conviteEmailInput.addEventListener("input", atualizarAvisoEmailAlias);

    async function carregarConvites() {
        const lista = document.getElementById("listaConvites");
        lista.innerHTML = "<tr><td colspan=\"7\">Carregando...</td></tr>";

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
                lista.innerHTML = "<tr><td colspan=\"7\">Não foi possível carregar os convites.</td></tr>";
                return;
            }

            const convites = await response.json();

            if (convites.length === 0) {
                lista.innerHTML = "<tr><td colspan=\"7\">Nenhum convite cadastrado.</td></tr>";
                return;
            }

            lista.innerHTML = convites.map(function (convite) {
                const podeCancelar = convite.status === "Pendente";
                const cancelar = podeCancelar
                    ? `<button type="button" class="btn-link-danger" data-cancelar="${convite.id}">Cancelar convite</button>`
                    : "-";

                return `
                    <tr>
                        <td>${escapeHtml(convite.identificadorFuncionario || "-")}</td>
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
            lista.innerHTML = "<tr><td colspan=\"7\">Não foi possível conectar à API.</td></tr>";
        }
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form.reportValidity()) {
            return;
        }

        const email = conviteEmailInput.value.trim();
        const confirmarEmail = conviteConfirmarEmailInput.value.trim();

        if (email.toLowerCase() !== confirmarEmail.toLowerCase()) {
            setMessage(mensagem, "Os e-mails não conferem.", "error");
            return;
        }

        if (emailTemAlias(email)) {
            const confirmado = window.confirm(`Este e-mail contém alias com "+":\n\n${email}\n\nDeseja enviar exatamente para este endereço?`);

            if (!confirmado) {
                setMessage(mensagem, "Confira o e-mail antes de gerar o convite.", "info");
                return;
            }
        }

        setMessage(mensagem, "Gerando convite...", "info");
        disableSubmit(form, true);

        const dados = {
            nomeCompleto: document.getElementById("conviteNome").value.trim(),
            email,
            confirmarEmail,
            perfil: document.getElementById("convitePerfil").value,
            diasParaExpirar: Number(document.getElementById("conviteDias").value),
            enviarEmail: document.getElementById("conviteEnviarEmail").checked
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
            const avisoLinkLocal = getAvisoLinkLocal(ultimoLink);
            const avisoAlias = resultado.avisoEmailAlias ? ` ${resultado.avisoEmailAlias}` : "";

            document.getElementById("identificadorGerado").textContent = resultado.identificadorFuncionario || "-";
            document.getElementById("codigoGerado").textContent = ultimoCodigo;
            document.getElementById("linkGerado").textContent = ultimoLink;
            document.getElementById("emailConviteStatus").textContent = `${formatResultadoEmailConvite(resultado)}${avisoAlias}${avisoLinkLocal}`;
            resultPanel.classList.remove("hidden");

            const mensagemSucesso = resultado.statusEmail
                ? `Convite criado com sucesso. ${formatResultadoEmailConvite(resultado)}${avisoAlias}${avisoLinkLocal}`
                : "Convite criado com sucesso. Envie o link para o funcionário criar a conta.";
            const tipoMensagem = resultado.statusEmail === "Falhou" || resultado.statusEmail === "NaoConfigurado"
                ? "info"
                : "success";

            setMessage(mensagem, mensagemSucesso, tipoMensagem);
            form.reset();
            document.getElementById("conviteDias").value = "7";
            document.getElementById("conviteEnviarEmail").checked = true;
            avisoEmailAlias.classList.add("hidden");
            await carregarConvites();
        } catch {
            setMessage(mensagem, "Não foi possível conectar à API.", "error");
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
            setMessage(mensagem, "Não foi possível conectar à API.", "error");
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
            setMessage(mensagem, "Não foi possível conectar à API.", "error");
        } finally {
            disableSubmit(form, false);
        }
    });
}

function setupRedefinirSenha() {
    const form = document.getElementById("formRedefinirSenha");

    if (!form) {
        return;
    }

    const mensagem = document.getElementById("mensagemRedefinirSenha");
    const aviso = document.getElementById("avisoRedefinirSenha");
    const emailInput = document.getElementById("emailRedefinir");
    const tokenInput = document.getElementById("tokenRedefinir");
    const params = new URLSearchParams(window.location.search);
    const email = params.get("email");
    const token = params.get("token");

    if (!email || !token) {
        aviso.textContent = "Abra o link de redefinição enviado por e-mail para criar uma nova senha.";
        aviso.className = "notice notice-error";
        form.classList.add("hidden");
        return;
    }

    emailInput.value = email;
    tokenInput.value = token;

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form.reportValidity()) {
            return;
        }

        const novaSenha = document.getElementById("novaSenhaRedefinir").value;
        const confirmarNovaSenha = document.getElementById("confirmarNovaSenhaRedefinir").value;

        if (novaSenha !== confirmarNovaSenha) {
            setMessage(mensagem, "Nova senha e confirmação não conferem.", "error");
            return;
        }

        setMessage(mensagem, "Salvando nova senha...", "info");
        disableSubmit(form, true);

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/redefinir-senha`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    email: emailInput.value.trim(),
                    token: tokenInput.value,
                    novaSenha,
                    confirmarNovaSenha
                })
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            const resultado = await response.json();
            sessionStorage.setItem("mensagemLogin", resultado.mensagem || "Senha redefinida com sucesso. Entre com a nova senha.");
            setMessage(mensagem, resultado.mensagem || "Senha redefinida com sucesso.", "success");

            setTimeout(function () {
                window.location.href = "index.html";
            }, 1000);
        } catch {
            setMessage(mensagem, "Não foi possível conectar à API.", "error");
        } finally {
            disableSubmit(form, false);
        }
    });
}

function setupSolicitarRedefinicaoSenha() {
    const form = document.getElementById("formSolicitarRedefinicao");

    if (!form) {
        return;
    }

    const mensagem = document.getElementById("mensagemSolicitarRedefinicao");

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form.reportValidity()) {
            return;
        }

        setMessage(mensagem, "Enviando instruções...", "info");
        disableSubmit(form, true);

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/solicitar-redefinicao-senha`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    identificadorFuncionario: document.getElementById("identificadorRedefinicao").value.trim()
                })
            });

            if (!response.ok) {
                setMessage(mensagem, await readApiMessage(response), "error");
                return;
            }

            const resultado = await response.json();
            setMessage(mensagem, resultado.mensagem || "Se os dados estiverem corretos, enviaremos as instruções para o e-mail cadastrado.", "success");
        } catch {
            setMessage(mensagem, "Não foi possível conectar à API.", "error");
        } finally {
            disableSubmit(form, false);
        }
    });
}

async function setupConfirmarEmailRecuperacao() {
    const mensagem = document.getElementById("mensagemConfirmarEmailRecuperacao");

    if (!mensagem) {
        return;
    }

    const params = new URLSearchParams(window.location.search);
    const email = params.get("email");
    const token = params.get("token");

    if (!email || !token) {
        setMessage(mensagem, "Abra o link enviado por e-mail para confirmar o e-mail de recuperação.", "error");
        return;
    }

    setMessage(mensagem, "Confirmando e-mail de recuperação...", "info");

    try {
        const response = await fetch(`${API_BASE_URL}/api/auth/email-recuperacao/confirmar`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                emailRecuperacao: email,
                token
            })
        });

        const resultado = await response.json();

        if (!response.ok) {
            setMessage(mensagem, resultado.mensagem || "Não foi possível confirmar o e-mail de recuperação.", "error");
            return;
        }

        setMessage(mensagem, resultado.mensagem || "E-mail de recuperação confirmado com sucesso.", "success");
    } catch {
        setMessage(mensagem, "Não foi possível conectar à API.", "error");
    }
}

async function setupFuncionarios() {
    const page = document.getElementById("funcionariosPage");

    if (!page) {
        return;
    }

    const conteudo = document.getElementById("funcionariosConteudo");
    const restrito = document.getElementById("funcionariosRestrito");
    const mensagem = document.getElementById("mensagemFuncionarios");
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
                lista.innerHTML = "<tr><td colspan=\"6\">Não foi possível carregar funcionários.</td></tr>";
                return;
            }

            const funcionarios = await response.json();

            lista.innerHTML = funcionarios.map(function (funcionario) {
                const status = funcionario.ativo ? "Ativo" : "Acesso desativado";
                const codigoSeguranca = funcionario.doisFatoresAtivo
                    ? "Ativo"
                    : funcionario.doisFatoresObrigatorio
                        ? "Obrigatório, pendente"
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
            lista.innerHTML = "<tr><td colspan=\"6\">Não foi possível conectar à API.</td></tr>";
        }
    }

    document.getElementById("btnAtualizarFuncionarios").addEventListener("click", carregarFuncionarios);

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
            setMessage(mensagem, "Não foi possível conectar à API.", "error");
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

        if (action === "resetar-senha") {
            const confirmado = window.confirm("Deseja enviar um link de redefinição de senha para o e-mail cadastrado deste funcionário?");

            if (!confirmado) {
                return;
            }

            url = `/api/funcionarios/${button.dataset.id}/enviar-redefinicao-senha`;
        }

        setMessage(mensagem, action === "resetar-senha" ? "Enviando link de redefinição..." : "Processando solicitação...", "info");
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

            let mensagemSucesso = "Ação realizada com sucesso.";

            if (action === "resetar-senha") {
                mensagemSucesso = `${resultado.mensagem || "Solicitação de redefinição processada."} ${formatResultadoEmailConvite(resultado)}`;
            }

            if (action === "resetar-2fa") {
                mensagemSucesso = "Aplicativo autenticador redefinido com sucesso.";
            }

            const tipoMensagem = action === "resetar-senha"
                && (resultado.statusEmail === "Falhou" || resultado.statusEmail === "NaoConfigurado")
                ? "info"
                : "success";

            setMessage(mensagem, mensagemSucesso, tipoMensagem);
            await carregarFuncionarios();
        } catch {
            setMessage(mensagem, "Não foi possível conectar à API.", "error");
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
                lista.innerHTML = "<tr><td colspan=\"5\">Não foi possível carregar auditoria.</td></tr>";
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
            lista.innerHTML = "<tr><td colspan=\"5\">Não foi possível conectar à API.</td></tr>";
        }
    }

    document.getElementById("btnAtualizarAuditoria").addEventListener("click", carregarAuditoria);
    carregarAuditoria();
}

async function setupEmails() {
    const page = document.getElementById("emailsPage");

    if (!page) {
        return;
    }

    const conteudo = document.getElementById("emailsConteudo");
    const restrito = document.getElementById("emailsRestrito");
    const mensagem = document.getElementById("mensagemEmails");
    bindLogoutButton("btnSairEmails");

    const usuario = await CasaMulherAuth.protegerPerfil("adm", {
        conteudoElement: conteudo,
        restritoElement: restrito,
        mensagemElement: mensagem
    });

    if (!usuario) {
        return;
    }

    async function carregarEmails() {
        const lista = document.getElementById("listaEmails");
        lista.innerHTML = "<tr><td colspan=\"6\">Carregando...</td></tr>";

        try {
            const response = await CasaMulherAuth.apiFetch("/api/emails", {
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
                lista.innerHTML = "<tr><td colspan=\"6\">Não foi possível carregar os e-mails.</td></tr>";
                return;
            }

            const eventos = await response.json();

            if (eventos.length === 0) {
                lista.innerHTML = "<tr><td colspan=\"6\">Nenhum envio registrado.</td></tr>";
                return;
            }

            lista.innerHTML = eventos.map(function (evento) {
                const statusClass = String(evento.status || "").toLowerCase();

                return `
                    <tr>
                        <td>${formatDateTime(evento.criadoEm)}</td>
                        <td>${escapeHtml(evento.destinatario)}</td>
                        <td>${escapeHtml(formatTipoEmail(evento.tipo))}</td>
                        <td>${escapeHtml(evento.assunto)}</td>
                        <td><span class="status-badge status-${escapeHtml(statusClass)}">${escapeHtml(evento.status)}</span></td>
                        <td>${escapeHtml(evento.erro || "-")}</td>
                    </tr>
                `;
            }).join("");

            setMessage(mensagem, "Logs de e-mail atualizados.", "success");
        } catch {
            lista.innerHTML = "<tr><td colspan=\"6\">Não foi possível conectar à API.</td></tr>";
        }
    }

    document.getElementById("btnAtualizarEmails").addEventListener("click", carregarEmails);
    carregarEmails();
}

async function setupSeguranca() {
    const page = document.getElementById("segurancaPage");

    if (!page) {
        return;
    }

    const mensagem = document.getElementById("mensagemSeguranca");
    const mensagemEmailRecuperacao = document.getElementById("mensagemEmailRecuperacao");
    const panel = document.getElementById("configuracao2fa");
    const formEmailRecuperacao = document.getElementById("formEmailRecuperacao");
    const emailRecuperacaoInput = document.getElementById("emailRecuperacaoInput");
    const emailRecuperacaoValor = document.getElementById("emailRecuperacaoValor");
    const emailRecuperacaoStatus = document.getElementById("emailRecuperacaoStatus");
    const btnRemoverEmailRecuperacao = document.getElementById("btnRemoverEmailRecuperacao");
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
            setMessage(mensagem, "Não foi possível carregar os dados de segurança.", "error");
            return;
        }

        document.getElementById("segurancaIdentificador").textContent = usuario.identificadorFuncionario;
        document.getElementById("segurancaStatus").textContent = usuario.doisFatoresAtivado
            ? "Ativado"
            : usuario.doisFatoresObrigatorio
                ? "Obrigatório, ainda não configurado"
                : "Opcional";

        if (emailRecuperacaoValor && emailRecuperacaoStatus && emailRecuperacaoInput && btnRemoverEmailRecuperacao) {
            emailRecuperacaoValor.textContent = usuario.emailRecuperacao || "-";
            emailRecuperacaoStatus.textContent = usuario.emailRecuperacao
                ? usuario.emailRecuperacaoConfirmado
                    ? "Confirmado"
                    : "Aguardando confirmação"
                : "Não cadastrado";
            emailRecuperacaoInput.value = usuario.emailRecuperacao || "";
            btnRemoverEmailRecuperacao.disabled = !usuario.emailRecuperacao;
        }
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
            setMessage(mensagem, resultado.mensagem || "Configuração iniciada. Escaneie o QR Code e confirme o código gerado pelo aplicativo.", "success");
        } catch {
            setMessage(mensagem, "Não foi possível conectar à API.", "error");
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

        setMessage(mensagem, "Confirmando código...", "info");

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

            setMessage(mensagem, "Código de segurança ativado.", "success");
            panel.classList.add("hidden");
            await atualizarStatus();
        } catch {
            setMessage(mensagem, "Não foi possível conectar à API.", "error");
        }
    });

    document.getElementById("btnDesativar2fa").addEventListener("click", async function () {
        setMessage(mensagem, "Desativando código de segurança...", "info");

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

            setMessage(mensagem, "Código de segurança desativado.", "success");
            await atualizarStatus();
        } catch {
            setMessage(mensagem, "Não foi possível conectar à API.", "error");
        }
    });

    if (formEmailRecuperacao) {
        formEmailRecuperacao.addEventListener("submit", async function (event) {
            event.preventDefault();

            if (!formEmailRecuperacao.reportValidity()) {
                return;
            }

            setMessage(mensagemEmailRecuperacao, "Enviando confirmação...", "info");
            disableSubmit(formEmailRecuperacao, true);

            try {
                const response = await CasaMulherAuth.apiFetch("/api/auth/email-recuperacao/solicitar", {
                    method: "POST",
                    headers: getAuthHeaders(true),
                    body: {
                        emailRecuperacao: emailRecuperacaoInput.value.trim()
                    },
                    mensagemElement: mensagemEmailRecuperacao
                });

                const resultado = await response.json();

                if (!response.ok) {
                    setMessage(mensagemEmailRecuperacao, resultado.mensagem || "Não foi possível solicitar confirmação.", "error");
                    return;
                }

                setMessage(mensagemEmailRecuperacao, resultado.mensagem || "Confirmação enviada.", "success");
                await atualizarStatus();
            } catch {
                setMessage(mensagemEmailRecuperacao, "Não foi possível conectar à API.", "error");
            } finally {
                disableSubmit(formEmailRecuperacao, false);
            }
        });
    }

    if (btnRemoverEmailRecuperacao) {
        btnRemoverEmailRecuperacao.addEventListener("click", async function () {
            if (!confirm("Remover o e-mail de recuperação?")) {
                return;
            }

            setMessage(mensagemEmailRecuperacao, "Removendo e-mail de recuperação...", "info");

            try {
                const response = await CasaMulherAuth.apiFetch("/api/auth/email-recuperacao", {
                    method: "DELETE",
                    headers: getAuthHeaders(false),
                    mensagemElement: mensagemEmailRecuperacao
                });

                const resultado = await response.json();

                if (!response.ok) {
                    setMessage(mensagemEmailRecuperacao, resultado.mensagem || "Não foi possível remover o e-mail.", "error");
                    return;
                }

                setMessage(mensagemEmailRecuperacao, resultado.mensagem || "E-mail de recuperação removido.", "success");
                await atualizarStatus();
            } catch {
                setMessage(mensagemEmailRecuperacao, "Não foi possível conectar à API.", "error");
            }
        });
    }

    atualizarStatus();
}

setupCadastro();
setupLogin();
setupPainel();
setupConvites();
setupSeguranca();
setupTrocarSenha();
setupRedefinirSenha();
setupSolicitarRedefinicaoSenha();
setupConfirmarEmailRecuperacao();
setupFuncionarios();
setupAuditoria();
setupEmails();
// --- PASSKEY HELPERS ---
// --- PASSKEY HELPERS ---
function bufferToBase64url(buffer) {
    const bytes = new Uint8Array(buffer);
    let str = "";
    for (let i = 0; i < bytes.byteLength; i++) {
        str += String.fromCharCode(bytes[i]);
    }
    return btoa(str).replace(/\+/g, "-").replace(/\//g, "_").replace(/=/g, "");
}

function base64urlToBuffer(base64url) {
    const padding = "==".slice(0, (4 - base64url.length % 4) % 4);
    const base64 = (base64url + padding).replace(/-/g, "+").replace(/_/g, "/");
    const rawData = atob(base64);
    const outputArray = new Uint8Array(rawData.length);
    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray.buffer;
}

function isPasskeySupported() {
    return window.PublicKeyCredential !== undefined;
}

function setupPasskeyLogin() {
    const container = document.getElementById("passkey-login-container");
    const btn = document.getElementById("btn-passkey-login");
    const msg = document.getElementById("mensagem-passkey-login");
    
    if (!container || !btn) return;
    
    if (!isPasskeySupported()) {
        container.hidden = true;
        return;
    } else {
        container.hidden = false;
    }
    
    btn.addEventListener("click", async () => {
        try {
            btn.disabled = true;
            setMessage(msg, "Iniciando login com chave de acesso...", "");
            const resInit = await fetch(`${API_BASE_URL}/api/auth/passkey/login/iniciar`, { method: "POST" });
            if (!resInit.ok) throw new Error(await readApiMessage(resInit));
            const initData = await resInit.json();
            const options = initData.publicKeyOptions;
            options.challenge = base64urlToBuffer(options.challenge);
            if (options.allowCredentials) {
                options.allowCredentials.forEach(c => c.id = base64urlToBuffer(c.id));
            }
            const credential = await navigator.credentials.get({ publicKey: options });
            const credData = {
                id: credential.id,
                rawId: bufferToBase64url(credential.rawId),
                type: credential.type,
                response: {
                    authenticatorData: bufferToBase64url(credential.response.authenticatorData),
                    clientDataJSON: bufferToBase64url(credential.response.clientDataJSON),
                    signature: bufferToBase64url(credential.response.signature),
                    userHandle: credential.response.userHandle ? bufferToBase64url(credential.response.userHandle) : null
                }
            };
            const resComplete = await fetch(`${API_BASE_URL}/api/auth/passkey/login/concluir`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ challengeId: initData.challengeId, credential: credData })
            });
            const result = await resComplete.json();
            if (!resComplete.ok) {
                throw new Error(result.mensagem || "Falha no login");
            }

            if (result.requerReconfirmacao || result.reconfirmacaoId) {
                sessionStorage.setItem("reconfirmacao_id", result.reconfirmacaoId);
                sessionStorage.setItem("reconfirmacao_motivo", result.motivoReconfirmacao || "prazo_7_dias");
                if (result.identificadorFuncionario) {
                    sessionStorage.setItem("reconfirmacao_identificador", result.identificadorFuncionario);
                }
                window.location.href = "confirmar-passkey.html";
                return;
            }

            if (!result.token) {
                throw new Error("Não foi possível concluir o login com chave de acesso.");
            }

            CasaMulherAuth.salvarSessao(result);
            window.location.href = "painel.html";
        } catch (err) {
            setMessage(msg, err.message, "error");
        } finally {
            btn.disabled = false;
        }
    });
}
setupPasskeyLogin();

function setupPasskeyRegistro() {
    const btn = document.getElementById("btn-cadastrar-passkey");
    const msg = document.getElementById("mensagem-passkey-cadastro");
    if (!btn) return;
    if (!isPasskeySupported()) {
        btn.style.display = "none";
        setMessage(msg, "O seu navegador ou dispositivo n\u00e3o suporta chaves de acesso (WebAuthn).", "error");
        return;
    }
    btn.addEventListener("click", async () => {
        try {
            btn.disabled = true;
            setMessage(msg, "Iniciando cadastro da chave de acesso...", "");
            const resInit = await fetch(`${API_BASE_URL}/api/passkeys/registrar/iniciar`, { method: "POST", headers: getAuthHeaders(false) });
            if (!resInit.ok) throw new Error(await readApiMessage(resInit));
            const initData = await resInit.json();
            const options = initData.publicKeyOptions;
            options.challenge = base64urlToBuffer(options.challenge);
            options.user.id = base64urlToBuffer(options.user.id);
            if (options.excludeCredentials) {
                options.excludeCredentials.forEach(c => c.id = base64urlToBuffer(c.id));
            }
            const credential = await navigator.credentials.create({ publicKey: options });
            const credData = {
                id: credential.id,
                rawId: bufferToBase64url(credential.rawId),
                type: credential.type,
                response: {
                    attestationObject: bufferToBase64url(credential.response.attestationObject),
                    clientDataJSON: bufferToBase64url(credential.response.clientDataJSON)
                }
            };
            const resComplete = await fetch(`${API_BASE_URL}/api/passkeys/registrar/concluir`, {
                method: "POST",
                headers: getAuthHeaders(true),
                body: JSON.stringify({ challengeId: initData.challengeId, credential: credData, nomeDispositivo: navigator.platform || "Dispositivo" })
            });
            if (!resComplete.ok) throw new Error(await readApiMessage(resComplete));
            setMessage(msg, "Chave de acesso cadastrada com sucesso!", "success");
            if (typeof carregarPasskeys === "function") carregarPasskeys();
        } catch (err) {
            setMessage(msg, err.message, "error");
        } finally {
            btn.disabled = false;
        }
    });
}
setupPasskeyRegistro();

function setupPasskeyReconfirmacao() {
    const form = document.getElementById("form-reconfirmacao-passkey");
    const msg = document.getElementById("mensagem-reconfirmacao");
    if (!form) return;

    const identificadorInput = document.getElementById("reconfirmar-identificador");
    const subtitulo = document.getElementById("reconfirmacao-subtitulo");
    const identificadorSalvo = sessionStorage.getItem("reconfirmacao_identificador");
    const motivo = sessionStorage.getItem("reconfirmacao_motivo");

    if (subtitulo) {
        subtitulo.textContent = motivo === "primeiro_acesso"
            ? "Como este é seu primeiro acesso por chave de acesso, precisamos confirmar sua identidade uma vez com ID e senha."
            : "Para sua segurança, como faz mais de 7 dias desde o último login completo, precisamos confirmar sua identidade.";
    }

    if (identificadorInput && identificadorSalvo) {
        identificadorInput.value = identificadorSalvo;
    }

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        try {
            disableSubmit(form, true);
            setMessage(msg, "Validando credenciais...", "");
            const reconfirmacaoId = sessionStorage.getItem("reconfirmacao_id");
            if (!reconfirmacaoId) throw new Error("ID de reconfirma\u00e7\u00e3o n\u00e3o encontrado.");
            const identificadorFuncionario = document.getElementById("reconfirmar-identificador").value.trim();
            const senha = document.getElementById("reconfirmar-senha").value;
            const codigo2fa = document.getElementById("reconfirmar-2fa")?.value;
            const payload = { reconfirmacaoId, identificadorFuncionario, senha };
            if (codigo2fa) payload.codigoDoAplicativo = codigo2fa;
            const res = await fetch(`${API_BASE_URL}/api/auth/passkey/reconfirmar`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });
            const result = await res.json();
            if (!res.ok) throw new Error(result.mensagem || "Falha na reconfirma\u00e7\u00e3o");
            sessionStorage.removeItem("reconfirmacao_id");
            sessionStorage.removeItem("reconfirmacao_identificador");
            sessionStorage.removeItem("reconfirmacao_motivo");
            CasaMulherAuth.salvarSessao(result);
            window.location.href = "painel.html";
        } catch (err) {
            setMessage(msg, err.message, "error");
        } finally {
            disableSubmit(form, false);
        }
    });
}
setupPasskeyReconfirmacao();

async function carregarPasskeys() {
    const ul = document.getElementById("lista-passkeys");
    if (!ul) return;
    try {
        const res = await fetch(`${API_BASE_URL}/api/passkeys`, { headers: getAuthHeaders(false) });
        if (!res.ok) throw new Error();
        const chaves = await res.json();
        ul.innerHTML = "";
        if (chaves.length === 0) {
            ul.innerHTML = `<li style="list-style-type: none; color: var(--color-text-light);">Nenhuma chave cadastrada.</li>`;
            return;
        }
        for (const c of chaves) {
            const li = document.createElement("li");
            li.style.listStyleType = "none";
            li.style.marginBottom = "0.5rem";
            li.style.display = "flex";
            li.style.justifyContent = "space-between";
            li.style.alignItems = "center";
            li.style.padding = "0.5rem";
            li.style.border = "1px solid var(--color-border)";
            li.style.borderRadius = "var(--border-radius)";
            li.innerHTML = `<div><strong>${escapeHtml(c.nomeDispositivo)}</strong><br><small style="color:var(--color-text-light)">Criada: ${new Date(c.criadoEm).toLocaleDateString()}</small></div>`;
            const btn = document.createElement("button");
            btn.textContent = "Remover";
            btn.className = "btn-secondary";
            btn.style.padding = "0.25rem 0.5rem";
            btn.style.fontSize = "0.8rem";
            btn.style.width = "auto";
            btn.onclick = async () => {
                if (confirm("Remover esta chave de acesso?")) {
                    await fetch(`${API_BASE_URL}/api/passkeys/${c.id}`, { method: "DELETE", headers: getAuthHeaders(false) });
                    carregarPasskeys();
                }
            };
            li.appendChild(btn);
            ul.appendChild(li);
        }
    } catch {
        ul.innerHTML = `<li style="list-style-type: none; color: red;">Erro ao carregar chaves.</li>`;
    }
}
if (window.location.pathname.endsWith("seguranca.html")) { carregarPasskeys(); }
