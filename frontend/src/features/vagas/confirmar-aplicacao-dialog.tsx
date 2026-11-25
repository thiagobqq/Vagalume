import { useEffect, useState } from "react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { CheckCircle2, ExternalLink, Loader2 } from "lucide-react"
import { safeExternalUrl } from "@/lib/url"
import type { PendenteVaga } from "@/hooks/use-aplicacao-pendente"

/**
 * Confirmação pós-redirect do "aplicar": a candidatura só é criada no "sim".
 * O apply pendente é controlado pelo pai (useAplicacaoPendente).
 */
export function ConfirmarAplicacaoDialog({
  pendente,
  onConfirm,
  onCancel,
}: {
  pendente: PendenteVaga | null
  /** cria de fato a candidatura (Aplicado). Deve lançar em caso de erro. */
  onConfirm: () => Promise<void>
  /** usuário disse que não aplicou / fechou o modal */
  onCancel: () => void
}) {
  const [confirmando, setConfirmando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)

  // ao trocar/encerrar o pendente, zera o estado interno
  useEffect(() => {
    if (!pendente) {
      setConfirmando(false)
      setErro(null)
    }
  }, [pendente])

  async function handleConfirm() {
    setConfirmando(true)
    setErro(null)
    try {
      await onConfirm()
    } catch (e) {
      setErro((e as Error).message || "Não foi possível registrar a candidatura")
      setConfirmando(false)
    }
  }

  const safeUrl = safeExternalUrl(pendente?.url)

  return (
    <Dialog open={!!pendente} onOpenChange={(o) => { if (!o && !confirmando) onCancel() }}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <CheckCircle2 className="h-4 w-4" /> Você aplicou para esta vaga?
          </DialogTitle>
          <DialogDescription>
            {pendente ? `${pendente.empresa} · ${pendente.titulo}` : ""}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-3 text-sm">
          <p className="text-muted-foreground">
            Abrimos a vaga em outra aba. Se você concluiu a candidatura lá, confirme para registrá-la
            em <span className="font-semibold text-foreground">Aplicado</span>.
          </p>
          {safeUrl && (
            <a
              href={safeUrl}
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-1 text-xs font-semibold underline underline-offset-4 hover:opacity-80"
            >
              abrir vaga novamente <ExternalLink className="h-3 w-3" />
            </a>
          )}
          {erro && (
            <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">
              {erro}
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={confirmando}>
            não apliquei
          </Button>
          <Button onClick={handleConfirm} disabled={confirmando} className="gap-1.5">
            {confirmando ? <Loader2 className="h-4 w-4 animate-spin" /> : <CheckCircle2 className="h-4 w-4" />}
            sim, apliquei
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
