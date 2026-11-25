export type VagaDto = {
  id: string
  titulo: string
  empresa: string
  localizacao: string | null
  modalidade: string
  nivel: string
  url: string
  descricao: string | null
  publicadaEm: string | null
  fonteTipo: string
  fonteNome: string
  status: string
  candidaturaId: string | null
  criadoEm: string
}

export type ResultadoFonteDto = {
  coletando: boolean
  aguardandoAgente: boolean
  fonteNome: string
  sucesso: boolean
  erro: string | null
  encontradas: number
  aprovadas: number
  duplicadas: number
  adicionadas: number
}

export type ExecucaoBuscaDto = {
  id: string
  iniciadaEm: string
  finalizadaEm: string | null
  sucesso: boolean | null
  erro: string | null
  encontradas: number
  aprovadas: number
  duplicadas: number
  adicionadas: number
  detalhesPorFonte: ResultadoFonteDto[]
}

export type FonteBuscaDto = {
  retornoMcp: boolean
  instrucoes: string | null
  id: string
  tipo: string
  identificador: string | null
  nomeEmpresa: string | null
  nome: string | null
  ativo: boolean
  criadoEm: string
}

export type PerfilBuscaDto = {
  ativo: boolean
  palavrasChave: string[]
  tecnologias: string[]
  niveis: string[]
  modalidades: string[]
  excluirPalavras: string[]
  localizacao: string | null
  combina: string
  horaExecucao: string
  diasSemana: string
  fusoHorario: string
  discordWebhookUrl: string | null
  ultimaExecucaoEm: string | null
}

export type SalvarPerfilBuscaRequest = Omit<PerfilBuscaDto, "ultimaExecucaoEm">

export type CriarFonteBuscaRequest = {
  tipo: string
  cabecalhos?: { nome: string; valor: string }[]
  segredoOpenBot?: string
  webhookUrl?: string
  instrucoes?: string
  identificador?: string | null
  nomeEmpresa?: string | null
  nome?: string | null
}

export const FONTE_TIPOS = ["Gupy", "Trampos", "Programathor", "InfoJobs", "Catho", "LinkedIn", "Rss", "Agente"] as const

export const FONTE_LABEL: Record<string, string> = {
  Agente: "Agente",
  Gupy: "Gupy",
  Trampos: "Trampos",
  Programathor: "Programathor",
  InfoJobs: "InfoJobs",
  Catho: "Catho",
  LinkedIn: "LinkedIn",
  Rss: "RSS",
}

export const NIVEIS = ["Junior", "Pleno", "Senior"] as const
export const MODALIDADES = ["Remoto", "Hibrido", "Presencial"] as const

export const NIVEL_LABEL: Record<string, string> = {
  Junior: "Júnior",
  Pleno: "Pleno",
  Senior: "Sênior",
  NaoInformado: "—",
}

export const MODALIDADE_LABEL: Record<string, string> = {
  Remoto: "Remoto",
  Hibrido: "Híbrido",
  Presencial: "Presencial",
  NaoInformado: "—",
}

export const DIAS_LABEL = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb"] as const

export type RequisitoCompatibilidadeDto = {
  requisito: string
  atendido: boolean
  evidencia: string | null
  gap: string | null
}

export type AnaliseCompatibilidadeDto = {
  vagaId: string
  vagaTitulo: string
  empresa: string
  curriculoId: string
  curriculoNome: string
  score: number
  resumo: string
  requisitos: RequisitoCompatibilidadeDto[]
  sugestoes: string[]
  provedor: string
  modelo: string
  desatualizada: boolean
  criadoEm: string
  atualizadoEm?: string
}
