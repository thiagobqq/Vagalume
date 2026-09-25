import { ApiError, apiFetch, apiJson } from "@/lib/api"
import type { AnaliseAtsDto } from "./types"

export async function obterAnaliseAts(curriculoId: string): Promise<AnaliseAtsDto | null> {
  const res = await apiFetch(`/api/curriculos/${curriculoId}/analise-ats`)
  const text = await res.text()
  if (res.status === 404) return null

  let envelope: { success: boolean; message: string; data: AnaliseAtsDto | null } | null = null
  try {
    envelope = text ? JSON.parse(text) : null
  } catch {
    throw new ApiError(res.status, text || res.statusText)
  }

  if (!res.ok || envelope?.success === false) {
    throw new ApiError(res.status, envelope?.message || `Erro ${res.status}`)
  }
  return envelope?.data ?? null
}

export function gerarAnaliseAts(curriculoId: string) {
  return apiJson<AnaliseAtsDto>(`/api/curriculos/${curriculoId}/analise-ats`, { method: "POST" })
}
