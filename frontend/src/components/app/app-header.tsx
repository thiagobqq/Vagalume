import { Link } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { ThemeToggle } from "@/components/theme-toggle"
import { useAuth } from "@/features/auth/auth-context"
import { LayoutDashboard, LogIn, Sparkles } from "lucide-react"

export function AppHeader() {
  const { isAuthenticated } = useAuth()

  return (
    <header className="sticky top-0 z-40 border-b bg-background/80 backdrop-blur-md">
      <div className="mx-auto flex h-14 max-w-6xl items-center justify-between gap-4 px-4 sm:px-6">
        <Link to="/" className="flex items-center gap-2.5">
          <div className="grid h-8 w-8 place-items-center rounded-lg bg-primary text-[15px] leading-none text-primary-foreground shadow-sm">
            ✦
          </div>
          <div className="leading-tight">
            <div className="text-sm font-extrabold tracking-tight">vagalume</div>
            <div className="text-[11px] font-medium text-muted-foreground">caderno de vagas</div>
          </div>
        </Link>

        <nav className="flex items-center gap-1.5">
          <ThemeToggle />
          {isAuthenticated ? (
            <Link to="/app">
              <Button size="sm" className="gap-1.5">
                <LayoutDashboard className="h-4 w-4" /> ir ao caderno
              </Button>
            </Link>
          ) : (
            <>
              <Link to="/login" className="hidden sm:inline-flex">
                <Button variant="ghost" size="sm" className="gap-1.5">
                  <LogIn className="h-4 w-4" /> entrar
                </Button>
              </Link>
              <Link to="/register">
                <Button size="sm" className="gap-1.5">
                  <Sparkles className="h-4 w-4" /> começar grátis
                </Button>
              </Link>
            </>
          )}
        </nav>
      </div>
    </header>
  )
}
