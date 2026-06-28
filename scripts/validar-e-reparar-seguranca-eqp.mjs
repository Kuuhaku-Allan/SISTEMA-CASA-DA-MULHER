#!/usr/bin/env node

import { existsSync } from "node:fs";
import { resolve } from "node:path";
import { DatabaseSync } from "node:sqlite";

const apply = process.argv.includes("--apply");
const databaseArgIndex = process.argv.indexOf("--database");
const databasePath = resolve(
  databaseArgIndex >= 0 && process.argv[databaseArgIndex + 1]
    ? process.argv[databaseArgIndex + 1]
    : process.env.SQLITE_DB_PATH || "CasaMulher.Api/casamulher.db"
);

function fail(message) {
  throw new Error(message);
}

function isPlaceholder(value) {
  return String(value || "").trim().toLowerCase().endsWith("@equipe.local");
}

function isRealEmail(value) {
  const normalized = String(value || "").trim();
  return normalized.includes("@") && !isPlaceholder(normalized);
}

function boolEnv(name) {
  return /^(1|true|sim|yes)$/i.test(String(process.env[name] || "").trim());
}

function securitySnapshot(db, userId) {
  return db.prepare(`
    SELECT PasswordHash, SecurityStamp, PhoneNumber, PhoneNumberConfirmed,
           TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
           (SELECT COUNT(*) FROM PasskeyCredentials p WHERE p.UserId = u.Id) AS Passkeys,
           (SELECT COUNT(*) FROM AspNetUserTokens t WHERE t.UserId = u.Id) AS SecurityTokens
    FROM AspNetUsers u
    WHERE Id = ?
  `).get(userId);
}

function auditRows(db) {
  return db.prepare(`
    SELECT u.Id AS UserId, u.Email, u.NormalizedEmail, u.EmailConfirmed,
           u.EmailRecuperacao, u.EmailRecuperacaoConfirmado,
           u.TwoFactorEnabled, u.DoisFatoresObrigatorio,
           (SELECT COUNT(*) FROM PasskeyCredentials p WHERE p.UserId = u.Id) AS Passkeys,
           (SELECT COUNT(*) FROM AspNetUserTokens t WHERE t.UserId = u.Id) AS SecurityTokens,
           GROUP_CONCAT(li.Identificador, ',') AS Aliases
    FROM AspNetUsers u
    JOIN UserLoginIdentifiers li ON li.UserId = u.Id AND li.Ativo = 1
    GROUP BY u.Id
    ORDER BY u.Id
  `).all();
}

function printAudit(rows) {
  if (rows.length === 0) {
    console.log("[OK] Nenhuma conta com alias EQP/ADM foi encontrada.");
    return;
  }

  console.log(`[INFO] ${rows.length} conta(s) com aliases EQP/ADM em ${databasePath}`);

  for (const row of rows) {
    const aliases = String(row.Aliases || "").split(",").filter(Boolean);
    const hasEqp = aliases.some((item) => item.toUpperCase().startsWith("EQP-"));
    const hasAdm = aliases.some((item) => item.toUpperCase().startsWith("ADM-"));
    const risks = [];

    if (isPlaceholder(row.Email)) risks.push("Email é placeholder");
    if (!isRealEmail(row.EmailRecuperacao)) risks.push("e-mail de recuperação pendente/placeholder");
    if (!hasEqp || !hasAdm) risks.push("par EQP/ADM incompleto ou divergente");

    console.log(`\nUserId: ${row.UserId}`);
    console.log(`Aliases: ${aliases.join(", ") || "(nenhum)"}`);
    console.log(`Email: ${row.Email || "(vazio)"}`);
    console.log(`EmailRecuperacao: ${row.EmailRecuperacao || "(vazio)"} (confirmado=${Boolean(row.EmailRecuperacaoConfirmado)})`);
    console.log(`2FA: ${Boolean(row.TwoFactorEnabled)} | obrigatório: ${Boolean(row.DoisFatoresObrigatorio)} | passkeys: ${row.Passkeys} | tokens de segurança: ${row.SecurityTokens}`);
    console.log(`Risco: ${risks.length ? risks.join("; ") : "nenhum indício atual"}`);
  }
}

function applyRepair(db) {
  if (String(process.env.CASA_MULHER_REPAIR_CONFIRM || "").trim().toUpperCase() !== "APLICAR") {
    fail("Modo apply exige CASA_MULHER_REPAIR_CONFIRM=APLICAR.");
  }

  const eqpId = String(process.env.CASA_MULHER_REPAIR_EQP_ID || "").trim().toUpperCase();
  const admId = String(process.env.CASA_MULHER_REPAIR_ADM_ID || "").trim().toUpperCase();
  const email = String(process.env.CASA_MULHER_REPAIR_EMAIL || "").trim();

  if (!eqpId.startsWith("EQP-") || !admId.startsWith("ADM-")) {
    fail("Informe CASA_MULHER_REPAIR_EQP_ID e CASA_MULHER_REPAIR_ADM_ID válidos.");
  }

  if (!isRealEmail(email)) {
    fail("CASA_MULHER_REPAIR_EMAIL deve ser um e-mail real, nunca @equipe.local.");
  }

  const aliases = db.prepare(`
    SELECT Identificador, UserId
    FROM UserLoginIdentifiers
    WHERE Ativo = 1 AND UPPER(Identificador) IN (?, ?)
    ORDER BY Identificador
  `).all(eqpId, admId);

  if (aliases.length !== 2 || !aliases.some((item) => item.Identificador.toUpperCase() === eqpId)
      || !aliases.some((item) => item.Identificador.toUpperCase() === admId)) {
    fail(`Os aliases ${eqpId}/${admId} não foram encontrados juntos; nenhum dado foi alterado.`);
  }

  const userIds = [...new Set(aliases.map((item) => item.UserId))];
  if (userIds.length !== 1) {
    fail(`Os aliases ${eqpId}/${admId} apontam para UserIds diferentes; reparo automático recusado.`);
  }

  const userId = userIds[0];
  const beforeSecurity = securitySnapshot(db, userId);
  const current = db.prepare(`
    SELECT EmailConfirmed, EmailRecuperacaoConfirmado, EmailRecuperacaoConfirmadoEm
    FROM AspNetUsers WHERE Id = ?
  `).get(userId);
  const emailConfirmed = Boolean(current.EmailConfirmed) || boolEnv("CASA_MULHER_REPAIR_EMAIL_CONFIRMED");
  const recoveryConfirmed = Boolean(current.EmailRecuperacaoConfirmado)
    || boolEnv("CASA_MULHER_REPAIR_RECOVERY_EMAIL_CONFIRMED")
    || boolEnv("CASA_MULHER_REPAIR_EMAIL_CONFIRMED");
  const confirmedAt = recoveryConfirmed
    ? current.EmailRecuperacaoConfirmadoEm || new Date().toISOString()
    : null;

  db.exec("BEGIN IMMEDIATE");
  try {
    db.prepare(`
      UPDATE AspNetUsers
      SET Email = ?, NormalizedEmail = ?, EmailConfirmed = ?,
          EmailRecuperacao = ?, EmailRecuperacaoConfirmado = ?, EmailRecuperacaoConfirmadoEm = ?
      WHERE Id = ?
    `).run(email, email.toUpperCase(), Number(emailConfirmed), email, Number(recoveryConfirmed), confirmedAt, userId);

    const afterSecurity = securitySnapshot(db, userId);
    if (JSON.stringify(beforeSecurity) !== JSON.stringify(afterSecurity)) {
      fail("A verificação detectou alteração inesperada de senha/2FA/passkeys; transação revertida.");
    }

    db.exec("COMMIT");
  } catch (error) {
    db.exec("ROLLBACK");
    throw error;
  }

  console.log(`[OK] E-mail reparado para ${eqpId}/${admId} no UserId ${userId}.`);
  console.log("[OK] PasswordHash, SecurityStamp, telefone, bloqueio, 2FA, passkeys e tokens de segurança foram preservados.");
}

if (!existsSync(databasePath)) {
  fail(`Banco SQLite não encontrado: ${databasePath}`);
}

const db = new DatabaseSync(databasePath);
try {
  db.exec("PRAGMA foreign_keys = ON");
  const rows = auditRows(db);
  printAudit(rows);
  if (apply) applyRepair(db);
} finally {
  db.close();
}
