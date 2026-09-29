import { useEffect, useRef, useState } from "react"
import { GoogleLogin } from "@react-oauth/google"
import { useNavigate } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/features/auth/auth-context"
import { GOOGLE_CLIENT_ID } from "@/features/auth/google"
import { useTheme } from "@/features/theme/theme-provider"

function GoogleIcon() {
  return (
    <svg viewBox="0 0 48 48" aria-hidden className="h-[18px] w-[18px]">
      <path
        fill="#EA4335"
        d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"
      />
      <path
        fill="#4285F4"
        d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"
      />
      <path
        fill="#FBBC05"
        d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"
      />
      <path
        fill="#34A853"
        d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"
      />
    </svg>
  )
}

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
    <div ref={containerRef} className="group relative w-full">
      <Button
        type="button"
        variant="outline"
        size="lg"
        aria-hidden
        tabIndex={-1}
        className="w-full group-hover:bg-accent group-hover:text-accent-foreground group-focus-within:ring-2 group-focus-within:ring-ring/70 group-focus-within:ring-offset-2 group-focus-within:ring-offset-background"
      >
        <GoogleIcon />
        entrar com Google
      </Button>

      <div className="absolute inset-0 z-10 opacity-0">
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
    </div>
  )
}
