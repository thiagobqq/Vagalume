import { Button } from "@/components/ui/button"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { ExternalLink, Trash2 } from "lucide-react"
import type { Application, Status } from "./types"
import { STATUS_LABEL } from "./types"

function daysSince(iso: string) {
  return Math.floor((Date.now() - new Date(iso).getTime()) / 86400000)
}

export function ApplicationCard({
  app,
  onMove,
  onDelete,
}: {
  app: Application
  onMove: (id: string, s: Status) => void
  onDelete: (id: string) => void
}) {
  const days = daysSince(app.dataAplicacao)
  const needsFollowUp = days >= 7 && app.status !== "recusado" && app.status !== "oferta"

  return (
    <div className="group bg-white border-[1.5px] border-foreground rounded-xl p-3 shadow-[3px_3px_0_var(--foreground)] hover:translate-y-[-1px] hover:shadow-[4px_4px_0_var(--foreground)] transition-all flex flex-col gap-2.5">
      {/* topo limpo: só empresa + cargo */}
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <div className="font-extrabold text-[14px] leading-tight truncate">{app.empresa}</div>
          <div className="text-[12.5px] leading-tight truncate text-muted-foreground">{app.cargo}</div>
        </div>
        <Button
          variant="ghost"
          size="icon"
          className="h-6 w-6 shrink-0 -mr-1 -mt-1 opacity-0 group-hover:opacity-100 border-transparent shadow-none text-muted-foreground hover:text-foreground transition-opacity"
          onClick={() => onDelete(app.id)}
          aria-label="remover"
        >
          <Trash2 className="w-3.5 h-3.5" />
        </Button>
      </div>

      {/* uma única linha de meta */}
      <div className="flex items-center gap-1.5 text-[11px] font-medium text-muted-foreground">
        <span>{days === 0 ? "hoje" : `${days}d`}</span>
        <span className="opacity-30">•</span>
        <a href={app.link} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 hover:text-foreground underline decoration-dotted underline-offset-2">
          link <ExternalLink className="w-3 h-3" />
        </a>
        <span className="opacity-30">•</span>
        <span className="truncate">{new Date(app.dataAplicacao).toLocaleDateString("pt-BR", { day: "2-digit", month: "short" })}</span>
      </div>

      {/* observação em 1 linha, sem bloco grande */}
      {app.observacoes && <p className="text-xs leading-snug text-foreground/70 line-clamp-2 border-l-2 border-amber-200 pl-2">{app.observacoes}</p>}

      {/* follow-up compacto */}
      {needsFollowUp && (
        <div className="inline-flex items-center gap-1.5 rounded-full bg-amber-100 border border-amber-300 px-2.5 py-1 self-start">
          <span className="w-1.5 h-1.5 rounded-full bg-amber-500" />
          <span className="text-[11px] font-bold text-amber-900 leading-none">{days}d sem retorno</span>
        </div>
      )}

      {/* footer único: só o select, limpo */}
      <Select value={app.status} onValueChange={(v) => onMove(app.id, v as Status)}>
        <SelectTrigger className="h-7 text-xs font-bold rounded-full bg-[#f8f5ec] border-foreground/15 shadow-none px-3 w-full">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="aplicado">{STATUS_LABEL.aplicado}</SelectItem>
          <SelectItem value="triagem">{STATUS_LABEL.triagem}</SelectItem>
          <SelectItem value="entrevista">{STATUS_LABEL.entrevista}</SelectItem>
          <SelectItem value="oferta">{STATUS_LABEL.oferta}</SelectItem>
          <SelectItem value="recusado">{STATUS_LABEL.recusado}</SelectItem>
        </SelectContent>
      </Select>
    </div>
  )
}
