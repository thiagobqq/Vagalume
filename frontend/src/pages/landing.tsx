import { Link } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { PaperSheet } from "@/components/app/paper-sheet"
import { AppHeader } from "@/components/app/app-header"
import { Sparkles, NotebookPen, BellRing, Kanban, ArrowRight, Check } from "lucide-react"

export function Landing() {
  return (
    <div className="min-h-screen bg-[#fef9f0]">
      <AppHeader />
      <main className="max-w-[1120px] mx-auto px-4 sm:px-6 py-8 sm:py-12 space-y-10">
        {/* hero */}
        <PaperSheet withSpiral tape="top" className="p-6 sm:p-10 overflow-visible">
          <div className="grid lg:grid-cols-[1.1fr_0.9fr] gap-8 items-center">
            <div>
              <div className="inline-flex items-center gap-2 rounded-full border-[1.5px] border-foreground bg-[#fef3c7] px-3 py-1 text-xs font-bold shadow-[2px_2px_0_var(--foreground)]">
                <span className="w-2 h-2 rounded-full bg-[#f7c948] border border-foreground" /> MVP gratuito no ar
              </div>
              <h1 className="mt-4 text-[34px] sm:text-[44px] font-extrabold tracking-tight leading-[0.95]">
                Não é tracker.
                <br />
                É seu <span className="doodle-underline">analista pessoal</span>
                <br />
                de vagas.
              </h1>
              <p className="mt-4 text-[15px] leading-relaxed text-muted-foreground max-w-[52ch]">
                Pare de perder controle em planilha. Anote como num caderno, veja seu funil e cole o link da vaga — a IA preenche empresa, cargo e requisitos sozinha.
              </p>
              <div className="mt-6 flex flex-wrap gap-3">
                <Link to="/app">
                  <Button size="lg" className="gap-2">
                    <Sparkles className="w-4 h-4" /> começar meu caderno <ArrowRight className="w-4 h-4" />
                  </Button>
                </Link>
                <Link to="/login">
                  <Button variant="outline" size="lg">
                    já tenho conta
                  </Button>
                </Link>
              </div>
              <div className="mt-4 flex flex-wrap gap-2 text-xs font-bold">
                <span className="inline-flex items-center gap-1.5 bg-white border border-foreground rounded-full px-3 py-1">
                  <Check className="w-3.5 h-3.5" /> sem cartão
                </span>
                <span className="inline-flex items-center gap-1.5 bg-white border border-foreground rounded-full px-3 py-1">✎ feito para quem aplica toda semana</span>
              </div>
            </div>

            {/* mini kanban preview */}
            <div className="relative">
              <div className="absolute -top-3 -right-2 rotate-[8deg] bg-[#f7c948] border-[1.5px] border-foreground rounded-lg px-3 py-1 text-xs font-extrabold shadow-[3px_3px_0_var(--foreground)] hand text-base">
                cole o link → preenche!
              </div>
              <div className="rounded-2xl border-[1.5px] border-foreground bg-white p-3 shadow-[6px_6px_0_var(--foreground)] grid grid-cols-3 gap-2">
                {[
                  { label: "Aplicado", count: 3, color: "bg-[#fef3c7]" },
                  { label: "Entrevista", count: 1, color: "bg-[#ede9fe]" },
                  { label: "Oferta", count: 1, color: "bg-[#dcfce7]" },
                ].map((c) => (
                  <div key={c.label} className={`rounded-xl border border-foreground p-2 ${c.color}`}>
                    <div className="text-[10px] font-bold uppercase tracking-widest">{c.label}</div>
                    <div className="text-lg font-extrabold">{c.count}</div>
                    <div className="mt-2 space-y-1.5">
                      <div className="h-10 rounded-lg bg-white border border-foreground/20" />
                      <div className="h-10 rounded-lg bg-white border border-foreground/20" />
                    </div>
                  </div>
                ))}
              </div>
              <div className="mt-3 rounded-xl border-[1.5px] border-foreground bg-[#f7c948] p-3 shadow-[3px_3px_0_var(--foreground)] flex gap-2 items-center">
                <span>⚡</span>
                <p className="text-xs font-bold">7 dias sem retorno de Nubank — envie follow-up</p>
              </div>
            </div>
          </div>
        </PaperSheet>

        {/* features */}
        <div className="grid sm:grid-cols-3 gap-4">
          {[
            { icon: Kanban, title: "Kanban caderno", desc: "5 colunas: aplicado → recusado. Visual de post-its, nada de tabela chata.", color: "bg-[#dbeafe]" },
            { icon: Sparkles, title: "Cole link → IA extrai", desc: "LinkedIn/Gupy/Greenhouse. Já preenche empresa, cargo e requisitos.", color: "bg-[#fef3c7]" },
            { icon: BellRing, title: "Lembrete 7 dias", desc: "Sem retorno? Aviso amarelo no card. Chega de esquecer follow-up.", color: "bg-[#fce7f3]" },
          ].map((f) => (
            <PaperSheet key={f.title} className={`p-5 ${f.color}`} withMargin={false} ruled={false}>
              <div className="w-9 h-9 rounded-xl bg-white border-[1.5px] border-foreground grid place-items-center shadow-[2px_2px_0_var(--foreground)]">
                <f.icon className="w-4 h-4" />
              </div>
              <h3 className="font-extrabold mt-3">{f.title}</h3>
              <p className="text-sm text-muted-foreground mt-1">{f.desc}</p>
            </PaperSheet>
          ))}
        </div>

        {/* pricing teaser */}
        <PaperSheet className="p-6 sm:p-8" withMargin={false} ruled={false}>
          <div className="flex flex-wrap gap-2 items-center">
            <span className="hand text-lg rotate-[-1deg]">próximos passos</span>
            <span className="text-xs font-bold bg-foreground text-white rounded-full px-2.5 py-1">pago depois</span>
          </div>
          <div className="grid sm:grid-cols-3 gap-3 mt-4 text-sm">
            <div className="rounded-xl bg-white border border-foreground p-3">
              <div className="font-bold">Score real de compatibilidade</div>
              <div className="text-muted-foreground text-xs mt-1">CV vs requisitos — gap específico, não score genérico.</div>
            </div>
            <div className="rounded-xl bg-white border border-foreground p-3">
              <div className="font-bold">Follow-up com IA</div>
              <div className="text-muted-foreground text-xs mt-1">Rascunho contextual por empresa/cargo/tempo.</div>
            </div>
            <div className="rounded-xl bg-white border border-foreground p-3">
              <div className="font-bold">Analytics do funil</div>
              <div className="text-muted-foreground text-xs mt-1">Taxa por tipo de vaga, por CV, tempo até resposta.</div>
            </div>
          </div>
          <div className="mt-6 flex gap-3">
            <Link to="/app">
              <Button className="gap-2">
                <NotebookPen className="w-4 h-4" /> testar grátis agora
              </Button>
            </Link>
            <span className="hand text-sm self-center rotate-[-1deg]">→ sem automação que queima seu nome</span>
          </div>
        </PaperSheet>

        <div className="text-center text-xs text-muted-foreground py-4">© 2026 Vagalume — seu caderno de vagas. Feito para quem aplica toda semana.</div>
      </main>
    </div>
  )
}
