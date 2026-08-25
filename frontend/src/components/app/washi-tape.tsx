import { cn } from "@/lib/utils"

export function WashiTape({ className, children, color = "yellow" }: { className?: string; children?: React.ReactNode; color?: "yellow" | "pink" | "blue" | "green" }) {
  const colors = {
    yellow: "bg-[#f7c948]/90",
    pink: "bg-[#f06292]/20 border-pink-200",
    blue: "bg-[#dbeafe]",
    green: "bg-[#dcfce7]",
  }
  return <div className={cn("tape rounded-sm px-3 py-1 text-xs font-bold border border-foreground/10", colors[color], className)}>{children}</div>
}
