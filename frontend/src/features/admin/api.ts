import { apiJson } from "@/lib/api"
import type { PermissaoDto, UsuarioPermissoesDto } from "./types"

export function listarCatalogo() {
  return apiJson<PermissaoDto[]>("/api/admin/permissoes")
}

export function listarUsuarios(busca?: string) {
  const qs = busca && busca.trim() ? `?busca=${encodeURIComponent(busca.trim())}` : ""
  return apiJson<UsuarioPermissoesDto[]>(`/api/admin/usuarios${qs}`)
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
