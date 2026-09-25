import { useCallback, useEffect, useState } from "react"
import { gerarAnaliseAts, obterAnaliseAts } from "@/features/curriculos/api"
import type { AnaliseAtsDto } from "@/features/curriculos/types"

export function useAnaliseAts(curriculoId: string | null) {
  const [analise, setAnalise] = useState<AnaliseAtsDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [carregado, setCarregado] = useState(false)
  const [gerando, setGerando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)

  const carregar = useCallback(async () => {
    if (!curriculoId) {
      setCarregado(false)
      return
    }
    setLoading(true)
    setErro(null)
    try {
      setAnalise(await obterAnaliseAts(curriculoId))
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setLoading(false)
      setCarregado(true)
    }
  }, [curriculoId])

  useEffect(() => {
    void carregar()
  }, [carregar])

  const gerar = useCallback(async () => {
    if (!curriculoId) return
    setGerando(true)
    setErro(null)
    try {
      setAnalise(await gerarAnaliseAts(curriculoId))
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setGerando(false)
    }
  }, [curriculoId])

  return { analise, loading, carregado, gerando, erro, carregar, gerar }
}
