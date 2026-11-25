import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react"

type Theme = "light" | "dark"

type ThemeContextValue = {
  theme: Theme
  isDark: boolean
  toggle: () => void
  setTheme: (t: Theme) => void
  isAnimating: boolean
  firefliesEnabled: boolean
  setFirefliesEnabled: (v: boolean) => void
  toggleFireflies: () => void
}

const ThemeContext = createContext<ThemeContextValue | null>(null)

const STORAGE_KEY = "vagalume:theme"
const FIREFLIES_STORAGE_KEY = "vagalume:fireflies"

function getInitialTheme(): Theme {
  const saved = localStorage.getItem(STORAGE_KEY) as Theme | null
  if (saved === "light" || saved === "dark") return saved
  if (window.matchMedia?.("(prefers-color-scheme: dark)").matches) return "dark"
  return "light"
}

function getInitialFireflies(): boolean {
  return localStorage.getItem(FIREFLIES_STORAGE_KEY) !== "off"
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
  const [firefliesEnabled, setFirefliesState] = useState<boolean>(() => {
    if (typeof window === "undefined") return true
    return getInitialFireflies()
  })

  useEffect(() => {
    applyTheme(theme)
  }, [theme])

  const setFirefliesEnabled = useCallback((next: boolean) => {
    setFirefliesState(next)
    localStorage.setItem(FIREFLIES_STORAGE_KEY, next ? "on" : "off")
  }, [])

  const toggleFireflies = useCallback(() => {
    setFirefliesState((prev) => {
      const next = !prev
      localStorage.setItem(FIREFLIES_STORAGE_KEY, next ? "on" : "off")
      return next
    })
  }, [])

  const applyAndStore = useCallback((next: Theme) => {
    applyTheme(next)
    setThemeState(next)
    localStorage.setItem(STORAGE_KEY, next)
  }, [])

  const setTheme = useCallback(
    (next: Theme) => {
      if (next === theme) return

      const prefersReduced = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches
      if (prefersReduced) {
        applyAndStore(next)
        return
      }

      const doc = document as unknown as {
        startViewTransition?: (cb: () => void) => { finished: Promise<void> }
      }

      // View Transitions API — cross-fade sutil (definido em index.css)
      if (doc.startViewTransition) {
        setIsAnimating(true)
        const transition = doc.startViewTransition(() => applyAndStore(next))
        transition.finished.catch(() => {}).finally(() => setIsAnimating(false))
        return
      }

      applyAndStore(next)
    },
    [theme, applyAndStore]
  )

  const toggle = useCallback(() => {
    setTheme(theme === "dark" ? "light" : "dark")
  }, [theme, setTheme])

  const value = useMemo<ThemeContextValue>(
    () => ({
      theme,
      isDark: theme === "dark",
      toggle,
      setTheme,
      isAnimating,
      firefliesEnabled,
      setFirefliesEnabled,
      toggleFireflies,
    }),
    [theme, toggle, setTheme, isAnimating, firefliesEnabled, setFirefliesEnabled, toggleFireflies]
  )

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}

export function useTheme() {
  const ctx = useContext(ThemeContext)
  if (!ctx) throw new Error("useTheme deve ser usado dentro de ThemeProvider")
  return ctx
}
