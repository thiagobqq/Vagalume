import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"

const badgeVariants = cva(
  "inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-ring/60 focus:ring-offset-2",
  {
    variants: {
      variant: {
        default: "border-transparent bg-primary text-primary-foreground",
        secondary: "border-transparent bg-secondary text-secondary-foreground",
        outline: "border-ink/70 bg-card text-foreground",
        applied: "border-transparent bg-[var(--st-applied-soft)] text-[color:var(--st-applied)]",
        triage: "border-transparent bg-[var(--st-triage-soft)] text-[color:var(--st-triage)]",
        interview: "border-transparent bg-[var(--st-interview-soft)] text-[color:var(--st-interview)]",
        offer: "border-transparent bg-[var(--st-offer-soft)] text-[color:var(--st-offer)]",
        closed: "border-transparent bg-[var(--st-closed-soft)] text-[color:var(--st-closed)]",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)

export interface BadgeProps extends React.HTMLAttributes<HTMLDivElement>, VariantProps<typeof badgeVariants> {}

function Badge({ className, variant, ...props }: BadgeProps) {
  return <div className={cn(badgeVariants({ variant }), className)} {...props} />
}

export { Badge, badgeVariants }
