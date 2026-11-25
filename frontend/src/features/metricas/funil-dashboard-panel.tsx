import { useEffect, useMemo, useState } from "react"
import { NotebookCard, NotebookPanel } from "@/components/app/notebook-panel"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { SankeyFunil } from "@/features/metricas/sankey-funil"
import { SANKEY_NODE_META } from "@/features/metricas/sankey-meta"
import type { CandidaturaFluxoDto, PeriodoFiltro, SelecaoSankey } from "@/features/metricas/types"
import { useMetricasFunil } from "@/hooks/use-metricas-funil"
import { ORIGEM_LABEL, ORIGEM_VALUES, STATUS_LABEL, type Origem, type Status } from "@/features/candidaturas/types"
import { exportarCandidaturasCsv } from "@/features/candidaturas/api"
import { STATUS_STYLE } from "@/features/candidaturas/status-style"
import {
  AlertTriangle,
  ArrowRight,
  CheckCircle2,
  Clock,
  Download,
  Filter,
  GitBranch,
  Info,
  Kanban,
  RefreshCw,
  RotateCcw,
  Sparkles,
  TrendingUp,
  XCircle,
} from "lucide-react"

const PERIODO_LABELS: Record<PeriodoFiltro, string> = {
  all: "Todo o período",
  "30d": "Últimos 30 dias",
  "90d": "Últimos 90 dias",
  "180d": "Últimos 6 meses",
  "365d": "Último ano",
  custom: "Personalizado",
}

function formatPct(value: number) {
  return `${value.toLocaleString("pt-BR", { maximumFractionDigits: 1 })}%`
}

function formatHoras(horas: number | null) {
  if (horas === null || Number.isNaN(horas)) return "—"
  if (horas < 1) return "< 1h"
  if (horas < 24) return `${Math.round(horas)}h`
  const dias = horas / 24
  return `${dias.toLocaleString("pt-BR", { maximumFractionDigits: 1 })}d`
}

function formatDias(dias: number | null) {
  if (dias === null || Number.isNaN(dias)) return "—"
  if (dias < 1) {
    const horas = Math.max(1, Math.round(dias * 24))
    return `${horas}h`
  }
  return `${dias.toLocaleString("pt-BR", { maximumFractionDigits: 1 })} dias`
}

function getStatusColor(status: string) {
  return SANKEY_NODE_META[status]?.color ?? STATUS_STYLE[status as Status]?.dot ?? "var(--muted-foreground)"
}

function getNodeLabel(id: string) {
  return SANKEY_NODE_META[id]?.label ?? STATUS_LABEL[id as Status] ?? id
}

function getOrigemLabel(origem: string) {
  return ORIGEM_LABEL[origem as Origem] ?? origem
}

function candidaturaPertenceASelecao(c: CandidaturaFluxoDto, selecao: SelecaoSankey): boolean {
  if (!selecao) return false
  if (selecao.tipo === "fluxo") {
    const { origem, destino } = selecao
    if (destino === "Aguardando" || destino === "EmAndamento") {
      return c.destinoFinalSankey === destino && c.caminho[c.caminho.length - 1] === origem
    }
    for (let i = 0; i < c.caminho.length - 1; i++) {
      if (c.caminho[i] === origem && c.caminho[i + 1] === destino) return true
    }
    return false
  }

  if (selecao.id === "Aguardando" || selecao.id === "EmAndamento") {
    return c.destinoFinalSankey === selecao.id
  }
  return c.caminho.includes(selecao.id)
}

type Props = {
  /** incrementa quando o Kanban cria, edita ou move uma candidatura */
  syncVersion?: number
  onOpenDetail: (id: string) => void
  onGoToKanban?: () => void
}

export function FunilDashboardPanel({ syncVersion = 0, onOpenDetail, onGoToKanban }: Props) {
  const [periodo, setPeriodo] = useState<PeriodoFiltro>("all")
  const [dataDe, setDataDe] = useState<string>("")
  const [dataAte, setDataAte] = useState<string>("")
  const [origem, setOrigem] = useState<string>("all")
  const [selecao, setSelecao] = useState<SelecaoSankey>(null)
  const [exportingCsv, setExportingCsv] = useState(false)
  const [exportErr, setExportErr] = useState<string | null>(null)

  const { data, loading, error, refresh } = useMetricasFunil(periodo, origem, {
    de: dataDe || undefined,
    ate: dataAte || undefined,
  })

  useEffect(() => {
    if (syncVersion > 0) void refresh()
  }, [syncVersion, refresh])

  const candidaturasFiltradas = useMemo(() => {
    if (!data || !selecao) return []
    return data.candidaturas.filter((c) => candidaturaPertenceASelecao(c, selecao))
  }, [data, selecao])

  const taxaDiretasEncerradas = useMemo(() => {
    if (!data || data.totalCandidaturas === 0) return 0
    return Math.round((data.diretasEncerradas / data.totalCandidaturas) * 1000) / 10
  }, [data])

  const taxaTriagem = useMemo(() => {
    if (!data || data.totalCandidaturas === 0) return 0
    return Math.round((data.avancaramParaTriagem / data.totalCandidaturas) * 1000) / 10
  }, [data])

  const taxaEntrevista = useMemo(() => {
    if (!data || data.totalCandidaturas === 0) return 0
    return Math.round((data.chegaramEntrevista / data.totalCandidaturas) * 1000) / 10
  }, [data])

  const pularamTriagem = useMemo(() => {
    if (!data) return 0
    return Math.max(0, data.avancaramDeAplicado - data.avancaramParaTriagem)
  }, [data])

  const selecaoTitulo = useMemo(() => {
    if (!selecao) return ""
    if (selecao.tipo === "fluxo") {
      return `${getNodeLabel(selecao.origem)} → ${getNodeLabel(selecao.destino)}`
    }
    return getNodeLabel(selecao.id)
  }, [selecao])

  function handlePeriodoChange(next: string) {
    setSelecao(null)
    setPeriodo(next as PeriodoFiltro)
  }

  function handleOrigemChange(next: string) {
    setSelecao(null)
    setOrigem(next)
  }

  async function handleExportarCsv() {
    setExportingCsv(true)
    setExportErr(null)
    try {
      await exportarCandidaturasCsv()
    } catch (e) {
      setExportErr((e as Error).message || "Falha ao exportar CSV")
    } finally {
      setExportingCsv(false)
    }
  }

  return (
    <div className="space-y-6 pb-8">
      {/* Barra de filtros */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <Select value={periodo} onValueChange={handlePeriodoChange}>
            <SelectTrigger className="h-9 w-[165px] border-ink/70 bg-card text-xs shadow-hard-xs">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {(Object.keys(PERIODO_LABELS) as PeriodoFiltro[]).map((key) => (
                <SelectItem key={key} value={key}>
                  {PERIODO_LABELS[key]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          {periodo === "custom" && (
            <div className="flex flex-wrap items-center gap-1.5 text-xs">
              <input
                type="date"
                aria-label="Data inicial"
                value={dataDe}
                onChange={(e) => {
                  setSelecao(null)
                  setDataDe(e.target.value)
                }}
                className="h-9 rounded-lg border border-ink/70 bg-card px-2 text-xs shadow-hard-xs"
              />
              <span className="text-muted-foreground">até</span>
              <input
                type="date"
                aria-label="Data final"
                value={dataAte}
                onChange={(e) => {
                  setSelecao(null)
                  setDataAte(e.target.value)
                }}
                className="h-9 rounded-lg border border-ink/70 bg-card px-2 text-xs shadow-hard-xs"
              />
            </div>
          )}

          <Select value={origem} onValueChange={handleOrigemChange}>
            <SelectTrigger className="h-9 w-[170px] border-ink/70 bg-card text-xs shadow-hard-xs">
              <Filter className="mr-1.5 h-3.5 w-3.5 text-muted-foreground" />
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todas as origens</SelectItem>
              {ORIGEM_VALUES.map((o) => (
                <SelectItem key={o} value={o}>
                  {ORIGEM_LABEL[o]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          {(periodo !== "all" || origem !== "all") && (
            <Button
              variant="ghost"
              size="sm"
              className="h-9 text-xs text-muted-foreground hover:text-foreground"
              onClick={() => {
                setSelecao(null)
                setPeriodo("all")
                setDataDe("")
                setDataAte("")
                setOrigem("all")
              }}
            >
              limpar filtros
            </Button>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleExportarCsv}
            disabled={exportingCsv}
            className="gap-1.5 text-muted-foreground hover:text-foreground"
          >
            <Download className="h-4 w-4" /> {exportingCsv ? "exportando..." : "exportar CSV"}
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={refresh}
            className="gap-1.5 text-muted-foreground hover:text-foreground"
          >
            <RefreshCw className="h-4 w-4" /> atualizar métricas
          </Button>
        </div>
      </div>

      {exportErr && (
        <div className="rounded-xl border-[1.5px] border-destructive/40 bg-destructive/5 px-4 py-2.5 text-xs text-destructive">
          {exportErr}
        </div>
      )}

      {error && (
        <div className="flex items-center justify-between gap-3 rounded-xl border-[1.5px] border-destructive/40 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          <span>{error}</span>
          <Button size="sm" variant="outline" onClick={refresh}>
            tentar novamente
          </Button>
        </div>
      )}

      {loading ? (
        <div className="space-y-6">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
            {Array.from({ length: 5 }).map((_, i) => (
              <div
                key={i}
                className="h-28 animate-pulse rounded-2xl border-[1.5px] border-ink/20 bg-muted/40"
              />
            ))}
          </div>
          <div className="h-[440px] animate-pulse rounded-2xl border-[1.5px] border-ink/20 bg-muted/40" />
        </div>
      ) : data && data.totalCandidaturas === 0 ? (
        <NotebookPanel
          holes
          ruled
          tape
          headerColor="var(--primary)"
          className="mx-auto max-w-2xl"
        >
          <div className="flex flex-col items-center gap-4 py-10 text-center">
            <div className="grid h-12 w-12 place-items-center rounded-xl border-[1.5px] border-ink bg-primary/20 shadow-hard-xs">
              <GitBranch className="h-6 w-6" />
            </div>
            <h2 className="text-base font-extrabold tracking-tight">
              {periodo !== "all" || origem !== "all"
                ? "Nenhuma candidatura encontrada para esse filtro"
                : "Seu funil ainda está em branco"}
            </h2>
            <div className="flex flex-wrap justify-center gap-2">
              {periodo !== "all" || origem !== "all" ? (
                <Button
                  variant="outline"
                  onClick={() => {
                    setPeriodo("all")
                    setDataDe("")
                    setDataAte("")
                    setOrigem("all")
                  }}
                >
                  Limpar filtros
                </Button>
              ) : onGoToKanban ? (
                <Button onClick={onGoToKanban} className="gap-1.5">
                  <Kanban className="h-4 w-4" /> Voltar para o Quadro
                </Button>
              ) : null}
            </div>
          </div>
        </NotebookPanel>
      ) : data ? (
        <>
          {/* Insights acionáveis */}
          {data.insights && data.insights.length > 0 && (
            <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
              {data.insights.map((insight, idx) => {
                const isPositivo = insight.tipo === "positivo"
                const isAtencao = insight.tipo === "atencao"
                const Icon = isPositivo ? CheckCircle2 : isAtencao ? AlertTriangle : Info
                const toneClass = isPositivo
                  ? "border-emerald-500/40 bg-emerald-500/5"
                  : isAtencao
                    ? "border-amber-500/45 bg-amber-500/5"
                    : "border-sky-500/40 bg-sky-500/5"
                const iconClass = isPositivo
                  ? "text-emerald-600 dark:text-emerald-400"
                  : isAtencao
                    ? "text-amber-600 dark:text-amber-400"
                    : "text-sky-600 dark:text-sky-400"
                return (
                  <div
                    key={`${insight.titulo}-${idx}`}
                    className={`flex items-start gap-2.5 rounded-xl border-[1.5px] p-3.5 shadow-hard-xs ${toneClass}`}
                  >
                    <Icon className={`mt-0.5 h-4 w-4 shrink-0 ${iconClass}`} />
                    <div className="min-w-0 space-y-0.5">
                      <div className="text-xs font-extrabold tracking-tight">{insight.titulo}</div>
                      <p className="text-xs leading-relaxed text-muted-foreground">{insight.descricao}</p>
                    </div>
                  </div>
                )
              })}
            </div>
          )}

          {/* KPIs principais */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
            <NotebookCard headerColor="var(--st-applied)">
              <div className="flex items-start justify-between gap-2">
                <div className="text-xs font-semibold text-muted-foreground">Candidaturas</div>
                <Kanban className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="mt-2 text-2xl font-extrabold tracking-tight">{data.totalCandidaturas}</div>
              <div className="mt-1 text-xs text-muted-foreground">
                {data.aguardandoResposta} aguardando · {data.emAndamento - data.aguardandoResposta} em processo
              </div>
            </NotebookCard>

            <NotebookCard headerColor="var(--st-closed)">
              <div className="flex items-start justify-between gap-2">
                <div className="text-xs font-semibold text-muted-foreground">Diretas encerradas</div>
                <XCircle className="h-4 w-4" style={{ color: "var(--st-closed)" }} />
              </div>
              <div className="mt-2 flex items-baseline gap-2">
                <span className="text-2xl font-extrabold tracking-tight">{data.diretasEncerradas}</span>
                <span className="text-xs font-bold" style={{ color: "var(--st-closed)" }}>
                  {formatPct(taxaDiretasEncerradas)}
                </span>
              </div>
            </NotebookCard>

            <NotebookCard headerColor="var(--st-triage)">
              <div className="flex items-start justify-between gap-2">
                <div className="text-xs font-semibold text-muted-foreground">Foram para triagem</div>
                <TrendingUp className="h-4 w-4" style={{ color: "var(--st-triage)" }} />
              </div>
              <div className="mt-2 flex items-baseline gap-2">
                <span className="text-2xl font-extrabold tracking-tight">{data.avancaramParaTriagem}</span>
                <span className="text-xs font-bold" style={{ color: "var(--st-triage)" }}>
                  {formatPct(taxaTriagem)}
                </span>
              </div>
              <div className="mt-1 text-xs text-muted-foreground">
                {pularamTriagem > 0
                  ? `+${pularamTriagem} pularam direto para etapa seguinte`
                  : `${data.avancaramDeAplicado} avançaram além de Aplicado`}
              </div>
            </NotebookCard>

            <NotebookCard headerColor="var(--st-interview)">
              <div className="flex items-start justify-between gap-2">
                <div className="text-xs font-semibold text-muted-foreground">Entrevistas & Ofertas</div>
                <Sparkles className="h-4 w-4" style={{ color: "var(--st-interview)" }} />
              </div>
              <div className="mt-2 flex items-baseline gap-2">
                <span className="text-2xl font-extrabold tracking-tight">{data.chegaramEntrevista}</span>
                <span className="text-xs font-bold" style={{ color: "var(--st-interview)" }}>
                  {formatPct(taxaEntrevista)}
                </span>
              </div>
              <div className="mt-1 text-xs text-muted-foreground">
                {data.chegaramOferta} oferta{data.chegaramOferta === 1 ? "" : "s"} recebida
                {data.chegaramOferta === 1 ? "" : "s"}
              </div>
            </NotebookCard>

            <NotebookCard headerColor="var(--primary)">
              <div className="flex items-start justify-between gap-2">
                <div className="text-xs font-semibold text-muted-foreground">1ª resposta (média)</div>
                <Clock className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="mt-2 text-2xl font-extrabold tracking-tight">
                {formatDias(data.tempoMedioPrimeiraRespostaDias)}
              </div>
              <div className="mt-1 text-xs text-muted-foreground">
                {data.totalTransicoes} movimentaç{data.totalTransicoes === 1 ? "ão" : "ões"} registrada
                {data.totalTransicoes === 1 ? "" : "s"}
              </div>
            </NotebookCard>
          </div>

          {/* Diagrama de Sankey */}
          <NotebookPanel
            title="Fluxo de transição das vagas"
            headerColor="var(--primary)"
            holes
            ruled
            badge={
              <span className="rounded-full border border-ink/20 bg-secondary px-2.5 py-0.5 text-[11px] font-bold">
                {data.totalCandidaturas} vaga{data.totalCandidaturas === 1 ? "" : "s"}
              </span>
            }
          >
            <SankeyFunil
              fluxos={data.fluxosSankey}
              totalCandidaturas={data.totalCandidaturas}
              selecao={selecao}
              onSelect={setSelecao}
            />

            {selecao && (
              <div className="mt-4 rounded-xl border-[1.5px] border-ink/20 bg-[var(--nb-paper)]/80 p-3.5">
                <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
                  <div className="flex items-center gap-2 text-xs font-extrabold tracking-tight">
                    <span>Vagas em: {selecaoTitulo}</span>
                    <Badge variant="outline" className="text-[11px]">
                      {candidaturasFiltradas.length}
                    </Badge>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-7 px-2 text-xs"
                    onClick={() => setSelecao(null)}
                  >
                    fechar lista
                  </Button>
                </div>

                {candidaturasFiltradas.length === 0 ? (
                  <p className="text-xs text-muted-foreground">Nenhuma candidatura encontrada neste filtro.</p>
                ) : (
                  <div className="grid max-h-[260px] grid-cols-1 gap-2 overflow-y-auto pr-1 sm:grid-cols-2 lg:grid-cols-3">
                    {candidaturasFiltradas.map((c) => (
                      <button
                        key={c.id}
                        type="button"
                        onClick={() => onOpenDetail(c.id)}
                        className="flex flex-col gap-1.5 rounded-xl border border-ink/20 bg-card p-3 text-left shadow-hard-xs transition-transform hover:translate-x-[1px] hover:translate-y-[1px] hover:shadow-none"
                      >
                        <div className="flex items-start justify-between gap-2">
                          <div className="min-w-0">
                            <div className="truncate text-xs font-bold">{c.cargo}</div>
                            <div className="truncate text-[11px] text-muted-foreground">{c.empresa}</div>
                          </div>
                          <span className="shrink-0 rounded-md bg-secondary px-1.5 py-0.5 text-[10px] font-semibold text-muted-foreground">
                            {getOrigemLabel(c.origem)}
                          </span>
                        </div>
                        <div className="flex flex-wrap items-center gap-1 text-[10px] font-semibold">
                          {c.caminho.map((etapa, idx) => (
                            <span key={`${c.id}-${etapa}-${idx}`} className="inline-flex items-center gap-1">
                              {idx > 0 && <ArrowRight className="h-2.5 w-2.5 text-muted-foreground" />}
                              <span
                                className="inline-flex items-center gap-1 rounded-full px-1.5 py-0.5"
                                style={{
                                  background: SANKEY_NODE_META[etapa]?.soft ?? "var(--muted)",
                                  color: getStatusColor(etapa),
                                }}
                              >
                                {getNodeLabel(etapa)}
                              </span>
                            </span>
                          ))}
                        </div>
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )}
          </NotebookPanel>

          {/* Histórico de transições + Desempenho por origem */}
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            <NotebookPanel
              title="Histórico de transições"
              headerColor="var(--st-triage)"
              badge={
                data.totalRetornos > 0 ? (
                  <span className="inline-flex items-center gap-1 rounded-full border border-ink/20 bg-accent px-2.5 py-0.5 text-[11px] font-bold">
                    <RotateCcw className="h-3 w-3" /> {data.totalRetornos} retorno
                    {data.totalRetornos === 1 ? "" : "s"}
                  </span>
                ) : undefined
              }
            >
              {data.transicoes.length === 0 ? (
                <div className="py-8 text-center text-xs text-muted-foreground">
                  Nenhuma transição registrada ainda.
                </div>
              ) : (
                <div className="space-y-3">
                  {data.transicoes.map((t) => {
                    const share =
                      data.totalTransicoes > 0
                        ? Math.round((t.quantidade / data.totalTransicoes) * 1000) / 10
                        : 0

                    return (
                      <div
                        key={`${t.origem}->${t.destino}`}
                        className="space-y-1.5 rounded-xl border border-ink/15 bg-[var(--nb-paper)]/55 p-3"
                      >
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <div className="flex items-center gap-1.5 text-xs font-bold">
                            <span
                              className="h-2.5 w-2.5 rounded-full border border-ink/25"
                              style={{ background: getStatusColor(t.origem) }}
                            />
                            <span>{getNodeLabel(t.origem)}</span>
                            <ArrowRight className="h-3.5 w-3.5 text-muted-foreground" />
                            <span
                              className="h-2.5 w-2.5 rounded-full border border-ink/25"
                              style={{ background: getStatusColor(t.destino) }}
                            />
                            <span>{getNodeLabel(t.destino)}</span>
                            {t.ehRetorno && (
                              <span className="ml-1 inline-flex items-center gap-0.5 rounded-full border border-ink/20 bg-accent px-1.5 py-0.5 text-[10px] font-semibold">
                                <RotateCcw className="h-2.5 w-2.5" /> retorno
                              </span>
                            )}
                          </div>

                          <div className="flex items-center gap-2 text-xs">
                            <span className="font-extrabold">{t.quantidade}x</span>
                            {t.candidaturasUnicas !== t.quantidade && (
                              <span className="text-[11px] text-muted-foreground">
                                ({t.candidaturasUnicas} vagas)
                              </span>
                            )}
                            <span className="rounded-md bg-secondary px-1.5 py-0.5 text-[11px] font-semibold text-muted-foreground">
                              {formatHoras(t.tempoMedioHoras)}
                            </span>
                          </div>
                        </div>

                        <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
                          <div
                            className="h-full rounded-full transition-all"
                            style={{
                              width: `${Math.max(share, 3)}%`,
                              background: getStatusColor(t.destino),
                            }}
                          />
                        </div>
                      </div>
                    )
                  })}
                </div>
              )}
            </NotebookPanel>

            <NotebookPanel
              title="Desempenho por origem"
              headerColor="var(--st-offer)"
            >
              <div className="space-y-3">
                {data.porOrigem.map((item) => {
                  const pctAvancou = item.total > 0 ? (item.avancaram / item.total) * 100 : 0
                  const pctDiretaEncerrada = item.total > 0 ? (item.diretasEncerradas / item.total) * 100 : 0
                  const pctAguardando = Math.max(0, 100 - pctAvancou - pctDiretaEncerrada)

                  return (
                    <div
                      key={item.origem}
                      className="space-y-2 rounded-xl border border-ink/15 bg-[var(--nb-paper)]/55 p-3"
                    >
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <div className="flex items-center gap-2">
                          <span className="text-xs font-extrabold">{getOrigemLabel(item.origem)}</span>
                          <span className="rounded-full bg-secondary px-2 py-0.5 text-[11px] font-semibold text-muted-foreground">
                            {item.total} vaga{item.total === 1 ? "" : "s"}
                          </span>
                        </div>
                        <span className="text-xs font-extrabold" style={{ color: "var(--st-offer)" }}>
                          {formatPct(item.taxaAvanco)} avançaram
                        </span>
                      </div>

                      <div className="flex h-2.5 w-full overflow-hidden rounded-full border border-ink/15 bg-muted">
                        {pctAvancou > 0 && (
                          <div
                            style={{ width: `${pctAvancou}%`, background: "var(--st-triage)" }}
                            title={`Avançaram: ${item.avancaram}`}
                          />
                        )}
                        {pctAguardando > 0 && (
                          <div
                            style={{ width: `${pctAguardando}%`, background: "var(--crayon-orange)" }}
                            title={`Aguardando: ${item.total - item.avancaram - item.diretasEncerradas}`}
                          />
                        )}
                        {pctDiretaEncerrada > 0 && (
                          <div
                            style={{ width: `${pctDiretaEncerrada}%`, background: "var(--st-closed)" }}
                            title={`Diretas encerradas: ${item.diretasEncerradas}`}
                          />
                        )}
                      </div>

                      <div className="flex flex-wrap gap-x-3 gap-y-1 text-[11px] text-muted-foreground">
                        <span>
                          <strong className="text-foreground">{item.avancaram}</strong> avançaram
                        </span>
                        <span>
                          <strong className="text-foreground">{item.entrevistas}</strong> entrevista
                          {item.entrevistas === 1 ? "" : "s"}
                        </span>
                        <span>
                          <strong className="text-foreground">{item.ofertas}</strong> oferta
                          {item.ofertas === 1 ? "" : "s"}
                        </span>
                        <span>
                          <strong style={{ color: "var(--st-closed)" }}>{item.diretasEncerradas}</strong> direta
                          {item.diretasEncerradas === 1 ? "" : "s"} encerrada
                          {item.diretasEncerradas === 1 ? "" : "s"}
                        </span>
                      </div>
                    </div>
                  )
                })}

                <div className="flex flex-wrap items-center gap-3 pt-1 text-[11px] text-muted-foreground">
                  <span className="inline-flex items-center gap-1.5">
                    <span className="h-2 w-2 rounded-full" style={{ background: "var(--st-triage)" }} />
                    Avançaram
                  </span>
                  <span className="inline-flex items-center gap-1.5">
                    <span className="h-2 w-2 rounded-full" style={{ background: "var(--crayon-orange)" }} />
                    Aguardando
                  </span>
                  <span className="inline-flex items-center gap-1.5">
                    <span className="h-2 w-2 rounded-full" style={{ background: "var(--st-closed)" }} />
                    Diretas encerradas
                  </span>
                </div>
              </div>
            </NotebookPanel>
          </div>
        </>
      ) : null}
    </div>
  )
}
