import { useMemo, useRef, useState } from "react"
import {
  DndContext,
  DragOverlay,
  MouseSensor,
  TouchSensor,
  closestCorners,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from "@dnd-kit/core"
import type { CandidaturaDto, Status } from "@/features/candidaturas/types"
import { STATUS_ORDER, STATUS_VALUES } from "@/features/candidaturas/types"
import { KanbanColumn } from "./kanban-column"
import { ApplicationCard } from "./application-card"

export function KanbanBoard({
  apps,
  onMove,
  onOpenDetail,
}: {
  apps: CandidaturaDto[]
  onMove: (id: string, s: Status) => void
  onOpenDetail?: (id: string) => void
}) {
  const [activeId, setActiveId] = useState<string | null>(null)
  const suppressClick = useRef(false)

  const sensors = useSensors(
    useSensor(MouseSensor, { activationConstraint: { distance: 8 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 200, tolerance: 8 } })
  )

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

  const activeApp = activeId ? apps.find((a) => a.id === activeId) ?? null : null

  function resolveStatus(overId: string): Status | undefined {
    if ((STATUS_VALUES as readonly string[]).includes(overId)) return overId as Status
    const overApp = apps.find((a) => a.id === overId)
    return overApp ? ((overApp.status as Status) || "Aplicado") : undefined
  }

  function handleDragStart(e: DragStartEvent) {
    suppressClick.current = true
    setActiveId(String(e.active.id))
  }

  function handleDragEnd(e: DragEndEvent) {
    setActiveId(null)
    const { active, over } = e
    if (!over) return
    const id = String(active.id)
    const target = resolveStatus(String(over.id))
    const current = apps.find((a) => a.id === id)
    if (!target || !current) return
    if ((current.status as Status) !== target) onMove(id, target)
  }

  function handleOpenDetail(id: string) {
    if (suppressClick.current) return
    onOpenDetail?.(id)
  }

  function clearSuppress() {
    setTimeout(() => {
      suppressClick.current = false
    }, 180)
  }

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCorners}
      onDragStart={handleDragStart}
      onDragEnd={(e) => {
        handleDragEnd(e)
        clearSuppress()
      }}
      onDragCancel={() => {
        setActiveId(null)
        clearSuppress()
      }}
    >
      <div className="grid min-h-0 flex-1 grid-cols-1 content-start items-start gap-4 sm:grid-cols-2 lg:grid-cols-5 lg:grid-rows-1">
        {STATUS_ORDER.map((s) => (
          <KanbanColumn key={s} status={s} apps={grouped[s]} onOpenDetail={handleOpenDetail} />
        ))}
      </div>

      <DragOverlay dropAnimation={null}>
        {activeApp ? <ApplicationCard app={activeApp} overlay /> : null}
      </DragOverlay>
    </DndContext>
  )
}
