import { Moon, Sun } from "lucide-react"
import { Button } from "@/components/ui/button"
import { useTheme } from "@/features/theme/theme-provider"

export function ThemeToggle({ variant = "ghost" as const }) {
  const { isDark, toggle, isAnimating } = useTheme()
  return (
    <Button
      variant={variant as never}
      size="icon"
      aria-label={isDark ? "Ativar modo claro" : "Ativar modo escuro"}
      aria-pressed={isDark}
      onClick={toggle}
      disabled={isAnimating}
      className="relative h-9 w-9 rounded-xl border-[1.5px] border-foreground bg-card shadow-[3px_3px_0_var(--foreground)] hover:translate-y-[-1px] hover:shadow-[4px_4px_0_var(--foreground)] transition-all overflow-hidden"
      title={isDark ? "Modo claro — virar página" : "Modo escuro — virar página"}
    >
      <span className="relative grid place-items-center w-full h-full">
        <Sun
          className={`absolute w-[18px] h-[18px] transition-all duration-500 ${
            isDark ? "rotate-90 scale-0 opacity-0" : "rotate-0 scale-100 opacity-100"
          }`}
        />
        <Moon
          className={`absolute w-[18px] h-[18px] transition-all duration-500 ${
            isDark ? "rotate-0 scale-100 opacity-100" : "-rotate-90 scale-0 opacity-0"
          }`}
        />
      </span>
    </Button>
  )
}
