# Guia Visual: Soft UI (Padrão Recepção)

Este guia documenta o padrão visual apelidado de **Soft UI** ou **Pastel UI**, introduzido inicialmente na tela da **Recepção** do Sistema Casa da Mulher.

A ideia central desta estética é substituir o design "administrativo seco" (fundo cinza, tabelas densas e bordas duras) por uma experiência mais acolhedora, limpa e responsiva, mantendo a integridade da lógica de negócios e as validações em Javascript.

---

## 1. Paleta de Cores e Estilo Geral

A identidade visual se baseia em uma paleta de tons pastel (rosa, lilás e branco), bordas altamente arredondadas (`border-radius: 1.5rem` a `2rem`) e sombreamentos (shadows) leves, para uma sensação de "glassmorphism" ou "soft ui".

### Fundo da Página
Sempre aplique a classe `.soft-page` na tag `<body>` para garantir o fundo em degradê linear (rosa muito claro).
```css
body.soft-page {
    background: linear-gradient(145deg, #FDF4F8 0%, #F9E9F2 100%);
    min-height: 100vh;
}
```

---

## 2. Estrutura de Componentes e Classes Reutilizáveis

Ao converter ou criar novas telas, utilize as seguintes classes injetadas no arquivo `style.css`:

### Cabeçalho (Header)
Substitui o `.admin-header`.
- **`<header class="soft-header">`**: Container flexível arredondado com efeito de desfoque.
- **`.soft-header-title`**: Texto de título (Ex: "Casa da Mulher") com gradiente embutido `background-clip`.
- **`.soft-header-subtitle`**: Subtítulo sutil logo abaixo do título.
- **`.soft-clock`**: Pílula de relógio e data para o canto direito.

### Abas de Navegação (Tabs)
Substitui o `.recepcao-tabs` e `.btn-secondary`.
- **`<div class="soft-tabs">`**: Container das abas.
- **`<button class="soft-tab">`**: Botão em formato de pílula. Adicione a classe `.active` quando a aba estiver selecionada.

### Painéis de Conteúdo e Formulários
Substitui as estruturas `.admin-panel` antigas.
- **`<div class="soft-card">`**: A caixa de conteúdo principal (arredondada, branca translúcida e com sombra suave).
- **`.soft-card-title`**: Título interno do card (ex: "Novo acolhimento").
- **`.soft-form-row`**: Grid para colocar 2 inputs lado a lado (`grid-template-columns: 1fr 1fr`).
- **`.soft-form-group`**: Envolve a `<label>` e o `<input>`. As labels já possuem formatação padrão `uppercase` colorida.
- **`.soft-input`**: A classe obrigatória para todos os `<input>`, `<select>` e `<textarea>`.

### Botões
- **`.soft-btn`**: Classe base para botões arredondados.
- **`.soft-btn-primary`**: Botão de ação principal (Salvar, Enviar) com gradiente e hover de "flutuação" (`translateY`).
- **`.soft-btn-warning`**: Botão de alerta/limpeza secundário (Rosa claro, texto escuro).
- **`.soft-btn-danger`**: Botão de exclusão (Vermelho claro).

---

## 3. Listagem de Dados (Entity Grid x Table)

O padrão **Soft UI descarta o uso de tabelas `<table class="table-wrap">` para listagem de dados densos**.

O texto não deve mais ser esmagado horizontalmente. Em vez disso, cada registro (ex: uma Mulher atendida) se torna um **Card** (`.entity-card`).

**Estrutura de Container:**
```html
<div id="minhaLista" class="entity-grid"></div>
```
A classe `.entity-grid` configura automaticamente o grid para adaptar de 1 a múltiplos cards dependendo do tamanho da tela.

**Estrutura do HTML renderizado pelo JS (`displayPacientes`, `displayCursos`, etc.):**
```html
<div class="entity-card">
    <div class="entity-title">Ana Maria da Silva</div>
    <div class="entity-info">CPF: ***.400.800-**</div>
    <div class="entity-info">Telefone: (11) 99999-9999</div>
    <!-- ... -->
    <div class="entity-actions">
        <button class="soft-btn soft-btn-warning">Editar</button>
        <button class="soft-btn soft-btn-danger">Remover</button>
    </div>
</div>
```

---

## 4. Evite Quebras Acidentais (Scope)

Se uma tela legada precisar ser mantida com a tabela padrão do painel (`admin-layout`), o CSS deve garantir que as alterações feitas pela "Soft UI" não afetem as outras páginas.

**Nota técnica importante:** A regra antiga de `white-space: nowrap;` nas tabelas foi isolada para agir estritamente sob `<div class="table-wrap">`. O `style.css` foi desenhado de forma que o uso do Soft UI seja ativado por classes (`.soft-page`, `.soft-card`, `.entity-grid`). Não use nomes de classe globais nas telas novas se houver risco de conflito com o `style.css` original.

---

## 5. Como Replicar no Futuro

Quando for modernizar a tela de **Cursos**, **Coordenação**, **Funcionários** ou **EQP**:
1. Troque a classe do `<main>` de `.admin-shell` para não utilizar nada atrelado à tabela (embora ela possa englobar), ou adicione `class="soft-page"` no `<body>`.
2. Troque o `<header class="admin-header">` pelo `<header class="soft-header">`.
3. Elimine o arquivo CSS inline da página antiga, se houver, garantindo que tudo possa ser puxado das classes `.soft-` em `style.css`.
4. Refatore a função JS que injeta linhas no `<tbody>` para em vez disso, injetar `.entity-card`s.
5. Remova as tags `<table>`, `<thead>`, e `<tbody>`.
