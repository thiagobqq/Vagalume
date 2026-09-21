import { useCallback, useEffect, useState } from "react"
import {
  atualizarFonteBusca,
  criarFonteBusca,
  listarFontesBusca,
  obterPerfilBusca,
  removerFonteBusca,
  salvarPerfilBusca,
} from "@/features/vagas/api"
import type {
  CriarFonteBuscaRequest,
  FonteBuscaDto,
  PerfilBuscaDto,
  SalvarPerfilBuscaRequest,
} from "@/features/vagas/types"

export function useBuscaConfig() {
  const [perfil, setPerfil] = useState<PerfilBuscaDto | null>(null)
  const [fontes, setFontes] = useState<FonteBuscaDto[]>([])
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState<string | null>(null)

  const refresh = useCallback(async () => {
    setLoading(true)
    setErro(null)
    try {
      const [p, f] = await Promise.all([obterPerfilBusca(), listarFontesBusca()])
      setPerfil(p)
      setFontes(f)
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void refresh()
  }, [refresh])

  const salvarPerfil = useCallback(async (req: SalvarPerfilBuscaRequest) => {
    const salvo = await salvarPerfilBusca(req)
    setPerfil(salvo)
    return salvo
  }, [])

  const adicionarFonte = useCallback(async (req: CriarFonteBuscaRequest) => {
    const criada = await criarFonteBusca(req)
    setFontes((p) => [...p, criada])
    return criada
  }, [])

  const alternarFonte = useCallback(async (id: string, ativo: boolean) => {
    const atualizada = await atualizarFonteBusca(id, { ativo })
    setFontes((p) => p.map((f) => (f.id === atualizada.id ? atualizada : f)))
    return atualizada
  }, [])

  const removerFonte = useCallback(async (id: string) => {
    await removerFonteBusca(id)
    setFontes((p) => p.filter((f) => f.id !== id))
  }, [])

  return { perfil, fontes, loading, erro, refresh, salvarPerfil, adicionarFonte, alternarFonte, removerFonte }
}
