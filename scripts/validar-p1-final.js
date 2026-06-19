#!/usr/bin/env node

const { spawnSync } = require("node:child_process");
const { join } = require("node:path");

const script = join(__dirname, "validar-eqp-adm-compartilhado.mjs");
const result = spawnSync(process.execPath, [script], {
  cwd: join(__dirname, ".."),
  stdio: "inherit",
  windowsHide: true
});

process.exit(result.status ?? 1);
