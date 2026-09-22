export const PERMISSOES = {
  admin: "admin",
  verifyUser: "verify_user",
} as const

export type PermissaoChave = (typeof PERMISSOES)[keyof typeof PERMISSOES]

export const PERMISSAO_LABEL: Record<string, string> = {
  [PERMISSOES.admin]: "Administrador",
  [PERMISSOES.verifyUser]: "Usuário verificado",
}
