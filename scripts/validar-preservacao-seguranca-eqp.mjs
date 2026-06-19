#!/usr/bin/env node

import { pbkdf2Sync, randomBytes, randomUUID } from "node:crypto";
import { existsSync, mkdirSync, rmSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { spawn, spawnSync } from "node:child_process";
import { DatabaseSync } from "node:sqlite";
import { createServer } from "node:net";
import { fileURLToPath } from "node:url";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const apiProject = join(repoRoot, "CasaMulher.Api", "CasaMulher.Api.csproj");
const apiDll = join(repoRoot, "CasaMulher.Api", "bin", "Release", "net8.0", "CasaMulher.Api.dll");
const runtimeDir = join(repoRoot, ".runtime");
const databasePath = join(runtimeDir, `validacao-preservacao-eqp-${process.pid}.db`);
let baseUrl;
const initialPassword = "Senha!Segura2026";
const newerPassword = "Senha!Nova2026";
let apiProcess;
let apiLogs = "";

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function runDotnet(args, description) {
  const result = spawnSync("dotnet", args, { cwd: repoRoot, encoding: "utf8", windowsHide: true });
  if (result.status !== 0) {
    throw new Error(`${description} falhou.\n${result.stdout || ""}\n${result.stderr || ""}`.trim());
  }
}

function identityHash(password) {
  const salt = randomBytes(16);
  const subkey = pbkdf2Sync(password, salt, 100_000, 32, "sha512");
  const header = Buffer.alloc(13);
  header[0] = 0x01;
  header.writeUInt32BE(2, 1);
  header.writeUInt32BE(100_000, 5);
  header.writeUInt32BE(salt.length, 9);
  return Buffer.concat([header, salt, subkey]).toString("base64");
}

function cleanup() {
  for (const suffix of ["", "-shm", "-wal"]) {
    try {
      rmSync(`${databasePath}${suffix}`, { force: true, maxRetries: 5, retryDelay: 200 });
    } catch (error) {
      if (error.code !== "EPERM" && error.code !== "EBUSY") throw error;
    }
  }
}

async function getFreeBaseUrl() {
  return new Promise((resolvePromise, reject) => {
    const server = createServer();
    server.once("error", reject);
    server.listen(0, "127.0.0.1", () => {
      const address = server.address();
      server.close((error) => {
        if (error) reject(error);
        else resolvePromise(`http://127.0.0.1:${address.port}`);
      });
    });
  });
}

function seedExistingUser(userId, passwordHash, now) {
  const db = new DatabaseSync(databasePath);
  try {
    const user = {
      Id: userId,
      NomeCompleto: "Owner Preservada",
      Perfil: "adm",
      Ativo: 1,
      CriadoEm: now,
      UserName: "ADM-000003",
      NormalizedUserName: "ADM-000003",
      Email: "owner.real@example.com",
      NormalizedEmail: "OWNER.REAL@EXAMPLE.COM",
      EmailConfirmed: 1,
      PasswordHash: passwordHash,
      SecurityStamp: "security-stamp-preservado",
      ConcurrencyStamp: "concurrency-preservado",
      PhoneNumber: "+5511999999999",
      PhoneNumberConfirmed: 1,
      TwoFactorEnabled: 1,
      LockoutEnd: null,
      LockoutEnabled: 1,
      AccessFailedCount: 0,
      DeveTrocarSenha: 0,
      DoisFatoresObrigatorio: 1,
      IdentificadorFuncionario: "ADM-000003",
      PasskeyReconfirmadoEm: now,
      EmailRecuperacao: "owner.recovery@example.com",
      EmailRecuperacaoConfirmado: 1,
      EmailRecuperacaoConfirmadoEm: now,
      EquipeDbPasswordUpdatedAt: now,
      EquipeDbPasswordVersion: 1
    };
    const columns = Object.keys(user);
    db.prepare(`INSERT INTO AspNetUsers (${columns.map((item) => `"${item}"`).join(",")}) VALUES (${columns.map(() => "?").join(",")})`)
      .run(...columns.map((item) => user[item]));
    db.prepare(`INSERT INTO AspNetUserTokens (UserId, LoginProvider, Name, Value) VALUES (?, ?, ?, ?)`)
      .run(userId, "[AspNetUserStore]", "AuthenticatorKey", "JBSWY3DPEHPK3PXP");
    db.prepare(`
      INSERT INTO PasskeyCredentials
        (UserId, CredentialId, PublicKey, SignatureCounter, NomeDispositivo, Transports, CriadoEm, UltimoUsoEm)
      VALUES (?, ?, ?, 7, ?, ?, ?, NULL)
    `).run(userId, randomBytes(32), randomBytes(64), "Passkey preservada", "internal", now);
  } finally {
    db.close();
  }
}

function readUserByAlias(alias) {
  const db = new DatabaseSync(databasePath, { readOnly: true });
  try {
    return db.prepare(`
      SELECT u.*,
             (SELECT COUNT(*) FROM PasskeyCredentials p WHERE p.UserId = u.Id) AS Passkeys,
             (SELECT COUNT(*) FROM AspNetUserTokens t WHERE t.UserId = u.Id) AS SecurityTokens
      FROM AspNetUsers u
      JOIN UserLoginIdentifiers li ON li.UserId = u.Id AND li.Ativo = 1
      WHERE UPPER(li.Identificador) = UPPER(?)
    `).get(alias);
  } finally {
    db.close();
  }
}

async function request(path, options = {}, expectedStatus = 200) {
  const response = await fetch(`${baseUrl}${path}`, {
    method: options.method || "GET",
    headers: options.body ? { "Content-Type": "application/json" } : {},
    body: options.body ? JSON.stringify(options.body) : undefined,
    signal: AbortSignal.timeout(10_000)
  });
  const raw = await response.text();
  let data = null;
  if (raw) {
    try { data = JSON.parse(raw); } catch { data = raw; }
  }
  assert(response.status === expectedStatus, `${path}: esperado ${expectedStatus}, recebido ${response.status}: ${raw}`);
  return data;
}

async function waitForApi() {
  for (let attempt = 0; attempt < 240; attempt += 1) {
    try {
      await request("/api/portal-eqp/status");
      return;
    } catch {
      await new Promise((resolvePromise) => setTimeout(resolvePromise, 250));
    }
  }
  throw new Error(`API não iniciou.\n${apiLogs}`);
}

function member({ eqpId, admId, name, passwordHash, securityStamp, version, updatedAt, email, recoveryEmail, confirmed }) {
  return {
    eqpId,
    admId,
    nome: name,
    email,
    emailRecuperacao: recoveryEmail,
    emailRecuperacaoConfirmado: Boolean(confirmed),
    githubId: eqpId,
    githubUsername: eqpId.toLowerCase(),
    papelEquipe: "contributor",
    fluxoTrabalho: "fork_codespaces",
    status: "ativo",
    passwordHash,
    securityStamp,
    senhaAtualizadaEm: updatedAt,
    passwordVersion: version,
    ativadoEm: updatedAt,
    atualizadoEm: updatedAt
  };
}

async function main() {
  mkdirSync(runtimeDir, { recursive: true });
  cleanup();
  baseUrl = await getFreeBaseUrl();
  runDotnet(["build", apiProject, "--configuration", "Release", "--no-restore"], "Build Release");
  runDotnet([
    "ef", "database", "update", "--configuration", "Release", "--no-build",
    "--project", apiProject, "--startup-project", apiProject,
    "--connection", `Data Source=${databasePath}`
  ], "Migration do banco limpo");
  assert(existsSync(apiDll), "DLL Release não encontrada.");

  const now = new Date().toISOString();
  const existingUserId = randomUUID();
  const initialHash = identityHash(initialPassword);
  const newerHash = identityHash(newerPassword);
  seedExistingUser(existingUserId, initialHash, now);

  apiProcess = spawn("dotnet", [apiDll], {
    cwd: join(repoRoot, "CasaMulher.Api"),
    windowsHide: true,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: "Development",
      ASPNETCORE_URLS: baseUrl,
      Database__Provider: "Sqlite",
      ConnectionStrings__DefaultConnection: `Data Source=${databasePath}`,
      Seed__RunDemoData: "false",
      EquipeSync__Automatico: "false",
      Jwt__Key: "validacao-preservacao-seguranca-eqp-2026",
      Jwt__Issuer: "CasaMulher.Api",
      Jwt__Audience: "CasaMulher.Api",
      Convites__HashSecret: "validacao-preservacao-convites-2026",
      Email__Provider: "Fake"
    },
    stdio: ["ignore", "pipe", "pipe"]
  });
  apiProcess.stdout.on("data", (chunk) => { apiLogs += chunk.toString(); });
  apiProcess.stderr.on("data", (chunk) => { apiLogs += chunk.toString(); });
  await waitForApi();

  const existing = member({
    eqpId: "EQP-000001", admId: "ADM-000003", name: "Owner Preservada",
    passwordHash: initialHash, securityStamp: "stamp-remoto-antigo", version: 1, updatedAt: now,
    email: "eqp-000001@equipe.local", recoveryEmail: "eqp-000001@equipe.local", confirmed: false
  });
  const newReal = member({
    eqpId: "EQP-000002", admId: "ADM-000004", name: "Membro Com Email",
    passwordHash: identityHash(initialPassword), securityStamp: "stamp-novo-real", version: 1, updatedAt: now,
    email: "membro.real@example.com", recoveryEmail: "membro.recovery@example.com", confirmed: true
  });
  const newPlaceholder = member({
    eqpId: "EQP-000003", admId: "ADM-000005", name: "Membro Sem Email",
    passwordHash: identityHash(initialPassword), securityStamp: "stamp-novo-placeholder", version: 1, updatedAt: now
  });
  const document = { schemaVersion: 1, updatedAt: now, settings: {}, allowlistGitHub: [], convites: [], membros: [existing, newReal, newPlaceholder] };

  await request("/api/equipe/sincronizar-github-db", { method: "POST", body: document });

  const preserved = readUserByAlias("EQP-000001");
  const preservedAdm = readUserByAlias("ADM-000003");
  assert(preserved.Id === existingUserId && preservedAdm.Id === existingUserId, "EQP/ADM existente não preservou o mesmo UserId.");
  assert(preserved.Email === "owner.real@example.com", "Email real existente foi sobrescrito.");
  assert(preserved.EmailRecuperacao === "owner.recovery@example.com", "EmailRecuperacao real existente foi sobrescrito.");
  assert(preserved.EmailConfirmed === 1 && preserved.EmailRecuperacaoConfirmado === 1, "Confirmações de e-mail foram sobrescritas.");
  assert(preserved.TwoFactorEnabled === 1 && preserved.DoisFatoresObrigatorio === 1, "2FA foi alterado.");
  assert(preserved.Passkeys === 1 && preserved.SecurityTokens === 1, "Passkey/token de segurança foi removido.");
  assert(preserved.PhoneNumber === "+5511999999999" && preserved.PhoneNumberConfirmed === 1, "Telefone foi alterado.");
  assert(preserved.SecurityStamp === "security-stamp-preservado" && preserved.PasswordHash === initialHash, "Senha/stamp antigo foram sobrescritos sem versão nova.");

  const importedReal = readUserByAlias("EQP-000002");
  assert(importedReal.Email === "membro.real@example.com", "Novo usuário não recebeu Email real.");
  assert(importedReal.EmailRecuperacao === "membro.recovery@example.com", "Novo usuário não recebeu EmailRecuperacao real.");
  assert(importedReal.EmailRecuperacaoConfirmado === 1, "Confirmação remota de recuperação não foi importada.");

  const importedPlaceholder = readUserByAlias("EQP-000003");
  assert(importedPlaceholder.Email === "eqp-000003@equipe.local", "Novo usuário sem e-mail não recebeu placeholder técnico.");
  assert(importedPlaceholder.EmailConfirmed === 0, "Placeholder técnico foi marcado como e-mail confirmado.");
  assert(!importedPlaceholder.EmailRecuperacao, "Placeholder foi gravado como EmailRecuperacao.");

  existing.passwordHash = newerHash;
  existing.securityStamp = "security-stamp-remoto-novo";
  existing.passwordVersion = 2;
  existing.senhaAtualizadaEm = new Date(Date.now() + 5_000).toISOString();
  existing.atualizadoEm = existing.senhaAtualizadaEm;
  await request("/api/equipe/sincronizar-github-db", { method: "POST", body: document });
  const updatedPassword = readUserByAlias("EQP-000001");
  assert(updatedPassword.PasswordHash === newerHash && updatedPassword.SecurityStamp === "security-stamp-remoto-novo", "Senha remota nova não foi aplicada.");
  assert(updatedPassword.Email === "owner.real@example.com" && updatedPassword.EmailRecuperacao === "owner.recovery@example.com", "Atualização de senha alterou e-mails.");
  assert(updatedPassword.TwoFactorEnabled === 1 && updatedPassword.Passkeys === 1 && updatedPassword.SecurityTokens === 1, "Atualização de senha alterou 2FA/passkeys.");

  existing.passwordHash = initialHash;
  existing.securityStamp = "stamp-remoto-antigo";
  existing.passwordVersion = 1;
  existing.senhaAtualizadaEm = new Date(Date.now() - 60_000).toISOString();
  existing.atualizadoEm = existing.senhaAtualizadaEm;
  await request("/api/equipe/sincronizar-github-db", { method: "POST", body: document });
  const afterOldSync = readUserByAlias("ADM-000003");
  assert(afterOldSync.PasswordHash === newerHash && afterOldSync.SecurityStamp === "security-stamp-remoto-novo", "Sync antigo reverteu a senha.");
  assert(afterOldSync.Email === "owner.real@example.com" && afterOldSync.EmailRecuperacao === "owner.recovery@example.com", "Sync antigo alterou e-mails.");

  console.log("[OK] Placeholder remoto não sobrescreveu e-mail/recuperação reais.");
  console.log("[OK] 2FA, telefone, passkey e token de segurança foram preservados.");
  console.log("[OK] Novos membros importaram e-mail real ou placeholder técnico não confirmado.");
  console.log("[OK] Senha remota nova foi aplicada; versão antiga não reverteu senha nem segurança.");
  console.log("[OK] Aliases EQP/ADM permaneceram no mesmo ApplicationUser.");
}

try {
  await main();
} catch (error) {
  console.error(`[FALHA] ${error.message}`);
  if (apiLogs) console.error(apiLogs.slice(-6000));
  process.exitCode = 1;
} finally {
  if (apiProcess && !apiProcess.killed) {
    apiProcess.kill();
    await new Promise((resolvePromise) => {
      apiProcess.once("exit", resolvePromise);
      setTimeout(resolvePromise, 3000);
    });
  }
  cleanup();
}
