import { Moon, Sun, Sparkles } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { useTheme } from "@/features/theme/theme-provider"

export function ThemeToggle() {
  const { theme, isDark, setTheme, isAnimating, firefliesEnabled, setFirefliesEnabled } = useTheme()

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          aria-label="Aparência"
          disabled={isAnimating}
          className="relative text-muted-foreground hover:text-foreground"
          title="Aparência"
        >
          <span className="relative grid h-full w-full place-items-center">
            <Sun
              className={`absolute h-[18px] w-[18px] transition-all duration-300 ${
                isDark ? "rotate-90 scale-0 opacity-0" : "rotate-0 scale-100 opacity-100"
              }`}
            />
            <Moon
              className={`absolute h-[18px] w-[18px] transition-all duration-300 ${
                isDark ? "rotate-0 scale-100 opacity-100" : "-rotate-90 scale-0 opacity-0"
              }`}
            />
          </span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-48">
        <DropdownMenuLabel>Aparência</DropdownMenuLabel>
        <DropdownMenuRadioGroup value={theme} onValueChange={(v) => setTheme(v as "light" | "dark")}>
          <DropdownMenuRadioItem value="light">
            <Sun className="h-4 w-4" /> Claro
          </DropdownMenuRadioItem>
          <DropdownMenuRadioItem value="dark">
            <Moon className="h-4 w-4" /> Escuro
          </DropdownMenuRadioItem>
        </DropdownMenuRadioGroup>
        <DropdownMenuSeparator />
        <DropdownMenuCheckboxItem
          checked={firefliesEnabled}
          onCheckedChange={(v) => setFirefliesEnabled(v === true)}
        >
          <Sparkles className="h-4 w-4" /> Vagalumes
        </DropdownMenuCheckboxItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
