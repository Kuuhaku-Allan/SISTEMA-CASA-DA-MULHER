#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import zlib from "node:zlib";
import { execSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT_DIR = path.resolve(__dirname, "..");
const RUNTIME_DIR = path.join(ROOT_DIR, ".runtime");
const ENV_FILE = path.join(RUNTIME_DIR, ".env.hml");
const SYNC_STATE_FILE = path.join(RUNTIME_DIR, "hml-sync-state.json");
const DB_PATH = path.join(ROOT_DIR, "CasaMulher.Api", "casamulher.db");

const REPO_OWNER = process.env.GITHUB_EQP_DB_REPO_OWNER ?? "Sistema-Casa-da-Mulher";
const REPO_NAME = process.env.GITHUB_EQP_DB_REPO ?? "ACESSO-EQUIPE";
const MANIFEST_PATH = "data/render-hml-db/manifest.json";

const MAGIC = Buffer.from("CMHML01", "ascii");

// ==========================================
// Criptografia e Hashes
// ==========================================
function sha256(buffer) {
    return crypto.createHash("sha256").update(buffer).digest("hex");
}

function parseKey(keyStr) {
    if (!keyStr) throw new Error("Chave HML_DB_SNAPSHOT_KEY não fornecida.");
    const key = Buffer.from(keyStr.trim(), "base64");
    if (key.length !== 32) throw new Error("A chave deve ter exatamente 32 bytes em Base64.");
    return key;
}

function encryptCompressed(databaseBuffer, keyBuffer) {
    const compressed = zlib.gzipSync(databaseBuffer, { level: zlib.constants.Z_BEST_COMPRESSION });
    const nonce = crypto.randomBytes(12);
    const cipher = crypto.createCipheriv("aes-256-gcm", keyBuffer, nonce);
    cipher.setAAD(MAGIC);
    
    let ciphertext = cipher.update(compressed);
    const finalBuffer = cipher.final();
    if (finalBuffer.length > 0) ciphertext = Buffer.concat([ciphertext, finalBuffer]);
    
    const tag = cipher.getAuthTag();
    return Buffer.concat([MAGIC, nonce, tag, ciphertext]);
}

function decryptDecompressed(snapshotBuffer, keyBuffer) {
    const headerSize = MAGIC.length + 12 + 16;
    if (snapshotBuffer.length <= headerSize || !snapshotBuffer.subarray(0, MAGIC.length).equals(MAGIC)) {
        throw new Error("Formato de snapshot inválido (Magic incorreto ou arquivo muito pequeno).");
    }
    const nonce = snapshotBuffer.subarray(MAGIC.length, MAGIC.length + 12);
    const tag = snapshotBuffer.subarray(MAGIC.length + 12, MAGIC.length + 28);
    const ciphertext = snapshotBuffer.subarray(MAGIC.length + 28);
    
    const decipher = crypto.createDecipheriv("aes-256-gcm", keyBuffer, nonce);
    decipher.setAuthTag(tag);
    decipher.setAAD(MAGIC);
    
    let compressed = decipher.update(ciphertext);
    const finalBuffer = decipher.final();
    if (finalBuffer.length > 0) compressed = Buffer.concat([compressed, finalBuffer]);
    
    return zlib.gunzipSync(compressed);
}

// ==========================================
// Utils de Ambiente e GitHub
// ==========================================
function loadEnv() {
    let key = process.env.HML_DB_SNAPSHOT_KEY;
    let token = process.env.GITHUB_EQP_WRITE_TOKEN;
    
    if (fs.existsSync(ENV_FILE)) {
        const content = fs.readFileSync(ENV_FILE, "utf8");
        for (const line of content.split("\n")) {
            const m = line.match(/^\s*([^=]+?)\s*=\s*(.*)$/);
            if (m) {
                if (m[1] === "HML_DB_SNAPSHOT_KEY" && !key) key = m[2].trim();
                if (m[1] === "GITHUB_EQP_WRITE_TOKEN" && !token) token = m[2].trim();
            }
        }
    }
    
    // Fallback: tentar obter token do gh cli se não houver token
    if (!token) {
        try {
            token = execSync("gh auth token", { encoding: "utf8", stdio: ["ignore", "pipe", "ignore"] }).trim();
        } catch {}
    }
    
    return { key, token };
}

function getSyncState() {
    if (fs.existsSync(SYNC_STATE_FILE)) {
        try {
            return JSON.parse(fs.readFileSync(SYNC_STATE_FILE, "utf8"));
        } catch { }
    }
    return { lastPulledGeneration: 0, lastPulledSnapshotId: null, localDbHashAfterPull: null };
}

function setSyncState(state) {
    if (!fs.existsSync(RUNTIME_DIR)) fs.mkdirSync(RUNTIME_DIR, { recursive: true });
    fs.writeFileSync(SYNC_STATE_FILE, JSON.stringify(state, null, 2), "utf8");
}

async function githubReq(token, pathStr, method = "GET", bodyObj = null) {
    if (!token) throw new Error("GitHub Token não configurado. Execute: hml init-key");
    const escapedPath = pathStr.split("/").map(encodeURIComponent).join("/");
    const url = `https://api.github.com/repos/${REPO_OWNER}/${REPO_NAME}/contents/${escapedPath}`;
    
    const headers = {
        "Accept": "application/vnd.github+json",
        "Authorization": `Bearer ${token}`,
        "User-Agent": "CasaMulherHmlGitOps/1.0",
        "X-GitHub-Api-Version": "2022-11-28"
    };
    
    const opts = { method, headers };
    if (bodyObj) {
        opts.body = JSON.stringify(bodyObj);
        opts.headers["Content-Type"] = "application/json";
    }
    
    const resp = await fetch(url, opts);
    if (!resp.ok) {
        if (resp.status === 404 && method === "GET") return null;
        const text = await resp.text();
        throw new Error(`GitHub API Error (${resp.status}): ${text}`);
    }
    return await resp.json();
}

async function getRemoteManifest(token) {
    const file = await githubReq(token, MANIFEST_PATH);
    if (!file) return { manifest: null, sha: null };
    const content = Buffer.from(file.content.replace(/\n/g, ""), "base64").toString("utf8");
    return { manifest: JSON.parse(content), sha: file.sha };
}

async function uploadFile(token, pathStr, contentBuffer, message, sha = null) {
    const body = {
        message,
        content: contentBuffer.toString("base64")
    };
    if (sha) body.sha = sha;
    return await githubReq(token, pathStr, "PUT", body);
}

// ==========================================
// Comandos
// ==========================================
async function cmdStatus() {
    const { token } = loadEnv();
    const state = getSyncState();
    console.log("[i] Status Local:");
    console.log(`    Geração base local (pull): ${state.lastPulledGeneration}`);
    
    if (fs.existsSync(DB_PATH)) {
        const hash = sha256(fs.readFileSync(DB_PATH));
        if (hash === state.localDbHashAfterPull) {
            console.log("    Banco de dados: INTÁCTO desde o último pull.");
        } else {
            console.log("    Banco de dados: MODIFICADO localmente.");
        }
    } else {
        console.log("    Banco de dados: NÃO EXISTE LOCALMENTE.");
    }
    
    console.log("\n[i] Buscando status remoto...");
    const remote = await getRemoteManifest(token);
    if (!remote.manifest) {
        console.log("    Manifesto não encontrado. O ambiente remoto está vazio.");
        return;
    }
    
    const m = remote.manifest.current;
    console.log(`    Geração atual remota: ${m.generation}`);
    console.log(`    Data do snapshot: ${m.createdAt}`);
    console.log(`    Criado por: ${m.source} (${m.sourceMachine})`);
    
    console.log("\n[Resultado]");
    if (m.generation === state.lastPulledGeneration) {
        console.log("✅ Local está sincronizado com a mesma geração do remoto.");
    } else if (m.generation > state.lastPulledGeneration) {
        console.log("⚠️ Remoto está na frente. Você precisa fazer 'hml pull'.");
    } else {
        console.log("⚠️ Local está na frente? Isso é inesperado.");
    }
}

async function cmdBackupLocal() {
    if (!fs.existsSync(DB_PATH)) {
        console.log("[!] Nenhum banco local para fazer backup.");
        return null;
    }
    const backupDir = path.join(RUNTIME_DIR, "backups");
    if (!fs.existsSync(backupDir)) fs.mkdirSync(backupDir, { recursive: true });
    
    const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
    const dest = path.join(backupDir, `casamulher-${timestamp}.db`);
    
    // O ideal seria usar a API do sqlite, mas copiando o arquivo enquanto a API tá parada serve
    fs.copyFileSync(DB_PATH, dest);
    console.log(`[OK] Backup local salvo em: ${dest}`);
    return dest;
}

async function cmdPull() {
    const { key, token } = loadEnv();
    if (!key) throw new Error("HML_DB_SNAPSHOT_KEY não configurada.");
    
    const remote = await getRemoteManifest(token);
    if (!remote.manifest) throw new Error("Manifesto não encontrado no remoto.");
    const m = remote.manifest.current;
    
    console.log(`[→] Baixando geração ${m.generation} (${m.file})...`);
    const file = await githubReq(token, `data/render-hml-db/${m.file}`);
    if (!file) throw new Error(`Arquivo não encontrado no GitHub: ${m.file}`);
    
    const encryptedBuf = Buffer.from(file.content.replace(/\n/g, ""), "base64");
    const encHash = sha256(encryptedBuf);
    if (encHash !== m.encryptedHash) throw new Error(`Hash criptografado inválido!\nEsperado: ${m.encryptedHash}\nObtido: ${encHash}`);
    
    console.log("[→] Descriptografando e validando banco...");
    const keyBuf = parseKey(key);
    const dbBuf = decryptDecompressed(encryptedBuf, keyBuf);
    
    const dbHash = sha256(dbBuf);
    if (dbHash !== m.databaseHash) throw new Error(`Hash do SQLite inválido!\nEsperado: ${m.databaseHash}\nObtido: ${dbHash}`);
    
    await cmdBackupLocal();
    
    console.log(`[→] Sobrescrevendo banco local em ${DB_PATH}...`);
    fs.writeFileSync(DB_PATH, dbBuf);
    
    const state = getSyncState();
    state.lastPulledGeneration = m.generation;
    state.lastPulledSnapshotId = m.snapshotId;
    state.localDbHashAfterPull = dbHash;
    setSyncState(state);
    
    console.log("[OK] Pull concluído com sucesso. O banco local foi atualizado.");
    console.log("Recomendação: Suba a API para que as migrations e sync EQP rodem se necessário.");
}

async function cmdPush() {
    const { key, token } = loadEnv();
    if (!key) throw new Error("HML_DB_SNAPSHOT_KEY não configurada.");
    if (!fs.existsSync(DB_PATH)) throw new Error("Banco local não encontrado.");
    
    const state = getSyncState();
    
    console.log("[→] Lendo manifesto remoto para evitar conflitos...");
    const remote = await getRemoteManifest(token);
    let remoteGen = 0;
    if (remote.manifest) {
        remoteGen = remote.manifest.current.generation;
    }
    
    if (remoteGen > state.lastPulledGeneration) {
        console.error("\n[ERRO] CONFLITO DE GERAÇÃO DETECTADO!");
        console.error(`O remoto avançou para a geração ${remoteGen}, mas o seu local é baseado na ${state.lastPulledGeneration}.`);
        console.error("Fazer o push agora iria sobrescrever as mudanças remotas acidentalmente.");
        
        const conflictDir = path.join(RUNTIME_DIR, "conflicts");
        if (!fs.existsSync(conflictDir)) fs.mkdirSync(conflictDir, { recursive: true });
        const conflictPath = path.join(conflictDir, `conflito-${Date.now()}.db`);
        fs.copyFileSync(DB_PATH, conflictPath);
        
        console.error(`\nUm backup do seu banco atual foi salvo em: ${conflictPath}`);
        console.error("Execute 'hml pull' para pegar as mudanças recentes, verifique os dados e então faça push se estiver tudo bem.");
        process.exit(1);
    }
    
    console.log("[→] Compactando e criptografando banco local...");
    // Ideally use vacuum into here. For node script, we assume the API is stopped (as instructed by user)
    const dbBuf = fs.readFileSync(DB_PATH);
    const dbHash = sha256(dbBuf);
    
    const keyBuf = parseKey(key);
    const encryptedBuf = encryptCompressed(dbBuf, keyBuf);
    const encHash = sha256(encryptedBuf);
    
    const newGen = remoteGen + 1;
    const snapshotId = crypto.randomUUID().replace(/-/g, "");
    const historyFile = `history/${snapshotId}.sqlite.gz.enc`;
    
    console.log(`[→] Fazendo upload do arquivo (Geração ${newGen})...`);
    await uploadFile(token, `data/render-hml-db/${historyFile}`, encryptedBuf, `Salva snapshot histórico de homologação (Gen ${newGen})`);
    
    console.log(`[→] Atualizando latest.sqlite.gz.enc...`);
    const latestExisting = await githubReq(token, `data/render-hml-db/latest.sqlite.gz.enc`);
    await uploadFile(token, `data/render-hml-db/latest.sqlite.gz.enc`, encryptedBuf, `Atualiza latest.sqlite.gz.enc`, latestExisting?.sha);
    
    console.log(`[→] Atualizando manifest.json...`);
    const newManifest = {
        schemaVersion: 1,
        current: {
            snapshotId: snapshotId,
            createdAt: new Date().toISOString(),
            source: "localhost",
            sourceMachine: process.env.COMPUTERNAME || process.env.HOSTNAME || "localhost",
            appCommit: "manual-push",
            databaseHash: dbHash,
            encryptedHash: encHash,
            generation: newGen,
            baseGeneration: remoteGen,
            file: historyFile,
            latestFile: "latest.sqlite.gz.enc"
        }
    };
    
    await uploadFile(token, MANIFEST_PATH, Buffer.from(JSON.stringify(newManifest, null, 2)), `Atualiza manifesto para geração ${newGen}`, remote.sha);
    
    state.lastPulledGeneration = newGen;
    state.localDbHashAfterPull = dbHash;
    setSyncState(state);
    
    console.log("[OK] Push concluído com sucesso.");
    console.log("No Render, faça redeploy ou reinicie a API para que ele restaure essa nova versão.");
}

async function cmdInitKey() {
    console.log("=== Configuração de Chaves (HML GitOps) ===");
    console.log(`O arquivo ${ENV_FILE} não será commitado.\n`);
    
    if (fs.existsSync(ENV_FILE)) {
        console.log(`[i] Arquivo já existe. Editando manualmente é preferido.\n`);
    } else {
        fs.writeFileSync(ENV_FILE, "HML_DB_SNAPSHOT_KEY=\nGITHUB_EQP_WRITE_TOKEN=\n", "utf8");
        console.log(`[OK] Arquivo criado vazio.`);
    }
    console.log(`Preencha o arquivo ${ENV_FILE} com suas credenciais:`);
    console.log("HML_DB_SNAPSHOT_KEY=... (chave de 32 bytes em base64)");
    console.log("GITHUB_EQP_WRITE_TOKEN=... (token do github, caso não use gh auth)");
}

// ==========================================
// CLI
// ==========================================
const cmd = process.argv[2];
try {
    if (cmd === "status") await cmdStatus();
    else if (cmd === "pull") await cmdPull();
    else if (cmd === "push") await cmdPush();
    else if (cmd === "backup-local") await cmdBackupLocal();
    else if (cmd === "init-key") await cmdInitKey();
    else {
        console.log("Uso: node banco-hml.mjs <status|pull|push|backup-local|init-key>");
        process.exit(1);
    }
} catch (err) {
    console.error(`\n[ERRO] ${err.message}`);
    process.exit(1);
}
