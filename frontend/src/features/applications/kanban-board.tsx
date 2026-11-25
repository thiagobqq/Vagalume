import { useEffect, useMemo, useRef, useState } from "react"
import {
  DndContext,
  DragOverlay,
  KeyboardSensor,
  MouseSensor,
  closestCorners,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from "@dnd-kit/core"
import type { CandidaturaDto, Status } from "@/features/candidaturas/types"
import { STATUS_LABEL, STATUS_ORDER, STATUS_VALUES } from "@/features/candidaturas/types"
import { STATUS_STYLE } from "@/features/candidaturas/status-style"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { useMediaQuery } from "@/hooks/use-media-query"
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
  const [activeTab, setActiveTab] = useState<Status>("Aplicado")
  const suppressClick = useRef(false)
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const isDesktop = useMediaQuery("(min-width: 1024px)")

  useEffect(() => {
    return () => {
      if (timerRef.current) clearTimeout(timerRef.current)
    }
  }, [])

  const sensors = useSensors(
    useSensor(MouseSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor)
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
    if (timerRef.current) clearTimeout(timerRef.current)
    timerRef.current = setTimeout(() => {
      suppressClick.current = false
      timerRef.current = null
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
      {isDesktop ? (
        <div className="grid min-h-0 flex-1 grid-cols-5 grid-rows-1 content-start items-start gap-4">
          {STATUS_ORDER.map((s) => (
            <KanbanColumn
              key={s}
              status={s}
              apps={grouped[s]}
              onOpenDetail={handleOpenDetail}
              onMove={onMove}
              draggable
            />
          ))}
        </div>
      ) : (
        <Tabs
          value={activeTab}
          onValueChange={(v) => setActiveTab(v as Status)}
          className="flex min-h-0 flex-1 flex-col"
        >
          <TabsList className="h-auto w-full justify-start gap-1 overflow-x-auto rounded-xl border-[1.5px] border-ink bg-card p-1 shadow-hard-xs">
            {STATUS_ORDER.map((s) => (
              <TabsTrigger key={s} value={s} className="shrink-0 gap-1.5 data-[state=active]:shadow-hard-xs">
                <span
                  className="h-2 w-2 shrink-0 rounded-full border border-ink/20"
                  style={{ background: STATUS_STYLE[s].dot }}
                  aria-hidden="true"
                />
                {STATUS_LABEL[s]}
                <span className="grid min-w-5 place-items-center rounded-full border border-ink/40 bg-card px-1.5 py-0.5 text-[11px] font-bold leading-none">
                  {grouped[s].length}
                </span>
              </TabsTrigger>
            ))}
          </TabsList>
          <TabsContent
            value={activeTab}
            className="mt-3 flex min-h-0 flex-1 flex-col focus-visible:outline-none"
          >
            <KanbanColumn
              status={activeTab}
              apps={grouped[activeTab]}
              onOpenDetail={handleOpenDetail}
              onMove={onMove}
              draggable={false}
            />
          </TabsContent>
        </Tabs>
      )}

      <DragOverlay dropAnimation={null}>
        {activeApp ? <ApplicationCard app={activeApp} overlay /> : null}
      </DragOverlay>
    </DndContext>
  )
}
