#!/usr/bin/env node

import { mkdirSync, rmSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const apiDir = join(repoRoot, "CasaMulher.Api");
const apiDll = join(apiDir, "bin", "Release", "net8.0", "CasaMulher.Api.dll");
const runtimeDir = join(repoRoot, ".runtime");
const databasePath = join(runtimeDir, `validacao-render-gate-${process.pid}.db`);
const port = 6000 + (process.pid % 300);
const baseUrl = `http://127.0.0.1:${port}`;
let child;
let logs = "";

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

async function request(path, options = {}) {
  return fetch(`${baseUrl}${path}`, {
    method: options.method || "GET",
    redirect: "manual",
    headers: options.body ? { "Content-Type": "application/json" } : {},
    body: options.body ? JSON.stringify(options.body) : undefined,
    signal: AbortSignal.timeout(10_000)
  });
}

async function esperarApi() {
  // Um banco SQLite vazio aplica todas as migrations antes de abrir o Kestrel.
  // Em maquinas mais lentas (e no primeiro boot do Render) isso pode passar de 20 s.
  for (let tentativa = 0; tentativa < 240; tentativa += 1) {
    try {
      const response = await request("/api/portal-eqp/status");
      if (response.status === 200) return;
    } catch {
      // Aguarda a migration e o Kestrel iniciarem.
    }
    await new Promise((resolvePromise) => setTimeout(resolvePromise, 250));
  }
  throw new Error(`API Staging não iniciou.\n${logs}`);
}

function limpar() {
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
  limpar();

  child = spawn("dotnet", [apiDll], {
    cwd: apiDir,
    windowsHide: true,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: "Staging",
      ASPNETCORE_URLS: baseUrl,
      ENABLE_RENDER_GITHUB_GATE: "true",
      ConnectionStrings__DefaultConnection: `Data Source=${databasePath}`,
      Database__Provider: "Sqlite",
      Seed__RunDemoData: "false",
      EquipeSync__Automatico: "false",
      Jwt__Key: "validacao-render-gate-chave-jwt-2026-segura",
      Jwt__Issuer: "CasaMulher.Api",
      Jwt__Audience: "CasaMulher.Api",
      Convites__HashSecret: "validacao-render-gate-convites-2026",
      Email__Provider: "Fake"
    },
    stdio: ["ignore", "pipe", "pipe"]
  });
  child.stdout.on("data", (chunk) => { logs += chunk.toString(); });
  child.stderr.on("data", (chunk) => { logs += chunk.toString(); });
  await esperarApi();

  const statusResponse = await request("/api/portal-eqp/status");
  const status = await statusResponse.json();
  assert(status.environment === "Staging", "Status não informou ambiente Staging.");
  assert(status.gitHubGateAtivo === true, "Status não informou GitHub Gate ativo.");

  const equipe = await request("/equipe.html");
  assert(equipe.status === 200, `/equipe.html deveria ser pública, recebeu ${equipe.status}.`);

  const index = await request("/index.html");
  assert(index.status === 302, `/index.html deveria redirecionar, recebeu ${index.status}.`);
  assert(index.headers.get("location")?.startsWith("/equipe.html"), "Redirecionamento de /index.html incorreto.");

  const login = await request("/api/auth/login", {
    method: "POST",
    body: { identificador: "EQP-000001", senha: "invalida" }
  });
  const loginBody = await login.json();
  assert(login.status === 401, `/api/auth/login deveria ser bloqueada pelo Gate, recebeu ${login.status}.`);
  assert(String(login.headers.get("content-type")).includes("application/json"), "Gate não retornou JSON para a API.");
  assert(String(loginBody.mensagem || "").includes("GitHub"), "Mensagem do Gate não orienta login GitHub.");

  const funcionarios = await request("/api/funcionarios");
  assert(funcionarios.status === 401, `/api/funcionarios deveria ser bloqueada, recebeu ${funcionarios.status}.`);
  assert(String(funcionarios.headers.get("content-type")).includes("application/json"), "API protegida não retornou JSON.");

  console.log("[OK] Staging aplicou migrations e iniciou com GitHub Gate ativo.");
  console.log("[OK] /equipe.html e status permanecem públicos.");
  console.log("[OK] /index.html redireciona sem sessão GitHub.");
  console.log("[OK] APIs sensíveis retornam 401 JSON antes da autenticação Identity.");
}

try {
  await main();
} catch (error) {
  console.error(`[FALHA] ${error.message}`);
  if (logs) console.error(logs.slice(-6000));
  process.exitCode = 1;
} finally {
  if (child && !child.killed) {
    child.kill();
    await new Promise((resolvePromise) => {
      child.once("exit", resolvePromise);
      setTimeout(resolvePromise, 3000);
    });
  }
  limpar();
}
