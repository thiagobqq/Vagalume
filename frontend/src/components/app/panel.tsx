import { cn } from "@/lib/utils"

type PanelProps = React.HTMLAttributes<HTMLDivElement> & {
  /** micro-textura pautada do caderno (sutil) */
  texture?: boolean
}

export function Panel({ className, children, texture = false, ...props }: PanelProps) {
  return (
    <div
      className={cn("rounded-xl border bg-card text-card-foreground shadow-card", texture && "paper-texture", className)}
      {...props}
    >
      {children}
    </div>
  )
}
