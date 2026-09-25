import { useMemo } from "react"
import { useTheme } from "@/features/theme/theme-provider"

type Firefly = {
  left: number
  top: number
  size: number
  duration: number
  delay: number
  driftX: number
  driftY: number
  opacity: number
}

function makeFireflies(count: number): Firefly[] {
  return Array.from({ length: count }, () => {
    const size = 2 + Math.random() * 3.5
    return {
      left: Math.random() * 100,
      top: Math.random() * 100,
      size,
      duration: 16 + Math.random() * 18,
      delay: -Math.random() * 32,
      driftX: (Math.random() - 0.5) * 180,
      driftY: (Math.random() - 0.5) * 140,
      opacity: 0.3 + Math.random() * 0.35,
    }
  })
}

export function Fireflies({ count = 35 }: { count?: number }) {
  const { firefliesEnabled } = useTheme()
  const flies = useMemo(() => makeFireflies(count), [count])

  if (!firefliesEnabled) return null

  return (
    <div aria-hidden="true" className="pointer-events-none fixed inset-0 -z-10 overflow-hidden">
      {flies.map((fly, i) => (
        <span
          key={i}
          className="firefly"
          style={
            {
              left: `${fly.left}%`,
              top: `${fly.top}%`,
              width: `${fly.size}px`,
              height: `${fly.size}px`,
              "--ff-duration": `${fly.duration}s`,
              "--ff-delay": `${fly.delay}s`,
              "--ff-x": `${fly.driftX}px`,
              "--ff-y": `${fly.driftY}px`,
              "--ff-opacity": fly.opacity,
            } as React.CSSProperties
          }
        />
      ))}
    </div>
  )
}
