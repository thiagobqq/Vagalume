export type ProvedorIaDto = {
  valor: string
  nome: string
  requerChave: boolean
  modeloPadrao: string | null
  modelosSugeridos: string[]
}

export type ConfiguracaoIaDto = {
  provedor: string
  modelo: string
  temChave: boolean
  chaveMascarada: string | null
  ativo: boolean
  atualizadoEm: string | null
}

export type SalvarConfiguracaoIaRequest = {
  provedor: string
  modelo: string
  apiKey?: string | null
  ativo: boolean
}

export type TestarIaRequest = {
  provedor: string
  modelo: string
  apiKey?: string | null
}

export type TestarIaResponse = {
  ok: boolean
  mensagem: string
}
