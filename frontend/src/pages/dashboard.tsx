import { useMemo, useState } from "react"
import { PageHeader } from "@/components/app/page-header"
import { KanbanBoard } from "@/features/applications/kanban-board"
import { AddByLinkDialog } from "@/features/applications/add-by-link-dialog"
import { CandidaturaDetailDialog } from "@/features/candidaturas/candidatura-detail-dialog"
import { useCandidaturas } from "@/hooks/use-candidaturas"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import type { Status } from "@/features/candidaturas/types"
import { Plus, Search, RefreshCw } from "lucide-react"

export function Dashboard() {
  const { apps, loading, error, move, create, refresh } = useCandidaturas()
  const [open, setOpen] = useState(false)
  const [detailId, setDetailId] = useState<string | null>(null)
  const [q, setQ] = useState("")
  const [actionError, setActionError] = useState<string | null>(null)

  const filtered = useMemo(() => {
    if (!q) return apps
    const s = q.toLowerCase()
    return apps.filter((a) => a.empresa.toLowerCase().includes(s) || a.cargo.toLowerCase().includes(s))
  }, [apps, q])

  async function handleMove(id: string, status: Status) {
    setActionError(null)
    try {
      await move(id, status)
    } catch (e) {
      setActionError((e as Error).message || "Não foi possível mover o card")
    }
  }

  const shownError = actionError ?? error

  return (
    <div className="flex h-full flex-col gap-6">
      <PageHeader
        title="Candidaturas"
        description="Seu funil de vagas em 5 etapas. Arraste os cards entre as colunas; clique para editar e ver o histórico."
      >
        <Button variant="outline" size="sm" onClick={refresh} className="gap-1.5 text-muted-foreground hover:text-foreground">
          <RefreshCw className="h-4 w-4" /> atualizar
        </Button>
        <Button onClick={() => setOpen(true)} className="gap-1.5">
          <Plus className="h-4 w-4" /> Nova candidatura
        </Button>
      </PageHeader>

      <div className="flex min-h-0 flex-1 flex-col gap-4">
        <div className="flex shrink-0 items-center gap-3">
          <div className="relative max-w-sm flex-1">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Buscar por empresa ou cargo..."
              value={q}
              onChange={(e) => setQ(e.target.value)}
              className="border-ink/70 bg-card pl-9 shadow-hard-xs"
            />
          </div>
          {q && (
            <span className="hidden text-xs text-muted-foreground sm:inline">
              {filtered.length} resultado{filtered.length === 1 ? "" : "s"}
            </span>
          )}
        </div>

        {shownError && (
          <div className="flex shrink-0 items-center justify-between gap-3 rounded-xl border-[1.5px] border-destructive/40 bg-destructive/5 px-4 py-3 text-sm text-destructive">
            <span>{shownError}</span>
            <Button size="sm" variant="outline" onClick={refresh}>
              tentar novamente
            </Button>
          </div>
        )}

        {loading ? (
          <div className="grid shrink-0 grid-cols-1 gap-4 lg:grid-cols-5">
            {Array.from({ length: 5 }).map((_, i) => (
              <div
                key={i}
                className={`h-[480px] animate-pulse rounded-2xl border-[1.5px] border-ink/20 bg-muted/40 ${
                  i > 0 ? "hidden lg:block" : ""
                }`}
              />
            ))}
          </div>
        ) : (
          <KanbanBoard apps={filtered} onMove={handleMove} onOpenDetail={(id) => setDetailId(id)} />
        )}
      </div>

      <AddByLinkDialog open={open} onOpenChange={setOpen} onCreate={create} />
      <CandidaturaDetailDialog
        id={detailId}
        open={!!detailId}
        onOpenChange={(o) => {
          if (!o) setDetailId(null)
        }}
        onUpdated={refresh}
        onStatusChange={async (status) => {
          if (detailId) await move(detailId, status)
        }}
      />
    </div>
  )
}
