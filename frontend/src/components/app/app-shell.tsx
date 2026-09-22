import { Link, NavLink, Outlet, useNavigate } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { ThemeToggle } from "@/components/theme-toggle"
import { LogoMark } from "@/components/app/logo"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { useAuth } from "@/features/auth/auth-context"
import { PERMISSOES } from "@/features/auth/permissoes"
import { Kanban, FileText, LogOut, Menu, Sparkles, ShieldCheck, Zap, Settings } from "lucide-react"
import { cn } from "@/lib/utils"

const NAV = [
  { to: "/app", label: "Candidaturas", icon: Kanban, end: true, permissao: undefined },
  { to: "/app/vagas", label: "Busca de vagas", icon: Zap, end: false, permissao: PERMISSOES.verifyUser },
  { to: "/app/curriculos", label: "Currículos", icon: FileText, end: false, permissao: undefined },
  { to: "/app/configuracoes", label: "Configurações", icon: Settings, end: false, permissao: PERMISSOES.verifyUser },
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
      <LogoMark />
      <div className="leading-tight">
        <div className="text-sm font-extrabold tracking-tight">vagalume</div>
        <div className="text-[11px] font-medium text-muted-foreground">caderno de vagas</div>
      </div>
    </Link>
  )
}

export function AppShell() {
  const nav = useNavigate()
  const { user, logout, hasPermissao } = useAuth()
  const itens = NAV.filter((item) => !item.permissao || hasPermissao(item.permissao))

  function handleLogout() {
    logout()
    nav("/login")
  }

  return (
    <div className="min-h-screen bg-background">
      <header className="sticky top-0 z-40 border-b-[1.5px] border-ink bg-[var(--nb-paper)]/90 backdrop-blur-md">
        <div className="mx-auto flex h-16 w-full max-w-[1600px] items-center gap-3 px-4 sm:px-6">
          <Brand />

          <nav className="ml-4 hidden items-center gap-1 md:flex">
            {itens.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  cn(
                    "flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-semibold transition-colors",
                    isActive
                      ? "border-[1.5px] border-ink bg-primary text-primary-foreground shadow-hard-xs"
                      : "text-muted-foreground hover:bg-accent hover:text-foreground"
                  )
                }
              >
                <item.icon className="h-4 w-4" />
                {item.label}
              </NavLink>
            ))}
          </nav>

          <div className="ml-auto flex items-center gap-1.5">
            <ThemeToggle />

            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button
                  className="hidden items-center gap-2 rounded-lg border-[1.5px] border-ink bg-card py-1 pl-1 pr-3 shadow-hard-xs transition-transform hover:translate-x-[1px] hover:translate-y-[1px] hover:shadow-none md:flex"
                  aria-label="Menu do usuário"
                >
                  <span className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-secondary text-[11px] font-bold">
                    {initials(user?.nome)}
                  </span>
                  <span className="max-w-[140px] truncate text-xs font-semibold">
                    {user?.nome ?? "visitante"}
                  </span>
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-56">
                <DropdownMenuLabel className="normal-case">
                  <div className="truncate text-xs font-bold tracking-normal text-foreground">
                    {user?.nome ?? "visitante"}
                  </div>
                  <div className="truncate text-[11px] font-medium tracking-normal text-muted-foreground">
                    {user?.email ?? ""}
                  </div>
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem asChild>
                  <Link to="/">
                    <Sparkles className="h-4 w-4" /> Página inicial
                  </Link>
                </DropdownMenuItem>
                <DropdownMenuItem onSelect={handleLogout} className="text-destructive focus:text-destructive">
                  <LogOut className="h-4 w-4" /> sair
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>

            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline" size="icon" className="md:hidden" aria-label="Abrir menu">
                  <Menu className="h-5 w-5" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-56 md:hidden">
                <DropdownMenuLabel className="normal-case">
                  <div className="truncate text-xs font-bold tracking-normal text-foreground">
                    {user?.nome ?? "visitante"}
                  </div>
                  <div className="truncate text-[11px] font-medium tracking-normal text-muted-foreground">
                    {user?.email ?? ""}
                  </div>
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                {itens.map((item) => (
                  <DropdownMenuItem key={item.to} asChild>
                    <NavLink to={item.to} end={item.end}>
                      <item.icon className="h-4 w-4" /> {item.label}
                    </NavLink>
                  </DropdownMenuItem>
                ))}
                <DropdownMenuSeparator />
                <DropdownMenuItem asChild>
                  <Link to="/">
                    <Sparkles className="h-4 w-4" /> Página inicial
                  </Link>
                </DropdownMenuItem>
                <DropdownMenuItem onSelect={handleLogout} className="text-destructive focus:text-destructive">
                  <LogOut className="h-4 w-4" /> sair
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>
      </header>

      <main className="mx-auto w-full max-w-[1600px] px-4 py-6 sm:px-6 lg:py-8">
        <Outlet />
      </main>
    </div>
  )
}
