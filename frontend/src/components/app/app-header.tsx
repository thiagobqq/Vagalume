import { Link, useLocation, useNavigate } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Sparkles, LayoutDashboard, LogIn, Plus, LogOut } from "lucide-react"
import { useAuth } from "@/features/auth/auth-context"
import { ThemeToggle } from "@/components/theme-toggle"

export function AppHeader({ onAdd }: { onAdd?: () => void }) {
  const loc = useLocation()
  const nav = useNavigate()
  const { isAuthenticated, logout, user } = useAuth()
  const isDashboard = loc.pathname === "/app"
  return (
    <header className="sticky top-0 z-40 bg-background/80 backdrop-blur-md border-b-[1.5px] border-foreground">
      <div className="max-w-[1280px] mx-auto px-4 sm:px-6 h-[64px] flex items-center justify-between gap-4">
        <Link to="/" className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-primary border-[1.5px] border-foreground shadow-[3px_3px_0_var(--foreground)] grid place-items-center">
            <span className="text-lg leading-none">✦</span>
          </div>
          <div className="leading-tight">
            <div className="font-extrabold text-[17px] tracking-tight leading-none">vagalume</div>
            <div className="text-[11px] font-semibold tracking-wide text-foreground/70 leading-none">caderno de vagas</div>
          </div>
        </Link>

        <nav className="flex items-center gap-2">
          <ThemeToggle />
          {isDashboard ? (
            <>
              {user && <span className="hidden sm:inline text-xs text-muted-foreground">olá, <b className="text-foreground">{user.nome}</b></span>}
              <Button size="sm" onClick={onAdd} className="gap-1.5">
                <Plus className="w-4 h-4" /> <span className="hidden sm:inline">Nova candidatura</span><span className="sm:hidden">Nova</span>
              </Button>
              {isAuthenticated ? (
                <Button
                  variant="ghost"
                  size="sm"
                  className="gap-1.5"
                  onClick={() => {
                    logout()
                    nav("/login")
                  }}
                >
                  <LogOut className="w-4 h-4" /> sair
                </Button>
              ) : (
                <Link to="/login">
                  <Button variant="ghost" size="sm">entrar</Button>
                </Link>
              )}
            </>
          ) : (
            <>
              <Link to="/app" className="hidden sm:inline-flex">
                <Button variant="ghost" size="sm" className="gap-1.5">
                  <LayoutDashboard className="w-4 h-4" /> dashboard
                </Button>
              </Link>
              {!isAuthenticated ? (
                <>
                  <Link to="/login">
                    <Button variant="outline" size="sm" className="gap-1.5">
                      <LogIn className="w-4 h-4" /> entrar
                    </Button>
                  </Link>
                  <Link to="/register">
                    <Button size="sm" className="gap-1.5">
                      <Sparkles className="w-4 h-4" /> começar grátis
                    </Button>
                  </Link>
                </>
              ) : (
                <Link to="/app">
                  <Button size="sm" className="gap-1.5">
                    <LayoutDashboard className="w-4 h-4" /> ir ao caderno
                  </Button>
                </Link>
              )}
            </>
          )}
        </nav>
      </div>
    </header>
  )
}
