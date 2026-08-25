import { useState } from "react"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { ExternalLink, Clock3 } from "lucide-react"
import type { CandidaturaDto, Status } from "@/features/candidaturas/types"
import { STATUS_LABEL } from "@/features/candidaturas/types"

function daysSince(iso: string) {
  return Math.floor((Date.now() - new Date(iso).getTime()) / 86400000)
}

export function ApplicationCard({
  app,
  onMove,
  onOpenDetail,
}: {
  app: CandidaturaDto
  onMove: (id: string, s: Status) => void
  onOpenDetail?: (id: string) => void
}) {
  const days = daysSince(app.criadoEm)
  const needsFollowUp = days >= 7 && app.status !== "Encerrado" && app.status !== "Oferta"
  const [updating, setUpdating] = useState(false)

  async function handleMove(v: string) {
    setUpdating(true)
    try {
      await onMove(app.id, v as Status)
    } finally {
      setUpdating(false)
    }
  }

  return (
    <div
      className="group bg-white border-[1.5px] border-foreground rounded-xl p-3 shadow-[3px_3px_0_var(--foreground)] hover:translate-y-[-1px] hover:shadow-[4px_4px_0_var(--foreground)] transition-all flex flex-col gap-2.5 cursor-pointer"
      onClick={() => onOpenDetail?.(app.id)}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === "Enter") onOpenDetail?.(app.id)
      }}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <div className="font-extrabold text-[14px] leading-tight truncate">{app.empresa}</div>
          <div className="text-[12.5px] leading-tight truncate text-muted-foreground">{app.cargo}</div>
        </div>
        <span className="shrink-0 text-[10px] font-bold px-2 py-0.5 rounded-full border border-foreground/15 bg-[#f8f5ec]">{app.origem}</span>
      </div>

      <div className="flex items-center gap-1.5 text-[11px] font-medium text-muted-foreground">
        <span className="inline-flex items-center gap-1">
          <Clock3 className="w-3 h-3" />
          {days === 0 ? "hoje" : `${days}d`}
        </span>
        {app.link ? (
          <>
            <span className="opacity-30">•</span>
            <a
              href={app.link}
              target="_blank"
              rel="noreferrer"
              onClick={(e) => e.stopPropagation()}
              className="inline-flex items-center gap-1 hover:text-foreground underline decoration-dotted underline-offset-2"
            >
              link <ExternalLink className="w-3 h-3" />
            </a>
          </>
        ) : null}
        <span className="opacity-30">•</span>
        <span className="truncate">{new Date(app.criadoEm).toLocaleDateString("pt-BR", { day: "2-digit", month: "short" })}</span>
      </div>

      {app.observacoes && <p className="text-xs leading-snug text-foreground/70 line-clamp-2 border-l-2 border-amber-200 pl-2">{app.observacoes}</p>}

      {needsFollowUp && (
        <div className="inline-flex items-center gap-1.5 rounded-full bg-amber-100 border border-amber-300 px-2.5 py-1 self-start">
          <span className="w-1.5 h-1.5 rounded-full bg-amber-500" />
          <span className="text-[11px] font-bold text-amber-900 leading-none">{days}d sem retorno</span>
        </div>
      )}

      <div onClick={(e) => e.stopPropagation()}>
        <Select value={app.status} onValueChange={handleMove} disabled={updating}>
          <SelectTrigger className="h-7 text-xs font-bold rounded-full bg-[#f8f5ec] border-foreground/15 shadow-none px-3 w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Aplicado">{STATUS_LABEL.Aplicado}</SelectItem>
            <SelectItem value="Triagem">{STATUS_LABEL.Triagem}</SelectItem>
            <SelectItem value="Entrevista">{STATUS_LABEL.Entrevista}</SelectItem>
            <SelectItem value="Oferta">{STATUS_LABEL.Oferta}</SelectItem>
            <SelectItem value="Encerrado">{STATUS_LABEL.Encerrado}</SelectItem>
          </SelectContent>
        </Select>
      </div>
    </div>
  )
}
