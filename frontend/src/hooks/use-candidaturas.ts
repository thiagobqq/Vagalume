import { useCallback, useEffect, useState } from "react"
import { ApiError } from "@/lib/api"
import { listarCandidaturas, criarCandidatura, mudarStatus, atualizarCandidatura, obterCandidatura } from "@/features/candidaturas/api"
import type { CandidaturaDto, CandidaturaDetalhadaDto, CriarCandidaturaRequest, Status } from "@/features/candidaturas/types"

export function useCandidaturas() {
  const [apps, setApps] = useState<CandidaturaDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const refresh = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listarCandidaturas()
      setApps(Array.isArray(data) ? data : [])
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    refresh()
  }, [refresh])

  const move = useCallback(async (id: string, status: Status, observacao?: string) => {
    //乐观更新
    const prev = apps
    setApps((p) => p.map((c) => (c.id === id ? { ...c, status } : c)))
    try {
      const updated = await mudarStatus(id, { status, observacao: observacao || null })
      setApps((p) => p.map((c) => (c.id === updated.id ? updated : c)))
      return updated
    } catch (e) {
      setApps(prev)
      throw e
    }
  }, [apps])

  const create = useCallback(async (req: CriarCandidaturaRequest) => {
    try {
      const created = await criarCandidatura(req)
      setApps((p) => [created, ...p])
      return created
    } catch (e) {
      if (e instanceof ApiError) throw e
      throw e
    }
  }, [])

  const update = useCallback(async (id: string, patch: { empresa?: string; cargo?: string; link?: string | null; origem?: string; observacoes?: string | null }) => {
    const updated = await atualizarCandidatura(id, patch)
    setApps((p) => p.map((c) => (c.id === updated.id ? updated : c)))
    return updated
  }, [])

  const getDetail = useCallback(async (id: string): Promise<CandidaturaDetalhadaDto> => {
    return obterCandidatura(id)
  }, [])

  return { apps, loading, error, refresh, move, create, update, getDetail }
}
