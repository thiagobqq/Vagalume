import { apiFetch, apiJson } from "@/lib/api"
import type {
  AtualizarCandidaturaRequest,
  CandidaturaDetalhadaDto,
  CandidaturaDto,
  CriarCandidaturaRequest,
  MudarStatusRequest,
  PrepEntrevistaDto,
  RascunhoFollowUpDto,
  VagaExtraidaDto,
} from "./types"
import type { AnaliseCompatibilidadeDto } from "@/features/vagas/types"

export async function listarCandidaturas() {
  return apiJson<CandidaturaDto[]>("/api/candidaturas")
}

export async function obterCandidatura(id: string) {
  return apiJson<CandidaturaDetalhadaDto>(`/api/candidaturas/${id}`)
}

export async function criarCandidatura(req: CriarCandidaturaRequest) {
  return apiJson<CandidaturaDto>("/api/candidaturas", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(req),
  })
}

export async function atualizarCandidatura(id: string, req: AtualizarCandidaturaRequest) {
  return apiJson<CandidaturaDto>(`/api/candidaturas/${id}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(req),
  })
}

export async function mudarStatus(id: string, req: MudarStatusRequest) {
  return apiJson<CandidaturaDto>(`/api/candidaturas/${id}/status`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(req),
  })
}

export async function removerCandidatura(id: string) {
  return apiJson<{ id: string }>(`/api/candidaturas/${id}`, {
    method: "DELETE",
  })
}

export async function extrairVagaPorLink(url: string) {
  return apiJson<VagaExtraidaDto>("/api/candidaturas/extrair-link", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ url }),
  })
}

export async function analisarCompatibilidadeCandidatura(id: string) {
  return apiJson<AnaliseCompatibilidadeDto>(`/api/candidaturas/${id}/compatibilidade`, {
    method: "POST",
  })
}

export async function gerarFollowUpCandidatura(id: string) {
  return apiJson<RascunhoFollowUpDto>(`/api/candidaturas/${id}/gerar-followup`, {
    method: "POST",
  })
}

export async function obterPrepEntrevista(id: string) {
  return apiJson<PrepEntrevistaDto | null>(`/api/candidaturas/${id}/preparar-entrevista`)
}

export async function gerarPrepEntrevista(id: string) {
  return apiJson<PrepEntrevistaDto>(`/api/candidaturas/${id}/preparar-entrevista`, {
    method: "POST",
  })
}

export async function exportarCandidaturasCsv() {
  const res = await apiFetch("/api/candidaturas/exportar.csv", { rawResponse: true })
  if (!res.ok) throw new Error(`Não foi possível exportar CSV (${res.status})`)
  const blob = await res.blob()
  const url = URL.createObjectURL(blob)
  const a = document.createElement("a")
  a.href = url
  a.download = `vagalume-candidaturas-${new Date().toISOString().slice(0, 10)}.csv`
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}

// curriculos
import type { CurriculoDto } from "./types"

export async function listarCurriculos() {
  return apiJson<CurriculoDto[]>("/api/curriculos")
}

export async function uploadCurriculo(file: File, rotulo?: string) {
  const fd = new FormData()
  fd.append("arquivo", file)
  if (rotulo?.trim()) fd.append("rotulo", rotulo.trim())
  return apiJson<CurriculoDto>("/api/curriculos", {
    method: "POST",
    body: fd,
  })
}

export async function atualizarCurriculo(id: string, rotulo: string | null) {
  return apiJson<CurriculoDto>(`/api/curriculos/${id}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ rotulo }),
  })
}

export async function removerCurriculo(id: string) {
  return apiJson<{ id: string }>(`/api/curriculos/${id}`, {
    method: "DELETE",
  })
}

export async function marcarCurriculoAtivo(id: string) {
  return apiJson<CurriculoDto>(`/api/curriculos/${id}/ativo`, { method: "PATCH" })
}

export async function downloadCurriculo(id: string, fileNameHint?: string) {
  const res = await apiFetch(`/api/curriculos/${id}/arquivo`, { rawResponse: true })
  if (!res.ok) {
    const text = await res.text()
    let msg = text
    try {
      const j = JSON.parse(text) as { message?: string }
      if (j.message) msg = j.message
    } catch {}
    throw new Error(msg || `Falha ao baixar (${res.status})`)
  }
  const blob = await res.blob()
  // tenta extrair filename do header
  const cd = res.headers.get("Content-Disposition") || ""
  let fileName = fileNameHint || "curriculo.pdf"
  const m = cd.match(/filename\*?=(?:UTF-8''|")?([^";\n]+)/i)
  if (m?.[1]) {
    try {
      fileName = decodeURIComponent(m[1].replace(/"/g, ""))
    } catch {
      fileName = m[1]
    }
  }
  const url = URL.createObjectURL(blob)
  const a = document.createElement("a")
  a.href = url
  a.download = fileName
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}
