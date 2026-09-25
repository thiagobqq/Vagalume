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
import { AlertTriangle, CheckCircle2, Loader2, RefreshCw, Sparkles, Wand2, XCircle } from "lucide-react"
import { useCompatibilidade } from "@/hooks/use-compatibilidade"
import type { AnaliseCompatibilidadeDto, VagaDto } from "./types"

function corScore(score: number) {
  if (score >= 70) return "border-emerald-500/40 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400"
  if (score >= 40) return "border-amber-500/40 bg-amber-500/10 text-amber-700 dark:text-amber-400"
  return "border-destructive/40 bg-destructive/10 text-destructive"
}

function AnaliseView({ analise, onReanalisar, gerando }: {
  analise: AnaliseCompatibilidadeDto
  onReanalisar: () => void
  gerando: boolean
}) {
  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <div className={`grid h-16 w-16 shrink-0 place-items-center rounded-xl border-[1.5px] ${corScore(analise.score)}`}>
          <div className="text-center leading-none">
            <div className="text-xl font-extrabold tabular-nums">{analise.score}</div>
            <div className="text-[10px] font-semibold uppercase tracking-wide">match</div>
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

      {analise.desatualizada && (
        <div className="flex items-center gap-2 rounded-lg border border-amber-500/30 bg-amber-500/5 px-3 py-2 text-xs text-amber-700 dark:text-amber-400">
          <AlertTriangle className="h-4 w-4 shrink-0" />
          Seu currículo ativo mudou desde esta análise. Reanalise para atualizar.
        </div>
      )}

      {analise.requisitos.length > 0 && (
        <div className="space-y-2">
          <div className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Requisitos</div>
          {analise.requisitos.map((r, i) => (
            <div key={i} className="rounded-lg border bg-muted/20 px-3 py-2">
              <div className="flex items-start gap-2">
                {r.atendido ? (
                  <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-emerald-600 dark:text-emerald-500" />
                ) : (
                  <XCircle className="mt-0.5 h-4 w-4 shrink-0 text-destructive" />
                )}
                <div className="min-w-0">
                  <div className="text-sm font-medium">{r.requisito}</div>
                  {r.evidencia && (
                    <div className="mt-0.5 text-xs text-muted-foreground">evidência: {r.evidencia}</div>
                  )}
                  {r.gap && <div className="mt-0.5 text-xs text-destructive">gap: {r.gap}</div>}
                </div>
              </div>
            </div>
          ))}
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

export function CompatibilidadeDialog({
  vaga,
  open,
  onOpenChange,
}: {
  vaga: VagaDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const { analise, loading, gerando, erro, gerar } = useCompatibilidade(open ? vaga?.id ?? null : null)

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Wand2 className="h-4 w-4" /> Compatibilidade com a vaga
          </DialogTitle>
          <DialogDescription>
            {vaga ? `${vaga.titulo} · ${vaga.empresa}` : ""}
          </DialogDescription>
        </DialogHeader>

        {loading ? (
          <div className="grid place-items-center gap-2 py-10 text-sm text-muted-foreground">
            <Loader2 className="h-5 w-5 animate-spin" /> carregando análise...
          </div>
        ) : gerando ? (
          <div className="grid place-items-center gap-2 py-10 text-center text-sm text-muted-foreground">
            <Loader2 className="h-5 w-5 animate-spin" />
            analisando seu currículo com a vaga... isso pode levar alguns segundos.
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
              <span className="text-muted-foreground">·</span>
              <Link to="/app/curriculos" className="font-semibold underline underline-offset-2">
                Enviar currículo
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
              Ainda não há análise para esta vaga. Vamos comparar seu currículo ativo com os requisitos da vaga.
            </p>
            <Button className="gap-1.5" onClick={() => void gerar()}>
              <Wand2 className="h-4 w-4" /> analisar compatibilidade
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
