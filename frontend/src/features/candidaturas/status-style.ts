import type { Status } from "./types"

export const STATUS_STYLE: Record<Status, { dot: string; soft: string; strong: string }> = {
  Aplicado: { dot: "var(--st-applied)", soft: "var(--st-applied-soft)", strong: "var(--st-applied)" },
  Triagem: { dot: "var(--st-triage)", soft: "var(--st-triage-soft)", strong: "var(--st-triage)" },
  Entrevista: { dot: "var(--st-interview)", soft: "var(--st-interview-soft)", strong: "var(--st-interview)" },
  Oferta: { dot: "var(--st-offer)", soft: "var(--st-offer-soft)", strong: "var(--st-offer)" },
  Encerrado: { dot: "var(--st-closed)", soft: "var(--st-closed-soft)", strong: "var(--st-closed)" },
}
