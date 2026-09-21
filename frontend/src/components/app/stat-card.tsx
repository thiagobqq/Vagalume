import { cn } from "@/lib/utils"

export function StatCard({
  label,
  value,
  hint,
  icon: Icon,
  tone = "default",
}: {
  label: string
  value: React.ReactNode
  hint?: string
  icon?: React.ComponentType<{ className?: string }>
  tone?: "default" | "warning"
}) {
  return (
    <div className="rounded-xl border bg-card p-4 shadow-card">
      <div className="flex items-center justify-between gap-2">
        <span className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">{label}</span>
        {Icon && (
          <Icon
            className={cn(
              "h-4 w-4",
              tone === "warning" ? "text-[color:var(--st-applied)]" : "text-muted-foreground/70"
            )}
          />
        )}
      </div>
      <div
        className={cn(
          "mt-2 text-2xl font-bold tracking-tight",
          tone === "warning" && "text-[color:var(--st-applied)]"
        )}
      >
        {value}
      </div>
      {hint && <div className="mt-0.5 text-xs text-muted-foreground">{hint}</div>}
    </div>
  )
}
