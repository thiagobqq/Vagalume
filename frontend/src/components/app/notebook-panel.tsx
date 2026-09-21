import { cn } from "@/lib/utils"

type NotebookPanelProps = Omit<React.HTMLAttributes<HTMLDivElement>, "title"> & {
  /** título do cabeçalho (opcional) */
  title?: React.ReactNode
  /** linha de apoio abaixo do título */
  description?: React.ReactNode
  /** conteúdo no canto direito do cabeçalho (ex.: badge) */
  badge?: React.ReactNode
  /** ações no canto direito do cabeçalho (ex.: botão primário) */
  actions?: React.ReactNode
  /** cor da aba/marcador post-it (aceita qualquer cor CSS; default: --primary) */
  headerColor?: string
  /** furos de fichário na margem esquerda (default: true) */
  holes?: boolean
  /** pauta (linhas de caderno) sutil ao fundo (default: false) */
  ruled?: boolean
  /** fita washi no topo (default: false) */
  tape?: boolean
  children: React.ReactNode
}

/**
 * Container "folha de caderno" reutilizável do Design System Vagalume.
 * Aplica borda tinta, sombra dura, cantos arredondados, furos de fichário
 * na margem esquerda e, opcionalmente, pauta e fita washi.
 */
export function NotebookPanel({
  title,
  description,
  badge,
  actions,
  headerColor,
  holes = true,
  ruled = false,
  tape = false,
  className,
  children,
  ...props
}: NotebookPanelProps) {
  const hasHeader = Boolean(title || description || badge || actions)

  return (
    <div className={cn("relative min-w-0", tape && "pt-2")}>
      {tape && (
        <div
          className="tape absolute left-1/2 top-0 z-20 h-6 w-24 -translate-x-1/2 -rotate-2 rounded-[2px]"
          aria-hidden="true"
        />
      )}

      <div
        className={cn(
          "relative min-w-0 overflow-hidden rounded-2xl border-[1.5px] border-ink bg-card text-card-foreground shadow-hard",
          className
        )}
        {...props}
      >
        {/* fita/aba post-it no topo (fichas sem cabeçalho) */}
        {!hasHeader && headerColor && (
          <span
            className="absolute right-3 top-0 z-20 h-2 w-10 rounded-b-[3px] border-x border-b border-ink/20"
            style={{ background: headerColor }}
            aria-hidden="true"
          />
        )}

        {/* furos de fichário na margem esquerda */}
        {holes && (
          <div
            className="spiral-holes pointer-events-none absolute inset-y-0 left-0 z-0 hidden w-6 border-r-[1.5px] border-ink/10 bg-[var(--nb-paper)]/50 sm:block"
            aria-hidden="true"
          />
        )}

        {/* pauta sutil (linhas de caderno) */}
        {ruled && (
          <div
            className="pointer-events-none absolute inset-0 opacity-[0.05]"
            style={{
              backgroundImage:
                "repeating-linear-gradient(to bottom, transparent 0 27px, var(--nb-ink) 27px 28px)",
            }}
            aria-hidden="true"
          />
        )}

        {hasHeader && (
          <div
            className={cn(
              "relative z-20 flex flex-wrap items-center justify-between gap-3 border-b-[1.5px] border-ink/15 bg-card/70 px-4 py-3.5 backdrop-blur-sm",
              holes && "sm:pl-10"
            )}
          >
            <div className="flex min-w-0 items-center gap-2.5">
              {/* aba/marcador post-it */}
              <span
                className="h-4 w-1.5 shrink-0 rounded-full border border-ink/20"
                style={{ background: headerColor ?? "var(--primary)" }}
                aria-hidden="true"
              />
              <div className="min-w-0">
                {title && <h3 className="truncate text-sm font-extrabold tracking-tight">{title}</h3>}
                {description && <p className="text-xs text-muted-foreground">{description}</p>}
              </div>
            </div>
            {(badge || actions) && (
              <div className="flex shrink-0 items-center gap-2">
                {badge}
                {actions}
              </div>
            )}
          </div>
        )}

        <div className={cn("relative z-20 p-4", holes && "sm:pl-10")}>
          {children}
        </div>
      </div>
    </div>
  )
}

/**
 * Atalho para uma "ficha" sem cabeçalho — usar dentro de grades de cards.
 */
export function NotebookCard({
  className,
  ...props
}: Omit<NotebookPanelProps, "title" | "description" | "badge" | "actions">) {
  return <NotebookPanel className={cn("shadow-hard-xs", className)} {...props} />
}
