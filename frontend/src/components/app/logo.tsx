import { cn } from "@/lib/utils"

/** Marca (vagalume) — fundo transparente, usada em barras e avatares. */
export function LogoMark({ className }: { className?: string }) {
  return (
    <img
      src="/logo.png"
      alt="Vagalume"
      draggable={false}
      className={cn("h-9 w-9 object-contain select-none", className)}
    />
  )
}

/** Logo completa — símbolo centralizado (fundo transparente). */
export function LogoFull({ className }: { className?: string }) {
  return (
    <img
      src="/logo.png"
      alt="Vagalume — tracker e gerenciador de vagas"
      draggable={false}
      className={cn("h-28 w-auto select-none", className)}
    />
  )
}
