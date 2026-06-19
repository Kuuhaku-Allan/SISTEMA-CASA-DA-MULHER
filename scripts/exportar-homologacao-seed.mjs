#!/usr/bin/env node

import { existsSync, mkdirSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { DatabaseSync } from "node:sqlite";

const databaseIndex = process.argv.indexOf("--database");
const outputIndex = process.argv.indexOf("--output");
const databasePath = resolve(databaseIndex >= 0 ? process.argv[databaseIndex + 1] : "CasaMulher.Api/casamulher.db");
const outputPath = resolve(outputIndex >= 0 ? process.argv[outputIndex + 1] : ".runtime/homologacao-seed.json");

if (!existsSync(databasePath)) throw new Error(`Banco não encontrado: ${databasePath}`);
const db = new DatabaseSync(databasePath, { readOnly: true });

try {
  const users = db.prepare(`
    SELECT IdentificadorFuncionario, Perfil
    FROM AspNetUsers
    WHERE Perfil <> 'equipe' AND IdentificadorFuncionario NOT LIKE 'EQP-%'
    ORDER BY IdentificadorFuncionario
  `).all();
  const invites = db.prepare(`
    SELECT IdentificadorFuncionario, Perfil
    FROM FuncionariosConvites
    WHERE IdentificadorFuncionario <> ''
    ORDER BY IdentificadorFuncionario
  `).all();
  const audits = db.prepare(`
    SELECT Acao, Entidade
    FROM AuditoriaEventos
    WHERE COALESCE(Escopo, 'Institucional') = 'Institucional'
    ORDER BY CriadoEm DESC LIMIT 20
  `).all();
  const emails = db.prepare(`
    SELECT Tipo, Status
    FROM EmailEventos
    WHERE Tipo NOT LIKE 'Equipe%' AND Destinatario NOT LIKE '%@equipe.local'
    ORDER BY CriadoEm DESC LIMIT 20
  `).all();
  const document = {
    version: 1,
    funcionarios: users.map((item, index) => ({
      identificador: item.IdentificadorFuncionario,
      nome: `Funcionária Fictícia ${String(index + 1).padStart(2, "0")}`,
      email: `${item.IdentificadorFuncionario.toLowerCase()}@example.invalid`,
      perfil: item.Perfil,
      ativo: false
    })),
    convites: invites.map((item, index) => ({
      identificador: item.IdentificadorFuncionario,
      nome: `Convite Fictício ${String(index + 1).padStart(2, "0")}`,
      email: `convite-${String(index + 1).padStart(2, "0")}@example.invalid`,
      perfil: item.Perfil
    })),
    auditoria: audits.map((item, index) => ({
      acao: item.Acao,
      entidade: item.Entidade,
      descricao: `Evento fictício de homologação ${index + 1}.`
    })),
    emails: emails.map((item, index) => ({
      destinatario: `destinataria-${index + 1}@example.invalid`,
      assunto: `Mensagem fictícia de homologação ${index + 1}`,
      tipo: item.Tipo,
      status: item.Status
    })),
    recepcao: []
  };

  mkdirSync(dirname(outputPath), { recursive: true });
  writeFileSync(outputPath, `${JSON.stringify(document, null, 2)}\n`, "utf8");
  console.log(`[OK] Seed sanitizado exportado para ${outputPath}`);
  console.log("[OK] Senhas, hashes, SecurityStamp, tokens, passkeys, 2FA, e-mails e nomes reais não foram exportados.");
} finally {
  db.close();
}
