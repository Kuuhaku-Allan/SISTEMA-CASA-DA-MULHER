#!/usr/bin/env node
/**
 * publicar-equipe-db.mjs
 *
 * Publica a allowlistGitHub local no repositório privado ACESSO-EQUIPE/data/equipe-db.json
 * usando merge CONSERVADOR:
 *   - lê remoto primeiro
 *   - NUNCA apaga membros, convites, settings, passwordHash, eventos
 *   - NUNCA muda IDs já emitidos
 *   - APENAS adiciona usernames à allowlistGitHub
 *   - Comparação case-insensitive
 *   - Relê e confirma após publicar
 *
 * Uso:
 *   node scripts/publicar-equipe-db.mjs             # modo pull (verifica) + push (merge + publica)
 *   node scripts/publicar-equipe-db.mjs --pull      # só baixa e exibe o remoto
 *   node scripts/publicar-equipe-db.mjs --push      # faz merge e publica
 *   node scripts/publicar-equipe-db.mjs --dry-run   # mostra o que publicaria, sem publicar
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT_DIR = path.resolve(__dirname, "..");
const LOCAL_DB_PATH = path.join(ROOT_DIR, "data", "equipe-db.json");
const RUNTIME_DIR = path.join(ROOT_DIR, ".runtime");
const ENV_FILE = path.join(RUNTIME_DIR, ".env.hml");

// --- Configuração via env ---
function loadEnv() {
    if (fs.existsSync(ENV_FILE)) {
        const content = fs.readFileSync(ENV_FILE, "utf8");
        for (const line of content.split("\n")) {
            const m = line.match(/^\s*([^#=][^=]*?)\s*=\s*(.*?)\s*$/);
            if (m) {
                const [, key, val] = m;
                if (!process.env[key]) process.env[key] = val;
            }
        }
    }
}

loadEnv();

const REPO_OWNER = process.env.GITHUB_EQP_DB_REPO_OWNER ?? "Sistema-Casa-da-Mulher";
const REPO_NAME  = process.env.GITHUB_EQP_DB_REPO       ?? "ACESSO-EQUIPE";
const REMOTE_PATH = "data/equipe-db.json";

function getToken() {
    return process.env.GITHUB_EQP_WRITE_TOKEN
        ?? process.env.GITHUB_EQP_READ_TOKEN
        ?? process.env.GH_TOKEN
        ?? process.env.GITHUB_TOKEN;
}

function apiUrl(filePath) {
    const escaped = filePath.split("/").map(encodeURIComponent).join("/");
    return `https://api.github.com/repos/${REPO_OWNER}/${REPO_NAME}/contents/${escaped}`;
}

async function githubGet(filePath) {
    const token = getToken();
    if (!token) {
        throw new Error(
            "Token GitHub não encontrado.\n" +
            "Configure GITHUB_EQP_WRITE_TOKEN no .runtime/.env.hml ou como variável de ambiente."
        );
    }

    const res = await fetch(apiUrl(filePath), {
        headers: {
            "Accept": "application/vnd.github+json",
            "Authorization": `Bearer ${token}`,
            "User-Agent": "CasaMulherEquipeDbPublisher/1.0",
            "X-GitHub-Api-Version": "2022-11-28"
        }
    });

    if (res.status === 404) return null;
    if (!res.ok) {
        const body = await res.text();
        throw new Error(`GitHub GET ${filePath} retornou ${res.status}: ${body}`);
    }

    const data = await res.json();
    if (data.type !== "file") throw new Error(`Conteúdo remoto para ${filePath} não é um arquivo.`);

    const base64 = (data.content || "").replace(/\n/g, "").replace(/\r/g, "");
    const content = Buffer.from(base64, "base64").toString("utf8");
    return { content, sha: data.sha };
}

async function githubPut(filePath, content, sha, commitMessage) {
    const token = getToken();
    if (!token) throw new Error("Token GitHub não encontrado.");

    const body = {
        message: commitMessage,
        content: Buffer.from(content, "utf8").toString("base64")
    };
    if (sha) body.sha = sha;

    const res = await fetch(apiUrl(filePath), {
        method: "PUT",
        headers: {
            "Accept": "application/vnd.github+json",
            "Authorization": `Bearer ${token}`,
            "Content-Type": "application/json",
            "User-Agent": "CasaMulherEquipeDbPublisher/1.0",
            "X-GitHub-Api-Version": "2022-11-28"
        },
        body: JSON.stringify(body)
    });

    if (!res.ok) {
        const text = await res.text();
        throw new Error(`GitHub PUT ${filePath} retornou ${res.status}: ${text}`);
    }
    return await res.json();
}

function parseRemote(content) {
    try {
        return JSON.parse(content);
    } catch {
        throw new Error("O arquivo remoto não é JSON válido.");
    }
}

function parseLocal() {
    if (!fs.existsSync(LOCAL_DB_PATH)) {
        throw new Error(`Arquivo local não encontrado: ${LOCAL_DB_PATH}`);
    }
    try {
        return JSON.parse(fs.readFileSync(LOCAL_DB_PATH, "utf8"));
    } catch {
        throw new Error("O arquivo local data/equipe-db.json não é JSON válido.");
    }
}

/**
 * Merge conservador: só atualiza allowlistGitHub no remoto.
 * Preserva TUDO mais do remoto (membros, convites, settings, etc.)
 */
function mergeConservador(remoto, local) {
    // allowlist local (o que queremos garantir que esteja no remoto)
    const localAllowlist = Array.isArray(local.allowlistGitHub) ? local.allowlistGitHub : [];

    // allowlist remota atual
    const remotoAllowlist = Array.isArray(remoto.allowlistGitHub) ? [...remoto.allowlistGitHub] : [];

    // Adicionar entradas locais que ainda não estão no remoto (case-insensitive)
    const novos = [];
    for (const username of localAllowlist) {
        const jaExiste = remotoAllowlist.some(
            u => u.toLowerCase() === username.toLowerCase()
        );
        if (!jaExiste) {
            remotoAllowlist.push(username);
            novos.push(username);
        }
    }

    // Resultado: remoto com allowlist atualizada, todo o resto preservado
    const resultado = {
        ...remoto,
        allowlistGitHub: remotoAllowlist,
        updatedAt: new Date().toISOString()
    };

    return { resultado, novos };
}

function printDocumentInfo(label, doc) {
    const allowlist = doc.allowlistGitHub || [];
    const membros   = doc.membros   || [];
    const convites  = doc.convites  || [];
    console.log(`\n📋 ${label}`);
    console.log(`   schemaVersion: ${doc.schemaVersion ?? "(ausente)"}`);
    console.log(`   updatedAt:     ${doc.updatedAt ?? "(ausente)"}`);
    console.log(`   allowlistGitHub (${allowlist.length}): ${allowlist.join(", ") || "(vazio)"}`);
    console.log(`   membros:  ${membros.length}`);
    console.log(`   convites: ${convites.length}`);
}

async function commandPull() {
    console.log(`\n🔽 Lendo ${REPO_OWNER}/${REPO_NAME}/${REMOTE_PATH} ...`);
    const remote = await githubGet(REMOTE_PATH);
    if (!remote) {
        console.log("⚠️  Arquivo remoto não existe ainda.");
        return;
    }
    const doc = parseRemote(remote.content);
    printDocumentInfo("Remoto atual", doc);

    // Salva cópia local
    const localCopy = JSON.stringify(doc, null, 2) + "\n";
    fs.mkdirSync(path.dirname(LOCAL_DB_PATH), { recursive: true });
    fs.writeFileSync(LOCAL_DB_PATH, localCopy, "utf8");
    console.log(`\n✅ Cópia salva em data/equipe-db.json`);
}

async function commandPush(dryRun = false) {
    console.log(`\n📤 Publicando allowlist em ${REPO_OWNER}/${REPO_NAME}/${REMOTE_PATH} ...`);

    // 1. Ler local
    const local = parseLocal();
    printDocumentInfo("Local (patch)", local);

    // 2. Ler remoto
    console.log("\n🔽 Lendo remoto atual...");
    const remote = await githubGet(REMOTE_PATH);

    let remoto;
    let remoteSha;
    if (!remote) {
        console.log("ℹ️  Arquivo remoto não existe. Criando novo baseado no local.");
        remoto = local;
        remoteSha = undefined;
    } else {
        remoto = parseRemote(remote.content);
        remoteSha = remote.sha;
        printDocumentInfo("Remoto antes do merge", remoto);
    }

    // 3. Merge conservador
    const { resultado, novos } = mergeConservador(remoto, local);

    if (novos.length === 0) {
        console.log("\n✅ allowlistGitHub já está atualizada no remoto. Nenhuma mudança necessária.");
    } else {
        console.log(`\n➕ Usuários a adicionar: ${novos.join(", ")}`);
    }

    printDocumentInfo("Resultado do merge (o que será publicado)", resultado);

    if (dryRun) {
        console.log("\n🔍 Modo dry-run: NÃO publicando. Use --push para publicar.");
        return;
    }

    if (novos.length === 0) {
        // Nada mudou — não faz commit desnecessário
        console.log("\n✅ Nenhum commit necessário.");
        return;
    }

    // 4. Publicar
    console.log("\n📤 Publicando...");
    const commitMsg = `Atualiza allowlistGitHub: adiciona ${novos.join(", ")}`;
    const content = JSON.stringify(resultado, null, 2) + "\n";
    await githubPut(REMOTE_PATH, content, remoteSha, commitMsg);
    console.log("✅ Publicado!");

    // 5. Reler e confirmar
    console.log("\n🔄 Relendo para confirmar...");
    const confirmRemote = await githubGet(REMOTE_PATH);
    if (!confirmRemote) {
        throw new Error("Arquivo não encontrado após publicação — algo deu errado.");
    }
    const confirmDoc = parseRemote(confirmRemote.content);
    printDocumentInfo("Remoto confirmado após publicação", confirmDoc);

    // Verificar que todos os usernames locais estão no remoto
    const localAllowlist = Array.isArray(local.allowlistGitHub) ? local.allowlistGitHub : [];
    const confirmAllowlist = confirmDoc.allowlistGitHub || [];
    const ausentes = localAllowlist.filter(
        u => !confirmAllowlist.some(r => r.toLowerCase() === u.toLowerCase())
    );
    if (ausentes.length > 0) {
        console.error(`\n❌ ERRO: Os seguintes usuários NÃO aparecem no remoto após publicação: ${ausentes.join(", ")}`);
        process.exit(1);
    }

    console.log(`\n✅ SUCESSO! Todos os ${localAllowlist.length} usuários confirmados no ACESSO-EQUIPE.`);
    console.log(`   allowlistGitHub remota: ${confirmAllowlist.join(", ")}`);
}

// --- Main ---
async function main() {
    const args = process.argv.slice(2);
    const isPull    = args.includes("--pull");
    const isPush    = args.includes("--push");
    const isDryRun  = args.includes("--dry-run");

    console.log("🏠 Casa da Mulher — Publicador de Equipe DB");
    console.log(`   Repositório: ${REPO_OWNER}/${REPO_NAME}`);
    console.log(`   Arquivo remoto: ${REMOTE_PATH}`);
    console.log(`   Arquivo local:  data/equipe-db.json`);

    if (isPull && !isPush) {
        await commandPull();
    } else if (isPush || isDryRun || (!isPull && !isPush)) {
        // Default: push (ou dry-run)
        await commandPush(isDryRun);
    }
}

main().catch(err => {
    console.error(`\n❌ Erro: ${err.message}`);
    process.exit(1);
});
