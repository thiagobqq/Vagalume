import { Link } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Panel } from "@/components/app/panel"
import { AppHeader } from "@/components/app/app-header"
import { LogoFull } from "@/components/app/logo"
import { Sparkles, NotebookPen, BellRing, Kanban, ArrowRight, Check, Zap } from "lucide-react"

export function Landing() {
  return (
    <div className="min-h-screen bg-background">
      <AppHeader />
      <main className="mx-auto max-w-6xl space-y-16 px-4 py-12 sm:px-6 sm:py-16">
        {/* hero */}
        <section className="grid items-center gap-10 lg:grid-cols-[1.05fr_0.95fr]">
          <div>
            <LogoFull className="mb-6 w-full max-w-[220px]" />
            <div className="inline-flex items-center gap-2 rounded-full border bg-card px-3 py-1 text-xs font-medium text-muted-foreground shadow-sm">
              <span className="h-2 w-2 rounded-full bg-primary" /> MVP gratuito no ar
            </div>
            <h1 className="mt-5 text-4xl font-extrabold leading-[1.02] tracking-tight sm:text-5xl lg:text-[3.4rem]">
              Não é tracker.
              <br />
              É seu <span className="doodle-underline">analista pessoal</span>
              <br />
              de vagas.
            </h1>
            <p className="mt-5 max-w-[52ch] text-[15px] leading-relaxed text-muted-foreground">
              Pare de perder controle em planilha. Anote como num caderno, veja seu funil e cole o link da vaga — a IA
              preenche empresa, cargo e requisitos sozinha.
            </p>
            <div className="mt-7 flex flex-wrap gap-3">
              <Link to="/app">
                <Button size="lg" className="gap-2">
                  <Sparkles className="h-4 w-4" /> começar meu caderno <ArrowRight className="h-4 w-4" />
                </Button>
              </Link>
              <Link to="/login">
                <Button variant="outline" size="lg">
                  já tenho conta
                </Button>
              </Link>
            </div>
            <div className="mt-5 flex flex-wrap gap-x-5 gap-y-2 text-xs font-medium text-muted-foreground">
              <span className="inline-flex items-center gap-1.5">
                <Check className="h-3.5 w-3.5 text-foreground" /> sem cartão de crédito
              </span>
              <span className="inline-flex items-center gap-1.5">
                <Check className="h-3.5 w-3.5 text-foreground" /> feito para quem aplica toda semana
              </span>
            </div>
          </div>

          {/* preview */}
          <div className="relative">
            <div className="hand absolute -top-3 right-4 z-10 rotate-3 rounded-md bg-primary px-3 py-1 text-base font-bold text-primary-foreground shadow-pop">
              cole o link → preenche!
            </div>
            <Panel texture notebook className="p-3">
              <div className="grid grid-cols-3 gap-2">
                {[
                  { label: "Aplicado", count: 3 },
                  { label: "Entrevista", count: 1 },
                  { label: "Oferta", count: 1 },
                ].map((c) => (
                  <div key={c.label} className="rounded-lg border bg-muted/50 p-2">
                    <div className="flex items-center justify-between">
                      <span className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
                        {c.label}
                      </span>
                      <span className="text-[10px] font-semibold text-muted-foreground">{c.count}</span>
                    </div>
                    <div className="mt-2 space-y-1.5">
                      <div className="h-9 rounded-md border bg-card shadow-sm" />
                      <div className="h-9 rounded-md border bg-card shadow-sm" />
                    </div>
                  </div>
                ))}
              </div>
              <div className="mt-3 flex items-center gap-2 rounded-lg border bg-[var(--st-applied-soft)] px-3 py-2">
                <Zap className="h-3.5 w-3.5 text-[color:var(--st-applied)]" />
                <p className="text-xs font-medium text-[color:var(--st-applied)]">
                  7 dias sem retorno de Nubank — envie follow-up
                </p>
              </div>
            </Panel>
          </div>
        </section>

        {/* features */}
        <section className="grid gap-4 sm:grid-cols-3">
          {[
            { icon: Kanban, title: "Kanban de 5 etapas", desc: "Aplicado → Encerrado. Visual de board, nada de tabela chata." },
            { icon: Sparkles, title: "Cole link → IA extrai", desc: "LinkedIn/Gupy/Greenhouse. Já preenche empresa, cargo e requisitos." },
            { icon: BellRing, title: "Lembrete de 7 dias", desc: "Sem retorno? Aviso no card. Chega de esquecer follow-up." },
          ].map((f) => (
            <Panel key={f.title} notebook className="p-5">
              <div className="grid h-9 w-9 place-items-center rounded-lg border bg-secondary">
                <f.icon className="h-4 w-4" />
              </div>
              <h3 className="mt-3 text-sm font-semibold">{f.title}</h3>
              <p className="mt-1 text-sm text-muted-foreground">{f.desc}</p>
            </Panel>
          ))}
        </section>

        {/* next steps */}
        <section>
          <Panel notebook tape className="p-6 sm:p-8">
            <div className="flex flex-wrap items-center gap-3">
              <span className="hand rotate-[-1deg] text-lg">próximos passos</span>
              <span className="rounded-full bg-secondary px-2.5 py-1 text-xs font-medium text-muted-foreground">
                pago depois
              </span>
            </div>
            <div className="mt-5 grid gap-3 text-sm sm:grid-cols-3">
              {[
                { title: "Score de compatibilidade", desc: "CV vs requisitos — gap específico, não score genérico." },
                { title: "Follow-up com IA", desc: "Rascunho contextual por empresa, cargo e tempo." },
                { title: "Analytics do funil", desc: "Taxa por tipo de vaga, por CV e tempo até resposta." },
              ].map((s) => (
                <div key={s.title} className="rounded-lg border bg-muted/40 p-4">
                  <div className="font-semibold">{s.title}</div>
                  <div className="mt-1 text-xs text-muted-foreground">{s.desc}</div>
                </div>
              ))}
            </div>
            <div className="mt-7 flex flex-wrap items-center gap-3">
              <Link to="/app">
                <Button className="gap-2">
                  <NotebookPen className="h-4 w-4" /> testar grátis agora
                </Button>
              </Link>
              <span className="hand rotate-[-1deg] text-sm text-muted-foreground">
                → sem automação que queima seu nome
              </span>
            </div>
          </Panel>
        </section>

        <footer className="pb-4 text-center text-xs text-muted-foreground">
          © 2026 Vagalume — seu caderno de vagas. Feito para quem aplica toda semana.
        </footer>
      </main>
    </div>
  )
}
