import { Link } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { NotebookCard, NotebookPanel } from "@/components/app/notebook-panel"
import { PaperSheet } from "@/components/app/paper-sheet"
import { WashiTape } from "@/components/app/washi-tape"
import { DoodleArrow, ScribbleCircle } from "@/components/app/doodle"
import { AppHeader } from "@/components/app/app-header"
import { LogoMark } from "@/components/app/logo"
import {
  ArrowRight,
  BellRing,
  Check,
  CheckCircle2,
  ChevronDown,
  FileText,
  Inbox,
  Kanban,
  KeyRound,
  Search,
  Sparkles,
  Wand2,
  XCircle,
  Zap,
} from "lucide-react"

/* ─────────── pedacinhos reutilizados na página ─────────── */

function Step({ n, title, desc }: { n: string; title: string; desc: string }) {
  return (
    <div className="relative">
      <span className="hand absolute -left-1 -top-4 z-10 rotate-[-6deg] text-2xl text-primary">{n}</span>
      <NotebookCard ruled holes className="h-full">
        <h3 className="text-sm font-extrabold tracking-tight">{title}</h3>
        <p className="mt-1 text-sm leading-relaxed text-muted-foreground">{desc}</p>
      </NotebookCard>
    </div>
  )
}

function Feature({
  icon: Icon,
  title,
  desc,
  color,
}: {
  icon: React.ComponentType<{ className?: string }>
  title: string
  desc: string
  color: string
}) {
  return (
    <NotebookCard holes className="h-full">
      <div
        className="grid h-9 w-9 place-items-center rounded-lg border-[1.5px] border-ink/20"
        style={{ background: `color-mix(in srgb, ${color} 16%, var(--card))` }}
      >
        <Icon className="h-4 w-4" />
      </div>
      <h3 className="mt-3 text-sm font-extrabold tracking-tight">{title}</h3>
      <p className="mt-1 text-sm leading-relaxed text-muted-foreground">{desc}</p>
    </NotebookCard>
  )
}

function Faq({ q, children }: { q: string; children: React.ReactNode }) {
  return (
    <details className="group rounded-xl border-[1.5px] border-ink/70 bg-card px-4 py-3 shadow-hard-xs open:bg-card">
      <summary className="flex cursor-pointer list-none items-center justify-between gap-3 text-sm font-bold [&::-webkit-details-marker]:hidden">
        {q}
        <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground transition-transform group-open:rotate-180" />
      </summary>
      <p className="mt-2 text-sm leading-relaxed text-muted-foreground">{children}</p>
    </details>
  )
}

/* ─────────── mock do caderno no hero ─────────── */

function KanbanPreview() {
  const cols = [
    { label: "Aplicado", color: "var(--st-applied)", soft: "var(--st-applied-soft)", cards: 2 },
    { label: "Triagem", color: "var(--st-triage)", soft: "var(--st-triage-soft)", cards: 1 },
    { label: "Entrevista", color: "var(--st-interview)", soft: "var(--st-interview-soft)", cards: 1 },
  ]
  return (
    <div className="relative">
      <div className="hand absolute -top-4 right-6 z-20 rotate-3 rounded-md bg-primary px-3 py-1 text-base font-bold text-primary-foreground shadow-pop">
        anota aí!
      </div>

      <div
        className="tape absolute left-1/2 top-0 z-30 h-6 w-24 -translate-x-1/2 -rotate-2 rounded-[2px]"
        aria-hidden="true"
      />

      <PaperSheet withSpiral className="rotate-[-1deg] p-3 pt-6 sm:p-4 sm:pt-7">
        <div className="grid grid-cols-3 gap-2">
          {cols.map((c) => (
            <div key={c.label} className="rounded-lg border-[1.5px] border-ink/15 p-2" style={{ background: c.soft }}>
              <div className="flex items-center gap-1.5">
                <span className="h-2.5 w-2.5 rounded-full border border-ink/20" style={{ background: c.color }} />
                <span className="truncate text-[10px] font-extrabold uppercase tracking-wider">{c.label}</span>
              </div>
              <div className="mt-2 space-y-1.5">
                {Array.from({ length: c.cards }).map((_, i) => (
                  <div key={i} className="space-y-1 rounded-md border-[1.5px] border-ink/70 bg-[var(--nb-card)] p-1.5 shadow-hard-xs">
                    <div className="h-2 w-3/4 rounded-full bg-foreground/20" />
                    <div className="h-2 w-1/2 rounded-full bg-foreground/10" />
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>

        <div className="mt-3 flex items-center gap-2 rounded-lg border-[1.5px] border-[color:var(--st-applied)]/40 bg-[var(--st-applied-soft)] px-3 py-2">
          <BellRing className="h-3.5 w-3.5 shrink-0 text-[color:var(--st-applied)]" />
          <p className="text-xs font-semibold text-[color:var(--st-applied)]">
            7 dias sem resposta da Nubank — que tal cobrar?
          </p>
        </div>
      </PaperSheet>

      {/* cartão de compatibilidade espiando */}
      <div className="absolute -bottom-6 -left-4 z-20 w-44 rotate-[-4deg] rounded-xl border-[1.5px] border-ink bg-card p-3 shadow-hard sm:-left-8">
        <div className="flex items-center gap-2">
          <span className="grid h-9 w-9 place-items-center rounded-lg border-[1.5px] border-emerald-500/40 bg-emerald-500/10 text-sm font-extrabold text-emerald-700 dark:text-emerald-400">
            82
          </span>
          <div className="leading-tight">
            <div className="text-[11px] font-bold">compatível</div>
            <div className="text-[10px] text-muted-foreground">com seu CV</div>
          </div>
        </div>
        <div className="mt-2 flex items-center gap-1.5 text-[10px] font-medium text-muted-foreground">
          <CheckCircle2 className="h-3 w-3 text-emerald-600 dark:text-emerald-400" /> React
          <XCircle className="ml-1 h-3 w-3 text-destructive" /> .NET
        </div>
      </div>
    </div>
  )
}

/* ─────────── página ─────────── */

export function Landing() {
  return (
    <div className="min-h-screen">
      <AppHeader />

      <main className="mx-auto max-w-6xl space-y-20 px-4 py-12 sm:px-6 sm:py-16">
        {/* ── hero ── */}
        <section className="grid items-center gap-12 lg:grid-cols-[1.05fr_0.95fr]">
          <div>
            <div className="mb-5 flex items-center gap-3">
              <LogoMark className="h-12 w-12" />
              <div className="leading-tight">
                <div className="text-2xl font-extrabold tracking-tight">vagalume</div>
                <div className="text-xs font-medium text-muted-foreground">seu caderno de vagas</div>
              </div>
            </div>

            <h1 className="mt-5 text-4xl font-extrabold leading-[1.03] tracking-tight sm:text-5xl lg:text-[3.3rem]">
              Sua busca de emprego
              <br />
              vira um <ScribbleCircle>caderno</ScribbleCircle>
              <br />
              que trabalha por você.
            </h1>

            <p className="mt-5 max-w-[54ch] text-[15px] leading-relaxed text-muted-foreground">
              Chega de 20 abas abertas e uma planilha que você esquece de atualizar. O Vagalume garimpa vagas nas
              fontes certas, guarda tudo num kanban gostoso de olhar e te mostra onde seu currículo encaixa — e onde
              falta só um detalhe.
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
                <Check className="h-3.5 w-3.5 text-foreground" /> leva um minuto pra começar
              </span>
              <span className="inline-flex items-center gap-1.5">
                <Check className="h-3.5 w-3.5 text-foreground" /> feito pra quem aplica toda semana
              </span>
            </div>
          </div>

          <KanbanPreview />
        </section>

        {/* ── como funciona ── */}
        <section className="space-y-6">
          <div className="flex flex-wrap items-end justify-between gap-3">
            <div>
              <WashiTape className="mb-3 inline-block">como funciona</WashiTape>
              <h2 className="text-2xl font-extrabold tracking-tight">Três passos. Nada de complicação.</h2>
            </div>
            <p className="max-w-[36ch] text-sm text-muted-foreground">
              Você continua no comando da sua busca — a gente só tira o trabalho chato do caminho.
            </p>
          </div>

          <div className="grid gap-6 sm:grid-cols-3 sm:gap-4">
            <Step
              n="1"
              title="Conte o que você procura"
              desc="Palavras-chave, tecnologias, nível, remoto ou presencial. A gente usa isso pra filtrar o barulho."
            />
            <Step
              n="2"
              title="A gente garimpa as vagas"
              desc="O Vagalume varre LinkedIn, Gupy, Trampos, Programathor, InfoJobs, Catho e os feeds que você indicar — e joga só o que interessa na sua inbox."
            />
            <Step
              n="3"
              title="Você organiza e vê o encaixe"
              desc="Arrasta os cards pelo funil e, quando quiser, compara a vaga com seu currículo pra saber onde você brilha e onde falta algo."
            />
          </div>
        </section>

        {/* ── features ── */}
        <section className="space-y-6">
          <div className="flex flex-wrap items-center gap-3">
            <WashiTape color="blue" className="inline-block">
              o que já tem dentro
            </WashiTape>
            <DoodleArrow className="hidden h-6 w-16 text-primary sm:block" />
          </div>

          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <Feature
              icon={Kanban}
              color="var(--crayon-blue)"
              title="Kanban de 5 etapas"
              desc="Aplicado → Triagem → Entrevista → Oferta → Encerrado. Visual de board, do jeito que a cabeça entende."
            />
            <Feature
              icon={Search}
              color="var(--crayon-green)"
              title="Busca automática"
              desc="Você define os critérios e o Vagalume procura sozinho nas fontes brasileiras, sem duplicar vaga."
            />
            <Feature
              icon={Inbox}
              color="var(--crayon-orange)"
              title="Inbox de novidades"
              desc="As vagas novas caem numa listinha só pra você decidir: aplicar ou descartar. Sem pressão."
            />
            <Feature
              icon={FileText}
              color="var(--crayon-purple)"
              title="Seus currículos"
              desc="Guarde quantas versões quiser e escolha qual fica ativo. Um clique pra trocar quando mudar de foco."
            />
            <Feature
              icon={Wand2}
              color="var(--crayon-pink)"
              title="Compatibilidade com IA"
              desc="Veja o quanto a vaga combina com seu currículo, com os gaps explicadinhos — nada de score genérico."
            />
            <Feature
              icon={KeyRound}
              color="var(--crayon-yellow)"
              title="Sua chave, seu controle"
              desc="A análise usa a sua própria chave de IA. Você decide o provedor, o custo e o que compartilhar."
            />
          </div>
        </section>

        {/* ── destaque: compatibilidade ── */}
        <section className="grid items-center gap-10 lg:grid-cols-2">
          <div>
            <WashiTape color="green" className="mb-4 inline-block">
              o diferencial
            </WashiTape>
            <h2 className="text-2xl font-extrabold leading-tight tracking-tight sm:text-3xl">
              “Será que essa vaga é pra mim?”
            </h2>
            <p className="mt-4 text-[15px] leading-relaxed text-muted-foreground">
              Em vez de mandar o mesmo currículo pra tudo, o Vagalume lê a vaga e o seu CV e responde com honestidade:
              o que você já tem, o que está faltando e o que dá pra ajustar antes de aplicar.
            </p>
            <ul className="mt-5 space-y-2 text-sm">
              {[
                "Score de 0 a 100, sem promessa mágica",
                "Requisitos um a um: atendido ou gap",
                "Sugestões pontuais, sem reescrever seu CV inteiro",
              ].map((t) => (
                <li key={t} className="flex items-start gap-2">
                  <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-[color:var(--st-offer)]" />
                  <span>{t}</span>
                </li>
              ))}
            </ul>
            <p className="hand mt-5 rotate-[-1deg] text-lg text-muted-foreground">
              e você usa a sua própria chave de IA — sem pegadinha.
            </p>
          </div>

          <div className="relative">
            <NotebookPanel holes title="Compatibilidade" headerColor="var(--crayon-green)" badge={<Zap className="h-4 w-4 text-primary" />}>
              <div className="flex items-center gap-3">
                <div className="grid h-16 w-16 shrink-0 place-items-center rounded-xl border-[1.5px] border-emerald-500/40 bg-emerald-500/10">
                  <div className="text-center leading-none">
                    <div className="text-xl font-extrabold tabular-nums text-emerald-700 dark:text-emerald-400">82</div>
                    <div className="text-[10px] font-semibold uppercase tracking-wide text-emerald-700 dark:text-emerald-400">
                      match
                    </div>
                  </div>
                </div>
                <p className="text-sm leading-relaxed">
                  Você tem uma base forte pra essa vaga. Só faltam dois pontos que dá pra resolver rápido.
                </p>
              </div>

              <div className="mt-4 space-y-2">
                {[
                  { ok: true, req: "React + TypeScript", note: "3 anos no currículo" },
                  { ok: true, req: "APIs REST", note: "experiência comprovada" },
                  { ok: false, req: "Docker em produção", note: "não aparece no CV" },
                ].map((r) => (
                  <div key={r.req} className="flex items-start gap-2 rounded-lg border-[1.5px] border-ink/15 bg-muted/30 px-3 py-2">
                    {r.ok ? (
                      <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-emerald-600 dark:text-emerald-400" />
                    ) : (
                      <XCircle className="mt-0.5 h-4 w-4 shrink-0 text-destructive" />
                    )}
                    <div className="text-xs">
                      <div className="font-semibold">{r.req}</div>
                      <div className="text-muted-foreground">{r.note}</div>
                    </div>
                  </div>
                ))}
              </div>

              <div className="mt-4 rounded-lg border-l-2 border-[color:var(--st-offer)] bg-[var(--st-offer-soft)] px-3 py-2 text-xs text-[color:var(--st-offer)]">
                <span className="font-bold">sugestão:</span> cite um projeto onde você subiu algo com Docker.
              </div>
            </NotebookPanel>
          </div>
        </section>

        {/* ── faq ── */}
        <section className="grid gap-8 lg:grid-cols-[0.8fr_1.2fr]">
          <div>
            <WashiTape className="mb-3 inline-block">perguntas frequentes</WashiTape>
            <h2 className="text-2xl font-extrabold tracking-tight">Ainda com dúvida?</h2>
            <p className="mt-2 text-sm text-muted-foreground">As respostas honestas, sem letra miúda.</p>
          </div>

          <div className="space-y-3">
            <Faq q="Preciso configurar IA pra usar?">
              Não. O caderno, os currículos e a busca de vagas funcionam sem IA nenhuma. A análise de compatibilidade é
              opcional — e, quando você quiser usar, conecta a sua própria chave em Currículos.
            </Faq>
            <Faq q="É grátis mesmo?">
              Começar é de graça. Como a análise de IA usa a sua chave, você paga direto ao provedor que escolher, só
              pelo que usar. O Vagalume não fica no meio disso.
            </Faq>
            <Faq q="De onde vêm as vagas?">
              Hoje: LinkedIn, Gupy, Trampos, Programathor, InfoJobs, Catho e feeds RSS que você cadastrar. Você define
              palavras-chave, nível, modalidade e localização pra filtrar o que chega.
            </Faq>
            <Faq q="O Vagalume aplica nas vagas por mim?">
              Não — e isso é de propósito. Ferramenta que aplica sozinha queima seu nome com recrutador. Aqui você
              aplica, e a gente só organiza e avisa quando vale cobrar resposta.
            </Faq>
            <Faq q="Onde fica meu currículo e meus dados?">
              Seu currículo fica guardado no seu espaço, e é seu. Na hora da análise, o texto do CV e a descrição da
              vaga vão pro provedor de IA que <span className="font-semibold text-foreground">você</span> escolher — e
              avisamos isso na tela antes de gerar.
            </Faq>
          </div>
        </section>

        {/* ── cta final ── */}
        <section>
          <NotebookPanel tape holes>
            <div className="flex flex-col items-center gap-4 px-2 py-6 text-center sm:px-8 sm:py-8">
              <span className="hand rotate-[-2deg] text-xl text-muted-foreground">chega de perder o fio da meada</span>
              <h2 className="max-w-[22ch] text-2xl font-extrabold leading-tight tracking-tight sm:text-3xl">
                Comece seu caderno de vagas hoje.
              </h2>
              <p className="max-w-[46ch] text-sm text-muted-foreground">
                Leva um minuto, não precisa de cartão e você já sai organizando as próximas candidaturas.
              </p>
              <div className="mt-1 flex flex-wrap justify-center gap-3">
                <Link to="/app">
                  <Button size="lg" className="gap-2">
                    <Sparkles className="h-4 w-4" /> criar meu caderno <ArrowRight className="h-4 w-4" />
                  </Button>
                </Link>
                <Link to="/login">
                  <Button variant="outline" size="lg">
                    entrar
                  </Button>
                </Link>
              </div>
            </div>
          </NotebookPanel>
        </section>

        <footer className="pb-4 text-center text-xs text-muted-foreground">
          © 2026 Vagalume — seu caderno de vagas. Feito pra quem aplica toda semana, sem prometer milagre.
        </footer>
      </main>
    </div>
  )
}
