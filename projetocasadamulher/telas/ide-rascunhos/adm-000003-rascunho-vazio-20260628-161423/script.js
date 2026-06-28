// Teste 8: O Javascript tenta buscar IDs e classes que NÃO existem no HTML
const btn = document.getElementById("id-que-nao-existe");

const caixas = document.querySelectorAll(".classe-inexistente");

if (btn) {
    btn.addEventListener("click", () => {
        console.log("Clicou");
    });
}