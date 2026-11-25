import { useEffect, useRef } from "react"
import { Link } from "react-router-dom"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { CheckCircle2, Loader2, RefreshCw, ShieldAlert, Sparkles, Wand2 } from "lucide-react"
import { useAnaliseAts } from "@/hooks/use-analise-ats"
import type { CurriculoDto } from "@/features/candidaturas/types"
import type { AnaliseAtsDto, CategoriaAtsDto } from "./types"

function corScore(score: number) {
  if (score >= 70) return "border-emerald-500/40 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400"
  if (score >= 40) return "border-amber-500/40 bg-amber-500/10 text-amber-700 dark:text-amber-400"
  return "border-destructive/40 bg-destructive/10 text-destructive"
}

function corNota(nota: number) {
  if (nota >= 70) return "bg-emerald-500"
  if (nota >= 40) return "bg-amber-500"
  return "bg-destructive"
}

function corSeveridade(severidade: string) {
  if (severidade === "alta") return "border-destructive/40 bg-destructive/5 text-destructive"
  if (severidade === "baixa") return "border-sky-500/40 bg-sky-500/5 text-sky-700 dark:text-sky-400"
  return "border-amber-500/40 bg-amber-500/5 text-amber-700 dark:text-amber-400"
}

function SeveridadeLabel({ severidade }: { severidade: string }) {
  const label = severidade === "alta" ? "alta" : severidade === "baixa" ? "baixa" : "média"
  return <span className="font-semibold uppercase">{label}</span>
}

function CategoriaView({ c }: { c: CategoriaAtsDto }) {
  return (
    <div className="rounded-lg border bg-muted/20 px-3 py-2.5">
      <div className="flex items-center justify-between gap-2">
        <div className="text-sm font-medium">{c.categoria}</div>
        <div className="text-xs font-semibold tabular-nums text-muted-foreground">{c.nota}</div>
      </div>
      <div className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-muted">
        <div className={`h-full rounded-full ${corNota(c.nota)}`} style={{ width: `${c.nota}%` }} />
      </div>
      {c.diagnostico && <p className="mt-1.5 text-xs text-muted-foreground">{c.diagnostico}</p>}
      {c.recomendacoes.length > 0 && (
        <ul className="mt-1.5 space-y-0.5">
          {c.recomendacoes.map((r, i) => (
            <li key={i} className="flex items-start gap-1.5 text-xs">
              <span className="mt-1 h-1 w-1 shrink-0 rounded-full bg-primary" />
              <span>{r}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

function AnaliseView({ analise, onReanalisar, gerando }: {
  analise: AnaliseAtsDto
  onReanalisar: () => void
  gerando: boolean
}) {
  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <div className={`grid h-16 w-16 shrink-0 place-items-center rounded-xl border-[1.5px] ${corScore(analise.score)}`}>
          <div className="text-center leading-none">
            <div className="text-xl font-extrabold tabular-nums">{analise.score}</div>
            <div className="text-[10px] font-semibold uppercase tracking-wide">ATS</div>
          </div>
        </div>
        <div className="min-w-0 flex-1">
          <p className="text-sm">{analise.resumo || "Análise concluída."}</p>
          <div className="mt-1 flex flex-wrap items-center gap-1.5 text-[11px] text-muted-foreground">
            <Badge variant="outline">{analise.provedor}</Badge>
            <span className="truncate">{analise.modelo}</span>
            <span>·</span>
            <span className="truncate">currículo: {analise.curriculoNome}</span>
          </div>
        </div>
      </div>

      {analise.categorias.length > 0 && (
        <div className="space-y-2">
          <div className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Categorias</div>
          {analise.categorias.map((c, i) => (
            <CategoriaView key={i} c={c} />
          ))}
        </div>
      )}

      {analise.alertas.length > 0 && (
        <div className="space-y-2">
          <div className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Alertas</div>
          {analise.alertas.map((a, i) => (
            <div key={i} className={`rounded-lg border px-3 py-2 ${corSeveridade(a.severidade)}`}>
              <div className="flex items-start gap-2">
                <ShieldAlert className="mt-0.5 h-4 w-4 shrink-0" />
                <div className="min-w-0">
                  <div className="text-sm">
                    <SeveridadeLabel severidade={a.severidade} /> · {a.problema}
                  </div>
                  {a.comoCorrigir && (
                    <div className="mt-0.5 text-xs opacity-90">{a.comoCorrigir}</div>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {analise.pontosFortes.length > 0 && (
        <div className="space-y-1.5">
          <div className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Pontos fortes</div>
          <ul className="space-y-1 text-sm">
            {analise.pontosFortes.map((p, i) => (
              <li key={i} className="flex items-start gap-2">
                <CheckCircle2 className="mt-0.5 h-3.5 w-3.5 shrink-0 text-emerald-600 dark:text-emerald-500" />
                <span>{p}</span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {analise.sugestoes.length > 0 && (
        <div className="space-y-1.5">
          <div className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Sugestões</div>
          <ul className="space-y-1 text-sm">
            {analise.sugestoes.map((s, i) => (
              <li key={i} className="flex items-start gap-2">
                <Sparkles className="mt-0.5 h-3.5 w-3.5 shrink-0 text-primary" />
                <span>{s}</span>
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="flex justify-end">
        <Button variant="outline" size="sm" className="gap-1.5" onClick={onReanalisar} disabled={gerando}>
          {gerando ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <RefreshCw className="h-3.5 w-3.5" />} reanalisar
        </Button>
      </div>
    </div>
  )
}

export function AnaliseAtsDialog({
  curriculo,
  open,
  onOpenChange,
}: {
  curriculo: CurriculoDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const { analise, loading, carregado, gerando, erro, gerar } = useAnaliseAts(open ? curriculo?.id ?? null : null)
  const autoGerou = useRef(false)

  useEffect(() => {
    if (!open) {
      autoGerou.current = false
      return
    }
    if (carregado && !gerando && !erro && analise === null && !autoGerou.current) {
      autoGerou.current = true
      void gerar()
    }
  }, [open, carregado, gerando, erro, analise, gerar])

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] max-w-xl overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Wand2 className="h-4 w-4" /> Análise ATS do currículo
          </DialogTitle>
          <DialogDescription>
            {curriculo ? curriculo.nomeArquivoOriginal : ""}
          </DialogDescription>
        </DialogHeader>

        {loading ? (
          <div className="grid place-items-center gap-2 py-10 text-sm text-muted-foreground">
            <Loader2 className="h-5 w-5 animate-spin" /> carregando análise...
          </div>
        ) : gerando ? (
          <div className="grid place-items-center gap-2 py-10 text-center text-sm text-muted-foreground">
            <Loader2 className="h-5 w-5 animate-spin" />
            analisando seu currículo para ATS... isso pode levar alguns segundos.
          </div>
        ) : erro ? (
          <div className="space-y-3">
            <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-sm text-destructive">
              {erro}
            </div>
            <div className="flex flex-wrap gap-2 text-xs">
              <Link to="/app/curriculos" className="font-semibold underline underline-offset-2">
                Configurar provedor de IA
              </Link>
            </div>
            <Button size="sm" className="gap-1.5" onClick={() => void gerar()}>
              <RefreshCw className="h-3.5 w-3.5" /> tentar novamente
            </Button>
          </div>
        ) : analise ? (
          <AnaliseView analise={analise} onReanalisar={() => void gerar()} gerando={gerando} />
        ) : (
          <div className="space-y-3 py-2">
            <p className="text-sm text-muted-foreground">
              Nenhuma análise gerada ainda.
            </p>
            <Button className="gap-1.5" onClick={() => void gerar()}>
              <Wand2 className="h-4 w-4" /> analisar currículo
            </Button>
          </div>
        )}

        <DialogFooter>
          <Button variant="ghost" onClick={() => onOpenChange(false)} className="gap-1.5 text-muted-foreground hover:text-foreground">
            fechar
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
