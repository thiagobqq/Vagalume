import { apiJson } from "@/lib/api"
import type { PermissaoDto, UsuarioPermissoesDto } from "./types"

export function listarCatalogo() {
  return apiJson<PermissaoDto[]>("/api/admin/permissoes")
}

export function listarUsuarios(busca?: string, somentePendentes = false) {
  const qs = new URLSearchParams()
  if (busca && busca.trim()) qs.set("busca", busca.trim())
  if (somentePendentes) qs.set("somentePendentes", "true")
  qs.set("limite", "100")
  const q = qs.toString()
  return apiJson<UsuarioPermissoesDto[]>(`/api/admin/usuarios${q ? `?${q}` : ""}`)
}

export function concederPermissao(usuarioId: string, chave: string) {
  return apiJson<UsuarioPermissoesDto>(`/api/admin/usuarios/${usuarioId}/permissoes`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ chave }),
  })
}

export function revogarPermissao(usuarioId: string, chave: string) {
  return apiJson<UsuarioPermissoesDto>(`/api/admin/usuarios/${usuarioId}/permissoes/${encodeURIComponent(chave)}`, {
    method: "DELETE",
  })
}
