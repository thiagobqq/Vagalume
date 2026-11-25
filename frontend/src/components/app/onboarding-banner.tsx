import { useState } from "react"
import { Link } from "react-router-dom"
import { CheckCircle2, Circle, FileText, Plus, Sparkles, X, Zap } from "lucide-react"

const STORAGE_KEY = "vagalume:onboarding-dismissed"

export function OnboardingBanner({
  temCandidatura,
  temCurriculo,
  onNovaCandidatura,
}: {
  temCandidatura: boolean
  temCurriculo: boolean
  onNovaCandidatura: () => void
}) {
  const [dismissed, setDismissed] = useState(() => localStorage.getItem(STORAGE_KEY) === "1")

  if (dismissed || (temCandidatura && temCurriculo)) return null

  const fechar = () => {
    localStorage.setItem(STORAGE_KEY, "1")
    setDismissed(true)
  }

  return (
    <div className="relative rounded-2xl border-[1.5px] border-ink/50 bg-[var(--nb-card)] p-4 shadow-hard-xs">
      <button
        type="button"
        onClick={fechar}
        aria-label="Dispensar guia inicial"
        className="absolute right-3 top-3 rounded-md p-1 text-muted-foreground hover:bg-muted hover:text-foreground"
      >
        <X className="h-4 w-4" />
      </button>

      <div className="flex items-center gap-2 text-sm font-extrabold">
        <Sparkles className="h-4 w-4 text-primary" /> Comece seu caderno em 3 passos
      </div>
      <p className="mt-1 text-xs text-muted-foreground">
        Configure o básico uma vez para acompanhar candidaturas, descobrir vagas e medir seu funil.
      </p>

      <div className="mt-3 grid grid-cols-1 gap-2 sm:grid-cols-3">
        <Link
          to="/app/curriculos"
          className="flex items-center gap-2.5 rounded-xl border border-ink/20 bg-background/60 p-3 text-xs transition-colors hover:border-ink/60"
        >
          {temCurriculo ? (
            <CheckCircle2 className="h-4 w-4 shrink-0 text-emerald-600" />
          ) : (
            <Circle className="h-4 w-4 shrink-0 text-muted-foreground" />
          )}
          <div className="min-w-0">
            <div className="font-bold">1. Envie seu currículo</div>
            <div className="text-muted-foreground">PDF usado nas análises</div>
          </div>
          <FileText className="ml-auto h-4 w-4 shrink-0 text-muted-foreground" />
        </Link>

        <Link
          to="/app/vagas"
          className="flex items-center gap-2.5 rounded-xl border border-ink/20 bg-background/60 p-3 text-xs transition-colors hover:border-ink/60"
        >
          <Circle className="h-4 w-4 shrink-0 text-muted-foreground" />
          <div className="min-w-0">
            <div className="font-bold">2. Configure a busca</div>
            <div className="text-muted-foreground">Critérios e fontes de vagas</div>
          </div>
          <Zap className="ml-auto h-4 w-4 shrink-0 text-muted-foreground" />
        </Link>

        <button
          type="button"
          onClick={onNovaCandidatura}
          className="flex items-center gap-2.5 rounded-xl border border-ink/20 bg-background/60 p-3 text-left text-xs transition-colors hover:border-ink/60"
        >
          {temCandidatura ? (
            <CheckCircle2 className="h-4 w-4 shrink-0 text-emerald-600" />
          ) : (
            <Circle className="h-4 w-4 shrink-0 text-muted-foreground" />
          )}
          <div className="min-w-0">
            <div className="font-bold">3. Registre uma vaga</div>
            <div className="text-muted-foreground">Cole o link ou cadastre</div>
          </div>
          <Plus className="ml-auto h-4 w-4 shrink-0 text-muted-foreground" />
        </button>
      </div>
    </div>
  )
}
