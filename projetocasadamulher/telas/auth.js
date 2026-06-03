(function () {
    window.API_BASE_URL = window.API_BASE_URL || "http://localhost:5001";

    const STORAGE_KEYS = [
        "token",
        "perfil",
        "email",
        "nomeCompleto",
        "identificadorFuncionario",
        "doisFatoresObrigatorio",
        "doisFatoresAtivado",
        "deveTrocarSenha"
    ];

    const PERMISSOES_POR_AREA = {
        convites: ["adm"],
        funcionarios: ["adm"],
        auditoria: ["adm"],
        emails: ["adm"],
        recepcao: ["adm", "recepcao"],
        cursos: ["adm", "professor"],
        social: ["adm", "as_social"],
        juridico: ["adm", "juridico"],
        relatorios: ["adm", "as_social", "juridico"]
    };

    function getToken() {
        return localStorage.getItem("token");
    }

    function getPerfil() {
        return localStorage.getItem("perfil");
    }

    function getUsuario() {
        return {
            token: getToken(),
            perfil: getPerfil(),
            email: localStorage.getItem("email") || "",
            nomeCompleto: localStorage.getItem("nomeCompleto") || "",
            identificadorFuncionario: localStorage.getItem("identificadorFuncionario") || "",
            doisFatoresObrigatorio: localStorage.getItem("doisFatoresObrigatorio") === "true",
            doisFatoresAtivado: localStorage.getItem("doisFatoresAtivado") === "true",
            deveTrocarSenha: localStorage.getItem("deveTrocarSenha") === "true"
        };
    }

    function estaLogado() {
        return Boolean(getToken());
    }

    function podeAcessar(area) {
        const perfil = getPerfil();
        const perfisPermitidos = PERMISSOES_POR_AREA[area];

        if (!perfil || !perfisPermitidos) {
            return false;
        }

        return perfisPermitidos.includes(perfil);
    }

    function mostrarMensagem(element, text, type) {
        if (!element) {
            return;
        }

        element.textContent = text;
        element.className = `message ${type || ""}`.trim();
    }

    function limparSessao() {
        STORAGE_KEYS.forEach(function (key) {
            localStorage.removeItem(key);
        });

        sessionStorage.removeItem("loginTemporario2fa");
    }

    function logout(message) {
        limparSessao();

        if (message) {
            sessionStorage.setItem("mensagemLogin", message);
        }

        window.location.href = "index.html";
    }

    function salvarSessao(resultado) {
        localStorage.setItem("token", resultado.token);
        salvarUsuario(resultado);
    }

    function saveToken(tokenOuResultado) {
        if (tokenOuResultado && typeof tokenOuResultado === "object") {
            salvarSessao(tokenOuResultado);
            return;
        }

        localStorage.setItem("token", tokenOuResultado || "");
    }

    function salvarUsuario(usuario) {
        localStorage.setItem("perfil", usuario.perfil || "");
        localStorage.setItem("email", usuario.email || "");
        localStorage.setItem("nomeCompleto", usuario.nomeCompleto || "");
        localStorage.setItem("identificadorFuncionario", usuario.identificadorFuncionario || "");
        localStorage.setItem("doisFatoresObrigatorio", String(Boolean(usuario.doisFatoresObrigatorio)));
        localStorage.setItem("doisFatoresAtivado", String(Boolean(usuario.doisFatoresAtivado)));
        localStorage.setItem("deveTrocarSenha", String(Boolean(usuario.deveTrocarSenha)));
    }

    function getAuthHeaders(includeJson) {
        const headers = {};

        if (includeJson) {
            headers["Content-Type"] = "application/json";
        }

        const token = getToken();

        if (token) {
            headers.Authorization = `Bearer ${token}`;
        }

        return headers;
    }

    async function apiFetch(url, options) {
        const settings = options || {};
        const mensagemElement = settings.mensagemElement;
        const forbiddenMessage = settings.forbiddenMessage || "Você não tem permissão para acessar esta área.";
        const fetchOptions = Object.assign({}, settings);

        delete fetchOptions.mensagemElement;
        delete fetchOptions.forbiddenMessage;

        const headers = new Headers(fetchOptions.headers || {});
        const token = getToken();

        if (!token) {
            logout("Sua sessão expirou. Faça login novamente.");
            return new Response(null, { status: 401 });
        }

        headers.set("Authorization", `Bearer ${token}`);

        if (fetchOptions.body && typeof fetchOptions.body === "object" && !(fetchOptions.body instanceof FormData)) {
            if (!headers.has("Content-Type")) {
                headers.set("Content-Type", "application/json");
            }

            fetchOptions.body = JSON.stringify(fetchOptions.body);
        }

        fetchOptions.headers = headers;

        const requestUrl = url.startsWith("http") ? url : `${window.API_BASE_URL}${url}`;
        let response;

        try {
            response = await fetch(requestUrl, fetchOptions);
        } catch {
            mostrarMensagem(mensagemElement, "Não foi possível conectar à API.", "error");
            throw new Error("Não foi possível conectar à API.");
        }

        if (response.status === 401) {
            logout("Sua sessão expirou. Faça login novamente.");
            return response;
        }

        if (response.status === 403) {
            mostrarMensagem(mensagemElement, forbiddenMessage, "error");
        }

        return response;
    }

    async function carregarUsuarioAtual(options) {
        const settings = options || {};

        if (!estaLogado()) {
            if (settings.redirect !== false) {
                logout("Sua sessão expirou. Faça login novamente.");
            }

            return null;
        }

        let response;

        try {
            response = await apiFetch("/api/auth/me", {
                mensagemElement: settings.mensagemElement
            });
        } catch {
            return null;
        }

        if (!response.ok) {
            return null;
        }

        const usuario = await response.json();
        salvarUsuario(usuario);
        return usuario;
    }

    async function protegerPagina(options) {
        const settings = options || {};
        const usuario = await carregarUsuarioAtual(settings);

        if (!usuario) {
            return null;
        }

        const paginaTrocaSenha = window.location.pathname.endsWith("trocar-senha.html");

        if (usuario.deveTrocarSenha && !paginaTrocaSenha && settings.permitirTrocaSenhaPendente !== true) {
            window.location.href = "trocar-senha.html";
            return null;
        }

        return usuario;
    }

    async function protegerPerfil(perfil, options) {
        const settings = options || {};
        const usuario = await protegerPagina(settings);

        if (!usuario) {
            return null;
        }

        if (usuario.perfil !== perfil) {
            if (settings.conteudoElement) {
                settings.conteudoElement.classList.add("hidden");
            }

            if (settings.restritoElement) {
                settings.restritoElement.classList.remove("hidden");
            }

            mostrarMensagem(settings.mensagemElement, "Você não tem permissão para acessar esta área.", "error");
            return null;
        }

        return usuario;
    }

    window.CasaMulherAuth = {
        apiFetch,
        carregarUsuarioAtual,
        estaLogado,
        getAuthHeaders,
        getPerfil,
        getToken,
        getUsuario,
        limparSessao,
        logout,
        podeAcessar,
        protegerPagina,
        protegerPerfil,
        saveToken,
        salvarSessao,
        salvarUsuario
    };
})();
