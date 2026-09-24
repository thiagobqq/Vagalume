import { useDroppable } from "@dnd-kit/core"
import type { Status, CandidaturaDto } from "@/features/candidaturas/types"
import { STATUS_LABEL } from "@/features/candidaturas/types"
import { STATUS_STYLE } from "@/features/candidaturas/status-style"
import { ApplicationCard } from "./application-card"
import { cn } from "@/lib/utils"

export function KanbanColumn({
  status,
  apps,
  onOpenDetail,
}: {
  status: Status
  apps: CandidaturaDto[]
  onOpenDetail?: (id: string) => void
}) {
  const { dot, soft, strong } = STATUS_STYLE[status]
  const { setNodeRef, isOver } = useDroppable({ id: status })

  return (
    <div className="relative min-w-0 pt-3">
      {/* fita washi que prende a folha na parede */}
      <div
        className="tape absolute left-1/2 top-0 z-20 h-7 w-24 -translate-x-1/2 -rotate-2 rounded-[2px]"
        style={{ background: `color-mix(in srgb, ${strong} 55%, transparent)` }}
        aria-hidden="true"
      />

      <div
        ref={setNodeRef}
        style={{ background: soft }}
        className={cn(
          "relative flex min-w-0 flex-col overflow-hidden rounded-2xl border-[1.5px] border-ink shadow-hard transition-shadow",
          isOver && "ring-4 ring-primary/60"
        )}
      >
        {/* pauta sutil */}
        <div
          className="pointer-events-none absolute inset-0 opacity-[0.05]"
          style={{
            backgroundImage: "repeating-linear-gradient(to bottom, transparent 0 27px, var(--nb-ink) 27px 28px)",
          }}
          aria-hidden="true"
        />

        <div className="relative z-20 flex items-center justify-between gap-2 border-b-[1.5px] border-ink/15 bg-card/70 px-4 py-3 backdrop-blur-sm">
          <div className="flex items-center gap-2">
            <span className="h-3 w-3 rounded-full border border-ink/20" style={{ background: dot }} />
            <h3 className="text-[13px] font-extrabold tracking-tight">{STATUS_LABEL[status]}</h3>
          </div>
          <span className="grid min-w-6 place-items-center rounded-full border-[1.5px] border-ink/70 bg-card px-2 py-0.5 text-xs font-bold shadow-hard-xs">
            {apps.length}
          </span>
        </div>

        <div className="relative z-20 flex min-h-[440px] flex-1 flex-col gap-3 p-3">
          {apps.length === 0 ? (
            <div className="grid flex-1 place-items-center rounded-xl border-[1.5px] border-dashed border-ink/25 bg-card/50 px-4 py-8 text-center">
              <div>
                <p className="text-sm font-bold text-foreground/60">vazio</p>
                <p className="mt-0.5 text-[11px] text-muted-foreground">arraste um card para cá</p>
              </div>
            </div>
          ) : (
            apps.map((a) => <ApplicationCard key={a.id} app={a} onOpenDetail={onOpenDetail} />)
          )}
        </div>
      </div>
    </div>
  )
}
