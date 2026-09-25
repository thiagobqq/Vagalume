import { useCallback, useMemo, useSyncExternalStore } from "react"

export function useMediaQuery(query: string) {
  const mql = useMemo(
    () => (typeof window === "undefined" ? null : window.matchMedia(query)),
    [query]
  )

  const subscribe = useCallback(
    (onChange: () => void) => {
      if (!mql) return () => {}
      mql.addEventListener("change", onChange)
      return () => mql.removeEventListener("change", onChange)
    },
    [mql]
  )

  const getSnapshot = useCallback(() => (mql ? mql.matches : false), [mql])

  return useSyncExternalStore(subscribe, getSnapshot, () => false)
}
