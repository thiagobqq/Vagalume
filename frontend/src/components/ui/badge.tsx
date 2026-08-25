import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"

const badgeVariants = cva(
  "inline-flex items-center rounded-full border-[1.5px] border-foreground px-2.5 py-0.5 text-xs font-bold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2",
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground shadow-[2px_2px_0_var(--foreground)]",
        secondary: "bg-secondary text-secondary-foreground",
        outline: "bg-white text-foreground",
        yellow: "bg-[#fef3c7] text-[#92400e] border-[#f7c948]",
        pink: "bg-[#fce7f3] text-[#9d174d]",
        blue: "bg-[#dbeafe] text-[#1e40af]",
        green: "bg-[#dcfce7] text-[#14532d]",
        orange: "bg-[#ffedd5] text-[#7c2d12]",
        purple: "bg-[#ede9fe] text-[#4c1d95]",
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
