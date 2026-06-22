function setupRecuperarSeguranca() {
    const formVerificar = document.getElementById("formVerificarOpcoesSeguranca");
    const formSolicitar = document.getElementById("formSolicitarRecuperacaoSeguranca");
    
    if (!formVerificar || !formSolicitar) return;

    const btnVerificar = document.getElementById("btnVerificarOpcoes");
    const btnEnviar = document.getElementById("btnEnviarLinkRecuperacao");
    const containerOpcoes = document.getElementById("opcoesEmailSeguranca");
    const mensagem = document.getElementById("mensagemRecuperarSeguranca");

    let identificadorAtual = "";
    let senhaAtual = "";

    formVerificar.addEventListener("submit", async function(event) {
        event.preventDefault();

        if (!formVerificar.reportValidity()) return;

        const identificador = document.getElementById("identificadorSeguranca").value.trim();
        const senha = document.getElementById("senhaSeguranca").value;

        setMessage(mensagem, "");
        disableSubmit(formVerificar, true, btnVerificar, "Verificando...");

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/recuperar-seguranca/opcoes`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ identificador, senha })
            });

            const resultado = await response.json();

            if (!response.ok) {
                setMessage(mensagem, resultado.mensagem || "Não foi possível verificar as opções.", "error");
                return;
            }

            // Guardar para o próximo passo
            identificadorAtual = identificador;
            senhaAtual = senha;

            // Montar opções
            containerOpcoes.innerHTML = "";
            resultado.opcoes.forEach((opcao, index) => {
                const checked = index === 0 ? "checked" : "";
                const labelText = opcao.id === "principal" ? "E-mail principal" : "E-mail de recuperação";
                
                const div = document.createElement("div");
                div.innerHTML = `
                    <label style="display: flex; align-items: center; gap: 8px; cursor: pointer; padding: 12px; border: 1px solid #F1C8D8; border-radius: 8px; background: #FFF;">
                        <input type="radio" name="destinoEmail" value="${opcao.id}" ${checked}>
                        <div>
                            <span style="display: block; font-weight: 600; color: #8A3D66; font-size: 0.95rem;">${labelText}</span>
                            <span style="display: block; color: #A26D85; font-size: 0.85rem;">${opcao.mascarado}</span>
                        </div>
                    </label>
                `;
                containerOpcoes.appendChild(div);
            });

            // Esconder o form de verificar, mostrar o form de solicitar
            formVerificar.classList.add("hidden");
            formSolicitar.classList.remove("hidden");
            setMessage(mensagem, "");

        } catch (error) {
            setMessage(mensagem, "Erro ao conectar com a API.", "error");
        } finally {
            disableSubmit(formVerificar, false, btnVerificar, "Continuar");
        }
    });

    formSolicitar.addEventListener("submit", async function(event) {
        event.preventDefault();

        const destinoSelecionado = document.querySelector('input[name="destinoEmail"]:checked');
        if (!destinoSelecionado) {
            setMessage(mensagem, "Selecione um e-mail para receber o link.", "error");
            return;
        }

        setMessage(mensagem, "");
        disableSubmit(formSolicitar, true, btnEnviar, "Enviando...");

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/recuperar-seguranca/solicitar`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    identificador: identificadorAtual,
                    senha: senhaAtual,
                    destinoEmail: destinoSelecionado.value
                })
            });

            const resultado = await response.json();

            if (!response.ok) {
                setMessage(mensagem, resultado.mensagem || "Não foi possível enviar o link.", "error");
                return;
            }

            setMessage(mensagem, "");
            formSolicitar.innerHTML = `<p style="color: #8A3D66; font-weight: bold; padding: 16px; background: #FFF2F7; border: 1px solid #F1C8D8; border-radius: 8px;">Acesse o e-mail selecionado e clique no link para concluir a recuperação.</p>`;
        } catch (error) {
            setMessage(mensagem, "Erro ao conectar com a API.", "error");
        } finally {
            if (btnEnviar) disableSubmit(formSolicitar, false, btnEnviar, "Enviar link");
        }
    });
}

function setupConfirmarRecuperacaoSeguranca() {
    const form = document.getElementById("formConfirmarRecuperacaoSeguranca");
    if (!form) return;

    const params = new URLSearchParams(window.location.search);
    const token = params.get("token");

    const loadingDiv = document.getElementById("loadingRecuperacao");
    const contentDiv = document.getElementById("contentRecuperacao");
    const errorDiv = document.getElementById("errorRecuperacao");
    const identificadorSpan = document.getElementById("identificadorConfirmacao");
    const opcoesContainer = document.getElementById("opcoesMetodosSeguranca");
    const mensagem = document.getElementById("mensagemConfirmarRecuperacao");
    const btnConfirmar = document.getElementById("btnConfirmarRecuperacao");

    if (!token) {
        loadingDiv.classList.add("hidden");
        errorDiv.classList.remove("hidden");
        return;
    }

    async function carregarDetalhes() {
        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/recuperar-seguranca/detalhes?token=${encodeURIComponent(token)}`, {
                method: "GET"
            });

            if (!response.ok) {
                throw new Error("Token inválido");
            }

            const resultado = await response.json();
            
            identificadorSpan.textContent = resultado.identificador;

            opcoesContainer.innerHTML = "";

            if (resultado.tem2fa) {
                opcoesContainer.innerHTML += `
                    <label style="display: flex; align-items: center; gap: 8px; cursor: pointer; padding: 12px; border: 1px solid #F1C8D8; border-radius: 8px; background: #FFF;">
                        <input type="checkbox" id="chkRedefinir2FA" class="soft-checkbox" checked>
                        <div>
                            <span style="display: block; font-weight: 600; color: #8A3D66; font-size: 0.95rem;">Aplicativo de Código (2FA)</span>
                            <span style="display: block; color: #A26D85; font-size: 0.85rem;">Remover o código atual (será necessário configurar outro depois)</span>
                        </div>
                    </label>
                `;
            }

            if (resultado.temPasskeys) {
                opcoesContainer.innerHTML += `
                    <label style="display: flex; align-items: center; gap: 8px; cursor: pointer; padding: 12px; border: 1px solid #F1C8D8; border-radius: 8px; background: #FFF;">
                        <input type="checkbox" id="chkRedefinirPasskeys" class="soft-checkbox" checked>
                        <div>
                            <span style="display: block; font-weight: 600; color: #8A3D66; font-size: 0.95rem;">Chaves de Acesso (Passkeys)</span>
                            <span style="display: block; color: #A26D85; font-size: 0.85rem;">Remover todas as chaves cadastradas</span>
                        </div>
                    </label>
                `;
            }

            loadingDiv.classList.add("hidden");
            contentDiv.classList.remove("hidden");

        } catch (error) {
            loadingDiv.classList.add("hidden");
            errorDiv.classList.remove("hidden");
        }
    }

    form.addEventListener("submit", async function(event) {
        event.preventDefault();

        const chk2FA = document.getElementById("chkRedefinir2FA");
        const chkPasskeys = document.getElementById("chkRedefinirPasskeys");

        const redefinir2fa = chk2FA ? chk2FA.checked : false;
        const redefinirPasskeys = chkPasskeys ? chkPasskeys.checked : false;

        if (!redefinir2fa && !redefinirPasskeys) {
            setMessage(mensagem, "Selecione pelo menos um método para redefinir.", "error");
            return;
        }

        setMessage(mensagem, "");
        disableSubmit(form, true, btnConfirmar, "Confirmando...");

        try {
            const response = await fetch(`${API_BASE_URL}/api/auth/recuperar-seguranca/confirmar`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    token: token,
                    redefinir2fa: redefinir2fa,
                    redefinirPasskeys: redefinirPasskeys
                })
            });

            const resultado = await response.json();

            if (!response.ok) {
                setMessage(mensagem, resultado.mensagem || "Erro ao redefinir.", "error");
                return;
            }

            setMessage(mensagem, "");
            mensagem.classList.add("soft-auth-message");
            form.innerHTML = `
                <div class="soft-auth-actions" style="margin-top: 16px; text-align: center;">
                    <p style="color: #8A3D66; font-weight: bold; margin-bottom: 24px;">Sua segurança foi redefinida.</p>
                    <a href="index.html" class="soft-btn soft-btn-primary" style="text-decoration: none; width: 100%;">Fazer login agora</a>
                </div>
            `;
        } catch (error) {
            setMessage(mensagem, "Erro ao conectar com a API.", "error");
        } finally {
            if (btnConfirmar) disableSubmit(form, false, btnConfirmar, "Confirmar redefinição");
        }
    });

    carregarDetalhes();
}

document.addEventListener("DOMContentLoaded", function() {
    setupRecuperarSeguranca();
    setupConfirmarRecuperacaoSeguranca();
});
