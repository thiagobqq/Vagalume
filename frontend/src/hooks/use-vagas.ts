import { useCallback, useEffect, useState } from "react"
import {
  aplicarVaga,
  atualizarStatusVaga,
  executarBusca,
  listarExecucoesBusca,
  listarVagas,
  obterExecucao,
} from "@/features/vagas/api"
import type { ExecucaoBuscaDto, VagaDto } from "@/features/vagas/types"

const INTERVALO_POLLING_MS = 3000

export function useVagas() {
  const [vagas, setVagas] = useState<VagaDto[]>([])
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState<string | null>(null)
  const [ultima, setUltima] = useState<ExecucaoBuscaDto | null>(null)
  const [execucaoAtual, setExecucaoAtual] = useState<ExecucaoBuscaDto | null>(null)

  const executando = !!execucaoAtual && execucaoAtual.finalizadaEm == null

  const refresh = useCallback(async () => {
    setLoading(true)
    setErro(null)
    try {
      setVagas(await listarVagas({ status: "Nova", limite: 200 }))
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setLoading(false)
    }
  }, [])

  const carregarUltima = useCallback(async () => {
    try {
      const execucoes = await listarExecucoesBusca()
      const maisRecente = execucoes[0] ?? null
      setUltima(maisRecente)
      if (maisRecente && maisRecente.finalizadaEm == null) setExecucaoAtual(maisRecente)
    } catch {
      /* métricas não bloqueiam a tela */
    }
  }, [])

  useEffect(() => {
    void refresh()
    void carregarUltima()
  }, [refresh, carregarUltima])

  // acompanha a execução em andamento (disparada aqui ou pelo agendador)
  useEffect(() => {
    if (!execucaoAtual || execucaoAtual.finalizadaEm != null) return
    const id = execucaoAtual.id
    let ativo = true

    const timer = setInterval(async () => {
      try {
        const ex = await obterExecucao(id)
        if (!ativo) return
        if (ex.finalizadaEm != null) {
          setExecucaoAtual(null)
          setUltima(ex)
          await refresh()
        } else {
          setExecucaoAtual(ex)
        }
      } catch {
        /* erro transitório: mantém o polling */
      }
    }, INTERVALO_POLLING_MS)

    return () => {
      ativo = false
      clearInterval(timer)
    }
  }, [execucaoAtual, refresh])

  const buscarAgora = useCallback(async () => {
    setErro(null)
    try {
      const execucao = await executarBusca()
      if (execucao.finalizadaEm != null) {
        setUltima(execucao)
        await refresh()
        return execucao
      }
      setExecucaoAtual(execucao)
      return execucao
    } catch (e) {
      setErro((e as Error).message)
      throw e
    }
  }, [refresh])

  const aplicar = useCallback(async (id: string) => {
    const candidatura = await aplicarVaga(id)
    setVagas((p) => p.filter((v) => v.id !== id))
    return candidatura
  }, [])

  const descartar = useCallback(async (id: string) => {
    await atualizarStatusVaga(id, "Descartada")
    setVagas((p) => p.filter((v) => v.id !== id))
  }, [])

  return { vagas, loading, erro, executando, ultima, execucaoAtual, refresh, buscarAgora, aplicar, descartar }
}
