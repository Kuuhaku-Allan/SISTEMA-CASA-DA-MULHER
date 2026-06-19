#!/usr/bin/env node

import { pbkdf2Sync, randomBytes } from "node:crypto";
import { existsSync, mkdirSync, rmSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { spawn, spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const apiProject = join(repoRoot, "CasaMulher.Api", "CasaMulher.Api.csproj");
const apiDll = join(repoRoot, "CasaMulher.Api", "bin", "Release", "net8.0", "CasaMulher.Api.dll");
const runtimeDir = join(repoRoot, ".runtime");
const databasePath = join(runtimeDir, `validacao-eqp-adm-${process.pid}.db`);
const port = 5600 + (process.pid % 300);
const apiBaseUrl = `http://127.0.0.1:${port}`;
const senhaInicial = "Senha!Teste2026";
const senhaNova = "Nova!Senha2026";
let apiProcess;
let apiLogs = "";

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function runDotnet(args, description) {
  const result = spawnSync("dotnet", args, {
    cwd: repoRoot,
    encoding: "utf8",
    windowsHide: true
  });

  if (result.status !== 0) {
    throw new Error(`${description} falhou.\n${result.stdout || ""}\n${result.stderr || ""}`.trim());
  }

  return `${result.stdout || ""}\n${result.stderr || ""}`;
}

function criarPasswordHashIdentity(password) {
  const salt = randomBytes(16);
  const subkey = pbkdf2Sync(password, salt, 100_000, 32, "sha512");
  const header = Buffer.alloc(13);

  header[0] = 0x01;
  header.writeUInt32BE(2, 1); // KeyDerivationPrf.HMACSHA512
  header.writeUInt32BE(100_000, 5);
  header.writeUInt32BE(salt.length, 9);

  return Buffer.concat([header, salt, subkey]).toString("base64");
}

function decodeJwt(token) {
  const payload = token?.split(".")[1];
  assert(payload, "JWT sem payload.");
  return JSON.parse(Buffer.from(payload, "base64url").toString("utf8"));
}

async function request(path, options = {}, expectedStatus = 200) {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: options.method || "GET",
    headers: {
      ...(options.body ? { "Content-Type": "application/json" } : {}),
      ...(options.token ? { Authorization: `Bearer ${options.token}` } : {})
    },
    body: options.body ? JSON.stringify(options.body) : undefined,
    signal: AbortSignal.timeout(10_000)
  });
  const raw = await response.text();
  let data = null;

  if (raw) {
    try {
      data = JSON.parse(raw);
    } catch {
      data = raw;
    }
  }

  assert(
    response.status === expectedStatus,
    `${path}: esperado HTTP ${expectedStatus}, recebido ${response.status}: ${raw}`
  );
  return data;
}

async function esperarApi() {
  for (let tentativa = 0; tentativa < 60; tentativa += 1) {
    try {
      await request("/api/portal-eqp/status");
      return;
    } catch {
      await new Promise((resolvePromise) => setTimeout(resolvePromise, 250));
    }
  }

  throw new Error(`A API não iniciou a tempo.\n${apiLogs}`);
}

function login(identificador, senha) {
  return request("/api/auth/login", {
    method: "POST",
    body: { identificador, senha }
  });
}

function limparTemporarios() {
  for (const suffix of ["", "-shm", "-wal"]) {
    try {
      rmSync(`${databasePath}${suffix}`, { force: true, maxRetries: 5, retryDelay: 200 });
    } catch (error) {
      if (error.code !== "EPERM" && error.code !== "EBUSY") throw error;
    }
  }
}

async function main() {
  mkdirSync(runtimeDir, { recursive: true });
  limparTemporarios();

  runDotnet(
    ["build", apiProject, "--configuration", "Release", "--no-restore"],
    "Compilação Release da API"
  );

  const migrations = runDotnet(
    ["ef", "migrations", "list", "--configuration", "Release", "--no-build", "--project", apiProject, "--startup-project", apiProject],
    "Listagem de migrations"
  );
  assert(migrations.includes("20260604090000_AddEmailRecuperacao"), "AddEmailRecuperacao não foi descoberta.");
  assert(migrations.includes("20260618043355_AddEquipeDbPasswordFields"), "AddEquipeDbPasswordFields não foi descoberta.");

  runDotnet([
    "ef", "database", "update",
    "--configuration", "Release",
    "--no-build",
    "--project", apiProject,
    "--startup-project", apiProject,
    "--connection", `Data Source=${databasePath}`
  ], "Criação do banco SQLite limpo");
  assert(existsSync(apiDll), "DLL Release da API não foi gerada.");

  apiProcess = spawn("dotnet", [apiDll], {
    cwd: join(repoRoot, "CasaMulher.Api"),
    windowsHide: true,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: "Development",
      ASPNETCORE_URLS: apiBaseUrl,
      Database__Provider: "Sqlite",
      ConnectionStrings__DefaultConnection: `Data Source=${databasePath}`,
      Seed__RunDemoData: "false",
      EquipeSync__Automatico: "false",
      Jwt__Key: "validacao-chave-jwt-casa-mulher-2026-segura",
      Jwt__Issuer: "CasaMulher.Api",
      Jwt__Audience: "CasaMulher.Api",
      Convites__HashSecret: "validacao-chave-convites-casa-mulher-2026",
      Email__Provider: "Fake"
    },
    stdio: ["ignore", "pipe", "pipe"]
  });
  apiProcess.stdout.on("data", (chunk) => { apiLogs += chunk.toString(); });
  apiProcess.stderr.on("data", (chunk) => { apiLogs += chunk.toString(); });
  await esperarApi();

  const agora = new Date();
  const membro = {
    eqpId: "EQP-000002",
    admId: "ADM-000004",
    nome: "Validação EQP ADM",
    githubId: "validacao-2",
    githubUsername: "validacao-eqp-adm",
    papelEquipe: "contributor",
    fluxoTrabalho: "fork_codespaces",
    status: "ativo",
    passwordHash: criarPasswordHashIdentity(senhaInicial),
    securityStamp: "validacao-security-stamp-1",
    senhaAtualizadaEm: agora.toISOString(),
    passwordVersion: 1,
    ativadoEm: agora.toISOString(),
    atualizadoEm: agora.toISOString()
  };
  const equipeDb = {
    schemaVersion: 1,
    updatedAt: agora.toISOString(),
    settings: {},
    allowlistGitHub: [],
    convites: [],
    membros: [membro]
  };

  const primeiraSync = await request("/api/equipe/sincronizar-github-db", { method: "POST", body: equipeDb });
  assert(primeiraSync.usuariosCriados === 1, "A importação não criou exatamente um usuário.");
  assert(primeiraSync.identificadoresCriados === 2, "A importação não criou os dois aliases.");

  const loginEqp = await login("EQP-000002", senhaInicial);
  const loginAdm = await login("ADM-000004", senhaInicial);
  const userId = decodeJwt(loginEqp.token).sub;
  assert(userId === decodeJwt(loginAdm.token).sub, "EQP e ADM não apontam para o mesmo UserId.");
  assert(loginEqp.perfil === "equipe", "O alias EQP não abriu contexto de equipe.");
  assert(loginAdm.perfil === "adm", "O alias ADM não abriu contexto administrativo.");

  const bloqueio = await request("/api/auth/trocar-senha-obrigatoria", {
    method: "POST",
    token: loginEqp.token,
    body: { senhaAtual: senhaInicial, novaSenha: senhaNova, confirmarNovaSenha: senhaNova }
  }, 409);
  assert(String(bloqueio.mensagem || "").includes("portal EQP"), "O bloqueio não orientou o portal EQP.");

  membro.passwordHash = criarPasswordHashIdentity(senhaNova);
  membro.securityStamp = "validacao-security-stamp-2";
  membro.senhaAtualizadaEm = new Date(agora.getTime() + 1000).toISOString();
  membro.passwordVersion = 2;
  membro.atualizadoEm = membro.senhaAtualizadaEm;
  equipeDb.updatedAt = membro.senhaAtualizadaEm;

  const segundaSync = await request("/api/equipe/sincronizar-github-db", { method: "POST", body: equipeDb });
  assert(segundaSync.usuariosCriados === 0, "A segunda sincronização duplicou o usuário.");

  const loginEqpNovo = await login("EQP-000002", senhaNova);
  const loginAdmNovo = await login("ADM-000004", senhaNova);
  assert(decodeJwt(loginEqpNovo.token).sub === userId, "O UserId do EQP mudou após redefinir a senha.");
  assert(decodeJwt(loginAdmNovo.token).sub === userId, "O UserId do ADM mudou após redefinir a senha.");

  console.log("[OK] Migrations críticas descobertas e aplicadas em banco novo.");
  console.log("[OK] Um ApplicationUser atende EQP-000002 e ADM-000004.");
  console.log("[OK] Troca normal foi bloqueada para a conta sincronizada.");
  console.log("[OK] Senha nova do portal foi aplicada aos dois aliases sem mudar o UserId.");
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
  limparTemporarios();
}
