import { apiFetch, apiJson } from "@/lib/api"
import type { CandidaturaDto, CandidaturaDetalhadaDto, CriarCandidaturaRequest, AtualizarCandidaturaRequest, MudarStatusRequest } from "./types"

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

// curriculos
import type { CurriculoDto } from "./types"

export async function listarCurriculos() {
  return apiJson<CurriculoDto[]>("/api/curriculos")
}

export async function uploadCurriculo(file: File) {
  const fd = new FormData()
  fd.append("arquivo", file)
  return apiJson<CurriculoDto>("/api/curriculos", {
    method: "POST",
    body: fd,
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
