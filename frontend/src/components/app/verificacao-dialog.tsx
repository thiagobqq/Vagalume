import { useEffect, useState } from "react"
import { CheckCircle2, Clock3, Loader2, Lock, Send } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { apiJson } from "@/lib/api"
import type { SolicitacaoVerificacaoDto } from "@/features/candidaturas/types"

export function VerificacaoDialog({
  open,
  onOpenChange,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const [solicitacao, setSolicitacao] = useState<SolicitacaoVerificacaoDto | null>(null)
  const [mensagem, setMensagem] = useState("")
  const [loading, setLoading] = useState(false)
  const [enviando, setEnviando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)
  const [sucesso, setSucesso] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    let ativo = true
    setLoading(true)
    setErro(null)
    setSucesso(null)
    apiJson<SolicitacaoVerificacaoDto | null>("/api/auth/solicitacao-verificacao")
      .then((res) => {
        if (!ativo) return
        setSolicitacao(res)
        if (res?.mensagem) setMensagem(res.mensagem)
      })
      .catch(() => {
        /* silencioso */
      })
      .finally(() => {
        if (ativo) setLoading(false)
      })
    return () => {
      ativo = false
    }
  }, [open])

  async function solicitar() {
    setEnviando(true)
    setErro(null)
    setSucesso(null)
    try {
      const res = await apiJson<SolicitacaoVerificacaoDto>("/api/auth/solicitacao-verificacao", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ mensagem: mensagem.trim() || null }),
      })
      setSolicitacao(res)
      setSucesso("Solicitação registrada! Assim que aprovada pelo mantenedor, os recursos serão liberados automaticamente.")
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setEnviando(false)
    }
  }

  const pendente = solicitacao?.status === "Pendente"

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Lock className="h-4 w-4" /> Recurso para contas verificadas
          </DialogTitle>
          <DialogDescription>
            Enquanto o Vagalume está em fase de testes, recursos de IA (BYOK) e fontes adicionais de busca exigem verificação manual.
          </DialogDescription>
        </DialogHeader>

        {loading ? (
          <div className="flex items-center justify-center gap-2 py-4 text-xs text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" /> verificando status...
          </div>
        ) : (
          <div className="space-y-3">
            {pendente && (
              <div className="flex items-start gap-2 rounded-lg border border-amber-500/30 bg-amber-500/5 px-3 py-2 text-xs text-amber-800 dark:text-amber-300">
                <Clock3 className="mt-0.5 h-4 w-4 shrink-0" />
                <div>
                  <div className="font-semibold">Solicitação pendente na fila</div>
                  <div className="opacity-85">
                    Enviada em {new Date(solicitacao.criadoEm).toLocaleDateString("pt-BR")}. Você pode atualizar a nota abaixo se quiser.
                  </div>
                </div>
              </div>
            )}

            {sucesso && (
              <div className="flex items-start gap-2 rounded-lg border border-emerald-500/30 bg-emerald-500/5 px-3 py-2 text-xs text-emerald-700 dark:text-emerald-400">
                <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" />
                <span>{sucesso}</span>
              </div>
            )}

            {erro && (
              <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">
                {erro}
              </div>
            )}

            <div className="space-y-1">
              <label htmlFor="verificacao-msg" className="text-xs font-medium text-muted-foreground">
                Nota opcional (ex.: sua área de atuação)
              </label>
              <Textarea
                id="verificacao-msg"
                rows={2}
                maxLength={500}
                value={mensagem}
                onChange={(e) => setMensagem(e.target.value)}
                placeholder="Ex.: Busco vagas de Backend .NET / React remoto"
              />
            </div>
          </div>
        )}

        <DialogFooter className="gap-2">
          <Button
            variant="ghost"
            onClick={() => onOpenChange(false)}
            className="text-muted-foreground hover:text-foreground"
          >
            fechar
          </Button>
          <Button onClick={() => void solicitar()} disabled={enviando || loading} className="gap-1.5">
            {enviando ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-3.5 w-3.5" />}
            {pendente ? "atualizar pedido" : "solicitar verificação"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
