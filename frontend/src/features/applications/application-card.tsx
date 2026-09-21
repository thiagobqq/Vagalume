import { useState } from "react"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Badge } from "@/components/ui/badge"
import { ExternalLink, Clock3 } from "lucide-react"
import type { CandidaturaDto, Status } from "@/features/candidaturas/types"
import { STATUS_LABEL } from "@/features/candidaturas/types"
import { STATUS_STYLE } from "@/features/candidaturas/status-style"

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
  const { dot } = STATUS_STYLE[(app.status as Status) || "Aplicado"]

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
      className="group flex cursor-pointer flex-col gap-2.5 rounded-lg border bg-card p-3 shadow-sm transition-all hover:border-foreground/20 hover:shadow-card"
      onClick={() => onOpenDetail?.(app.id)}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === "Enter") onOpenDetail?.(app.id)
      }}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <div className="truncate text-sm font-semibold leading-tight">{app.empresa}</div>
          <div className="truncate text-xs leading-tight text-muted-foreground">{app.cargo}</div>
        </div>
        <Badge variant="outline" className="shrink-0 text-[10px] text-muted-foreground">
          {app.origem}
        </Badge>
      </div>

      <div className="flex items-center gap-1.5 text-[11px] font-medium text-muted-foreground">
        <span className="inline-flex items-center gap-1">
          <Clock3 className="h-3 w-3" />
          {days === 0 ? "hoje" : `${days}d`}
        </span>
        {app.link ? (
          <>
            <span className="opacity-40">•</span>
            <a
              href={app.link}
              target="_blank"
              rel="noreferrer"
              onClick={(e) => e.stopPropagation()}
              className="inline-flex items-center gap-1 hover:text-foreground hover:underline"
            >
              link <ExternalLink className="h-3 w-3" />
            </a>
          </>
        ) : null}
        <span className="opacity-40">•</span>
        <span className="truncate">
          {new Date(app.criadoEm).toLocaleDateString("pt-BR", { day: "2-digit", month: "short" })}
        </span>
      </div>

      {app.observacoes && (
        <p className="line-clamp-2 border-l-2 border-border pl-2 text-xs leading-snug text-muted-foreground">
          {app.observacoes}
        </p>
      )}

      {needsFollowUp && (
        <div className="inline-flex items-center gap-1.5 self-start rounded-full bg-[var(--st-applied-soft)] px-2.5 py-1">
          <span className="h-1.5 w-1.5 rounded-full" style={{ background: "var(--st-applied)" }} />
          <span className="text-[11px] font-medium leading-none text-[color:var(--st-applied)]">
            {days}d sem retorno
          </span>
        </div>
      )}

      <div onClick={(e) => e.stopPropagation()}>
        <Select value={app.status} onValueChange={handleMove} disabled={updating}>
          <SelectTrigger className="h-7 w-full rounded-md border-transparent bg-transparent px-2 text-xs font-medium text-muted-foreground shadow-none hover:bg-accent focus:ring-2">
            <span className="flex items-center gap-1.5">
              <span className="h-1.5 w-1.5 rounded-full" style={{ background: dot }} />
              <SelectValue />
            </span>
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
