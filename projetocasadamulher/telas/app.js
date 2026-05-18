const API_BASE_URL = "http://localhost:5001";

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

function setupCadastro() {
    const form = document.getElementById("formCadastroFuncionario");
    const mensagem = document.getElementById("mensagemCadastro");

    if (!form) {
        return;
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
            setMessage(mensagem, resultado.mensagem || "Cadastro realizado com sucesso.", "success");
            form.reset();

            setTimeout(function () {
                window.location.href = "index.html";
            }, 1200);
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

    if (!form) {
        return;
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (!form.reportValidity()) {
            return;
        }

        setMessage(mensagem, "Entrando...", "info");
        disableSubmit(form, true);

        const dados = {
            email: document.getElementById("email").value.trim(),
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

            localStorage.setItem("token", resultado.token);
            localStorage.setItem("perfil", resultado.perfil);
            localStorage.setItem("email", resultado.email);
            localStorage.setItem("nomeCompleto", resultado.nomeCompleto);

            setMessage(mensagem, "Login realizado com sucesso.", "success");

            setTimeout(function () {
                window.location.href = "painel.html";
            }, 600);
        } catch {
            setMessage(mensagem, "Erro ao conectar com a API. Verifique se o servidor esta rodando.", "error");
        } finally {
            disableSubmit(form, false);
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
    document.getElementById("painelEmail").textContent = localStorage.getItem("email") || "-";
    document.getElementById("painelPerfil").textContent = localStorage.getItem("perfil") || "-";

    document.getElementById("btnSair").addEventListener("click", function () {
        localStorage.removeItem("token");
        localStorage.removeItem("perfil");
        localStorage.removeItem("email");
        localStorage.removeItem("nomeCompleto");
        window.location.href = "index.html";
    });
}

setupCadastro();
setupLogin();
setupPainel();
