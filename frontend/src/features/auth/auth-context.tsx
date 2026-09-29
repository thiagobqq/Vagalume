import { createContext, useContext, useCallback, useEffect, useMemo, useState } from "react"
import { apiEnvelope, clearTokens } from "@/lib/api"
import type { UsuarioDto, AuthResponseDto } from "@/features/candidaturas/types"

type AuthState = {
  user: UsuarioDto | null
  accessToken: string | null
  loading: boolean
  login: (email: string, senha: string) => Promise<void>
  register: (nome: string, email: string, senha: string) => Promise<void>
  loginWithGoogle: (idToken: string) => Promise<void>
  logout: () => void
  isAuthenticated: boolean
  hasPermissao: (chave: string) => boolean
  refreshUser: () => Promise<void>
}

const AuthContext = createContext<AuthState | null>(null)

function loadUser(): UsuarioDto | null {
  try {
    const raw = localStorage.getItem("vagalume:user")
    return raw ? (JSON.parse(raw) as UsuarioDto) : null
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<UsuarioDto | null>(() => loadUser())
  const [accessToken, setAccessToken] = useState<string | null>(() => localStorage.getItem("vagalume:accessToken"))
  const [loading] = useState(false)

  const persist = useCallback((auth: AuthResponseDto) => {
    // backend serializa camelCase: accessToken, refreshToken, usuario
    const raw = auth as unknown as Record<string, unknown>
    const at = (raw.accessToken as string) ?? (raw.AccessToken as string)
    const rt = (raw.refreshToken as string) ?? (raw.RefreshToken as string)
    const usu = (raw.usuario as UsuarioDto) ?? (raw.Usuario as UsuarioDto)
    if (at) localStorage.setItem("vagalume:accessToken", at)
    if (rt) localStorage.setItem("vagalume:refreshToken", rt)
    if (usu) localStorage.setItem("vagalume:user", JSON.stringify(usu))
    setAccessToken(at ?? null)
    setUser(usu ?? null)
  }, [])

  const login = useCallback(
    async (email: string, senha: string) => {
      const { res, envelope, raw } = await apiEnvelope<AuthResponseDto>("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, senha }),
        auth: false,
      })
      if (!res.ok || !envelope?.success || !envelope.data) {
        throw new Error(envelope?.message || raw || "Falha no login")
      }
      persist(envelope.data)
    },
    [persist]
  )

  const register = useCallback(
    async (nome: string, email: string, senha: string) => {
      const { res, envelope, raw } = await apiEnvelope<{ id: string; nome: string; email: string }>("/api/auth/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ nome, email, senha }),
        auth: false,
      })
      if (!res.ok || !envelope?.success) {
        throw new Error(envelope?.message || raw || "Falha no cadastro")
      }
      // após registrar, faz login automático
      await login(email, senha)
    },
    [login]
  )

  const loginWithGoogle = useCallback(
    async (idToken: string) => {
      const { res, envelope, raw } = await apiEnvelope<AuthResponseDto>("/api/auth/google", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ idToken }),
        auth: false,
      })
      if (!res.ok || !envelope?.success || !envelope.data) {
        throw new Error(envelope?.message || raw || "Falha no login com Google")
      }
      persist(envelope.data)
    },
    [persist]
  )

  const logout = useCallback(() => {
    clearTokens()
    setUser(null)
    setAccessToken(null)
  }, [])

  const refreshUser = useCallback(async () => {
    const { res, envelope } = await apiEnvelope<UsuarioDto>("/api/auth/me")
    if (res.ok && envelope?.success && envelope.data) {
      localStorage.setItem("vagalume:user", JSON.stringify(envelope.data))
      setUser(envelope.data)
    }
  }, [])

  const hasPermissao = useCallback(
    (chave: string) => !!user?.permissoes?.includes(chave),
    [user]
  )

  // permissões mudam no servidor: recarrega o usuário quando há token
  useEffect(() => {
    if (accessToken) void refreshUser()
  }, [accessToken, refreshUser])

  // se token mudou externamente (ex: refresh), refletir
  useEffect(() => {
    const onStorage = () => {
      setAccessToken(localStorage.getItem("vagalume:accessToken"))
      setUser(loadUser())
    }
    window.addEventListener("storage", onStorage)
    return () => window.removeEventListener("storage", onStorage)
  }, [])

  const value = useMemo<AuthState>(
    () => ({
      user,
      accessToken,
      loading,
      login,
      register,
      loginWithGoogle,
      logout,
      isAuthenticated: !!accessToken,
      hasPermissao,
      refreshUser,
    }),
    [user, accessToken, loading, login, register, loginWithGoogle, logout, hasPermissao, refreshUser]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error("useAuth deve ser usado dentro de AuthProvider")
  return ctx
}
