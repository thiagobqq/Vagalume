import { cn } from "@/lib/utils"

type PostItStatProps = {
  label: string
  value: number | string
  /** cor do marcador no topo (qualquer cor CSS) */
  color: string
  className?: string
}

/**
 * Pequeno painel estilo post-it usado para contadores/métricas.
 * Tem um marcador colorido no topo e fundo levemente tingido.
 */
export function PostItStat({ label, value, color, className }: PostItStatProps) {
  return (
    <div
      className={cn(
        "relative overflow-hidden rounded-xl border-[1.5px] border-ink/70 px-3 pb-2.5 pt-4 shadow-hard-xs",
        className
      )}
      style={{ background: `color-mix(in srgb, ${color} 12%, var(--card))` }}
    >
      {/* marcador colorido no topo */}
      <span className="absolute inset-x-0 top-0 h-1.5" style={{ background: color }} aria-hidden="true" />
      <div className="text-lg font-bold tabular-nums">{value}</div>
      <div className="text-[11px] uppercase tracking-wide text-muted-foreground">{label}</div>
    </div>
  )
}
