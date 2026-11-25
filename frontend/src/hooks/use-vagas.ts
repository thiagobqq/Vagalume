import { useCallback, useEffect, useRef, useState } from "react"
import {
  aplicarVaga,
  atualizarStatusVaga,
  cancelarExecucaoBusca,
  executarBusca,
  listarExecucoesBusca,
  listarVagas,
  obterExecucao,
} from "@/features/vagas/api"
import type { ExecucaoBuscaDto, VagaDto } from "@/features/vagas/types"

const INTERVALO_POLLING_MS = 3000
const MAX_FALHAS_POLLING = 5

export function useVagas(statusFiltro: string = "Nova") {
  const [vagas, setVagas] = useState<VagaDto[]>([])
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState<string | null>(null)
  const [ultima, setUltima] = useState<ExecucaoBuscaDto | null>(null)
  const [historicoExecucoes, setHistoricoExecucoes] = useState<ExecucaoBuscaDto[]>([])
  const [execucaoAtual, setExecucaoAtual] = useState<ExecucaoBuscaDto | null>(null)

  const adicionadasRef = useRef<number>(0)
  const executando = !!execucaoAtual && execucaoAtual.finalizadaEm == null
  const execucaoAtivaId = executando ? execucaoAtual.id : null

  const refresh = useCallback(
    async (opts?: { silent?: boolean }) => {
      if (!opts?.silent) setLoading(true)
      setErro(null)
      try {
        setVagas(await listarVagas({ status: statusFiltro, limite: 200 }))
      } catch (e) {
        setErro((e as Error).message)
      } finally {
        if (!opts?.silent) setLoading(false)
      }
    },
    [statusFiltro]
  )

  const carregarUltima = useCallback(async () => {
    try {
      const execucoes = await listarExecucoesBusca()
      setHistoricoExecucoes(execucoes)
      const maisRecente = execucoes[0] ?? null
      setUltima(maisRecente)
      if (maisRecente && maisRecente.finalizadaEm == null) {
        adicionadasRef.current = maisRecente.adicionadas
        setExecucaoAtual(maisRecente)
      }
    } catch {
      /* métricas não bloqueiam a tela */
    }
  }, [])

  useEffect(() => {
    void refresh()
  }, [refresh])

  useEffect(() => {
    void carregarUltima()
  }, [carregarUltima])

  // Polling estável: depende apenas do ID da execução ativa (não recria timer a cada tick)
  useEffect(() => {
    if (!execucaoAtivaId) return
    let ativo = true
    let falhasSeguidas = 0

    const timer = setInterval(async () => {
      try {
        const ex = await obterExecucao(execucaoAtivaId)
        if (!ativo) return
        falhasSeguidas = 0

        if (ex.finalizadaEm != null) {
          setExecucaoAtual(null)
          setUltima(ex)
          await Promise.all([refresh({ silent: true }), carregarUltima()])
        } else {
          setExecucaoAtual(ex)
          if (ex.adicionadas > adicionadasRef.current) {
            adicionadasRef.current = ex.adicionadas
            await refresh({ silent: true })
          }
        }
      } catch {
        falhasSeguidas++
        if (falhasSeguidas >= MAX_FALHAS_POLLING && ativo) {
          setExecucaoAtual(null)
          setErro("Não foi possível acompanhar o status da busca. Tente atualizar a página.")
        }
      }
    }, INTERVALO_POLLING_MS)

    return () => {
      ativo = false
      clearInterval(timer)
    }
  }, [execucaoAtivaId, refresh, carregarUltima])

  const buscarAgora = useCallback(async () => {
    setErro(null)
    try {
      const execucao = await executarBusca()
      if (execucao.finalizadaEm != null) {
        setUltima(execucao)
        await Promise.all([refresh({ silent: true }), carregarUltima()])
        return execucao
      }
      adicionadasRef.current = execucao.adicionadas
      setExecucaoAtual(execucao)
      return execucao
    } catch (e) {
      setErro((e as Error).message)
      throw e
    }
  }, [refresh, carregarUltima])

  const cancelarBusca = useCallback(async () => {
    if (!execucaoAtivaId) return
    setErro(null)
    try {
      const cancelada = await cancelarExecucaoBusca(execucaoAtivaId)
      setExecucaoAtual(null)
      setUltima(cancelada)
      await carregarUltima()
    } catch (e) {
      setErro((e as Error).message)
    }
  }, [execucaoAtivaId, carregarUltima])

  const aplicar = useCallback(async (id: string) => {
    const candidatura = await aplicarVaga(id)
    setVagas((p) => p.filter((v) => v.id !== id))
    return candidatura
  }, [])

  const mudarStatus = useCallback(
    async (id: string, novoStatus: "Nova" | "Vista" | "Descartada") => {
      const atualizada = await atualizarStatusVaga(id, novoStatus)
      if (statusFiltro !== novoStatus) {
        setVagas((p) => p.filter((v) => v.id !== id))
      } else {
        setVagas((p) => p.map((v) => (v.id === id ? atualizada : v)))
      }
      return atualizada
    },
    [statusFiltro]
  )

  const descartar = useCallback(async (id: string) => {
    await mudarStatus(id, "Descartada")
  }, [mudarStatus])

  return {
    vagas,
    loading,
    erro,
    executando,
    ultima,
    historicoExecucoes,
    execucaoAtual,
    refresh,
    buscarAgora,
    cancelarBusca,
    aplicar,
    descartar,
    mudarStatus,
  }
}
