#!/usr/bin/env node

const API_BASE_URL = process.env.API_BASE_URL || "http://localhost:5001";

const contas = {
  master: cred("CASA_MULHER_MASTER"),
  eqpOwner: cred("CASA_MULHER_EQP_OWNER"),
  eqpComum: cred("CASA_MULHER_EQP_COMUM"),
  admComum: cred("CASA_MULHER_ADM_COMUM"),
  recepcao: cred("CASA_MULHER_RECEPCAO")
};

const resultados = [];

function cred(prefix) {
  const id = process.env[`${prefix}_ID`];
  const senha = process.env[`${prefix}_SENHA`];
  return id && senha ? { id, senha } : null;
}

function ok(nome) {
  resultados.push({ status: "OK", nome });
}

function falha(nome, detalhe) {
  resultados.push({ status: "FALHA", nome, detalhe });
}

function manual(nome, detalhe) {
  resultados.push({ status: "MANUAL", nome, detalhe });
}

async function request(path, options = {}) {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: {
      ...(options.body ? { "Content-Type": "application/json" } : {}),
      ...(options.token ? { Authorization: `Bearer ${options.token}` } : {}),
      ...(options.headers || {})
    },
    body: options.body ? JSON.stringify(options.body) : undefined
  });

  let data = null;
  const text = await response.text();

  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = text;
    }
  }

  return { response, data };
}

async function login(nome, conta) {
  if (!conta) {
    manual(`login ${nome}`, `Defina ${nome}_ID e ${nome}_SENHA nas variaveis CASA_MULHER_*. Exemplo: CASA_MULHER_MASTER_ID.`);
    return null;
  }

  const { response, data } = await request("/api/auth/login", {
    method: "POST",
    body: {
      identificador: conta.id,
      senha: conta.senha
    }
  });

  if (!response.ok) {
    falha(`login ${nome}`, `HTTP ${response.status}: ${JSON.stringify(data)}`);
    return null;
  }

  if (data?.requerDoisFatores) {
    manual(`login ${nome}`, "Conta exige 2FA; valide manualmente ou use uma conta de teste sem 2FA.");
    return null;
  }

  if (!data?.token) {
    falha(`login ${nome}`, "Resposta nao retornou token.");
    return null;
  }

  ok(`login ${nome}`);
  return data.token;
}

function garantirNenhum(array, predicado, nome, detalhe) {
  const itens = Array.isArray(array) ? array : [];
  const achados = itens.filter(predicado);

  if (achados.length > 0) {
    falha(nome, `${detalhe}: ${JSON.stringify(achados.slice(0, 3))}`);
    return;
  }

  ok(nome);
}

async function validarIsolamentoInstitucional(token) {
  const funcionarios = await request("/api/funcionarios", { token });

  if (funcionarios.response.ok) {
    garantirNenhum(
      funcionarios.data,
      (item) => String(item.identificadorFuncionario || "").startsWith("EQP-") || item.perfil === "equipe",
      "funcionarios institucionais nao retornam EQP",
      "Encontrou EQP na lista de funcionarios"
    );
  } else {
    falha("listar funcionarios institucionais", `HTTP ${funcionarios.response.status}`);
  }

  const convites = await request("/api/convites-funcionarios", { token });

  if (convites.response.ok) {
    garantirNenhum(
      convites.data,
      (item) => String(item.identificadorFuncionario || "").startsWith("EQP-") || item.perfil === "equipe",
      "convites institucionais nao retornam EQP",
      "Encontrou convite EQP na lista institucional"
    );
  } else {
    falha("listar convites institucionais", `HTTP ${convites.response.status}`);
  }

  const auditoria = await request("/api/auditoria", { token });

  if (auditoria.response.ok) {
    garantirNenhum(
      auditoria.data,
      (item) => item.perfilFuncionario === "equipe" || String(item.acao || "").startsWith("EQUIPE_"),
      "auditoria institucional nao retorna eventos EQP",
      "Encontrou evento de equipe na auditoria institucional"
    );
  } else {
    falha("listar auditoria institucional", `HTTP ${auditoria.response.status}`);
  }

  const emails = await request("/api/emails", { token });

  if (emails.response.ok) {
    garantirNenhum(
      emails.data,
      (item) => String(item.tipo || "").startsWith("Equipe") || String(item.destinatario || "").endsWith("@equipe.local"),
      "logs de e-mail institucionais nao retornam EQP",
      "Encontrou evento de equipe nos logs de e-mail"
    );
  } else {
    falha("listar logs de e-mail", `HTTP ${emails.response.status}`);
  }
}

async function validarEquipeOwner(token) {
  const membros = await request("/api/equipe/membros", { token });

  if (membros.response.ok) {
    ok("owner/master acessa membros EQP");
  } else {
    falha("owner/master acessa membros EQP", `HTTP ${membros.response.status}`);
  }

  const logs = await request("/api/equipe/logs", { token });

  if (logs.response.ok) {
    ok("owner/master acessa logs de equipe");
  } else {
    falha("owner/master acessa logs de equipe", `HTTP ${logs.response.status}`);
  }
}

async function validarBloqueiosComuns(nome, token) {
  if (!token) {
    return;
  }

  const patchMembro = await request("/api/equipe/membros/0", {
    method: "PATCH",
    token,
    body: {
      papelEquipe: "owner",
      precisaFork: true,
      usaCodespaces: true,
      fluxoTrabalho: "fork_codespaces",
      podeCriarConvitesEquipe: false,
      ativo: true
    }
  });

  if (patchMembro.response.status === 403) {
    ok(`${nome} nao altera membro EQP`);
  } else {
    falha(`${nome} nao altera membro EQP`, `Esperado 403, recebeu HTTP ${patchMembro.response.status}`);
  }

  const resetMembro = await request("/api/equipe/membros/0/gerar-redefinicao-senha", {
    method: "POST",
    token
  });

  if (resetMembro.response.status === 403) {
    ok(`${nome} nao gera reset EQP`);
  } else {
    falha(`${nome} nao gera reset EQP`, `Esperado 403, recebeu HTTP ${resetMembro.response.status}`);
  }
}

async function main() {
  console.log(`Validando regras contra ${API_BASE_URL}`);

  const masterToken = await login("CASA_MULHER_MASTER", contas.master);

  if (masterToken) {
    await validarIsolamentoInstitucional(masterToken);
  }

  const eqpOwnerToken = await login("CASA_MULHER_EQP_OWNER", contas.eqpOwner);

  if (eqpOwnerToken) {
    await validarEquipeOwner(eqpOwnerToken);
  }

  const eqpComumToken = await login("CASA_MULHER_EQP_COMUM", contas.eqpComum);
  await validarBloqueiosComuns("EQP comum", eqpComumToken);

  const admComumToken = await login("CASA_MULHER_ADM_COMUM", contas.admComum);
  await validarBloqueiosComuns("ADM comum", admComumToken);

  if (!contas.recepcao) {
    manual("recepcao continua funcionando", "Defina CASA_MULHER_RECEPCAO_ID e CASA_MULHER_RECEPCAO_SENHA para validar login de recepcao.");
  } else {
    const token = await login("CASA_MULHER_RECEPCAO", contas.recepcao);
    if (token) {
      ok("recepcao login valido");
    }
  }

  console.log("");
  for (const item of resultados) {
    const detalhe = item.detalhe ? ` - ${item.detalhe}` : "";
    console.log(`[${item.status}] ${item.nome}${detalhe}`);
  }

  const falhas = resultados.filter((item) => item.status === "FALHA");
  const manuais = resultados.filter((item) => item.status === "MANUAL");

  console.log("");
  console.log(`Resumo: ${resultados.length - falhas.length - manuais.length} OK, ${falhas.length} falha(s), ${manuais.length} manual(is).`);

  if (falhas.length > 0) {
    process.exit(1);
  }
}

main().catch((error) => {
  console.error("Falha inesperada na validacao:", error);
  process.exit(1);
});
