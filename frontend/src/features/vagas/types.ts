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

export const FONTE_HINT: Record<string, string> = {
  Agente: "O workflow recebe os critérios do perfil e envia as vagas ao Vagalume pelo endereço de retorno.",
  Gupy: "Busca em todo o portal pelas suas palavras-chave. Opcional: informe o slug da empresa (ex.: daycoval-tech) para restringir.",
  Trampos: "Busca por palavra-chave nas oportunidades do Trampos (TI, comunicação e design).",
  Programathor: "Portal de vagas de tecnologia; busca por palavra-chave.",
  InfoJobs: "Busca por palavra-chave no portal InfoJobs.",
  Catho: "Busca por palavra-chave no portal Catho.",
  LinkedIn: "Busca pública do LinkedIn, sem login. Opcional: informe a localização (ex.: São Paulo, Brasil); vazio = Brasil.",
  Rss: "Informe a URL do feed RSS/Atom da empresa ou portal.",
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
}
