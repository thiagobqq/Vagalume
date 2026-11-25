import { useCallback, useEffect, useRef, useState } from "react"
import { gerarAnaliseAts, obterAnaliseAts } from "@/features/curriculos/api"
import type { AnaliseAtsDto } from "@/features/curriculos/types"

export function useAnaliseAts(curriculoId: string | null) {
  const [analise, setAnalise] = useState<AnaliseAtsDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [carregado, setCarregado] = useState(false)
  const [gerando, setGerando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)
  const reqIdRef = useRef(0)

  const carregar = useCallback(async () => {
    const reqId = ++reqIdRef.current
    if (!curriculoId) {
      setAnalise(null)
      setErro(null)
      setCarregado(false)
      setLoading(false)
      return
    }
    setAnalise(null)
    setLoading(true)
    setCarregado(false)
    setErro(null)
    try {
      const res = await obterAnaliseAts(curriculoId)
      if (reqId === reqIdRef.current) setAnalise(res)
    } catch (e) {
      if (reqId === reqIdRef.current) setErro((e as Error).message)
    } finally {
      if (reqId === reqIdRef.current) {
        setLoading(false)
        setCarregado(true)
      }
    }
  }, [curriculoId])

  useEffect(() => {
    void carregar()
  }, [carregar])

  const gerar = useCallback(async () => {
    if (!curriculoId) return
    const reqId = ++reqIdRef.current
    setGerando(true)
    setErro(null)
    try {
      const res = await gerarAnaliseAts(curriculoId)
      if (reqId === reqIdRef.current) setAnalise(res)
    } catch (e) {
      if (reqId === reqIdRef.current) setErro((e as Error).message)
    } finally {
      if (reqId === reqIdRef.current) setGerando(false)
    }
  }, [curriculoId])

  return { analise, loading, carregado, gerando, erro, carregar, gerar }
}
