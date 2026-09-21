import { cn } from "@/lib/utils"

const COLOR: Record<"yellow" | "pink" | "blue" | "green", string> = {
  yellow: "bg-[var(--crayon-yellow)]/85",
  pink: "bg-[var(--crayon-pink)]/25",
  blue: "bg-[var(--crayon-blue)]/25",
  green: "bg-[var(--crayon-green)]/25",
}

export function WashiTape({
  className,
  children,
  color = "yellow",
}: {
  className?: string
  children?: React.ReactNode
  color?: "yellow" | "pink" | "blue" | "green"
}) {
  return (
    <div
      className={cn(
        "tape rounded-sm px-3 py-1 text-xs font-bold text-foreground",
        COLOR[color],
        className
      )}
    >
      {children}
    </div>
  )
}
