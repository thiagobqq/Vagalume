import { useRef, useState } from "react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { useCurriculos } from "@/hooks/use-curriculos"
import { Loader2, Upload, Download, Star, FileText } from "lucide-react"

export function CurriculosPanel() {
  const { items, loading, error, upload, setAtivo, download, refresh } = useCurriculos()
  const [uploading, setUploading] = useState(false)
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  async function onFile(e: React.ChangeEvent<HTMLInputElement>) {
    const f = e.target.files?.[0]
    if (!f) return
    setErr(null)
    setMsg(null)
    setUploading(true)
    try {
      await upload(f)
      setMsg("Currículo enviado com sucesso")
    } catch (ex) {
      setErr((ex as Error).message)
    } finally {
      setUploading(false)
      if (inputRef.current) inputRef.current.value = ""
    }
  }

  async function onAtivo(id: string) {
    setErr(null)
    try {
      await setAtivo(id)
      setMsg("Currículo marcado como ativo")
    } catch (ex) {
      setErr((ex as Error).message)
    }
  }

  async function onDownload(id: string, nome: string) {
    try {
      await download(id, nome)
    } catch (ex) {
      setErr((ex as Error).message)
    }
  }

  return (
    <div className="overflow-hidden rounded-xl border-[1.5px] border-ink bg-card shadow-hard">
      <div className="flex items-center justify-between gap-3 border-b bg-muted/30 px-5 py-3.5">
        <div>
          <h3 className="text-sm font-semibold">Seus currículos</h3>
          <p className="text-xs text-muted-foreground">PDF até 10 MB. Um fica ativo por vez.</p>
        </div>
        <Button size="sm" className="gap-2" onClick={() => inputRef.current?.click()} disabled={uploading}>
          {uploading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Upload className="h-4 w-4" />} enviar PDF
        </Button>
        <input ref={inputRef} type="file" accept=".pdf" className="hidden" onChange={onFile} />
      </div>

      <div className="space-y-3 p-5">
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
            {error} <button onClick={refresh} className="ml-2 underline underline-offset-4">tentar novamente</button>
          </div>
        )}

        {loading ? (
          <div className="grid place-items-center gap-2 py-8 text-sm text-muted-foreground">
            <Loader2 className="h-5 w-5 animate-spin" /> carregando...
          </div>
        ) : items.length === 0 ? (
          <div className="rounded-lg border border-dashed px-4 py-10 text-center">
            <FileText className="mx-auto h-6 w-6 text-muted-foreground" />
            <p className="mt-2 text-sm font-medium">nenhum currículo ainda</p>
            <p className="text-xs text-muted-foreground">envie seu PDF para usar depois nas candidaturas</p>
          </div>
        ) : (
          <div className="space-y-2">
            {items.map((c) => (
              <div key={c.id} className="flex items-center gap-3 rounded-lg border bg-muted/30 px-3 py-2.5">
                <div className="grid h-8 w-8 shrink-0 place-items-center rounded-lg border bg-card">
                  <FileText className="h-4 w-4 text-muted-foreground" />
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 truncate text-sm font-medium">
                    {c.nomeArquivoOriginal}
                    {c.ativo && (
                      <Badge className="gap-1 text-[10px]">
                        <Star className="h-3 w-3" /> ativo
                      </Badge>
                    )}
                  </div>
                  <div className="text-xs text-muted-foreground">
                    {(c.tamanhoBytes / 1024).toFixed(1)} KB • {new Date(c.criadoEm).toLocaleString("pt-BR")}
                  </div>
                </div>
                <div className="flex shrink-0 gap-1.5">
                  <Button variant="outline" size="sm" className="h-7 gap-1.5 text-xs" onClick={() => onDownload(c.id, c.nomeArquivoOriginal)}>
                    <Download className="h-3.5 w-3.5" /> baixar
                  </Button>
                  {!c.ativo && (
                    <Button size="sm" className="h-7 text-xs" onClick={() => onAtivo(c.id)}>
                      marcar ativo
                    </Button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
