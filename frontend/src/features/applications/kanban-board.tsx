import { useMemo } from "react"
import type { CandidaturaDto, Status } from "@/features/candidaturas/types"
import { STATUS_ORDER } from "@/features/candidaturas/types"
import { KanbanColumn } from "./kanban-column"

export function KanbanBoard({
  apps,
  onMove,
  onOpenDetail,
}: {
  apps: CandidaturaDto[]
  onMove: (id: string, s: Status, observacao?: string) => void
  onOpenDetail?: (id: string) => void
}) {
  const grouped = useMemo(() => {
    const map: Record<Status, CandidaturaDto[]> = {
      Aplicado: [],
      Triagem: [],
      Entrevista: [],
      Oferta: [],
      Encerrado: [],
    }
    for (const a of apps) {
      const st = (a.status as Status) || "Aplicado"
      if (map[st]) map[st].push(a)
      else map["Aplicado"].push(a)
    }
    return map
  }, [apps])

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4 items-start">
      {STATUS_ORDER.map((s) => (
        <KanbanColumn key={s} status={s} apps={grouped[s]} onMove={onMove} onOpenDetail={onOpenDetail} />
      ))}
    </div>
  )
}
