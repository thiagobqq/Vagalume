import { useMemo, useState } from "react"
import { useSearchParams } from "react-router-dom"
import { PageHeader } from "@/components/app/page-header"
import { OnboardingBanner } from "@/components/app/onboarding-banner"
import { KanbanBoard } from "@/features/applications/kanban-board"
import { AddByLinkDialog } from "@/features/applications/add-by-link-dialog"
import { CandidaturaDetailDialog } from "@/features/candidaturas/candidatura-detail-dialog"
import { FunilDashboardPanel } from "@/features/metricas/funil-dashboard-panel"
import { useCandidaturas } from "@/hooks/use-candidaturas"
import { useCurriculos } from "@/hooks/use-curriculos"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import {
  ORIGEM_LABEL,
  ORIGEM_VALUES,
  type CriarCandidaturaRequest,
  type Status,
} from "@/features/candidaturas/types"
import { BarChart3, Clock3, Kanban, Plus, RefreshCw, Search } from "lucide-react"

function diasSemAtualizacao(iso: string) {
  return Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 86400000))
}

export function Dashboard() {
  const { apps, loading, error, move, create, remove, refresh } = useCandidaturas()
  const { items: curriculos } = useCurriculos()
  const [searchParams, setSearchParams] = useSearchParams()
  const aba = searchParams.get("aba") === "dashboard" ? "dashboard" : "kanban"

  const [open, setOpen] = useState(false)
  const [detailId, setDetailId] = useState<string | null>(null)
  const [q, setQ] = useState("")
  const [origemFiltro, setOrigemFiltro] = useState<string>("all")
  const [somenteFollowUp, setSomenteFollowUp] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [syncVersion, setSyncVersion] = useState(0)

  function setAba(next: string) {
    if (next === "dashboard") {
      setSearchParams({ aba: "dashboard" }, { replace: true })
    } else {
      setSearchParams({}, { replace: true })
    }
  }

  const pendentesFollowUpCount = useMemo(
    () =>
      apps.filter(
        (a) =>
          a.status !== "Encerrado" &&
          a.status !== "Oferta" &&
          diasSemAtualizacao(a.atualizadoEm ?? a.criadoEm) >= 7
      ).length,
    [apps]
  )

  const filtered = useMemo(() => {
    const s = q.trim().toLowerCase()
    return apps.filter((a) => {
      if (origemFiltro !== "all" && a.origem !== origemFiltro) return false
      if (somenteFollowUp) {
        if (a.status === "Encerrado" || a.status === "Oferta") return false
        if (diasSemAtualizacao(a.atualizadoEm ?? a.criadoEm) < 7) return false
      }
      if (s && !a.empresa.toLowerCase().includes(s) && !a.cargo.toLowerCase().includes(s)) return false
      return true
    })
  }, [apps, q, origemFiltro, somenteFollowUp])

  async function handleMove(id: string, status: Status, observacao?: string, rethrow = false) {
    setActionError(null)
    try {
      await move(id, status, observacao)
      setSyncVersion((v) => v + 1)
    } catch (e) {
      const msg = (e as Error).message || "Não foi possível mover o card"
      if (rethrow) throw new Error(msg)
      setActionError(msg)
    }
  }

  async function handleCreate(req: CriarCandidaturaRequest) {
    const created = await create(req)
    setSyncVersion((v) => v + 1)
    return created
  }

  async function handleDelete(id: string) {
    await remove(id)
    setDetailId(null)
    setSyncVersion((v) => v + 1)
  }

  function handleUpdated() {
    void refresh({ silent: true })
    setSyncVersion((v) => v + 1)
  }

  function handleRetry() {
    setActionError(null)
    void refresh()
  }

  const shownError = actionError ?? error
  const temFiltroAtivo = !!q.trim() || origemFiltro !== "all" || somenteFollowUp

  return (
    <div className="flex h-full flex-col gap-5">
      <PageHeader title="Candidaturas">
        {aba === "kanban" && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => void refresh()}
            className="gap-1.5 text-muted-foreground hover:text-foreground"
          >
            <RefreshCw className="h-4 w-4" /> atualizar
          </Button>
        )}
        <Button onClick={() => setOpen(true)} className="gap-1.5">
          <Plus className="h-4 w-4" /> Nova candidatura
        </Button>
      </PageHeader>

      {!loading && (
        <OnboardingBanner
          temCandidatura={apps.length > 0}
          temCurriculo={curriculos.length > 0}
          onNovaCandidatura={() => setOpen(true)}
        />
      )}

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
            <div className="flex min-w-0 flex-1 flex-wrap items-center justify-end gap-2">
              <div className="relative w-full sm:max-w-xs">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder="Buscar por empresa ou cargo..."
                  value={q}
                  onChange={(e) => setQ(e.target.value)}
                  className="border-ink/70 bg-card pl-9 shadow-hard-xs"
                />
              </div>

              <Select value={origemFiltro} onValueChange={setOrigemFiltro}>
                <SelectTrigger className="h-9 w-[150px] border-ink/70 bg-card text-xs shadow-hard-xs">
                  <SelectValue placeholder="Todas origens" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todas origens</SelectItem>
                  {ORIGEM_VALUES.map((o) => (
                    <SelectItem key={o} value={o}>
                      {ORIGEM_LABEL[o]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>

              <Button
                type="button"
                variant={somenteFollowUp ? "secondary" : "outline"}
                size="sm"
                onClick={() => setSomenteFollowUp((v) => !v)}
                className="h-9 gap-1.5 border-ink/70 text-xs shadow-hard-xs"
              >
                <Clock3 className="h-3.5 w-3.5" />
                Follow-up {pendentesFollowUpCount > 0 && `(${pendentesFollowUpCount})`}
              </Button>

              {temFiltroAtivo && (
                <span className="text-xs text-muted-foreground">
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
              <Button size="sm" variant="outline" onClick={handleRetry}>
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
            <KanbanBoard
              apps={filtered}
              onMove={(id, st) => void handleMove(id, st)}
              onOpenDetail={(id) => setDetailId(id)}
            />
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
        onDelete={handleDelete}
        onStatusChange={async (status, observacao) => {
          if (detailId) await handleMove(detailId, status, observacao, true)
        }}
      />
    </div>
  )
}
