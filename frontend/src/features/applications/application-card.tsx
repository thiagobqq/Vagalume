import { useDraggable } from "@dnd-kit/core"
import { CSS } from "@dnd-kit/utilities"
import { ArrowRightLeft, Clock3, ExternalLink } from "lucide-react"
import type { CandidaturaDto, Origem, Status } from "@/features/candidaturas/types"
import { ORIGEM_LABEL, STATUS_LABEL, STATUS_ORDER } from "@/features/candidaturas/types"
import { STATUS_STYLE } from "@/features/candidaturas/status-style"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { safeExternalUrl } from "@/lib/url"
import { cn } from "@/lib/utils"

function daysSince(iso: string) {
  return Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 86400000))
}

export function ApplicationCard({
  app,
  onOpenDetail,
  onMove,
  overlay = false,
  draggable = true,
}: {
  app: CandidaturaDto
  onOpenDetail?: (id: string) => void
  onMove?: (id: string, status: Status) => void
  overlay?: boolean
  draggable?: boolean
}) {
  const daysSemAtualizacao = daysSince(app.atualizadoEm ?? app.criadoEm)
  const daysCriacao = daysSince(app.criadoEm)
  const needsFollowUp = daysSemAtualizacao >= 7 && app.status !== "Encerrado" && app.status !== "Oferta"
  const safeLink = safeExternalUrl(app.link)
  const origemLabel = ORIGEM_LABEL[app.origem as Origem] ?? app.origem

  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: app.id,
    disabled: overlay || !draggable,
  })

  const dragStyle = transform ? { transform: CSS.Translate.toString(transform) } : undefined

  return (
    <div
      ref={overlay ? undefined : setNodeRef}
      style={overlay ? undefined : dragStyle}
      {...(overlay || !draggable ? {} : listeners)}
      {...(overlay || !draggable ? {} : attributes)}
      role="button"
      tabIndex={0}
      onClick={() => onOpenDetail?.(app.id)}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault()
          onOpenDetail?.(app.id)
        }
      }}
      className={cn(
        "relative flex shrink-0 select-none flex-col gap-3 overflow-hidden rounded-xl border-[1.5px] border-ink/70 bg-[var(--nb-card)] p-4 text-left shadow-hard-xs outline-none transition-[transform,box-shadow,border-color]",
        "focus-visible:ring-2 focus-visible:ring-ink/30 focus-visible:ring-offset-2 focus-visible:ring-offset-background",
        overlay
          ? "rotate-[-3deg] cursor-grabbing shadow-hard"
          : draggable
            ? "cursor-grab hover:-translate-y-0.5 hover:border-ink hover:shadow-hard-sm active:cursor-grabbing"
            : "cursor-pointer hover:border-ink hover:shadow-hard-sm",
        isDragging && "opacity-40"
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <div className="truncate text-[15px] font-extrabold leading-tight">{app.empresa}</div>
          <div className="mt-0.5 truncate text-[13px] leading-tight text-muted-foreground">{app.cargo}</div>
        </div>
        <span className="shrink-0 rounded-full border border-ink/25 bg-muted px-2 py-0.5 text-[10px] font-bold text-muted-foreground">
          {origemLabel}
        </span>
      </div>

      <div className="flex items-center gap-1.5 text-[11px] font-semibold text-muted-foreground">
        <span className="inline-flex items-center gap-1" title="Dias desde o cadastro">
          <Clock3 className="h-3 w-3" />
          {daysCriacao === 0 ? "hoje" : `${daysCriacao}d`}
        </span>
        {safeLink ? (
          <>
            <span className="opacity-40">•</span>
            <a
              href={safeLink}
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

        {!overlay && onMove && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                type="button"
                onClick={(e) => e.stopPropagation()}
                onPointerDown={(e) => e.stopPropagation()}
                aria-label="Mover etapa"
                title="Mover para outra etapa"
                className="ml-auto inline-flex items-center gap-1 rounded-md border border-ink/20 bg-background/80 px-1.5 py-0.5 text-[10px] font-bold text-foreground hover:border-ink/60 lg:opacity-70 lg:hover:opacity-100"
              >
                <ArrowRightLeft className="h-3 w-3" />
                <span className="lg:hidden">mover</span>
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              align="end"
              onClick={(e) => e.stopPropagation()}
              onPointerDown={(e) => e.stopPropagation()}
            >
              {STATUS_ORDER.filter((s) => s !== app.status).map((s) => (
                <DropdownMenuItem key={s} onSelect={() => onMove(app.id, s)}>
                  <span
                    className="h-2 w-2 rounded-full"
                    style={{ background: STATUS_STYLE[s].dot }}
                    aria-hidden="true"
                  />
                  Mover para {STATUS_LABEL[s]}
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
        )}
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
            {daysSemAtualizacao}d sem atualização
          </span>
        </div>
      )}
    </div>
  )
}
