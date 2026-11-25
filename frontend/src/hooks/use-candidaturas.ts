import { useCallback, useEffect, useRef, useState } from "react"
import { ApiError } from "@/lib/api"
import {
  listarCandidaturas,
  criarCandidatura,
  mudarStatus,
  atualizarCandidatura,
  obterCandidatura,
  removerCandidatura,
} from "@/features/candidaturas/api"
import type {
  AtualizarCandidaturaRequest,
  CandidaturaDto,
  CandidaturaDetalhadaDto,
  CriarCandidaturaRequest,
  Status,
} from "@/features/candidaturas/types"

export function useCandidaturas() {
  const [apps, setApps] = useState<CandidaturaDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const appsRef = useRef<CandidaturaDto[]>([])
  appsRef.current = apps

  const refresh = useCallback(async (opts?: { silent?: boolean }) => {
    if (!opts?.silent) setLoading(true)
    setError(null)
    try {
      const data = await listarCandidaturas()
      setApps(Array.isArray(data) ? data : [])
    } catch (e) {
      setError((e as Error).message)
    } finally {
      if (!opts?.silent) setLoading(false)
    }
  }, [])

  useEffect(() => {
    void refresh()
  }, [refresh])

  const move = useCallback(async (id: string, status: Status, observacao?: string) => {
    const anterior = appsRef.current.find((c) => c.id === id)
    if (!anterior) throw new Error("Candidatura não encontrada")

    // Atualização otimista cirúrgica (preserva movimentos paralelos de outros cards)
    setApps((p) => p.map((c) => (c.id === id ? { ...c, status } : c)))
    try {
      const updated = await mudarStatus(id, { status, observacao: observacao || null })
      setApps((p) => p.map((c) => (c.id === updated.id ? updated : c)))
      return updated
    } catch (e) {
      setApps((p) => p.map((c) => (c.id === id ? anterior : c)))
      throw e
    }
  }, [])

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

  const update = useCallback(async (id: string, patch: AtualizarCandidaturaRequest) => {
    const updated = await atualizarCandidatura(id, patch)
    setApps((p) => p.map((c) => (c.id === updated.id ? updated : c)))
    return updated
  }, [])

  const remove = useCallback(async (id: string) => {
    await removerCandidatura(id)
    setApps((p) => p.filter((c) => c.id !== id))
  }, [])

  const getDetail = useCallback(async (id: string): Promise<CandidaturaDetalhadaDto> => {
    return obterCandidatura(id)
  }, [])

  return { apps, loading, error, refresh, move, create, update, remove, getDetail }
}
