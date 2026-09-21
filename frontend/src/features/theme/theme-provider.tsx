import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react"

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
  root.style.colorScheme = theme
}

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [theme, setThemeState] = useState<Theme>(() => {
    if (typeof window === "undefined") return "light"
    return getInitialTheme()
  })
  const [isAnimating, setIsAnimating] = useState(false)

  useEffect(() => {
    applyTheme(theme)
  }, [theme])

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

    const doc = document as unknown as {
      startViewTransition?: (cb: () => void) => { finished: Promise<void> }
    }

    // View Transitions API — cross-fade sutil (definido em index.css)
    if (doc.startViewTransition) {
      setIsAnimating(true)
      const transition = doc.startViewTransition(() => setTheme(next))
      transition.finished.finally(() => setIsAnimating(false))
      return
    }

    setTheme(next)
  }, [theme, setTheme])

  const value = useMemo<ThemeContextValue>(
    () => ({ theme, isDark: theme === "dark", toggle, setTheme, isAnimating }),
    [theme, toggle, setTheme, isAnimating]
  )

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}

export function useTheme() {
  const ctx = useContext(ThemeContext)
  if (!ctx) throw new Error("useTheme deve ser usado dentro de ThemeProvider")
  return ctx
}
