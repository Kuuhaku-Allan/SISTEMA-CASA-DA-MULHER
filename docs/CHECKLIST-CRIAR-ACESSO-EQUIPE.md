# Checklist - criar ACESSO-EQUIPE

1. Criar repositorio privado:

   ```text
   Sistema-Casa-da-Mulher/ACESSO-EQUIPE
   ```

2. Adicionar apenas pessoas autorizadas da equipe.

3. Criar estrutura privada:

   ```text
   data/equipe-db.json
   data/equipe-events.ndjson
   data/equipe-db.example.json
   data/README.md
   ```

4. Copiar o conteudo de:

   ```text
   docs/templates/README-ACESSO-EQUIPE.md
   ```

   para o `README.md` do repositorio privado.

5. Criar uma issue no repositorio privado com o conteudo de:

   ```text
   docs/templates/ISSUE-COMECE-AQUI-EQUIPE.md
   ```

6. Fixar essa issue no repositorio privado.

7. Configurar o portal Render conforme:

   ```text
   docs/HOMOLOGACAO-PORTAL-EQP-RENDER.md
   ```

8. Colocar o link do portal Render no README do `ACESSO-EQUIPE`.

9. Confirmar bootstrap inicial:

   - `EQP-000001` + `ADM-000003` reservado para `Kuuhaku-Allan`;
   - `EQP-000002` + `ADM-000004` disponivel;
   - `EQP-000003` + `ADM-000005` disponivel;
   - `EQP-000004` + `ADM-000006` disponivel;
   - `EQP-000005` + `ADM-000007` disponivel.

10. Orientar cada pessoa:

    - entrar com GitHub no portal;
    - ativar seu EQP;
    - usar senha propria deste projeto;
    - sincronizar local/Codespaces depois da ativacao.

11. Confirmar que colaboradoras trabalham primeiro em:

    ```text
    prototipos/
    ```

12. Confirmar que PR de fork fora de `prototipos/` esta bloqueado pelo workflow `validar-prototipos.yml`.
