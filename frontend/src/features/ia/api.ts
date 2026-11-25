import { ApiError, apiEnvelope, apiJson } from "@/lib/api"
import type {
  ConfiguracaoIaDto,
  ProvedorIaDto,
  SalvarConfiguracaoIaRequest,
  TestarIaRequest,
  TestarIaResponse,
} from "./types"

export function listarProvedoresIa() {
  return apiJson<ProvedorIaDto[]>("/api/ia/provedores")
}

export async function obterConfiguracaoIa(): Promise<ConfiguracaoIaDto | null> {
  const { res, envelope } = await apiEnvelope<ConfiguracaoIaDto>("/api/ia/configuracao")
  if (!res.ok || !envelope?.success) {
    throw new ApiError(res.status, envelope?.message || `Erro ${res.status}`)
  }
  return envelope.data ?? null
}

export function salvarConfiguracaoIa(req: SalvarConfiguracaoIaRequest) {
  return apiJson<ConfiguracaoIaDto>("/api/ia/configuracao", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(req),
  })
}

export function testarIa(req: TestarIaRequest) {
  return apiJson<TestarIaResponse>("/api/ia/testar", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(req),
  })
}

export function removerConfiguracaoIa() {
  return apiJson<null>("/api/ia/configuracao", {
    method: "DELETE",
  })
}
