import { cn } from "@/lib/utils"

type PanelProps = React.HTMLAttributes<HTMLDivElement> & {
  /** micro-textura pautada do caderno (sutil) */
  texture?: boolean
  /** borda tinta + sombra dura (cara de cartão desenhado) */
  notebook?: boolean
  /** fita washi no topo (só faz sentido com notebook) */
  tape?: boolean
}

export function Panel({ className, children, texture = false, notebook = false, tape = false, ...props }: PanelProps) {
  return (
    <div
      className={cn(
        "relative rounded-xl border bg-card text-card-foreground shadow-card",
        texture && "paper-texture",
        notebook && "border-[1.5px] border-ink shadow-hard",
        className
      )}
      {...props}
    >
      {tape && (
        <div className="tape absolute -top-3 left-1/2 z-10 h-6 w-24 -translate-x-1/2 rotate-[-2deg] rounded-sm" />
      )}
      {children}
    </div>
  )
}
