import type { Status } from "./types"

export const STATUS_STYLE: Record<Status, { dot: string }> = {
  Aplicado: { dot: "var(--st-applied)" },
  Triagem: { dot: "var(--st-triage)" },
  Entrevista: { dot: "var(--st-interview)" },
  Oferta: { dot: "var(--st-offer)" },
  Encerrado: { dot: "var(--st-closed)" },
}
