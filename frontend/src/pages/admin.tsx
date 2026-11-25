import { useCallback, useEffect, useMemo, useState } from "react"
import { PageHeader } from "@/components/app/page-header"
import { Panel } from "@/components/app/panel"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import { PERMISSAO_LABEL } from "@/features/auth/permissoes"
import {
  concederPermissao,
  listarCatalogo,
  listarUsuarios,
  revogarPermissao,
} from "@/features/admin/api"
import type { PermissaoDto, UsuarioPermissoesDto } from "@/features/admin/types"
import { Loader2, Plus, Search, X } from "lucide-react"

function initials(nome: string) {
  const parts = nome.trim().split(/\s+/)
  return ((parts[0]?.[0] ?? "") + (parts[1]?.[0] ?? "")).toUpperCase() || "?"
}

export function AdminPage() {
  const [busca, setBusca] = useState("")
  const [usuarios, setUsuarios] = useState<UsuarioPermissoesDto[]>([])
  const [catalogo, setCatalogo] = useState<PermissaoDto[]>([])
  const [carregando, setCarregando] = useState(true)
  const [erro, setErro] = useState<string | null>(null)
  const [ocupado, setOcupado] = useState<string | null>(null)

  useEffect(() => {
    listarCatalogo()
      .then(setCatalogo)
      .catch((e) => setErro(e instanceof Error ? e.message : "Falha ao carregar permissões"))
  }, [])

  const carregar = useCallback(async (termo: string) => {
    setCarregando(true)
    try {
      setUsuarios(await listarUsuarios(termo))
      setErro(null)
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Falha ao carregar usuários")
    } finally {
      setCarregando(false)
    }
  }, [])

  useEffect(() => {
    const t = setTimeout(() => void carregar(busca), 300)
    return () => clearTimeout(t)
  }, [busca, carregar])

  const chaves = useMemo(() => {
    if (catalogo.length > 0) return catalogo.map((c) => c.chave)
    return Array.from(new Set(usuarios.flatMap((u) => u.permissoes)))
  }, [catalogo, usuarios])

  const alternar = async (usuario: UsuarioPermissoesDto, chave: string, tem: boolean) => {
    setOcupado(`${usuario.id}:${chave}`)
    try {
      const atualizado = tem
        ? await revogarPermissao(usuario.id, chave)
        : await concederPermissao(usuario.id, chave)
      setUsuarios((prev) => prev.map((u) => (u.id === atualizado.id ? atualizado : u)))
      setErro(null)
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Falha ao atualizar permissão")
    } finally {
      setOcupado(null)
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Permissões" />

      <div className="relative max-w-sm">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          value={busca}
          onChange={(e) => setBusca(e.target.value)}
          placeholder="Buscar por nome ou e-mail"
          className="pl-9"
        />
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
          <div className="py-16 text-center text-sm text-muted-foreground">Nenhum usuário encontrado.</div>
        ) : (
          <ul className="divide-y">
            {usuarios.map((u) => (
              <li key={u.id} className="flex flex-col gap-3 px-4 py-4 sm:flex-row sm:items-center sm:gap-4">
                <div className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-secondary text-xs font-bold">
                  {initials(u.nome)}
                </div>
                <div className="min-w-0 sm:w-56 sm:shrink-0">
                  <div className="truncate text-sm font-semibold">{u.nome}</div>
                  <div className="truncate text-xs text-muted-foreground">{u.email}</div>
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  {chaves.map((chave) => {
                    const tem = u.permissoes.includes(chave)
                    const chaveOcupada = `${u.id}:${chave}`
                    return tem ? (
                      <Badge key={chave} variant="secondary" className="gap-1 pr-1">
                        {PERMISSAO_LABEL[chave] ?? chave}
                        <button
                          type="button"
                          aria-label={`Revogar ${chave}`}
                          disabled={ocupado === chaveOcupada}
                          onClick={() => void alternar(u, chave, true)}
                          className="grid h-4 w-4 place-items-center rounded-full hover:bg-foreground/10 disabled:opacity-50"
                        >
                          {ocupado === chaveOcupada ? (
                            <Loader2 className="h-3 w-3 animate-spin" />
                          ) : (
                            <X className="h-3 w-3" />
                          )}
                        </button>
                      </Badge>
                    ) : (
                      <Button
                        key={chave}
                        variant="outline"
                        size="sm"
                        className="h-6 gap-1 rounded-full px-2 text-xs text-muted-foreground"
                        disabled={ocupado === chaveOcupada}
                        onClick={() => void alternar(u, chave, false)}
                      >
                        {ocupado === chaveOcupada ? (
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
