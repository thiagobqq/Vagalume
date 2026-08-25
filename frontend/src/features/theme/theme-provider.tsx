import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from "react"

type Theme = "light" | "dark"

type ThemeContextValue = {
  theme: Theme
  isDark: boolean
  toggle: () => void
  setTheme: (t: Theme) => void
  isAnimating: boolean
}

const ThemeContext = createContext<ThemeContextValue | null>(null)

const STORAGE_KEY = "vagalume:theme"

function getInitialTheme(): Theme {
  const saved = localStorage.getItem(STORAGE_KEY) as Theme | null
  if (saved === "light" || saved === "dark") return saved
  if (window.matchMedia?.("(prefers-color-scheme: dark)").matches) return "dark"
  return "light"
}

function applyTheme(theme: Theme) {
  const root = document.documentElement
  root.classList.toggle("dark", theme === "dark")
  // para <meta name="color-scheme">
  root.style.colorScheme = theme
}

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [theme, setThemeState] = useState<Theme>(() => {
    if (typeof window === "undefined") return "light"
    return getInitialTheme()
  })
  const [isAnimating, setIsAnimating] = useState(false)
  const [overlayBg, setOverlayBg] = useState<string | null>(null)
  const animTimeout = useRef<number | null>(null)
  const switchTimeout = useRef<number | null>(null)

  // aplica na primeira montagem e em mudanças sem animação
  useEffect(() => {
    applyTheme(theme)
  }, [theme])

  // inicializa .dark sem animação (evita flash)
  useEffect(() => {
    const t = getInitialTheme()
    applyTheme(t)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const setTheme = useCallback((next: Theme) => {
    applyTheme(next)
    setThemeState(next)
    localStorage.setItem(STORAGE_KEY, next)
  }, [])

  const toggle = useCallback(() => {
    const prefersReduced = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches
    const next: Theme = theme === "dark" ? "light" : "dark"

    if (prefersReduced) {
      setTheme(next)
      return
    }

    const doc = document as unknown as { startViewTransition?: (cb: () => void) => { finished: Promise<void>; ready: Promise<void> } }

    // View Transitions API — troca com wipe diagonal reta, tema muda gradualmente atrás da frontier
    if (doc.startViewTransition) {
      setIsAnimating(true)
      const transition = doc.startViewTransition(() => {
        setTheme(next)
      })
      // libera botão quando a animação de view-transition termina (~1100ms)
      transition.finished.finally(() => setIsAnimating(false))
      return
    }

    // Fallback sem View Transitions — overlay com clip-path diagonal (mesma reta)
    const nextBg = next === "dark" ? "#1e1c19" : "#fef9f0"
    setOverlayBg(nextBg)
    setIsAnimating(true)

    // mantém tema antigo por baixo enquanto a reta varre; troca só no final quando já cobre tudo
    if (switchTimeout.current) window.clearTimeout(switchTimeout.current)
    switchTimeout.current = window.setTimeout(() => {
      setTheme(next)
    }, 1050)

    if (animTimeout.current) window.clearTimeout(animTimeout.current)
    animTimeout.current = window.setTimeout(() => {
      setIsAnimating(false)
      setOverlayBg(null)
    }, 1150)
  }, [theme, setTheme])

  useEffect(() => {
    return () => {
      if (animTimeout.current) window.clearTimeout(animTimeout.current)
      if (switchTimeout.current) window.clearTimeout(switchTimeout.current)
    }
  }, [])

  const value = useMemo<ThemeContextValue>(
    () => ({ theme, isDark: theme === "dark", toggle, setTheme, isAnimating }),
    [theme, toggle, setTheme, isAnimating]
  )

  return (
    <ThemeContext.Provider value={value}>
      {children}
      {/* Fallback overlay — só aparece quando View Transitions não está disponível */}
      {isAnimating && overlayBg && (
        <div className="diagonal-wipe-overlay" aria-hidden style={{ background: overlayBg } as React.CSSProperties} />
      )}
    </ThemeContext.Provider>
  )
}

export function useTheme() {
  const ctx = useContext(ThemeContext)
  if (!ctx) throw new Error("useTheme deve ser usado dentro de ThemeProvider")
  return ctx
}
