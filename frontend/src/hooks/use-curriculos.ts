import { useCallback, useEffect, useState } from "react"
import { listarCurriculos, uploadCurriculo as apiUpload, marcarCurriculoAtivo, downloadCurriculo as apiDownload } from "@/features/candidaturas/api"
import type { CurriculoDto } from "@/features/candidaturas/types"

export function useCurriculos() {
  const [items, setItems] = useState<CurriculoDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const refresh = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listarCurriculos()
      setItems(Array.isArray(data) ? data : [])
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    refresh()
  }, [refresh])

  const upload = useCallback(async (file: File) => {
    if (file.size > 10 * 1024 * 1024) throw new Error("Arquivo excede o limite de 10 MB")
    if (!file.name.toLowerCase().endsWith(".pdf")) throw new Error("Apenas arquivos PDF são permitidos")
    const created = await apiUpload(file)
    setItems((prev) => [created, ...prev.map((p) => ({ ...p, ativo: false }))])
    // garante que somente o novo fica ativo (backend já desativa)
    return created
  }, [])

  const setAtivo = useCallback(async (id: string) => {
    const updated = await marcarCurriculoAtivo(id)
    setItems((prev) => prev.map((p) => (p.id === id ? updated : { ...p, ativo: false })))
    return updated
  }, [])

  const download = useCallback(async (id: string, nome?: string) => {
    await apiDownload(id, nome)
  }, [])

  return { items, loading, error, refresh, upload, setAtivo, download }
}
