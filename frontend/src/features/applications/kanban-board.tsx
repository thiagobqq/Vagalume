import { useMemo } from "react"
import type { Application, Status } from "./types"
import { STATUS_ORDER } from "./types"
import { KanbanColumn } from "./kanban-column"

export function KanbanBoard({
  apps,
  onMove,
  onDelete,
}: {
  apps: Application[]
  onMove: (id: string, s: Status) => void
  onDelete: (id: string) => void
}) {
  const grouped = useMemo(() => {
    const map: Record<Status, Application[]> = {
      aplicado: [],
      triagem: [],
      entrevista: [],
      oferta: [],
      recusado: [],
    }
    for (const a of apps) map[a.status].push(a)
    return map
  }, [apps])

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4 items-start">
      {STATUS_ORDER.map((s) => (
        <KanbanColumn key={s} status={s} apps={grouped[s]} onMove={onMove} onDelete={onDelete} />
      ))}
    </div>
  )
}
