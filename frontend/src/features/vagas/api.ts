import { apiJson } from "@/lib/api"
import type {
  CriarFonteBuscaRequest,
  ExecucaoBuscaDto,
  FonteBuscaDto,
  PerfilBuscaDto,
  SalvarPerfilBuscaRequest,
  VagaDto,
} from "./types"

export function listarVagas(params?: { status?: string; busca?: string; limite?: number }) {
  const qs = new URLSearchParams()
  if (params?.status) qs.set("status", params.status)
  if (params?.busca) qs.set("busca", params.busca)
  if (params?.limite) qs.set("limite", String(params.limite))
  const q = qs.toString()
  return apiJson<VagaDto[]>(`/api/vagas${q ? `?${q}` : ""}`)
}

export function atualizarStatusVaga(id: string, status: string) {
  return apiJson<VagaDto>(`/api/vagas/${id}/status`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status }),
  })
}

export function aplicarVaga(id: string) {
  return apiJson<{ id: string; status: string }>(`/api/vagas/${id}/aplicar`, { method: "POST" })
}

export function obterPerfilBusca() {
  return apiJson<PerfilBuscaDto>("/api/busca/perfil")
}

export function salvarPerfilBusca(req: SalvarPerfilBuscaRequest) {
  return apiJson<PerfilBuscaDto>("/api/busca/perfil", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(req),
  })
}

export function listarFontesBusca() {
  return apiJson<FonteBuscaDto[]>("/api/busca/fontes")
}

export function criarFonteBusca(req: CriarFonteBuscaRequest) {
  return apiJson<FonteBuscaDto>("/api/busca/fontes", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(req),
  })
}

export function atualizarFonteBusca(id: string, patch: { ativo?: boolean; identificador?: string; nomeEmpresa?: string; nome?: string }) {
  return apiJson<FonteBuscaDto>(`/api/busca/fontes/${id}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(patch),
  })
}

export function removerFonteBusca(id: string) {
  return apiJson<{ id: string }>(`/api/busca/fontes/${id}`, { method: "DELETE" })
}

export function executarBusca() {
  return apiJson<ExecucaoBuscaDto>("/api/busca/executar", { method: "POST" })
}

export function obterExecucao(id: string) {
  return apiJson<ExecucaoBuscaDto>(`/api/busca/execucoes/${id}`)
}

export function listarExecucoesBusca() {
  return apiJson<ExecucaoBuscaDto[]>("/api/busca/execucoes")
}
