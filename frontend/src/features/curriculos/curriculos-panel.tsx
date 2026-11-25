import { useRef, useState } from "react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { NotebookPanel } from "@/components/app/notebook-panel"
import { useCurriculos } from "@/hooks/use-curriculos"
import { VerificacaoDialog } from "@/components/app/verificacao-dialog"
import { useAuth } from "@/features/auth/auth-context"
import { PERMISSOES } from "@/features/auth/permissoes"
import { AlertTriangle, Download, FileText, Loader2, Lock, Star, Trash2, Upload, Wand2 } from "lucide-react"
import { AnaliseAtsDialog } from "./analise-ats-dialog"
import type { CurriculoDto } from "@/features/candidaturas/types"

export function CurriculosPanel() {
  const { items, loading, error, upload, setAtivo, remove, download, refresh } = useCurriculos()
  const { hasPermissao } = useAuth()
  const verificado = hasPermissao(PERMISSOES.verifyUser) || hasPermissao(PERMISSOES.admin)
  const [uploading, setUploading] = useState(false)
  const [dragging, setDragging] = useState(false)
  const [rotulo, setRotulo] = useState("")
  const [busyId, setBusyId] = useState<string | null>(null)
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null)
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)
  const [analiseAlvo, setAnaliseAlvo] = useState<CurriculoDto | null>(null)
  const [analiseOpen, setAnaliseOpen] = useState(false)
  const [verificacaoOpen, setVerificacaoOpen] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  async function enviarArquivo(f: File) {
    setErr(null)
    setMsg(null)
    setUploading(true)
    try {
      const criado = await upload(f, rotulo.trim() || undefined)
      setRotulo("")
      setMsg(
        criado.textoLegivel === false
          ? "Currículo salvo, mas parece ser um PDF escaneado (sem texto selecionável). A análise por IA precisa de texto."
          : "Currículo enviado e marcado como ativo"
      )
    } catch (ex) {
      setErr((ex as Error).message)
    } finally {
      setUploading(false)
      if (inputRef.current) inputRef.current.value = ""
    }
  }

  function onFile(e: React.ChangeEvent<HTMLInputElement>) {
    const f = e.target.files?.[0]
    if (f) void enviarArquivo(f)
  }

  async function onAtivo(id: string) {
    setErr(null)
    setBusyId(`ativo:${id}`)
    try {
      await setAtivo(id)
      setMsg("Currículo marcado como ativo")
    } catch (ex) {
      setErr((ex as Error).message)
    } finally {
      setBusyId(null)
    }
  }

  async function onDownload(id: string, nome: string) {
    setBusyId(`down:${id}`)
    try {
      await download(id, nome)
    } catch (ex) {
      setErr((ex as Error).message)
    } finally {
      setBusyId(null)
    }
  }

  async function onRemover(id: string) {
    setErr(null)
    setBusyId(`del:${id}`)
    try {
      await remove(id)
      setConfirmDeleteId(null)
      setMsg("Currículo removido")
    } catch (ex) {
      setErr((ex as Error).message)
    } finally {
      setBusyId(null)
    }
  }

  return (
    <NotebookPanel
      title="Seus currículos"
      badge={
        items.length > 0 ? (
          <Badge variant="outline" className="tabular-nums">
            {items.length}
          </Badge>
        ) : undefined
      }
      actions={
        <div className="flex flex-wrap items-center gap-2">
          <Input
            value={rotulo}
            onChange={(e) => setRotulo(e.target.value)}
            placeholder="Rótulo opcional (ex.: Backend .NET)"
            maxLength={120}
            className="h-8 w-52 text-xs"
          />
          <Button size="sm" className="gap-2" onClick={() => inputRef.current?.click()} disabled={uploading}>
            {uploading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Upload className="h-4 w-4" />} enviar PDF
          </Button>
        </div>
      }
    >
      <input ref={inputRef} type="file" accept=".pdf,application/pdf" className="hidden" onChange={onFile} />

      <div
        className="space-y-3"
        onDragOver={(e) => {
          e.preventDefault()
          setDragging(true)
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={(e) => {
          e.preventDefault()
          setDragging(false)
          const f = e.dataTransfer.files?.[0]
          if (f) void enviarArquivo(f)
        }}
      >
        {dragging && (
          <div className="rounded-xl border-2 border-dashed border-primary bg-primary/5 px-4 py-6 text-center text-xs font-semibold text-primary">
            Solte o arquivo PDF aqui para enviar
          </div>
        )}

        {msg && (
          <div className="rounded-lg border border-[color:var(--st-offer)]/30 bg-[var(--st-offer-soft)] px-3 py-2 text-xs text-[color:var(--st-offer)]">
            {msg}
          </div>
        )}
        {err && (
          <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">
            {err}
          </div>
        )}
        {error && (
          <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">
            {error}{" "}
            <button onClick={() => void refresh()} className="ml-2 underline underline-offset-4">
              tentar novamente
            </button>
          </div>
        )}

        {loading ? (
          <div className="grid place-items-center gap-2 py-8 text-sm text-muted-foreground">
            <Loader2 className="h-5 w-5 animate-spin" /> carregando...
          </div>
        ) : items.length === 0 ? (
          <div
            onClick={() => inputRef.current?.click()}
            className="cursor-pointer rounded-xl border border-dashed border-ink/25 px-4 py-10 text-center transition-colors hover:border-ink/60"
          >
            <FileText className="mx-auto h-6 w-6 text-muted-foreground" />
            <p className="mt-2 text-sm font-medium">Nenhum currículo enviado ainda</p>
            <p className="mt-1 text-xs text-muted-foreground">
              Clique ou arraste um PDF aqui. O currículo ativo é usado nas análises ATS e de compatibilidade.
            </p>
          </div>
        ) : (
          <div className="space-y-2">
            {items.map((c) => (
              <div
                key={c.id}
                className="flex flex-col gap-3 rounded-lg border-[1.5px] border-ink/20 bg-[var(--nb-card)] px-3 py-2.5 shadow-hard-xs sm:flex-row sm:items-center"
              >
                <div className="flex min-w-0 flex-1 items-start gap-3">
                  <div className="grid h-8 w-8 shrink-0 place-items-center rounded-lg border-[1.5px] border-ink/20 bg-card">
                    <FileText className="h-4 w-4 text-muted-foreground" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-1.5 text-sm font-medium">
                      <span className="truncate font-semibold">{c.rotulo || c.nomeArquivoOriginal}</span>
                      {c.ativo && (
                        <Badge variant="offer" className="shrink-0 gap-1 text-[10px]">
                          <Star className="h-3 w-3" /> ativo
                        </Badge>
                      )}
                      {typeof c.scoreAts === "number" && (
                        <Badge variant="outline" className="shrink-0 tabular-nums text-[10px]">
                          ATS {c.scoreAts}/100
                        </Badge>
                      )}
                      {c.textoLegivel === false && (
                        <Badge
                          variant="outline"
                          className="shrink-0 gap-1 border-amber-500/40 bg-amber-500/10 text-[10px] text-amber-800 dark:text-amber-300"
                        >
                          <AlertTriangle className="h-3 w-3" /> sem texto
                        </Badge>
                      )}
                    </div>
                    <div className="truncate text-xs text-muted-foreground">
                      {c.rotulo ? `${c.nomeArquivoOriginal} • ` : ""}
                      {(c.tamanhoBytes / 1024).toFixed(1)} KB • {new Date(c.criadoEm).toLocaleString("pt-BR")}
                    </div>
                  </div>
                </div>

                <div className="flex shrink-0 flex-wrap justify-end gap-1.5">
                  <Button
                    variant="outline"
                    size="sm"
                    className="h-7 gap-1.5 text-xs"
                    onClick={() => {
                      if (!verificado) {
                        setVerificacaoOpen(true)
                        return
                      }
                      setAnaliseAlvo(c)
                      setAnaliseOpen(true)
                    }}
                  >
                    {verificado ? <Wand2 className="h-3.5 w-3.5" /> : <Lock className="h-3.5 w-3.5" />} analisar ATS
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    className="h-7 gap-1.5 text-xs"
                    disabled={busyId === `down:${c.id}`}
                    onClick={() => void onDownload(c.id, c.nomeArquivoOriginal)}
                  >
                    {busyId === `down:${c.id}` ? (
                      <Loader2 className="h-3.5 w-3.5 animate-spin" />
                    ) : (
                      <Download className="h-3.5 w-3.5" />
                    )}
                    baixar
                  </Button>
                  {!c.ativo && (
                    <Button
                      size="sm"
                      className="h-7 text-xs"
                      disabled={busyId === `ativo:${c.id}`}
                      onClick={() => void onAtivo(c.id)}
                    >
                      {busyId === `ativo:${c.id}` ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : "marcar ativo"}
                    </Button>
                  )}
                  {confirmDeleteId === c.id ? (
                    <Button
                      variant="destructive"
                      size="sm"
                      className="h-7 text-xs"
                      disabled={busyId === `del:${c.id}`}
                      onClick={() => void onRemover(c.id)}
                    >
                      {busyId === `del:${c.id}` ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : "confirmar"}
                    </Button>
                  ) : (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-7 w-7 text-muted-foreground hover:text-destructive"
                      aria-label={`Remover ${c.nomeArquivoOriginal}`}
                      onClick={() => setConfirmDeleteId(c.id)}
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <AnaliseAtsDialog
        curriculo={analiseAlvo}
        open={analiseOpen}
        onOpenChange={(o) => {
          setAnaliseOpen(o)
          if (!o) void refresh({ silent: true })
        }}
      />
      <VerificacaoDialog open={verificacaoOpen} onOpenChange={setVerificacaoOpen} />
    </NotebookPanel>
  )
}
