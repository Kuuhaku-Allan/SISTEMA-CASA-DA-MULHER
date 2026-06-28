# IDE da Equipe - Casa da Mulher

Bem-vindo(a) à **IDE da Equipe**! 🚀
Este ambiente foi criado para que você possa prototipar telas, criar layouts e sugerir melhorias visuais no sistema Casa da Mulher sem precisar configurar um ambiente de desenvolvimento complexo na sua máquina.

---

## 1. O que é a IDE da Equipe?
A IDE da Equipe é um **editor de código colaborativo embarcado no navegador**. Ela permite que você escreva HTML, CSS e JavaScript e veja o resultado instantaneamente (Preview). E o melhor: você pode enviar suas alterações diretamente para revisão da equipe com segurança.

Atualmente, a IDE foca no frontend (telas, layouts e protótipos visuais).

## 2. O que ela já faz hoje?
- Criação e edição de arquivos `index.html`, `style.css` e `script.js` direto no navegador.
- **Tarefas Guiadas:** a IDE orienta o tipo de protótipo que você deve criar e aplica checklists adequados para a revisão.
- Auto-save contínuo dos seus rascunhos no seu navegador.
- Preview imediato da sua tela de forma isolada.
- Integração segura com o repositório principal, gerando Pull Requests automáticos.

---

## 3. Como abrir a IDE
A IDE pode ser acessada a partir da **Área da Equipe** do sistema Casa da Mulher. Basta estar logado e acessar o menu "IDE". 

## 4. Como usar Modelos Iniciais
Você não precisa começar do zero. Se você selecionar a tarefa de criar uma tela "Soft UI", a IDE sugerirá automaticamente um modelo base com todas as cores, sombras e a estética correta.
Você também pode escolher o modelo clicando na seção "Modelos Iniciais" no explorador lateral.

## 5. Como usar Tarefas Guiadas (Fase 2)
A IDE agora possui uma área chamada **Tarefas Guiadas**. Ao iniciar um trabalho, escolha uma tarefa, por exemplo, `Criar formulário simples` ou `Ajustar visual de tela`.
- Isso define os **limites da sua edição** e ajuda os revisores a entenderem o objetivo do seu rascunho.
- O seu checklist de envio muda de acordo com a tarefa escolhida!

## 6. Como testar o Preview
Toda alteração que você faz no código é automaticamente sincronizada com a janela de **Preview** ao lado direito da IDE. Você também pode forçar a atualização clicando no botão "Forçar Atualização".
A IDE funciona sem servidor backend, ou seja, tudo que você testar deve ser isolado (frontend puro).

---

## 7. Como preparar e enviar sua Revisão
Quando sua tela estiver pronta, basta clicar no botão **"Preparar revisão"**.
Um resumo da sua tarefa será gerado, e você terá duas opções de envio:

### Modo seguro da equipe (Padrão)
O rascunho será enviado para a nuvem através de um bot da Casa da Mulher. Isso garante que a sua modificação vá direto para o repositório oficial da equipe, em uma pasta bloqueada (`ide-rascunhos/`) sem que você precise usar conta do GitHub.

### Fork Pessoal
Ideal para contribuidoras técnicas. Se você conectar sua conta do GitHub na IDE, ela criará um Fork (uma cópia do repositório) no seu perfil e abrirá o Pull Request assinando com a sua autoria.

## 8. Como conectar o GitHub
Clique no avatar/menu no canto superior direito da IDE e escolha "Conectar GitHub", ou clique no botão de conectar no próprio modal de Revisão. A IDE gerenciará seu login de forma segura e não trafegará senhas no frontend.

## 9. Onde os arquivos ficam salvos?
Seus protótipos são limitados, por segurança, à seguinte pasta do repositório principal:
`projetocasadamulher/telas/ide-rascunhos/...`
Isso garante que nenhum rascunho de tela afete o sistema real em produção antes de passar por revisão detalhada.

---

## 10. Limites de Segurança
- A IDE **nunca** permite edição de arquivos oficiais do sistema, apenas arquivos da pasta `ide-rascunhos`.
- Todo o código passa por processos rígidos de **sanitização** antes de chegar no GitHub (evitando injeções de caracteres ocultos ou quebras do sistema).
- Tokens de acesso do GitHub nunca são expostos no navegador.

---

## 11. O Mapa do Projeto (Fase 3)
A IDE agora possui uma aba chamada **Mapa do Projeto**. Esta aba mostra a você o contexto e as áreas reais do sistema Casa da Mulher. O mapa te ajuda a entender *onde* você está mexendo e *qual* o impacto disso.

### Como usar o Mapa do Projeto?
1. Clique no segundo ícone na barra lateral esquerda (Activity Bar) para abrir o **Mapa do Projeto**.
2. Clique em **Ver contexto** em qualquer área para abrir o painel lateral com detalhes completos (quais os arquivos principais, o perfil que usa aquela área, cuidados e restrições).
3. Dentro do painel de contexto, você pode clicar em **Associar ao rascunho atual**. Isso vinculará o seu rascunho àquela área.

> **Importante:** Associar uma área ao rascunho *não* te dá permissão para alterar os arquivos reais daquela área em produção. Isso serve como um "contexto seguro" para orientar os desenvolvedores que revisarão o seu rascunho, sabendo de onde a sua ideia veio e para onde ela vai.

## 12. O que a IDE ainda não faz (Próximas fases)
- Ela ainda não possui um **Mapa do Sistema** que mostre o projeto inteiro (Fase 3).
- Ela não lida com edição do backend em C# ou execução da API local.
- Ela ainda não força vinculação obrigatória com *Issues* do GitHub ou ferramentas de Inteligência Artificial.

> Estamos construindo a ponte para um desenvolvimento Full-Stack gradualmente. Aproveite a criação de protótipos de alta qualidade!
