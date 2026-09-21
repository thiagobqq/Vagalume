import { useState } from "react"
import { Link, NavLink, Outlet, useNavigate } from "react-router-dom"
import { Dialog, DialogContent } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { ThemeToggle } from "@/components/theme-toggle"
import { useAuth } from "@/features/auth/auth-context"
import { PERMISSOES } from "@/features/auth/permissoes"
import { Kanban, FileText, LogOut, Menu, X, Sparkles, ShieldCheck, Zap } from "lucide-react"
import { cn } from "@/lib/utils"

const NAV = [
  { to: "/app", label: "Candidaturas", icon: Kanban, end: true, permissao: undefined },
  { to: "/app/vagas", label: "Busca de vagas", icon: Zap, end: false, permissao: PERMISSOES.buscaVagas },
  { to: "/app/curriculos", label: "Currículos", icon: FileText, end: false, permissao: undefined },
  { to: "/app/admin", label: "Permissões", icon: ShieldCheck, end: false, permissao: PERMISSOES.admin },
]

function initials(nome?: string) {
  if (!nome) return "?"
  const parts = nome.trim().split(/\s+/)
  return ((parts[0]?.[0] ?? "") + (parts[1]?.[0] ?? "")).toUpperCase()
}

function Brand() {
  return (
    <Link to="/" className="flex items-center gap-2.5">
      <div className="grid h-8 w-8 place-items-center rounded-lg bg-primary text-[15px] leading-none text-primary-foreground shadow-sm">
        ✦
      </div>
      <div className="leading-tight">
        <div className="text-sm font-extrabold tracking-tight">vagalume</div>
        <div className="text-[11px] font-medium text-muted-foreground">caderno de vagas</div>
      </div>
    </Link>
  )
}

function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
  const nav = useNavigate()
  const { user, logout, hasPermissao } = useAuth()

  const itens = NAV.filter((item) => !item.permissao || hasPermissao(item.permissao))

  return (
    <div className="flex h-full flex-col">
      <div className="flex h-16 items-center px-5">
        <Brand />
      </div>

      <nav className="flex-1 space-y-1 px-3 py-2">
        {itens.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            onClick={onNavigate}
            className={({ isActive }) =>
              cn(
                "flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
                isActive
                  ? "bg-secondary text-foreground"
                  : "text-muted-foreground hover:bg-accent hover:text-foreground"
              )
            }
          >
            <item.icon className="h-4 w-4" />
            {item.label}
          </NavLink>
        ))}

        <div className="pt-2">
          <div className="px-3 pb-1 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground/70">
            Atalhos
          </div>
          <Link
            to="/"
            onClick={onNavigate}
            className="flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
          >
            <Sparkles className="h-4 w-4" />
            Página inicial
          </Link>
        </div>
      </nav>

      <div className="border-t p-3">
        <div className="flex items-center gap-3 rounded-lg px-2 py-2">
          <div className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-secondary text-xs font-bold">
            {initials(user?.nome)}
          </div>
          <div className="min-w-0 flex-1">
            <div className="truncate text-xs font-semibold">{user?.nome ?? "visitante"}</div>
            <div className="truncate text-[11px] text-muted-foreground">{user?.email ?? ""}</div>
          </div>
        </div>
        <div className="mt-1 flex items-center gap-1">
          <ThemeToggle />
          <Button
            variant="ghost"
            size="sm"
            className="flex-1 justify-start gap-2 text-muted-foreground hover:text-foreground"
            onClick={() => {
              logout()
              nav("/login")
            }}
          >
            <LogOut className="h-4 w-4" /> sair
          </Button>
        </div>
      </div>
    </div>
  )
}

export function AppShell() {
  const [mobileOpen, setMobileOpen] = useState(false)

  return (
    <div className="min-h-screen bg-background">
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-64 border-r bg-card lg:block">
        <SidebarContent />
      </aside>

      <div className="lg:pl-64">
        <header className="sticky top-0 z-20 flex h-14 items-center gap-3 border-b bg-background/80 px-4 backdrop-blur-md sm:px-6">
          <button
            onClick={() => setMobileOpen(true)}
            className="-ml-1 grid h-9 w-9 place-items-center rounded-lg text-muted-foreground transition-colors hover:bg-accent hover:text-foreground lg:hidden"
            aria-label="Abrir menu"
          >
            <Menu className="h-5 w-5" />
          </button>
          <span className="text-sm font-semibold tracking-tight lg:hidden">vagalume</span>
          <div className="ml-auto flex items-center gap-1">
            <ThemeToggle />
          </div>
        </header>

        <main className="mx-auto w-full max-w-[1400px] px-4 py-6 sm:px-6 lg:py-8">
          <Outlet />
        </main>
      </div>

      <Dialog open={mobileOpen} onOpenChange={setMobileOpen}>
        <DialogContent className="!left-0 !top-0 !h-full !w-[280px] !max-w-[85vw] !translate-x-0 !translate-y-0 !rounded-none !rounded-r-xl !p-0 data-[state=closed]:slide-out-to-left data-[state=open]:slide-in-from-left">
          <div className="absolute right-3 top-3 z-10">
            <Button variant="ghost" size="icon" onClick={() => setMobileOpen(false)} aria-label="Fechar menu">
              <X className="h-4 w-4" />
            </Button>
          </div>
          <SidebarContent onNavigate={() => setMobileOpen(false)} />
        </DialogContent>
      </Dialog>
    </div>
  )
}
