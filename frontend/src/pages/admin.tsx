import { useCallback, useEffect, useMemo, useRef, useState } from "react"
import { PageHeader } from "@/components/app/page-header"
import { Panel } from "@/components/app/panel"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import { useAuth } from "@/features/auth/auth-context"
import { PERMISSAO_LABEL, PERMISSOES } from "@/features/auth/permissoes"
import {
  concederPermissao,
  listarCatalogo,
  listarUsuarios,
  revogarPermissao,
} from "@/features/admin/api"
import type { PermissaoDto, UsuarioPermissoesDto } from "@/features/admin/types"
import { Clock3, Loader2, Plus, Search, X } from "lucide-react"

function initials(nome: string) {
  const parts = nome.trim().split(/\s+/)
  return ((parts[0]?.[0] ?? "") + (parts[1]?.[0] ?? "")).toUpperCase() || "?"
}

export function AdminPage() {
  const { user: eu } = useAuth()
  const [busca, setBusca] = useState("")
  const [somentePendentes, setSomentePendentes] = useState(false)
  const [usuarios, setUsuarios] = useState<UsuarioPermissoesDto[]>([])
  const [catalogo, setCatalogo] = useState<PermissaoDto[]>([])
  const [carregando, setCarregando] = useState(true)
  const [erro, setErro] = useState<string | null>(null)
  const [ocupados, setOcupados] = useState<Set<string>>(() => new Set())
  const reqIdRef = useRef(0)

  useEffect(() => {
    listarCatalogo()
      .then(setCatalogo)
      .catch((e) => setErro(e instanceof Error ? e.message : "Falha ao carregar permissões"))
  }, [])

  const carregar = useCallback(async (termo: string, pendentes: boolean) => {
    const reqId = ++reqIdRef.current
    setCarregando(true)
    try {
      const lista = await listarUsuarios(termo, pendentes)
      if (reqId === reqIdRef.current) {
        setUsuarios(lista)
        setErro(null)
      }
    } catch (e) {
      if (reqId === reqIdRef.current) {
        setErro(e instanceof Error ? e.message : "Falha ao carregar usuários")
      }
    } finally {
      if (reqId === reqIdRef.current) setCarregando(false)
    }
  }, [])

  useEffect(() => {
    const t = setTimeout(() => void carregar(busca, somentePendentes), 250)
    return () => clearTimeout(t)
  }, [busca, somentePendentes, carregar])

  const chaves = useMemo(() => {
    if (catalogo.length > 0) return catalogo.map((c) => c.chave)
    return Array.from(new Set(usuarios.flatMap((u) => u.permissoes)))
  }, [catalogo, usuarios])

  const pendentesCount = useMemo(
    () => usuarios.filter((u) => u.solicitacaoPendente).length,
    [usuarios]
  )

  const alternar = async (usuario: UsuarioPermissoesDto, chave: string, tem: boolean) => {
    if (tem && chave === PERMISSOES.admin && usuario.id === eu?.id) {
      setErro("Você não pode revogar sua própria permissão de administrador.")
      return
    }
    const chaveOcupada = `${usuario.id}:${chave}`
    setOcupados((prev) => new Set(prev).add(chaveOcupada))
    try {
      const atualizado = tem
        ? await revogarPermissao(usuario.id, chave)
        : await concederPermissao(usuario.id, chave)
      setUsuarios((prev) => prev.map((u) => (u.id === atualizado.id ? atualizado : u)))
      setErro(null)
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Falha ao atualizar permissão")
    } finally {
      setOcupados((prev) => {
        const next = new Set(prev)
        next.delete(chaveOcupada)
        return next
      })
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Permissões" />

      <div className="flex flex-wrap items-center gap-3">
        <div className="relative w-full max-w-sm">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={busca}
            onChange={(e) => setBusca(e.target.value)}
            placeholder="Buscar por nome ou e-mail"
            className="pl-9"
          />
        </div>
        <Button
          type="button"
          variant={somentePendentes ? "secondary" : "outline"}
          size="sm"
          className="gap-1.5"
          onClick={() => setSomentePendentes((v) => !v)}
        >
          <Clock3 className="h-3.5 w-3.5" />
          Pedidos de verificação {pendentesCount > 0 && `(${pendentesCount})`}
        </Button>
      </div>

      {erro && (
        <div className="rounded-lg border border-destructive/40 bg-destructive/10 px-4 py-2 text-sm text-destructive">
          {erro}
        </div>
      )}

      <Panel>
        {carregando ? (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" /> carregando usuários...
          </div>
        ) : usuarios.length === 0 ? (
          <div className="py-16 text-center text-sm text-muted-foreground">
            {somentePendentes ? "Nenhum pedido de verificação pendente." : "Nenhum usuário encontrado."}
          </div>
        ) : (
          <ul className="divide-y">
            {usuarios.map((u) => (
              <li key={u.id} className="flex flex-col gap-3 px-4 py-4 sm:flex-row sm:items-center sm:gap-4">
                <div className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-secondary text-xs font-bold">
                  {initials(u.nome)}
                </div>
                <div className="min-w-0 sm:w-64 sm:shrink-0">
                  <div className="flex items-center gap-2">
                    <span className="truncate text-sm font-semibold">{u.nome}</span>
                    {u.solicitacaoPendente && (
                      <Badge variant="outline" className="shrink-0 border-amber-500/40 bg-amber-500/10 text-[10px] text-amber-800 dark:text-amber-300">
                        pediu acesso
                      </Badge>
                    )}
                  </div>
                  <div className="truncate text-xs text-muted-foreground">{u.email}</div>
                  {u.solicitacaoPendente && u.solicitacaoMensagem && (
                    <div className="mt-1 line-clamp-2 text-[11px] italic text-muted-foreground">
                      “{u.solicitacaoMensagem}”
                    </div>
                  )}
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  {chaves.map((chave) => {
                    const tem = u.permissoes.includes(chave)
                    const chaveOcupada = `${u.id}:${chave}`
                    const isSelfAdmin = tem && chave === PERMISSOES.admin && u.id === eu?.id
                    const carregandoItem = ocupados.has(chaveOcupada)
                    return tem ? (
                      <Badge key={chave} variant="secondary" className="gap-1 pr-1">
                        {PERMISSAO_LABEL[chave] ?? chave}
                        {!isSelfAdmin && (
                          <button
                            type="button"
                            aria-label={`Revogar ${chave}`}
                            disabled={carregandoItem}
                            onClick={() => void alternar(u, chave, true)}
                            className="grid h-4 w-4 place-items-center rounded-full hover:bg-foreground/10 disabled:opacity-50"
                          >
                            {carregandoItem ? (
                              <Loader2 className="h-3 w-3 animate-spin" />
                            ) : (
                              <X className="h-3 w-3" />
                            )}
                          </button>
                        )}
                      </Badge>
                    ) : (
                      <Button
                        key={chave}
                        variant="outline"
                        size="sm"
                        className="h-6 gap-1 rounded-full px-2 text-xs text-muted-foreground"
                        disabled={carregandoItem}
                        onClick={() => void alternar(u, chave, false)}
                      >
                        {carregandoItem ? (
                          <Loader2 className="h-3 w-3 animate-spin" />
                        ) : (
                          <Plus className="h-3 w-3" />
                        )}
                        {PERMISSAO_LABEL[chave] ?? chave}
                      </Button>
                    )
                  })}
                </div>
              </li>
            ))}
          </ul>
        )}
      </Panel>
    </div>
  )
}
