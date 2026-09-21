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
    <div className="-mx-4 flex gap-4 overflow-x-auto px-4 pb-3 sm:mx-0 sm:px-0">
      {STATUS_ORDER.map((s) => (
        <KanbanColumn key={s} status={s} apps={grouped[s]} onMove={onMove} onOpenDetail={onOpenDetail} />
      ))}
    </div>
  )
}
