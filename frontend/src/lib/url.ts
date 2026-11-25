import type { Origem } from "@/features/candidaturas/types"

/**
 * Garante que apenas URLs HTTP/HTTPS válidas sejam usadas em `<a href>` e `window.open`,
 * bloqueando esquemas perigosos como `javascript:` ou `data:`.
 */
export function safeExternalUrl(raw?: string | null): string | null {
  if (!raw) return null
  const trimmed = raw.trim()
  if (!trimmed) return null
  try {
    const parsed = new URL(trimmed)
    if (parsed.protocol !== "http:" && parsed.protocol !== "https:") return null
    return parsed.toString()
  } catch {
    return null
  }
}

/**
 * Infere a origem da candidatura com base no domínio da URL colada pelo usuário.
 */
export function inferOrigemFromUrl(raw?: string | null): Origem | null {
  const safe = safeExternalUrl(raw)
  if (!safe) return null
  const host = new URL(safe).hostname.toLowerCase()
  if (host.includes("linkedin.com")) return "LinkedIn"
  if (host.includes("gupy.io")) return "Gupy"
  if (host.includes("catho.com")) return "Catho"
  return null
}
