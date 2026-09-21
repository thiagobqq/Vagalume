import { cn } from "@/lib/utils"

type PaperSheetProps = React.HTMLAttributes<HTMLDivElement> & {
  withSpiral?: boolean
  withMargin?: boolean
  ruled?: boolean
  tape?: "top" | "none"
  rotate?: string
}

export function PaperSheet({
  className,
  children,
  withSpiral = false,
  withMargin = true,
  ruled = true,
  tape = "none",
  rotate,
  ...props
}: PaperSheetProps) {
  return (
    <div
      className={cn(
        "relative overflow-hidden rounded-2xl border-[1.5px] border-ink bg-card shadow-hard",
        ruled && "ruled-paper",
        rotate,
        className
      )}
      {...props}
    >
      {tape === "top" && (
        <div className="tape absolute -top-3 left-1/2 z-10 h-7 w-28 -translate-x-1/2 rotate-[-2deg] rounded-sm" />
      )}

      {withSpiral && (
        <div className="absolute inset-y-0 left-0 hidden w-14 flex-col items-center border-r-[1.5px] border-ink/15 bg-[var(--nb-paper)] py-4 sm:flex">
          <div className="spiral-holes h-full w-full" />
        </div>
      )}

      {withMargin && (
        <div className="absolute inset-y-0 left-14 hidden w-[2px] bg-[#ff6b6b]/50 sm:block" />
      )}

      <div className={cn("relative", (withSpiral || withMargin) && "sm:pl-[72px]")}>{children}</div>
    </div>
  )
}
