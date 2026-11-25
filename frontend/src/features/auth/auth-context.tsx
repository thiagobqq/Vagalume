import { createContext, useContext, useCallback, useEffect, useMemo, useState } from "react"
import { AUTH_CLEARED_EVENT, apiEnvelope, apiJson, clearTokens, getTokens } from "@/lib/api"
import type { UsuarioDto, AuthResponseDto } from "@/features/candidaturas/types"

type AuthState = {
  user: UsuarioDto | null
  accessToken: string | null
  loading: boolean
  login: (email: string, senha: string) => Promise<void>
  register: (nome: string, email: string, senha: string) => Promise<void>
  loginWithGoogle: (idToken: string) => Promise<void>
  logout: (todasSessoes?: boolean) => void
  updateProfile: (nome: string) => Promise<UsuarioDto>
  changePassword: (senhaAtual: string | null, novaSenha: string) => Promise<void>
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
  const [loading, setLoading] = useState<boolean>(() => !!localStorage.getItem("vagalume:accessToken"))

  const persist = useCallback((auth: AuthResponseDto) => {
    const raw = auth as unknown as Record<string, unknown>
    const at = (raw.accessToken as string) ?? (raw.AccessToken as string)
    const rt = (raw.refreshToken as string) ?? (raw.RefreshToken as string)
    const usu = (raw.usuario as UsuarioDto) ?? (raw.Usuario as UsuarioDto)
    if (at) localStorage.setItem("vagalume:accessToken", at)
    if (rt) localStorage.setItem("vagalume:refreshToken", rt)
    if (usu) localStorage.setItem("vagalume:user", JSON.stringify(usu))
    setAccessToken(at ?? null)
    setUser(usu ?? null)
    setLoading(false)
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

  const logout = useCallback((todasSessoes = false) => {
    const { refreshToken } = getTokens()
    if (refreshToken || todasSessoes) {
      void apiEnvelope("/api/auth/logout", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken, todasSessoes }),
        auth: todasSessoes,
      }).catch(() => {
        /* revogação best-effort */
      })
    }
    clearTokens()
    setUser(null)
    setAccessToken(null)
    setLoading(false)
  }, [])

  const refreshUser = useCallback(async () => {
    try {
      const { res, envelope } = await apiEnvelope<UsuarioDto>("/api/auth/me")
      if (res.ok && envelope?.success && envelope.data) {
        localStorage.setItem("vagalume:user", JSON.stringify(envelope.data))
        setUser(envelope.data)
      } else if (res.status === 401) {
        clearTokens()
        setUser(null)
        setAccessToken(null)
      }
    } finally {
      setLoading(false)
    }
  }, [])

  const updateProfile = useCallback(async (nome: string) => {
    const updated = await apiJson<UsuarioDto>("/api/auth/me", {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ nome }),
    })
    localStorage.setItem("vagalume:user", JSON.stringify(updated))
    setUser(updated)
    return updated
  }, [])

  const changePassword = useCallback(async (senhaAtual: string | null, novaSenha: string) => {
    await apiJson<null>("/api/auth/alterar-senha", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ senhaAtual: senhaAtual || null, novaSenha }),
    })
    await refreshUser()
  }, [refreshUser])

  const hasPermissao = useCallback(
    (chave: string) => !!user?.permissoes?.includes(chave),
    [user]
  )

  useEffect(() => {
    if (accessToken) {
      void refreshUser()
    } else {
      setLoading(false)
    }
  }, [accessToken, refreshUser])

  useEffect(() => {
    const syncAuth = () => {
      setAccessToken(localStorage.getItem("vagalume:accessToken"))
      setUser(loadUser())
      setLoading(false)
    }
    window.addEventListener("storage", syncAuth)
    window.addEventListener(AUTH_CLEARED_EVENT, syncAuth)
    return () => {
      window.removeEventListener("storage", syncAuth)
      window.removeEventListener(AUTH_CLEARED_EVENT, syncAuth)
    }
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
      updateProfile,
      changePassword,
      isAuthenticated: !!accessToken,
      hasPermissao,
      refreshUser,
    }),
    [user, accessToken, loading, login, register, loginWithGoogle, logout, updateProfile, changePassword, hasPermissao, refreshUser]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error("useAuth deve ser usado dentro de AuthProvider")
  return ctx
}
