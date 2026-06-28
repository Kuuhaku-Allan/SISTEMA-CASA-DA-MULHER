#!/usr/bin/env node
// Faz upload do homologacao-seed.json para ACESSO-EQUIPE/data/homologacao-seed.json
// Uso: node scripts/publicar-homologacao-seed.mjs [--token TOKEN]
// Ou: GITHUB_EQP_WRITE_TOKEN=... node scripts/publicar-homologacao-seed.mjs

import { readFileSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const seedPath = resolve(root, ".runtime", "homologacao-seed.json");

const repoOwner = process.env.GITHUB_EQP_DB_REPO_OWNER ?? "Sistema-Casa-da-Mulher";
const repoName  = process.env.GITHUB_EQP_DB_REPO  ?? "ACESSO-EQUIPE";
const filePath  = process.env.HML_SEED_PATH ?? "data/homologacao-seed.json";

// Aceita token via arg ou env
let token = process.env.GITHUB_EQP_WRITE_TOKEN ?? "";
const tokenArgIndex = process.argv.indexOf("--token");
if (tokenArgIndex !== -1 && process.argv[tokenArgIndex + 1]) {
    token = process.argv[tokenArgIndex + 1];
}

if (!token) {
    console.error("[ERRO] Token não encontrado. Defina GITHUB_EQP_WRITE_TOKEN ou passe --token TOKEN.");
    process.exit(1);
}

let seedContent;
try {
    seedContent = readFileSync(seedPath, "utf-8");
    JSON.parse(seedContent); // valida JSON
} catch (err) {
    console.error(`[ERRO] Não foi possível ler ${seedPath}: ${err.message}`);
    console.error("       Rode primeiro: node scripts/exportar-homologacao-seed.mjs");
    process.exit(1);
}

const url = `https://api.github.com/repos/${repoOwner}/${repoName}/contents/${filePath.split("/").map(encodeURIComponent).join("/")}`;
const headers = {
    "Accept": "application/vnd.github+json",
    "Authorization": `Bearer ${token}`,
    "User-Agent": "CasaMulherHmlSeed/1.0",
    "X-GitHub-Api-Version": "2022-11-28",
    "Content-Type": "application/json"
};

console.log(`[→] Verificando se ${filePath} já existe em ${repoOwner}/${repoName}...`);

// Lê SHA atual se existir
let sha = null;
try {
    const getResp = await fetch(url, { headers });
    if (getResp.ok) {
        const current = await getResp.json();
        sha = current.sha;
        console.log(`[i] Arquivo existente encontrado (sha: ${sha.slice(0, 7)}...). Será atualizado.`);
    } else if (getResp.status === 404) {
        console.log("[i] Arquivo não existe. Será criado.");
    } else {
        const text = await getResp.text();
        throw new Error(`GitHub GET retornou ${getResp.status}: ${text}`);
    }
} catch (err) {
    if (!err.message.startsWith("GitHub")) throw err;
    console.error(`[ERRO] ${err.message}`);
    process.exit(1);
}

// Faz upload
const body = {
    message: "Atualiza seed fictício de homologação (dados sanitizados)",
    content: Buffer.from(seedContent, "utf-8").toString("base64")
};
if (sha) body.sha = sha;

console.log(`[→] Fazendo upload de ${seedPath}...`);

const putResp = await fetch(url, {
    method: "PUT",
    headers,
    body: JSON.stringify(body)
});

if (!putResp.ok) {
    const text = await putResp.text();
    console.error(`[ERRO] GitHub PUT retornou ${putResp.status}: ${text}`);
    process.exit(1);
}

const result = await putResp.json();
console.log(`[OK] Seed publicado com sucesso.`);
console.log(`[OK] Commit: ${result.commit?.sha?.slice(0, 7) ?? "?"} — ${result.commit?.message ?? ""}`);
console.log(`[OK] Arquivo: ${result.content?.html_url ?? url}`);
console.log();
console.log("Próximo passo: faça redeploy no Render para o seed ser aplicado.");
console.log("O seed é aplicado automaticamente quando o banco está vazio (sem o marcador HML_SEED_APLICADO).");
