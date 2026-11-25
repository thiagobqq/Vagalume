import { useCallback, useEffect, useState } from "react"
import {
  atualizarCurriculo,
  downloadCurriculo,
  listarCurriculos,
  marcarCurriculoAtivo,
  removerCurriculo,
  uploadCurriculo,
} from "@/features/candidaturas/api"
import type { CurriculoDto } from "@/features/candidaturas/types"

export function useCurriculos() {
  const [items, setItems] = useState<CurriculoDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const refresh = useCallback(async (opts?: { silent?: boolean }) => {
    if (!opts?.silent) setLoading(true)
    setError(null)
    try {
      const data = await listarCurriculos()
      setItems(Array.isArray(data) ? data : [])
    } catch (e) {
      setError((e as Error).message)
    } finally {
      if (!opts?.silent) setLoading(false)
    }
  }, [])

  useEffect(() => {
    void refresh()
  }, [refresh])

  const upload = useCallback(async (file: File, rotulo?: string) => {
    const created = await uploadCurriculo(file, rotulo)
    setItems((prev) => [created, ...prev.map((c) => ({ ...c, ativo: false }))])
    return created
  }, [])

  const setAtivo = useCallback(async (id: string) => {
    const updated = await marcarCurriculoAtivo(id)
    setItems((prev) => prev.map((c) => ({ ...c, ativo: c.id === updated.id })))
    return updated
  }, [])

  const updateRotulo = useCallback(async (id: string, rotulo: string | null) => {
    const updated = await atualizarCurriculo(id, rotulo)
    setItems((prev) => prev.map((c) => (c.id === updated.id ? { ...c, rotulo: updated.rotulo } : c)))
    return updated
  }, [])

  const remove = useCallback(async (id: string) => {
    await removerCurriculo(id)
    await refresh({ silent: true })
  }, [refresh])

  const download = useCallback(async (id: string, fileName?: string) => {
    await downloadCurriculo(id, fileName)
  }, [])

  return { items, loading, error, refresh, upload, setAtivo, updateRotulo, remove, download }
}
