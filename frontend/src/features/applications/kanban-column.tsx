import type { Status, CandidaturaDto } from "@/features/candidaturas/types"
import { STATUS_LABEL } from "@/features/candidaturas/types"
import { STATUS_STYLE } from "@/features/candidaturas/status-style"
import { ApplicationCard } from "./application-card"

export function KanbanColumn({
  status,
  apps,
  onMove,
  onOpenDetail,
}: {
  status: Status
  apps: CandidaturaDto[]
  onMove: (id: string, s: Status) => void
  onOpenDetail?: (id: string) => void
}) {
  const { dot } = STATUS_STYLE[status]
  return (
    <div className="flex w-[280px] shrink-0 flex-col rounded-xl border bg-muted/40 sm:w-[300px] lg:w-0 lg:min-w-0 lg:flex-1">
      <div className="flex items-center justify-between gap-2 px-3 py-2.5">
        <div className="flex items-center gap-2">
          <span className="h-2 w-2 rounded-full" style={{ background: dot }} />
          <h3 className="text-sm font-semibold tracking-tight">{STATUS_LABEL[status]}</h3>
        </div>
        <span className="rounded-full border bg-card px-2 py-0.5 text-xs font-medium text-muted-foreground">
          {apps.length}
        </span>
      </div>

      <div className="flex min-h-[140px] flex-1 flex-col gap-2 px-2 pb-2">
        {apps.length === 0 ? (
          <div className="grid flex-1 place-items-center rounded-lg border border-dashed px-4 py-8 text-center">
            <div>
              <p className="text-xs font-medium text-muted-foreground">Nenhuma vaga</p>
              <p className="mt-0.5 text-[11px] text-muted-foreground/70">mova um card para cá</p>
            </div>
          </div>
        ) : (
          apps.map((a) => <ApplicationCard key={a.id} app={a} onMove={onMove} onOpenDetail={onOpenDetail} />)
        )}
      </div>
    </div>
  )
}
