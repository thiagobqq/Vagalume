export const PERMISSOES = {
  admin: "admin",
  buscaVagas: "busca_vagas.acesso",
} as const

export type PermissaoChave = (typeof PERMISSOES)[keyof typeof PERMISSOES]

export const PERMISSAO_LABEL: Record<string, string> = {
  [PERMISSOES.admin]: "Administrador",
  [PERMISSOES.buscaVagas]: "Busca automática de vagas",
}
