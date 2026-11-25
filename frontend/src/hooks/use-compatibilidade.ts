import { useCallback, useEffect, useRef, useState } from "react"
import { gerarCompatibilidade, obterCompatibilidade } from "@/features/vagas/api"
import type { AnaliseCompatibilidadeDto } from "@/features/vagas/types"

export function useCompatibilidade(vagaId: string | null) {
  const [analise, setAnalise] = useState<AnaliseCompatibilidadeDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [gerando, setGerando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)
  const reqIdRef = useRef(0)

  const carregar = useCallback(async () => {
    const reqId = ++reqIdRef.current
    if (!vagaId) {
      setAnalise(null)
      setErro(null)
      setLoading(false)
      return
    }
    setAnalise(null)
    setLoading(true)
    setErro(null)
    try {
      const res = await obterCompatibilidade(vagaId)
      if (reqId === reqIdRef.current) setAnalise(res)
    } catch (e) {
      if (reqId === reqIdRef.current) setErro((e as Error).message)
    } finally {
      if (reqId === reqIdRef.current) setLoading(false)
    }
  }, [vagaId])

  useEffect(() => {
    void carregar()
  }, [carregar])

  const gerar = useCallback(async () => {
    if (!vagaId) return
    const reqId = ++reqIdRef.current
    setGerando(true)
    setErro(null)
    try {
      const res = await gerarCompatibilidade(vagaId)
      if (reqId === reqIdRef.current) setAnalise(res)
    } catch (e) {
      if (reqId === reqIdRef.current) setErro((e as Error).message)
    } finally {
      if (reqId === reqIdRef.current) setGerando(false)
    }
  }, [vagaId])

  return { analise, loading, gerando, erro, carregar, gerar }
}
