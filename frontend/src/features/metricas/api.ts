import { apiJson } from "@/lib/api"
import type { MetricasFunilDto } from "./types"

export type MetricasFunilParams = {
  de?: string
  ate?: string
  origem?: string
}

export async function obterMetricasFunil(params: MetricasFunilParams = {}) {
  const qs = new URLSearchParams()
  if (params.de) qs.set("de", params.de)
  if (params.ate) qs.set("ate", params.ate)
  if (params.origem) qs.set("origem", params.origem)

  const suffix = qs.toString() ? `?${qs.toString()}` : ""
  return apiJson<MetricasFunilDto>(`/api/candidaturas/metricas${suffix}`)
}
