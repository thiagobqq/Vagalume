import { useDraggable } from "@dnd-kit/core"
import { CSS } from "@dnd-kit/utilities"
import { ExternalLink, Clock3 } from "lucide-react"
import type { CandidaturaDto } from "@/features/candidaturas/types"
import { cn } from "@/lib/utils"

function daysSince(iso: string) {
  return Math.floor((Date.now() - new Date(iso).getTime()) / 86400000)
}

export function ApplicationCard({
  app,
  onOpenDetail,
  overlay = false,
}: {
  app: CandidaturaDto
  onOpenDetail?: (id: string) => void
  overlay?: boolean
}) {
  const days = daysSince(app.criadoEm)
  const needsFollowUp = days >= 7 && app.status !== "Encerrado" && app.status !== "Oferta"

  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: app.id,
    disabled: overlay,
  })

  const dragStyle = transform ? { transform: CSS.Translate.toString(transform) } : undefined

  return (
    <div
      ref={overlay ? undefined : setNodeRef}
      style={overlay ? undefined : dragStyle}
      {...(overlay ? {} : listeners)}
      {...(overlay ? {} : attributes)}
      onClick={() => onOpenDetail?.(app.id)}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault()
          onOpenDetail?.(app.id)
        }
      }}
      className={cn(
        "relative flex select-none flex-col gap-3 overflow-hidden rounded-xl border-[1.5px] border-ink/70 bg-[var(--nb-card)] p-4 text-left shadow-hard-xs outline-none transition-[transform,box-shadow,border-color]",
        "focus-visible:ring-2 focus-visible:ring-ink/30 focus-visible:ring-offset-2 focus-visible:ring-offset-background",
        overlay
          ? "rotate-[-3deg] cursor-grabbing shadow-hard"
          : "cursor-grab hover:-translate-y-0.5 hover:border-ink hover:shadow-hard-sm active:cursor-grabbing",
        isDragging && "opacity-40"
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <div className="truncate text-[15px] font-extrabold leading-tight">{app.empresa}</div>
          <div className="mt-0.5 truncate text-[13px] leading-tight text-muted-foreground">{app.cargo}</div>
        </div>
        <span className="shrink-0 rounded-full border border-ink/25 bg-muted px-2 py-0.5 text-[10px] font-bold text-muted-foreground">
          {app.origem}
        </span>
      </div>

      <div className="flex items-center gap-1.5 text-[11px] font-semibold text-muted-foreground">
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
              onPointerDown={(e) => e.stopPropagation()}
              className="inline-flex items-center gap-1 text-foreground underline decoration-dotted underline-offset-2 hover:opacity-80"
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

      {needsFollowUp && (
        <div
          className="inline-flex items-center gap-1.5 self-start rounded-full border px-2.5 py-1"
          style={{
            background: "var(--st-applied-soft)",
            borderColor: "color-mix(in srgb, var(--st-applied) 45%, transparent)",
          }}
        >
          <span className="h-1.5 w-1.5 rounded-full" style={{ background: "var(--st-applied)" }} />
          <span className="text-[11px] font-bold leading-none text-[color:var(--st-applied)]">
            {days}d sem retorno
          </span>
        </div>
      )}
    </div>
  )
}
