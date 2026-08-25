import { cn } from "@/lib/utils"

type PaperSheetProps = React.HTMLAttributes<HTMLDivElement> & {
  withSpiral?: boolean
  withMargin?: boolean
  ruled?: boolean
  tape?: "top" | "none"
  rotate?: string
}

export function PaperSheet({ className, children, withSpiral = false, withMargin = true, ruled = true, tape = "none", rotate, ...props }: PaperSheetProps) {
  return (
    <div
      className={cn(
        "relative bg-white border-[1.5px] border-foreground rounded-2xl shadow-[6px_6px_0_var(--foreground)] overflow-hidden",
        ruled && "ruled-paper",
        rotate,
        className
      )}
      {...props}
    >
      {tape === "top" && (
        <div className="absolute -top-3 left-1/2 -translate-x-1/2 rotate-[-2deg] w-28 h-7 tape rounded-sm z-10" />
      )}

      {withSpiral && (
        <div className="absolute left-0 top-0 bottom-0 w-[56px] border-r-[2px] border-foreground bg-[#fef9f0] hidden sm:flex flex-col items-center py-4 gap-2">
          <div className="w-full h-full spiral-holes opacity-90" />
        </div>
      )}

      {withMargin && (
        <div className="absolute top-0 bottom-0 left-[56px] sm:left-[64px] w-[2px] bg-[#ff6b6b]/60 hidden sm:block" />
      )}

      <div className={cn(withSpiral && "sm:pl-[72px]", withMargin && "sm:pl-[72px]", "relative")}>{children}</div>
    </div>
  )
}
