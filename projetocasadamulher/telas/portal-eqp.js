(function () {
    const config = window.CasaMulherConfig || {};
    const apiBaseUrl = (config.apiBaseUrl || window.API_BASE_URL || "").replace(/\/$/, "");

    function $(id) {
        return document.getElementById(id);
    }

    function apiUrl(path) {
        return `${apiBaseUrl}${path}`;
    }

    async function apiFetch(path, options) {
        const response = await fetch(apiUrl(path), Object.assign({
            credentials: "include",
            headers: {
                "Content-Type": "application/json"
            }
        }, options || {}));

        const contentType = response.headers.get("content-type") || "";
        const body = contentType.includes("application/json")
            ? await response.json()
            : await response.text();

        if (!response.ok) {
            const error = new Error(extractMessage(body, response.status));
            error.body = body;
            error.status = response.status;
            throw error;
        }

        return body;
    }

    function extractMessage(body, status) {
        if (body && typeof body === "object") {
            const errors = Array.isArray(body.erros) ? ` ${body.erros.join(" ")}` : "";
            return `${body.mensagem || "Operação não concluída."}${errors}`;
        }

        if (typeof body === "string" && body.trim()) {
            return body.trim();
        }

        return `Operação não concluída (${status}).`;
    }

    function setMessage(element, text, type) {
        if (!element) {
            return;
        }

        element.textContent = text || "";
        element.classList.remove("info", "success", "error", "notice-success", "notice-error");

        if (type) {
            element.classList.add(type);
        }

        if (element.classList.contains("notice")) {
            if (type === "success") {
                element.classList.add("notice-success");
            }

            if (type === "error") {
                element.classList.add("notice-error");
            }
        }
    }

    function show(element, visible) {
        if (!element) {
            return;
        }

        element.classList.toggle("hidden", !visible);
    }

    function setupLoginLinks() {
        document.querySelectorAll("[data-portal-login]").forEach((link) => {
            link.href = apiUrl("/api/portal-eqp/github/login");
        });
    }

    async function loadStatus() {
        const statusElements = [
            $("portalEqpStatus"),
            $("portalEquipeStatus")
        ].filter(Boolean);

        if (statusElements.length === 0) {
            return null;
        }

        try {
            const status = await apiFetch("/api/portal-eqp/status", { method: "GET" });
            const type = status.oauthConfigurado && status.escritaConfigurada ? "success" : "error";

            statusElements.forEach((element) => {
                setMessage(element, status.mensagem, type);
            });

            return status;
        } catch (error) {
            statusElements.forEach((element) => {
                setMessage(element, `Não foi possível consultar o portal: ${error.message}`, "error");
            });
            return null;
        }
    }

    async function loadPortal() {
        let me = { autenticado: false, logado: false };
        try {
            me = await apiFetch("/api/portal-eqp/me", { method: "GET" });
        } catch (error) {
            setMessage($("portalEqpActivationMessage"), error.message, "error");
        }

        updateDashboardState(me);
        updateDiagnostic(me);

        const page = $("portalEqpPage");
        if (!page) {
            return;
        }

        const loginBox = $("portalEqpLoginBox");
        const userBox = $("portalEqpUserBox");
        const memberBox = $("portalEqpMemberBox");
        const convitesBox = $("portalEqpConvitesBox");
        const activationBox = $("portalEqpActivationBox");
        const accessRequestBox = $("portalEqpAccessRequestBox");
        const userName = $("portalEqpUserName");
        const userStatus = $("portalEqpUserStatus");
        const inviteList = $("portalEqpInviteList");

        show(userBox, false);
        show(memberBox, false);
        show(convitesBox, false);
        show(activationBox, false);
        show(accessRequestBox, false);

        try {
            show(loginBox, !me.autenticado && !me.logado);
            show(userBox, me.autenticado || me.logado);

            if (!me.autenticado && !me.logado) {
                setMessage($("portalEqpActivationMessage"), "Entre com GitHub antes de ativar um convite.", "info");
                return;
            }

            const githubUsername = me.gitHubUsername || me.githubUsername || "";
            userName.textContent = `@${githubUsername}`;
            userStatus.textContent = me.autorizado ? "Autorizado" : "Não autorizado";

            if (!me.autorizado) {
                try {
                    const diagnostico = await apiFetch("/api/portal-eqp/github/diagnostico", { method: "GET" });
                    renderAccessRequest(me, diagnostico);
                    show(accessRequestBox, true);
                } catch (err) {
                    renderAccessRequest(me, null);
                    show(accessRequestBox, true);
                }
                return;
            }

            if (me.membro) {
                renderMember(me.membro);
                show(memberBox, true);
                setMessage($("portalEqpResetMessage"), "Você pode redefinir apenas a sua própria senha.", "info");
                return;
            }

            const convites = await apiFetch("/api/portal-eqp/convites-disponiveis", { method: "GET" });
            renderInvites(convites, inviteList);
            show(convitesBox, true);
        } catch (error) {
            setMessage($("portalEqpActivationMessage"), error.message, "error");
        }
    }

    function updateDashboardState(me) {
        show($("sessao-verificando"), false);
        const logado = me.autenticado || me.logado;

        if (logado) {
            show($("sessao-deslogada"), false);
            show($("sessao-logada"), true);
            
            const usernameDisplay = $("github-username-display");
            if (usernameDisplay) {
                usernameDisplay.textContent = `@${me.gitHubUsername || me.githubUsername || ""}`;
            }

            const roleTags = $("github-role-tags");
            if (roleTags) {
                const tags = [];
                if (me.ehOwner) tags.push('<strong style="color: #0056b3;">[Owner]</strong>');
                if (me.ehMembro) tags.push('<span style="color: #28a745;">[Equipe]</span>');
                roleTags.innerHTML = tags.join(' ');
            }

            if (me.ehOwner === true) {
                show($("ownerRecoverySection"), true);
            }
        } else {
            show($("sessao-logada"), false);
            show($("sessao-deslogada"), true);
        }
    }

    function updateDiagnostic(me) {
        const diag = $("portalDiagnostico");
        if (!diag) return;

        const info = [
            `Ambiente: ${config.apiBaseUrl?.includes('localhost') ? 'Local/Dev' : 'Staging'}`,
            `Autenticado: ${me.autenticado || me.logado ? 'Sim' : 'Não'}`,
            `GitHub Usuário: ${me.gitHubUsername || me.githubUsername || 'Nenhum'}`,
            `É Owner: ${me.ehOwner ? 'Sim' : 'Não'}`,
            `É Membro EQP: ${me.ehMembro ? 'Sim' : 'Não'}`,
            `Autorizado: ${me.autorizado ? 'Sim' : 'Não'}`,
            `Sessão Expira Em: ${me.sessionExpiresAt ? new Date(me.sessionExpiresAt).toLocaleString() : 'N/A'}`
        ];

        diag.textContent = info.join('\n');
    }

    function renderMember(membro) {
        const info = $("portalEqpMemberInfo");

        if (!info) {
            return;
        }

        info.replaceChildren(
            profileItem("EQP", membro.eqpId),
            profileItem("ADM pareado", membro.admId),
            profileItem("Nome", membro.nome),
            profileItem("GitHub", `@${membro.gitHubUsername || membro.githubUsername || ""}`),
            profileItem("Fluxo", membro.fluxoTrabalho)
        );
    }

    function profileItem(label, value) {
        const wrapper = document.createElement("div");
        const dt = document.createElement("dt");
        const dd = document.createElement("dd");

        dt.textContent = label;
        dd.textContent = value || "-";
        wrapper.append(dt, dd);
        return wrapper;
    }

    function renderAccessRequest(me, diagnostico) {
        const username = me.gitHubUsername || me.githubUsername || "";
        const request = me.solicitacaoAcesso;
        const summary = $("portalEqpAccessRequestSummary");
        const info = $("portalEqpAccessRequestInfo");
        const message = $("portalEqpAccessRequestMessage");

        if (summary) summary.textContent = `Você entrou com GitHub como @${username}, mas seu acesso ainda não foi liberado.`;
        if (info) {
            info.replaceChildren(
                profileItem("E-mail verificado", diagnostico?.primaryVerifiedEmail || request?.primaryVerifiedEmail || "Não disponível"),
                profileItem("Organização", diagnostico?.orgMembership === true ? diagnostico.orgConfigurada : "Não confirmada"),
                profileItem("Time", diagnostico?.teamMembership === true ? diagnostico.teamSlugConfigurado : "Não confirmado"),
                profileItem("Permissão read:org", diagnostico?.readOrgPresente ? "Concedida" : "Ausente"),
                profileItem("Permissão user:email", diagnostico?.userEmailScopePresente ? "Concedida" : "Ausente")
            );
        }

        if (request?.status === "pending") {
            setMessage(message, `Solicitação pendente desde ${new Date(request.requestedAt).toLocaleString()}.`, "info");
        } else if (request?.status === "reauthorization_requested") {
            setMessage(message, "O owner pediu que você reautorize o GitHub com e-mail e organização.", "error");
        } else {
            setMessage(message, diagnostico?.motivoNegacao || "Envie uma solicitação para o owner analisar seu acesso.", "info");
        }
    }

    function renderInvites(convites, container) {
        if (!container) {
            return;
        }

        container.replaceChildren();

        if (!Array.isArray(convites) || convites.length === 0) {
            const empty = document.createElement("p");
            empty.className = "hint";
            empty.textContent = "Nenhum convite disponível para o seu GitHub agora.";
            container.append(empty);
            return;
        }

        convites.forEach((convite) => {
            const card = document.createElement("button");
            card.type = "button";
            card.className = "invite-card";
            const eqp = document.createElement("strong");
            const adm = document.createElement("span");
            const status = document.createElement("small");

            eqp.textContent = convite.eqpId;
            adm.textContent = `ADM pareado: ${convite.admId}`;
            status.textContent = convite.reservadoParaGitHub
                ? `Reservado para @${convite.reservadoParaGitHub}`
                : "Disponível";
            card.append(eqp, adm, status);
            card.addEventListener("click", () => selectInvite(convite));
            container.append(card);
        });
    }

    function selectInvite(convite) {
        $("portalEqpId").value = convite.eqpId || "";
        $("portalAdmId").value = convite.admId || "";
        show($("portalEqpActivationBox"), true);
        setMessage($("portalEqpActivationMessage"), "Informe seu nome e uma senha criada só para este projeto.", "info");
    }

    function setupForms() {
        const activationForm = $("formPortalEqpAtivar");
        const resetForm = $("formPortalEqpReset");
        const logoutButton = $("portalEqpLogout");
        const copyButton = $("portalEqpCopySync");
        const requestAccessButton = $("portalEqpRequestAccess");
        const requestLogoutButton = $("portalEqpRequestLogout");

        if (activationForm) {
            activationForm.addEventListener("submit", submitActivation);
        }

        if (resetForm) {
            resetForm.addEventListener("submit", submitReset);
        }

        if (logoutButton) {
            logoutButton.addEventListener("click", async () => {
                await apiFetch("/api/portal-eqp/github/logout", { method: "POST", body: "{}" });
                window.location.reload();
            });
        }

        requestAccessButton?.addEventListener("click", async () => {
            requestAccessButton.disabled = true;
            try {
                const request = await apiFetch("/api/portal-eqp/acesso/solicitar", { method: "POST", body: "{}" });
                setMessage($("portalEqpAccessRequestMessage"), `Sua solicitação foi enviada para o owner em ${new Date(request.requestedAt).toLocaleString()}.`, "success");
            } catch (error) {
                setMessage($("portalEqpAccessRequestMessage"), error.message, "error");
            } finally {
                requestAccessButton.disabled = false;
            }
        });

        requestLogoutButton?.addEventListener("click", async () => {
            await apiFetch("/api/portal-eqp/github/logout", { method: "POST", body: "{}" });
            window.location.reload();
        });

        const btnGithubLogout = $("btn-github-logout");
        if (btnGithubLogout) {
            btnGithubLogout.addEventListener("click", async () => {
                await apiFetch("/api/portal-eqp/github/logout", { method: "POST", body: "{}" });
                window.location.reload();
            });
        }

        if (copyButton) {
            copyButton.addEventListener("click", async () => {
                const command = $("portalEqpSyncCommand")?.textContent || ".\\casa_da_mulher.cmd equipe sync";
                await navigator.clipboard.writeText(command);
                copyButton.textContent = "Copiado";
                setTimeout(() => {
                    copyButton.textContent = "Copiar comando";
                }, 1600);
            });
        }
    }

    async function submitActivation(event) {
        event.preventDefault();
        const button = event.submitter;
        const message = $("portalEqpActivationMessage");

        const body = {
            eqpId: $("portalEqpId").value,
            nome: $("portalNome").value,
            emailRecuperacao: $("portalEmailRecuperacao").value,
            senha: $("portalSenha").value,
            confirmarSenha: $("portalConfirmarSenha").value
        };

        try {
            if (button) {
                button.disabled = true;
            }

            const membro = await apiFetch("/api/portal-eqp/ativar", {
                method: "POST",
                body: JSON.stringify(body)
            });

            setMessage(message, `Seu acesso foi ativado: ${membro.eqpId} / ${membro.admId}. O ambiente local será atualizado automaticamente em até um minuto.`, "success");
            await loadPortal();
        } catch (error) {
            setMessage(message, error.message, "error");
        } finally {
            if (button) {
                button.disabled = false;
            }
        }
    }

    async function submitReset(event) {
        event.preventDefault();
        const button = event.submitter;
        const message = $("portalEqpResetMessage");

        const body = {
            novaSenha: $("portalNovaSenha").value,
            confirmarSenha: $("portalConfirmarNovaSenha").value
        };

        try {
            if (button) {
                button.disabled = true;
            }

            const membro = await apiFetch("/api/portal-eqp/redefinir-minha-senha", {
                method: "POST",
                body: JSON.stringify(body)
            });

            setMessage(message, `Senha redefinida para ${membro.eqpId}. O ambiente local será atualizado automaticamente em até um minuto.`, "success");
        } catch (error) {
            setMessage(message, error.message, "error");
        } finally {
            if (button) {
                button.disabled = false;
            }
        }
    }

    window.addEventListener("DOMContentLoaded", async () => {
        setupLoginLinks();
        setupForms();
        await loadStatus();
        await loadPortal();
    });
})();
