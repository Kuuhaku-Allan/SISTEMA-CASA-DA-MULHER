# Checklist - criar ACESSO-EQUIPE

1. Criar repositório privado:

   ```text
   Sistema-Casa-da-Mulher/ACESSO-EQUIPE
   ```

2. Adicionar apenas pessoas autorizadas da equipe.

3. Copiar o conteúdo de:

   ```text
   docs/templates/README-ACESSO-EQUIPE.md
   ```

   para o `README.md` do repositório privado.

4. Criar uma issue no repositório privado com o conteúdo de:

   ```text
   docs/templates/ISSUE-COMECE-AQUI-EQUIPE.md
   ```

5. Fixar essa issue no repositório privado.

6. No ambiente local do mantenedor, abrir a Área da Equipe:

   ```powershell
   .\casa_da_mulher.cmd equipe
   ```

7. Gerar os convites iniciais EQP:

   ```powershell
   .\casa_da_mulher.cmd equipe bootstrap
   ```

8. Entregar individualmente para cada pessoa:

   - ID EQP;
   - código de ativação;
   - link do repositório privado `ACESSO-EQUIPE`;
   - orientação para abrir a Área da Equipe.

9. Nunca publicar códigos EQP no repositório principal.

10. Confirmar que colaboradoras trabalham primeiro em:

    ```text
    prototipos/
    ```

11. Confirmar que PR de fork fora de `prototipos/` está bloqueado pelo workflow `validar-prototipos.yml`.

12. Confirmar que o README e a issue fixada não mandam abrir arquivos manualmente; devem dizer para abrir a Área da Equipe e clicar em `Ativar meu EQP`.
