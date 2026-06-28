#!/usr/bin/env node

import { createServer } from "node:net";
import { mkdirSync, rmSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const apiDir = join(root, "CasaMulher.Api");
const dll = join(apiDir, "bin", "Release", "net8.0", "CasaMulher.Api.dll");
const runtime = join(root, ".runtime");
const db = join(runtime, `webauthn-${process.pid}.db`);

function assert(value, message) { if (!value) throw new Error(message); }
async function freeUrl() {
  return new Promise((ok, fail) => {
    const server = createServer();
    server.once("error", fail);
    server.listen(0, "127.0.0.1", () => {
      const port = server.address().port;
      server.close((error) => error ? fail(error) : ok(`http://127.0.0.1:${port}`));
    });
  });
}
function clean() {
  for (const suffix of ["", "-shm", "-wal"]) rmSync(`${db}${suffix}`, { force: true });
}
function start(extraEnv, url) {
  let logs = "";
  const process = spawn("dotnet", [dll], {
    cwd: apiDir,
    windowsHide: true,
    env: {
      ...globalThis.process.env,
      ASPNETCORE_ENVIRONMENT: "Staging",
      ASPNETCORE_URLS: url,
      ENABLE_RENDER_GITHUB_GATE: "false",
      ConnectionStrings__DefaultConnection: `Data Source=${db}`,
      Database__Provider: "Sqlite",
      EquipeSync__Automatico: "false",
      HML_SEED_ENABLED: "false",
      HML_DB_SNAPSHOT_ENABLED: "false",
      Jwt__Key: "validacao-webauthn-render-chave-2026",
      Convites__HashSecret: "validacao-webauthn-convites-2026",
      ...extraEnv
    },
    stdio: ["ignore", "pipe", "pipe"]
  });
  process.stdout.on("data", (chunk) => { logs += chunk.toString(); });
  process.stderr.on("data", (chunk) => { logs += chunk.toString(); });
  return { process, logs: () => logs };
}
async function stop(child) {
  if (!child.killed) child.kill();
  await new Promise((ok) => { child.once("exit", ok); setTimeout(ok, 3000); });
}

mkdirSync(runtime, { recursive: true });
clean();
let active;
try {
  const url = await freeUrl();
  active = start({
    WEBAUTHN_RP_ID: "casa-mulher-eqp.onrender.com",
    WEBAUTHN_RP_NAME: "Sistema Casa da Mulher - Homologação",
    WEBAUTHN_ORIGINS: "https://casa-mulher-eqp.onrender.com"
  }, url);
  for (let attempt = 0; attempt < 240; attempt += 1) {
    try {
      const response = await fetch(`${url}/api/portal-eqp/status`, { signal: AbortSignal.timeout(2000) });
      if (response.ok) break;
    } catch { /* aguarda */ }
    await new Promise((ok) => setTimeout(ok, 250));
  }
  assert(active.logs().includes("RP ID=casa-mulher-eqp.onrender.com"), `Startup não registrou RP ID do Render.\n${active.logs()}`);
  assert(active.logs().includes("Origins=https://casa-mulher-eqp.onrender.com"), "Startup não registrou origin HTTPS do Render.");
  await stop(active.process);
  active = null;

  const invalid = start({
    WEBAUTHN_RP_ID: "https://casa-mulher-eqp.onrender.com",
    WEBAUTHN_ORIGINS: "https://casa-mulher-eqp.onrender.com"
  }, await freeUrl());
  const exitCode = await new Promise((ok) => {
    invalid.process.once("exit", ok);
    setTimeout(() => { invalid.process.kill(); ok(null); }, 15_000);
  });
  assert(exitCode !== 0 && exitCode !== null, "RP ID com protocolo não foi recusado no startup.");
  assert(invalid.logs().includes("sem protocolo, porta ou caminho"), "Erro de RP ID inválido não foi claro.");

  console.log("[OK] Staging usa RP ID e origin HTTPS explícitos do Render.");
  console.log("[OK] RP ID com protocolo/porta/caminho falha antes de iniciar.");
} finally {
  if (active) await stop(active.process);
  clean();
}
