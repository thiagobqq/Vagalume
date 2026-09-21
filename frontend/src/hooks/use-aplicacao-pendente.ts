import { useCallback, useEffect, useState } from "react"
import type { VagaDto } from "@/features/vagas/types"

const STORAGE_KEY = "vagalume:aplicar-pendente"

/** Apply em andamento (aguardando confirmação do usuário) — vive só no navegador. */
export type PendenteVaga = {
  id: string
  titulo: string
  empresa: string
  url: string
  fonteNome: string
  criadoEm: string
}

function ler(): PendenteVaga | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw) as PendenteVaga
    if (!parsed?.id || !parsed?.url) return null
    return parsed
  } catch {
    return null
  }
}

function gravar(p: PendenteVaga | null) {
  try {
    if (p) sessionStorage.setItem(STORAGE_KEY, JSON.stringify(p))
    else sessionStorage.removeItem(STORAGE_KEY)
  } catch {
    /* sessionStorage indisponível: segue apenas em memória */
  }
}

export function useAplicacaoPendente() {
  const [pendente, setPendente] = useState<PendenteVaga | null>(() =>
    typeof window === "undefined" ? null : ler()
  )

  // re-sincroniza ao voltar para a aba (após aplicar no site da vaga)
  useEffect(() => {
    const sync = () => setPendente(ler())
    window.addEventListener("focus", sync)
    document.addEventListener("visibilitychange", sync)
    return () => {
      window.removeEventListener("focus", sync)
      document.removeEventListener("visibilitychange", sync)
    }
  }, [])

  const iniciar = useCallback((vaga: VagaDto) => {
    const p: PendenteVaga = {
      id: vaga.id,
      titulo: vaga.titulo,
      empresa: vaga.empresa,
      url: vaga.url,
      fonteNome: vaga.fonteNome,
      criadoEm: new Date().toISOString(),
    }
    gravar(p)
    setPendente(p)
    return p
  }, [])

  const limpar = useCallback(() => {
    gravar(null)
    setPendente(null)
  }, [])

  return { pendente, iniciar, limpar }
}
