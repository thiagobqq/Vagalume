import { useMemo, useState } from "react"
import { useSearchParams } from "react-router-dom"
import { PageHeader } from "@/components/app/page-header"
import { KanbanBoard } from "@/features/applications/kanban-board"
import { AddByLinkDialog } from "@/features/applications/add-by-link-dialog"
import { CandidaturaDetailDialog } from "@/features/candidaturas/candidatura-detail-dialog"
import { FunilDashboardPanel } from "@/features/metricas/funil-dashboard-panel"
import { useCandidaturas } from "@/hooks/use-candidaturas"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import type { CriarCandidaturaRequest, Status } from "@/features/candidaturas/types"
import { BarChart3, Kanban, Plus, Search, RefreshCw } from "lucide-react"

export function Dashboard() {
  const { apps, loading, error, move, create, refresh } = useCandidaturas()
  const [searchParams, setSearchParams] = useSearchParams()
  const aba = searchParams.get("aba") === "dashboard" ? "dashboard" : "kanban"

  const [open, setOpen] = useState(false)
  const [detailId, setDetailId] = useState<string | null>(null)
  const [q, setQ] = useState("")
  const [actionError, setActionError] = useState<string | null>(null)
  const [syncVersion, setSyncVersion] = useState(0)

  function setAba(next: string) {
    if (next === "dashboard") {
      setSearchParams({ aba: "dashboard" }, { replace: true })
    } else {
      setSearchParams({}, { replace: true })
    }
  }

  const filtered = useMemo(() => {
    if (!q) return apps
    const s = q.toLowerCase()
    return apps.filter((a) => a.empresa.toLowerCase().includes(s) || a.cargo.toLowerCase().includes(s))
  }, [apps, q])

  async function handleMove(id: string, status: Status) {
    setActionError(null)
    try {
      await move(id, status)
      setSyncVersion((v) => v + 1)
    } catch (e) {
      setActionError((e as Error).message || "Não foi possível mover o card")
    }
  }

  async function handleCreate(req: CriarCandidaturaRequest) {
    const created = await create(req)
    setSyncVersion((v) => v + 1)
    return created
  }

  function handleUpdated() {
    void refresh()
    setSyncVersion((v) => v + 1)
  }

  const shownError = actionError ?? error

  return (
    <div className="flex h-full flex-col gap-6">
      <PageHeader title="Candidaturas">
        {aba === "kanban" && (
          <Button variant="outline" size="sm" onClick={refresh} className="gap-1.5 text-muted-foreground hover:text-foreground">
            <RefreshCw className="h-4 w-4" /> atualizar
          </Button>
        )}
        <Button onClick={() => setOpen(true)} className="gap-1.5">
          <Plus className="h-4 w-4" /> Nova candidatura
        </Button>
      </PageHeader>

      <Tabs value={aba} onValueChange={setAba} className="flex min-h-0 flex-1 flex-col gap-4">
        <div className="flex shrink-0 flex-wrap items-center justify-between gap-3">
          <TabsList className="border border-ink/20">
            <TabsTrigger value="kanban" className="gap-1.5">
              <Kanban className="h-3.5 w-3.5" /> Quadro
            </TabsTrigger>
            <TabsTrigger value="dashboard" className="gap-1.5">
              <BarChart3 className="h-3.5 w-3.5" /> Dashboard
            </TabsTrigger>
          </TabsList>

          {aba === "kanban" && (
            <div className="flex min-w-0 flex-1 items-center justify-end gap-3">
              <div className="relative w-full max-w-sm">
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
          )}
        </div>

        <TabsContent value="kanban" className="mt-0 flex min-h-0 flex-1 flex-col gap-4">
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
        </TabsContent>

        <TabsContent value="dashboard" className="mt-0">
          <FunilDashboardPanel
            syncVersion={syncVersion}
            onOpenDetail={(id) => setDetailId(id)}
            onGoToKanban={() => setAba("kanban")}
          />
        </TabsContent>
      </Tabs>

      <AddByLinkDialog open={open} onOpenChange={setOpen} onCreate={handleCreate} />
      <CandidaturaDetailDialog
        id={detailId}
        open={!!detailId}
        onOpenChange={(o) => {
          if (!o) setDetailId(null)
        }}
        onUpdated={handleUpdated}
        onStatusChange={async (status) => {
          if (detailId) await handleMove(detailId, status)
        }}
      />
    </div>
  )
}
