import { useCallback, useEffect, useState } from "react"
import { gerarCompatibilidade, obterCompatibilidade } from "@/features/vagas/api"
import type { AnaliseCompatibilidadeDto } from "@/features/vagas/types"

export function useCompatibilidade(vagaId: string | null) {
  const [analise, setAnalise] = useState<AnaliseCompatibilidadeDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [gerando, setGerando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)

  const carregar = useCallback(async () => {
    if (!vagaId) return
    setLoading(true)
    setErro(null)
    try {
      setAnalise(await obterCompatibilidade(vagaId))
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setLoading(false)
    }
  }, [vagaId])

  useEffect(() => {
    void carregar()
  }, [carregar])

  const gerar = useCallback(async () => {
    if (!vagaId) return
    setGerando(true)
    setErro(null)
    try {
      setAnalise(await gerarCompatibilidade(vagaId))
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setGerando(false)
    }
  }, [vagaId])

  return { analise, loading, gerando, erro, carregar, gerar }
}
