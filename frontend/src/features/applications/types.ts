// legado: mantido para compatibilidade — re-exporta tipos do backend
export type { Status, Origem, CandidaturaDto as Application, CandidaturaDto, CandidaturaDetalhadaDto, CurriculoDto } from "@/features/candidaturas/types"
export { STATUS_LABEL, STATUS_ORDER, STATUS_VALUES, ORIGEM_LABEL, ORIGEM_VALUES } from "@/features/candidaturas/types"

import type { CandidaturaDto } from "@/features/candidaturas/types"

// helper legado para quem ainda usa Application com dataAplicacao
export type LegacyApplication = CandidaturaDto & { dataAplicacao?: string; requisitos?: string[]; observacoes?: string | null }

// mapa de cores mantido (chaves backend)
export const STATUS_COLOR: Record<string, string> = {
  Aplicado: "yellow",
  Triagem: "blue",
  Entrevista: "purple",
  Oferta: "green",
  Encerrado: "pink",
}
