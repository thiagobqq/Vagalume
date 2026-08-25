import { Badge } from "@/components/ui/badge"
import { cn } from "@/lib/utils"
import type { Status, Application } from "./types"
import { STATUS_LABEL } from "./types"
import { ApplicationCard } from "./application-card"

const columnStyles: Record<Status, { bg: string; dot: string; tape: string }> = {
  aplicado: { bg: "bg-[#fef3c7]", dot: "bg-[#f7c948]", tape: "bg-[#f7c948]/60" },
  triagem: { bg: "bg-[#dbeafe]", dot: "bg-[#5b9bd5]", tape: "bg-[#93c5fd]/60" },
  entrevista: { bg: "bg-[#ede9fe]", dot: "bg-[#a78bfa]", tape: "bg-[#c4b5fd]/60" },
  oferta: { bg: "bg-[#dcfce7]", dot: "bg-[#4cb878]", tape: "bg-[#86efac]/60" },
  recusado: { bg: "bg-[#fce7f3]", dot: "bg-[#f06292]", tape: "bg-[#f9a8d4]/60" },
}

export function KanbanColumn({
  status,
  apps,
  onMove,
  onDelete,
}: {
  status: Status
  apps: Application[]
  onMove: (id: string, s: Status) => void
  onDelete: (id: string) => void
}) {
  const sty = columnStyles[status]
  return (
    <div className={cn("relative rounded-2xl border-[1.5px] border-foreground shadow-[4px_4px_0_var(--foreground)] flex flex-col min-h-[500px] overflow-hidden", sty.bg)}>
      {/* washi tape no topo - essencia caderno */}
      <div className={cn("absolute -top-2 left-1/2 -translate-x-1/2 w-16 h-3.5 rounded-sm border border-foreground/10 rotate-[-1.5deg] opacity-70", sty.tape)} />

      {/* linhas pautadas sutis */}
      <div className="absolute inset-0 opacity-[0.06] pointer-events-none" style={{ backgroundImage: "repeating-linear-gradient(to bottom, transparent 0 27px, #151613 27px 28px)" }} />

      <div className="relative px-4 py-3 flex items-center justify-between border-b-[1.5px] border-foreground/10 bg-white/75 backdrop-blur-sm">
        <div className="flex items-center gap-2">
          <span className={cn("w-2.5 h-2.5 rounded-full border border-foreground/15", sty.dot)} />
          <h3 className="font-extrabold text-[13px] tracking-tight">{STATUS_LABEL[status]}</h3>
        </div>
        <Badge variant="outline" className="bg-white text-xs font-bold border-foreground/15 shadow-none px-2 py-0">
          {apps.length}
        </Badge>
      </div>

      <div className="relative p-3 flex flex-col gap-3 flex-1 overflow-auto">
        {apps.length === 0 ? (
          <div className="flex-1 grid place-items-center p-4 text-center">
            <div className="rounded-xl bg-white/80 border-[1.5px] border-dashed border-foreground/20 px-4 py-8 w-full">
              <p className="text-sm font-bold text-foreground/60">vazio</p>
              <p className="text-xs text-muted-foreground mt-1">mova um card para cá</p>
            </div>
          </div>
        ) : (
          apps.map((a) => <ApplicationCard key={a.id} app={a} onMove={onMove} onDelete={onDelete} />)
        )}
      </div>
    </div>
  )
}
