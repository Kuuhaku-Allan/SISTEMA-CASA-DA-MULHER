/**
 * equipe-ide-validacoes.js
 * Motor de validação automática para a IDE da Equipe (Fase 4)
 */

(function() {
    function gerarRelatorioValidacao(rascunho, checklistGeral) {
        const relatorio = {
            bloqueios: [],
            avisos: [],
            infos: []
        };

        const html = rascunho.arquivos['index.html'] || '';
        const css = rascunho.arquivos['style.css'] || '';
        const js = rascunho.arquivos['script.js'] || '';
        const ehTarefaVisual = rascunho.tarefa && (rascunho.tarefa.id === "criar-tela-soft-ui" || rascunho.tarefa.id === "criar-prototipo-html-simples" || rascunho.tarefa.id === "criar-lista-cards");

        // 1. Infos Iniciais
        if (rascunho.tarefa) {
            relatorio.infos.push({ id: 'info-tarefa', titulo: 'Tarefa selecionada', mensagem: rascunho.tarefa.titulo, severidade: 'info', arquivo: '' });
        }
        if (rascunho.areaProjeto) {
            relatorio.infos.push({ id: 'info-area', titulo: 'Área relacionada', mensagem: `${rascunho.areaProjeto.nome} (${rascunho.areaProjeto.perfil})`, severidade: 'info', arquivo: '' });
        } else {
            relatorio.avisos.push({ id: 'aviso-area', titulo: 'Área não informada', mensagem: 'Nenhuma área foi associada a este rascunho.', severidade: 'aviso', arquivo: '' });
        }

        // 2. Validações de Escopo e Tarefa
        const arquivosPermitidos = ['index.html', 'style.css', 'script.js'];
        Object.keys(rascunho.arquivos).forEach(nome => {
            if (!arquivosPermitidos.includes(nome)) {
                relatorio.bloqueios.push({ id: 'arquivo-inesperado', titulo: 'Arquivo inesperado', mensagem: `O arquivo ${nome} não é permitido no modo rascunho.`, arquivo: nome, severidade: 'bloqueio' });
            }
        });

        if (checklistGeral) {
            if (!checklistGeral.previewTestado || !checklistGeral.semDadosSensiveis || !checklistGeral.escopoConfirmado) {
                relatorio.bloqueios.push({ id: 'checklist-incompleto', titulo: 'Checklist incompleto', mensagem: 'Todos os itens do checklist geral devem estar marcados.', arquivo: '', severidade: 'bloqueio' });
            }
        }

        if (rascunho.checklistTarefa) {
            const faltando = rascunho.checklistTarefa.filter(t => !t.marcado);
            if (faltando.length > 0) {
                relatorio.bloqueios.push({ id: 'checklist-tarefa-incompleto', titulo: 'Checklist da tarefa', mensagem: 'Conclua todos os itens exigidos pela tarefa.', arquivo: '', severidade: 'bloqueio' });
            }
        }

        // 3. Validações HTML
        if (ehTarefaVisual && html.trim() === '') {
            relatorio.bloqueios.push({ id: 'html-vazio', titulo: 'HTML vazio', mensagem: 'O arquivo index.html está vazio para uma tarefa visual.', arquivo: 'index.html', severidade: 'bloqueio' });
        } else if (html.trim() !== '') {
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            
            // IDs duplicados
            const todosIds = Array.from(doc.querySelectorAll('[id]')).map(el => el.id);
            const idsDuplicados = todosIds.filter((item, index) => todosIds.indexOf(item) !== index);
            if (idsDuplicados.length > 0) {
                relatorio.bloqueios.push({ id: 'ids-duplicados', titulo: 'IDs duplicados', mensagem: `Os seguintes IDs estão repetidos: ${[...new Set(idsDuplicados)].join(', ')}.`, arquivo: 'index.html', severidade: 'bloqueio' });
            }

            // Conteúdo Útil
            const temConteudo = doc.querySelector('h1, h2, p, button, input, section, article, main, div:not(:empty)');
            if (!temConteudo && ehTarefaVisual) {
                relatorio.avisos.push({ id: 'sem-conteudo-util', titulo: 'Sem conteúdo útil', mensagem: 'O HTML parece não conter elementos visuais.', arquivo: 'index.html', severidade: 'aviso' });
            }

            // Botão sem texto
            const botoes = doc.querySelectorAll('button');
            botoes.forEach(b => {
                if (b.textContent.trim() === '' && !b.querySelector('img, svg, i')) {
                    relatorio.avisos.push({ id: 'botao-sem-texto', titulo: 'Botão sem texto', mensagem: 'Foi encontrado um botão vazio.', arquivo: 'index.html', severidade: 'aviso' });
                }
            });

            // Input sem label
            const inputs = doc.querySelectorAll('input:not([type="submit"]):not([type="button"]):not([type="hidden"])');
            inputs.forEach(input => {
                const id = input.id;
                const temLabel = (id && doc.querySelector(`label[for="${id}"]`)) || input.closest('label') || input.hasAttribute('aria-label') || input.hasAttribute('placeholder');
                if (!temLabel) {
                    relatorio.avisos.push({ id: 'input-sem-label', titulo: 'Input sem label', mensagem: 'Campo de formulário sem label associada.', arquivo: 'index.html', severidade: 'aviso' });
                }
            });

            // Links vazios
            const links = doc.querySelectorAll('a[href="#"], a:not([href])');
            if (links.length > 3) {
                relatorio.avisos.push({ id: 'links-vazios', titulo: 'Muitos links vazios', mensagem: 'Foram encontrados vários links com href="#".', arquivo: 'index.html', severidade: 'aviso' });
            }

            // Muitos estilos inline
            const inlines = doc.querySelectorAll('[style]');
            if (inlines.length > 5) {
                relatorio.avisos.push({ id: 'muitos-inlines', titulo: 'Estilos inline', mensagem: 'Foram encontrados muitos estilos inline. Prefira usar o style.css.', arquivo: 'index.html', severidade: 'aviso' });
            }

            // Dados reais aparentes
            const htmlText = doc.body.textContent || "";
            const cpfRegex = /\b\d{3}\.\d{3}\.\d{3}-\d{2}\b/;
            const emailRegex = /\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b/;
            if (cpfRegex.test(htmlText) || emailRegex.test(htmlText)) {
                relatorio.avisos.push({ id: 'dados-reais', titulo: 'Possível dado real encontrado', mensagem: 'Confirme se CPFs ou E-mails presentes são apenas exemplos fictícios.', arquivo: 'index.html', severidade: 'aviso' });
            }
        }

        // 4. Validações CSS
        if (css.trim() !== '') {
            if (css.includes('width: 100vw')) {
                relatorio.avisos.push({ id: 'css-100vw', titulo: 'Possível scroll horizontal', mensagem: 'Foi encontrado width: 100vw. Considere usar 100%.', arquivo: 'style.css', severidade: 'aviso' });
            }
            if (css.includes('overflow-x: scroll') || css.includes('overflow-x: auto')) {
                relatorio.avisos.push({ id: 'css-overflow', titulo: 'Scroll horizontal explícito', mensagem: 'Verifique se overflow-x é realmente necessário.', arquivo: 'style.css', severidade: 'aviso' });
            }
            const importantCount = (css.match(/!important/g) || []).length;
            if (importantCount > 3) {
                relatorio.avisos.push({ id: 'css-important', titulo: 'Muitos !important', mensagem: 'O uso excessivo de !important pode dificultar a manutenção.', arquivo: 'style.css', severidade: 'aviso' });
            }
        }

        // 5. Validações JS
        if (js.trim() !== '') {
            try {
                new Function(js);
            } catch (e) {
                relatorio.bloqueios.push({ id: 'js-sintaxe', titulo: 'Erro de sintaxe JavaScript', mensagem: e.message, arquivo: 'script.js', severidade: 'bloqueio' });
            }

            // Checar IDs que não existem
            const regexGetId = /getElementById\(['"]([^'"]+)['"]\)/g;
            const regexQueryId = /querySelector\(['"]#([^'"]+)['"]\)/g;
            const htmlIds = html ? Array.from(new DOMParser().parseFromString(html, 'text/html').querySelectorAll('[id]')).map(el => el.id) : [];
            
            let match;
            while ((match = regexGetId.exec(js)) !== null) {
                if (!htmlIds.includes(match[1])) {
                    relatorio.avisos.push({ id: 'js-id-ausente', titulo: 'ID referenciado no JS não existe', mensagem: `O ID '${match[1]}' foi usado no script mas não existe no HTML.`, arquivo: 'script.js', severidade: 'aviso' });
                }
            }
            while ((match = regexQueryId.exec(js)) !== null) {
                if (!htmlIds.includes(match[1])) {
                    relatorio.avisos.push({ id: 'js-id-ausente', titulo: 'ID referenciado no JS não existe', mensagem: `O ID '${match[1]}' foi usado no script mas não existe no HTML.`, arquivo: 'script.js', severidade: 'aviso' });
                }
            }

            // Checar Classes que não existem
            const regexQueryClass = /querySelector(?:All)?\(['"]\.([^'"]+)['"]\)/g;
            const htmlClassesRaw = html ? Array.from(new DOMParser().parseFromString(html, 'text/html').querySelectorAll('[class]')).map(el => el.className) : [];
            const htmlClasses = [];
            htmlClassesRaw.forEach(c => c.split(/\s+/).forEach(cls => htmlClasses.push(cls)));

            while ((match = regexQueryClass.exec(js)) !== null) {
                if (!htmlClasses.includes(match[1])) {
                    relatorio.avisos.push({ id: 'js-class-ausente', titulo: 'Classe referenciada no JS não existe', mensagem: `A classe '${match[1]}' foi usada no script mas não existe no HTML.`, arquivo: 'script.js', severidade: 'aviso' });
                }
            }
        }

        // 6. Dependência de Backend
        if (rascunho.areaProjeto && rascunho.areaProjeto.dependeBackend) {
            relatorio.infos.push({ 
                id: 'depende-backend', 
                titulo: 'Dependência de Backend', 
                mensagem: 'A área relacionada depende da API. A validação atual é estática e não executa endpoints, banco, build ou testes de backend.', 
                arquivo: 'Contexto', 
                severidade: 'info' 
            });
        }

        return relatorio;
    }

    function temBloqueios(relatorio) {
        return relatorio.bloqueios && relatorio.bloqueios.length > 0;
    }

    function achatarRelatorio(relatorio) {
        return [
            ...(relatorio.bloqueios || []),
            ...(relatorio.avisos || []),
            ...(relatorio.infos || [])
        ];
    }

    window.IdeValidacoes = {
        gerarRelatorioValidacao,
        temBloqueios,
        achatarRelatorio
    };
})();
