import { useMemo, useState } from "react"
import { PageHeader } from "@/components/app/page-header"
import { StatCard } from "@/components/app/stat-card"
import { KanbanBoard } from "@/features/applications/kanban-board"
import { AddByLinkDialog } from "@/features/applications/add-by-link-dialog"
import { CandidaturaDetailDialog } from "@/features/candidaturas/candidatura-detail-dialog"
import { useCandidaturas } from "@/hooks/use-candidaturas"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Plus, Search, RefreshCw, Inbox, BellRing, TrendingUp } from "lucide-react"

export function Dashboard() {
  const { apps, loading, error, move, create, refresh } = useCandidaturas()
  const [open, setOpen] = useState(false)
  const [detailId, setDetailId] = useState<string | null>(null)
  const [q, setQ] = useState("")

  const filtered = useMemo(() => {
    if (!q) return apps
    const s = q.toLowerCase()
    return apps.filter((a) => a.empresa.toLowerCase().includes(s) || a.cargo.toLowerCase().includes(s))
  }, [apps, q])

  const stats = useMemo(() => {
    const total = apps.length
    const follow = apps.filter((a) => {
      const days = Math.floor((Date.now() - new Date(a.criadoEm).getTime()) / 86400000)
      return days >= 7 && a.status !== "Encerrado" && a.status !== "Oferta"
    }).length
    const taxa = total
      ? Math.round((apps.filter((a) => a.status !== "Aplicado" && a.status !== "Encerrado").length / total) * 100)
      : 0
    return { total, follow, taxa }
  }, [apps])

  return (
    <div className="space-y-6">
      <PageHeader
        title="Candidaturas"
        description="Seu funil de vagas em 5 etapas. Clique em um card para editar e ver o histórico."
      >
        <Button variant="outline" size="sm" onClick={refresh} className="gap-1.5 text-muted-foreground hover:text-foreground">
          <RefreshCw className="h-4 w-4" /> atualizar
        </Button>
        <Button onClick={() => setOpen(true)} className="gap-1.5">
          <Plus className="h-4 w-4" /> Nova candidatura
        </Button>
      </PageHeader>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <StatCard label="Total" value={stats.total} hint="vagas anotadas" icon={Inbox} />
        <StatCard label="Follow-up" value={stats.follow} hint="precisam de mensagem" icon={BellRing} tone="warning" />
        <StatCard label="Taxa de resposta" value={`${stats.taxa}%`} hint="triagem em diante" icon={TrendingUp} />
      </div>

      <div className="space-y-4">
        <div className="flex items-center gap-3">
          <div className="relative max-w-sm flex-1">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Buscar por empresa ou cargo..."
              value={q}
              onChange={(e) => setQ(e.target.value)}
              className="pl-9"
            />
          </div>
          {q && (
            <span className="hidden text-xs text-muted-foreground sm:inline">
              {filtered.length} resultado{filtered.length === 1 ? "" : "s"}
            </span>
          )}
        </div>

        {error && (
          <div className="flex items-center justify-between gap-3 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
            <span>{error}</span>
            <Button size="sm" variant="outline" onClick={refresh}>
              tentar novamente
            </Button>
          </div>
        )}

        {loading ? (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
            {Array.from({ length: 5 }).map((_, i) => (
              <div key={i} className="h-[420px] animate-pulse rounded-xl border bg-muted/40" />
            ))}
          </div>
        ) : (
          <KanbanBoard apps={filtered} onMove={move} onOpenDetail={(id) => setDetailId(id)} />
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
      />
    </div>
  )
}
