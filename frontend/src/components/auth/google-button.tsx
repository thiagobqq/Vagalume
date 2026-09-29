import { useEffect, useRef, useState } from "react"
import { GoogleLogin } from "@react-oauth/google"
import { useNavigate } from "react-router-dom"
import { useAuth } from "@/features/auth/auth-context"
import { GOOGLE_CLIENT_ID } from "@/features/auth/google"
import { useTheme } from "@/features/theme/theme-provider"

export function GoogleButton({ onError }: { onError?: (message: string) => void }) {
  const nav = useNavigate()
  const { loginWithGoogle } = useAuth()
  const { isDark } = useTheme()
  const containerRef = useRef<HTMLDivElement>(null)
  const [width, setWidth] = useState(320)

  useEffect(() => {
    const el = containerRef.current
    if (!el) return
    const update = () => setWidth(Math.min(400, Math.max(200, Math.round(el.offsetWidth))))
    update()
    if (typeof ResizeObserver === "undefined") return
    const observer = new ResizeObserver(update)
    observer.observe(el)
    return () => observer.disconnect()
  }, [])

  if (!GOOGLE_CLIENT_ID) return null

  return (
    <div ref={containerRef} className="w-full">
      <GoogleLogin
        width={String(width)}
        size="large"
        shape="rectangular"
        theme={isDark ? "filled_black" : "outline"}
        text="continue_with"
        logo_alignment="left"
        onSuccess={async (response) => {
          if (!response.credential) {
            onError?.("Não foi possível obter o token do Google")
            return
          }
          try {
            await loginWithGoogle(response.credential)
            nav("/app")
          } catch (ex) {
            onError?.((ex as Error).message || "Falha no login com Google")
          }
        }}
        onError={() => onError?.("Falha no login com Google")}
      />
    </div>
  )
}
