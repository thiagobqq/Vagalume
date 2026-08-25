const API_URL = import.meta.env.VITE_API_URL?.replace(/\/$/, "") ?? ""

// envelope do backend: GenericResponseData<T> { success, message, data }
export type ApiEnvelope<T> = {
  success: boolean
  message: string
  data: T | null
}

export class ApiError extends Error {
  status: number
  message: string
  constructor(status: number, message: string) {
    super(message)
    this.status = status
    this.message = message
  }
}

function getTokens() {
  return {
    accessToken: localStorage.getItem("vagalume:accessToken"),
    refreshToken: localStorage.getItem("vagalume:refreshToken"),
  }
}

function setTokens(accessToken: string, refreshToken: string) {
  localStorage.setItem("vagalume:accessToken", accessToken)
  localStorage.setItem("vagalume:refreshToken", refreshToken)
}

export function clearTokens() {
  localStorage.removeItem("vagalume:accessToken")
  localStorage.removeItem("vagalume:refreshToken")
  localStorage.removeItem("vagalume:user")
}

let refreshing: Promise<string | null> | null = null

async function doRefresh(): Promise<string | null> {
  const { refreshToken } = getTokens()
  if (!refreshToken) return null
  if (refreshing) return refreshing
  refreshing = (async () => {
    try {
      const res = await fetch(`${API_URL}/api/auth/refresh`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken }),
      })
      const envelope = (await res.json()) as ApiEnvelope<{ accessToken: string; refreshToken: string }>
      // backend usa camelCase: accessToken / refreshToken
      const data = envelope.data as unknown as Record<string, string> | null
      const newAccess = data?.accessToken ?? (data as unknown as { AccessToken?: string })?.AccessToken
      const newRefresh = data?.refreshToken ?? (data as unknown as { RefreshToken?: string })?.RefreshToken
      if (!res.ok || !envelope.success || !newAccess || !newRefresh) {
        clearTokens()
        return null
      }
      setTokens(newAccess, newRefresh)
      return newAccess
    } catch {
      clearTokens()
      return null
    } finally {
      refreshing = null
    }
  })()
  return refreshing
}

type FetchOpts = RequestInit & { auth?: boolean; rawResponse?: boolean }

export async function apiFetch(path: string, opts: FetchOpts = {}) {
  const url = path.startsWith("http") ? path : `${API_URL}${path}`
  const { auth = true, rawResponse, headers, ...rest } = opts
  const finalHeaders: Record<string, string> = { ...(headers as Record<string, string> | undefined) }

  if (auth) {
    const { accessToken } = getTokens()
    if (accessToken) finalHeaders["Authorization"] = `Bearer ${accessToken}`
  }

  // não forçar Content-Type para FormData
  const isFormData = rest.body instanceof FormData
  if (!isFormData && rest.body && !finalHeaders["Content-Type"]) {
    // se body for string já serializado, assume json foi setado pelo chamador
  }

  let res = await fetch(url, { ...rest, headers: finalHeaders })

  if (res.status === 401 && auth && !path.includes("/api/auth/refresh") && !path.includes("/api/auth/login")) {
    const newToken = await doRefresh()
    if (newToken) {
      finalHeaders["Authorization"] = `Bearer ${newToken}`
      res = await fetch(url, { ...rest, headers: finalHeaders })
    }
  }

  if (rawResponse) return res
  return res
}

export async function apiJson<T>(path: string, opts: FetchOpts = {}): Promise<T> {
  const res = await apiFetch(path, opts)
  const text = await res.text()
  let json: ApiEnvelope<T> | null = null
  try {
    json = text ? (JSON.parse(text) as ApiEnvelope<T>) : null
  } catch {
    // resposta não-json (ex: 413 html)
    throw new ApiError(res.status, text || res.statusText)
  }

  // arquivo binário (File) nunca passa por aqui — caller usa rawResponse

  if (!res.ok || (json && json.success === false)) {
    const msg = json?.message || `Erro ${res.status}`
    throw new ApiError(res.status, msg)
  }

  // backend serializa camelCase; mas garante fallback
  // Data pode ser null para 200 com lista vazia? backend retorna data=[]
  return (json?.data as T) ?? (json as unknown as T)
}

// helper para extrair envelope sem throw (usado em login que trata 401 como erro de UX)
export async function apiEnvelope<T>(path: string, opts: FetchOpts = {}): Promise<{ res: Response; envelope: ApiEnvelope<T> | null; raw: string }> {
  const res = await apiFetch(path, { ...opts, auth: opts.auth ?? true })
  const raw = await res.text()
  let envelope: ApiEnvelope<T> | null = null
  try {
    envelope = raw ? (JSON.parse(raw) as ApiEnvelope<T>) : null
  } catch {
    envelope = null
  }
  return { res, envelope, raw }
}

export function getApiUrl() {
  return API_URL || window.location.origin
}
