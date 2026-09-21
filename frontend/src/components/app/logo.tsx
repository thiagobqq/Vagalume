import { cn } from "@/lib/utils"

/** Marca (vagalume) — quadrado amarelo, usada em barras e avatares. */
export function LogoMark({ className }: { className?: string }) {
  return (
    <img
      src="/logo.png"
      alt="Vagalume"
      draggable={false}
      className={cn("h-9 w-9 rounded-lg border-[1.5px] border-ink object-cover shadow-hard-xs", className)}
    />
  )
}

/** Logo completa com assinatura — versões clara e escura (fundo transparente). */
export function LogoFull({ className }: { className?: string }) {
  return (
    <>
      <img
        src="/logo-full.png"
        alt="Vagalume — tracker e gerenciador de vagas"
        draggable={false}
        className={cn("w-full select-none dark:hidden", className)}
      />
      <img
        src="/logo-full-dark.png"
        alt=""
        aria-hidden="true"
        draggable={false}
        className={cn("hidden w-full select-none dark:block", className)}
      />
    </>
  )
}
