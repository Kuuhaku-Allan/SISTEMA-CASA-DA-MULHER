#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";

const raiz = process.argv[2] || ".";
const ignorarPastas = new Set([".git", "bin", "obj", ".runtime", "node_modules"]);
let total = 0;
let falhas = 0;

function caminhar(diretorio) {
  for (const nome of fs.readdirSync(diretorio)) {
    const caminho = path.join(diretorio, nome);
    const stat = fs.statSync(caminho);

    if (stat.isDirectory()) {
      if (!ignorarPastas.has(nome)) {
        caminhar(caminho);
      }

      continue;
    }

    if (!stat.isFile() || !nome.endsWith(".html")) {
      continue;
    }

    validarArquivo(caminho);
  }
}

function validarArquivo(caminho) {
  const html = fs.readFileSync(caminho, "utf8");
  const scripts = [...html.matchAll(/<script(?![^>]*\bsrc=)[^>]*>([\s\S]*?)<\/script>/gi)];

  scripts.forEach((match, index) => {
    total += 1;

    try {
      new Function(match[1]);
    } catch (error) {
      falhas += 1;
      console.error(`${caminho} script inline ${index + 1}: ${error.message}`);
    }
  });
}

caminhar(raiz);

if (falhas > 0) {
  process.exit(1);
}

console.log(`Scripts inline OK: ${total}`);
