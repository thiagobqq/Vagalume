import { useCallback, useEffect, useRef, useState } from "react"
import { obterMetricasFunil } from "@/features/metricas/api"
import type { MetricasFunilDto, PeriodoFiltro } from "@/features/metricas/types"

function calcularDataInicio(periodo: PeriodoFiltro): string | undefined {
  if (periodo === "all" || periodo === "custom") return undefined
  const dias =
    periodo === "30d" ? 30 :
    periodo === "90d" ? 90 :
    periodo === "180d" ? 180 : 365
  const d = new Date()
  d.setUTCDate(d.getUTCDate() - dias)
  return d.toISOString()
}

export function useMetricasFunil(
  periodo: PeriodoFiltro,
  origem: string,
  customRange?: { de?: string; ate?: string }
) {
  const [data, setData] = useState<MetricasFunilDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const reqIdRef = useRef(0)

  const deCustom = customRange?.de
  const ateCustom = customRange?.ate

  const refresh = useCallback(async () => {
    const reqId = ++reqIdRef.current
    setLoading(true)
    setError(null)
    try {
      const de = periodo === "custom" ? deCustom || undefined : calcularDataInicio(periodo)
      const ate = periodo === "custom" ? ateCustom || undefined : undefined
      const res = await obterMetricasFunil({
        de,
        ate,
        origem: origem === "all" ? undefined : origem,
      })
      if (reqId === reqIdRef.current) setData(res)
    } catch (e) {
      if (reqId === reqIdRef.current) {
        setError((e as Error).message || "Não foi possível carregar as métricas do funil")
      }
    } finally {
      if (reqId === reqIdRef.current) setLoading(false)
    }
  }, [periodo, origem, deCustom, ateCustom])

  useEffect(() => {
    void refresh()
  }, [refresh])

  return { data, loading, error, refresh }
}
