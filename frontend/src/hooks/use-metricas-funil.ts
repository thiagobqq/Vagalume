import { useCallback, useEffect, useState } from "react"
import { obterMetricasFunil } from "@/features/metricas/api"
import type { MetricasFunilDto, PeriodoFiltro } from "@/features/metricas/types"

function calcularDataInicio(periodo: PeriodoFiltro): string | undefined {
  if (periodo === "all") return undefined
  const dias =
    periodo === "30d" ? 30 :
    periodo === "90d" ? 90 :
    periodo === "180d" ? 180 : 365
  const d = new Date()
  d.setUTCDate(d.getUTCDate() - dias)
  return d.toISOString()
}

export function useMetricasFunil(periodo: PeriodoFiltro, origem: string) {
  const [data, setData] = useState<MetricasFunilDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const refresh = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await obterMetricasFunil({
        de: calcularDataInicio(periodo),
        origem: origem === "all" ? undefined : origem,
      })
      setData(res)
    } catch (e) {
      setError((e as Error).message || "Não foi possível carregar as métricas do funil")
    } finally {
      setLoading(false)
    }
  }, [periodo, origem])

  useEffect(() => {
    refresh()
  }, [refresh])

  return { data, loading, error, refresh }
}
