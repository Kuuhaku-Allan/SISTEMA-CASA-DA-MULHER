document.addEventListener('DOMContentLoaded', async () => {
    const loadingDiv = document.getElementById('loading');
    const contentDiv = document.getElementById('recovery-content');
    const statusInfo = document.getElementById('status-info');
    const form = document.getElementById('recovery-form');
    const tokenGroup = document.getElementById('token-group');
    const tokenInput = document.getElementById('token');
    const resultMessage = document.getElementById('result-message');
    const btnSubmit = document.getElementById('btn-submit');

    let currentNonce = null;

    try {
        // GET Status - requires GitHub Gate session
        const response = await fetch(`${API_BASE_URL}/api/homologacao/owner-recovery/status`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            }
        });

        if (response.status === 401) {
            window.location.href = '/api/portal-eqp/github/login';
            return;
        }

        if (response.status === 403) {
            const error = await response.json().catch(() => ({ mensagem: 'Acesso negado' }));
            document.body.innerHTML = `<div style="text-align:center; margin-top:50px; color:red;">
                <h2>Acesso Negado</h2>
                <p>${error.mensagem}</p>
                <p>Apenas o owner configurado no GitHub Gate pode acessar esta tela.</p>
                <a href="equipe.html">Voltar</a>
            </div>`;
            return;
        }

        if (!response.ok) {
            throw new Error('Falha ao obter status de recuperação.');
        }

        const data = await response.json();
        
        // Show interface
        loadingDiv.classList.add('hidden');
        contentDiv.classList.remove('hidden');

        currentNonce = data.nonce;

        statusInfo.innerHTML = `
            <strong>Ambiente:</strong> ${data.ambiente} <br>
            <strong>Usuário GitHub Detectado:</strong> ${data.usuarioGitHubAtual} <br>
            <strong>Target IDs:</strong> ${data.eqpId} e ${data.admId}
        `;

        if (data.tokenObrigatorio) {
            tokenGroup.classList.remove('hidden');
            tokenInput.required = true;
        }

    } catch (error) {
        loadingDiv.innerHTML = `<span style="color:red;">Erro: ${error.message}</span>`;
    }

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        const confirmacao = document.getElementById('confirmacao').value.trim();
        const token = tokenInput.value.trim();

        if (confirmacao !== 'RESETAR_SEGURANCA_OWNER') {
            resultMessage.style.color = 'red';
            resultMessage.textContent = 'Erro: A confirmação textual não bate (precisa ser exata).';
            return;
        }

        if (!currentNonce) {
            resultMessage.style.color = 'red';
            resultMessage.textContent = 'Erro: Nonce de segurança ausente. Recarregue a página.';
            return;
        }

        btnSubmit.disabled = true;
        btnSubmit.textContent = 'Executando...';
        resultMessage.textContent = '';

        try {
            const payload = {
                confirmacao: confirmacao,
                nonce: currentNonce
            };

            if (!tokenGroup.classList.contains('hidden')) {
                payload.ownerRecoveryToken = token;
            }

            const response = await fetch(`${API_BASE_URL}/api/homologacao/owner-recovery/reset-security`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            const result = await response.json();

            if (!response.ok) {
                resultMessage.style.color = 'red';
                resultMessage.textContent = `Erro: ${result.mensagem || 'Falha na recuperação'}`;
                // Nonce is single use, so if it fails, we should reload to get a new one
                if (result.mensagem && result.mensagem.includes('Nonce')) {
                    setTimeout(() => window.location.reload(), 2000);
                }
            } else {
                resultMessage.style.color = 'green';
                resultMessage.innerHTML = `
                    Sucesso!<br><br>
                    ${result.mensagem || ''}<br><br>
                    Redirecionando para o login da equipe...
                `;
                setTimeout(() => {
                    window.location.href = 'equipe.html';
                }, 4000);
            }
        } catch (error) {
            resultMessage.style.color = 'red';
            resultMessage.textContent = `Erro de rede: ${error.message}`;
        } finally {
            btnSubmit.disabled = false;
            btnSubmit.textContent = 'Executar Recuperação do Owner';
        }
    });
});
