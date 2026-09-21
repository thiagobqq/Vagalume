/*
  Tipos espelhando backend — fonte da verdade: backend/src/Vagalume.Domain/Enums/*
  NÃO seguir o que estava no frontend mockado.
*/

// Backend enums (case-sensitive)
export const STATUS_VALUES = ["Aplicado", "Triagem", "Entrevista", "Oferta", "Encerrado"] as const
export type Status = (typeof STATUS_VALUES)[number]

export const ORIGEM_VALUES = ["LinkedIn", "Gupy", "Catho", "Indicacao", "SiteEmpresa", "Outro"] as const
export type Origem = (typeof ORIGEM_VALUES)[number]

export const STATUS_LABEL: Record<Status, string> = {
  Aplicado: "Aplicado",
  Triagem: "Triagem",
  Entrevista: "Entrevista",
  Oferta: "Oferta",
  Encerrado: "Encerrado",
}

export const STATUS_ORDER: Status[] = ["Aplicado", "Triagem", "Entrevista", "Oferta", "Encerrado"]

export const ORIGEM_LABEL: Record<Origem, string> = {
  LinkedIn: "LinkedIn",
  Gupy: "Gupy",
  Catho: "Catho",
  Indicacao: "Indicação",
  SiteEmpresa: "Site da empresa",
  Outro: "Outro",
}

// DTOs do backend (camelCase serializado)
export type CandidaturaDto = {
  id: string
  empresa: string
  cargo: string
  link: string | null
  origem: string // Origem enum string
  observacoes: string | null
  status: string // Status enum string
  criadoEm: string
  atualizadoEm: string
}

export type EventoTransicaoDto = {
  id: string
  statusAnterior: string | null
  statusNovo: string
  observacao: string | null
  criadoEm: string
}

export type CandidaturaDetalhadaDto = CandidaturaDto & {
  eventos: EventoTransicaoDto[]
}

export type CurriculoDto = {
  id: string
  nomeArquivoOriginal: string
  tamanhoBytes: number
  ativo: boolean
  criadoEm: string
}

// Requests
export type CriarCandidaturaRequest = {
  empresa: string
  cargo: string
  link?: string | null
  origem: string
  observacoes?: string | null
}

export type AtualizarCandidaturaRequest = {
  empresa?: string | null
  cargo?: string | null
  link?: string | null
  origem?: string | null
  observacoes?: string | null
}

export type MudarStatusRequest = {
  status: string
  observacao?: string | null
}

// Auth
export type UsuarioDto = { id: string; nome: string; email: string; permissoes?: string[] }
export type AuthResponseDto = { accessToken: string; refreshToken: string; usuario: UsuarioDto }
export type RefreshResponseDto = { accessToken: string; refreshToken: string }
