import { useMemo, useState } from "react"
import { AppHeader } from "@/components/app/app-header"
import { KanbanBoard } from "@/features/applications/kanban-board"
import { AddByLinkDialog } from "@/features/applications/add-by-link-dialog"
import { CandidaturaDetailDialog } from "@/features/candidaturas/candidatura-detail-dialog"
import { CurriculosPanel } from "@/features/curriculos/curriculos-panel"
import { useCandidaturas } from "@/hooks/use-candidaturas"
import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Sparkles, Search, RefreshCw } from "lucide-react"
import { Input } from "@/components/ui/input"

export function Dashboard() {
  const { apps, loading, error, move, create, refresh } = useCandidaturas()
  const [open, setOpen] = useState(false)
  const [detailId, setDetailId] = useState<string | null>(null)
  const [q, setQ] = useState("")
  const [tab, setTab] = useState("candidaturas")

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
    const taxa = total ? Math.round((apps.filter((a) => a.status !== "Aplicado" && a.status !== "Encerrado").length / total) * 100) : 0
    return { total, follow, taxa }
  }, [apps])

  return (
    <div className="min-h-screen bg-[#fef9f0]">
      <AppHeader onAdd={() => setOpen(true)} />

      <main className="max-w-[1280px] mx-auto px-4 sm:px-6 py-6 space-y-5">
        <div className="flex flex-col gap-4">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div>
              <h1 className="text-[22px] font-extrabold tracking-tight leading-none">Suas candidaturas</h1>
              <p className="text-sm text-muted-foreground mt-1">Kanban com status do backend. Clique no card para editar e ver histórico.</p>
            </div>
            <div className="flex gap-2 shrink-0">
              <Button onClick={() => setOpen(true)} className="gap-2">
                <Sparkles className="w-4 h-4" /> Nova candidatura
              </Button>
              <Button variant="outline" onClick={refresh} className="gap-1.5 bg-white" size="sm">
                <RefreshCw className="w-4 h-4" /> atualizar
              </Button>
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div className="rounded-xl border border-foreground/15 bg-white px-4 py-3">
              <div className="text-[11px] font-bold uppercase tracking-widest text-muted-foreground">Total</div>
              <div className="text-xl font-extrabold leading-none mt-1">{stats.total}</div>
              <div className="text-xs text-muted-foreground">vagas anotadas</div>
            </div>
            <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3">
              <div className="text-[11px] font-bold uppercase tracking-widest text-amber-700">Follow-up</div>
              <div className="text-xl font-extrabold leading-none mt-1 text-amber-900">{stats.follow}</div>
              <div className="text-xs text-amber-700/80">precisam de mensagem</div>
            </div>
            <div className="rounded-xl border border-foreground/15 bg-white px-4 py-3">
              <div className="text-[11px] font-bold uppercase tracking-widest text-muted-foreground">Taxa resposta</div>
              <div className="text-xl font-extrabold leading-none mt-1">{stats.taxa}%</div>
              <div className="text-xs text-muted-foreground">triagem em diante</div>
            </div>
          </div>
        </div>

        <Tabs value={tab} onValueChange={setTab}>
          <TabsList className="bg-white border">
            <TabsTrigger value="candidaturas">Candidaturas</TabsTrigger>
            <TabsTrigger value="curriculos">Currículos</TabsTrigger>
          </TabsList>

          <TabsContent value="candidaturas" className="space-y-4 mt-4">
            <div className="flex items-center gap-3">
              <div className="relative flex-1 max-w-md">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                <Input placeholder="Buscar por empresa ou cargo..." value={q} onChange={(e) => setQ(e.target.value)} className="pl-9 bg-white border-foreground/15" />
              </div>
              <span className="hidden sm:inline text-xs text-muted-foreground">{filtered.length} resultados</span>
            </div>

            {error && (
              <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 flex items-center justify-between gap-3">
                <span>{error}</span>
                <Button size="sm" variant="outline" className="bg-white" onClick={refresh}>tentar novamente</Button>
              </div>
            )}

            {loading ? (
              <div className="py-16 text-center">
                <div className="inline-flex items-center gap-2 rounded-full border border-foreground/15 bg-white px-4 py-2 text-sm">
                  <span className="w-2 h-2 rounded-full bg-primary animate-pulse" /> carregando...
                </div>
              </div>
            ) : (
              <KanbanBoard apps={filtered} onMove={move} onOpenDetail={(id) => setDetailId(id)} />
            )}

            <div className="rounded-xl border border-dashed border-foreground/15 bg-white/60 px-4 py-3 flex items-center justify-between gap-3">
              <p className="text-xs text-muted-foreground">
                <span className="font-semibold text-foreground">Backend:</span> status `Aplicado, Triagem, Entrevista, Oferta, Encerrado` • origem obrigatória • eventos append-only.
              </p>
              <span className="text-xs text-muted-foreground hidden sm:inline">vagalume ✦</span>
            </div>
          </TabsContent>

          <TabsContent value="curriculos" className="mt-4">
            <CurriculosPanel />
          </TabsContent>
        </Tabs>
      </main>

      <AddByLinkDialog open={open} onOpenChange={setOpen} onCreate={create} />
      <CandidaturaDetailDialog id={detailId} open={!!detailId} onOpenChange={(o) => { if (!o) setDetailId(null) }} onUpdated={refresh} />
    </div>
  )
}
