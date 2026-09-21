import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-lg text-sm font-semibold transition-[transform,box-shadow,background-color,border-color] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/70 focus-visible:ring-offset-2 focus-visible:ring-offset-background disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0",
  {
    variants: {
      variant: {
        default:
          "border-[1.5px] border-ink bg-primary text-primary-foreground shadow-hard-xs hover:bg-primary/85 active:translate-x-[2px] active:translate-y-[2px] active:shadow-none",
        secondary: "border-[1.5px] border-transparent bg-secondary text-secondary-foreground hover:bg-secondary/70",
        outline:
          "border-[1.5px] border-ink bg-card text-foreground shadow-hard-xs hover:bg-accent hover:text-accent-foreground active:translate-x-[2px] active:translate-y-[2px] active:shadow-none",
        ghost: "border-transparent text-foreground/80 hover:bg-accent hover:text-accent-foreground",
        destructive:
          "border-[1.5px] border-ink bg-destructive text-destructive-foreground shadow-hard-xs hover:bg-destructive/90 active:translate-x-[2px] active:translate-y-[2px] active:shadow-none",
        link: "text-foreground underline-offset-4 hover:underline",
      },
      size: {
        default: "h-9 px-4 py-2",
        sm: "h-8 rounded-md px-3 text-xs",
        lg: "h-10 rounded-lg px-6",
        icon: "h-9 w-9",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : "button"
    return (
      <Comp
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        {...props}
      />
    )
  }
)
Button.displayName = "Button"

export { Button, buttonVariants }
