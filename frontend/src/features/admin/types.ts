export type PermissaoDto = {
  chave: string
}

export type UsuarioPermissoesDto = {
  id: string
  nome: string
  email: string
  permissoes: string[]
  solicitacaoPendente?: boolean
  solicitacaoMensagem?: string | null
  solicitacaoEm?: string | null
}
