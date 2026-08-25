import { useRef, useState } from "react"
import { Button } from "@/components/ui/button"
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
    <div className="rounded-2xl border-[1.5px] border-foreground bg-white shadow-[4px_4px_0_var(--foreground)] overflow-hidden">
      <div className="px-4 py-3 border-b bg-[#fef9f0] flex items-center justify-between gap-3">
        <div>
          <h3 className="font-extrabold text-sm">Currículos</h3>
          <p className="text-xs text-muted-foreground">PDF até 10 MB. Um fica ativo por vez (backend desativa os anteriores).</p>
        </div>
        <Button size="sm" className="gap-2" onClick={() => inputRef.current?.click()} disabled={uploading}>
          {uploading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Upload className="w-4 h-4" />} enviar PDF
        </Button>
        <input ref={inputRef} type="file" accept=".pdf" className="hidden" onChange={onFile} />
      </div>

      <div className="p-4 space-y-3">
        {msg && <div className="rounded-xl border border-green-200 bg-green-50 px-3 py-2 text-xs text-green-800">{msg}</div>}
        {err && <div className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700">{err}</div>}
        {error && <div className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700">{error} <button onClick={refresh} className="underline ml-2">tentar novamente</button></div>}

        {loading ? (
          <div className="py-8 grid place-items-center gap-2 text-sm"><Loader2 className="w-5 h-5 animate-spin" /> carregando...</div>
        ) : items.length === 0 ? (
          <div className="rounded-xl border border-dashed bg-[#f8f5ec] px-4 py-8 text-center">
            <FileText className="w-6 h-6 mx-auto text-muted-foreground" />
            <p className="text-sm font-bold mt-2">nenhum currículo ainda</p>
            <p className="text-xs text-muted-foreground">envie seu PDF para usar depois nas candidaturas</p>
          </div>
        ) : (
          <div className="space-y-2">
            {items.map((c) => (
              <div key={c.id} className="rounded-xl border bg-[#fef9f0] px-3 py-2.5 flex items-center gap-3">
                <div className="w-8 h-8 rounded-lg bg-white border grid place-items-center shrink-0"><FileText className="w-4 h-4" /></div>
                <div className="min-w-0 flex-1">
                  <div className="text-sm font-bold truncate flex items-center gap-2">
                    {c.nomeArquivoOriginal} {c.ativo && <span className="inline-flex items-center gap-1 rounded-full bg-primary border border-foreground px-2 py-0.5 text-[10px] leading-none"><Star className="w-3 h-3" /> ativo</span>}
                  </div>
                  <div className="text-xs text-muted-foreground">{(c.tamanhoBytes / 1024).toFixed(1)} KB • {new Date(c.criadoEm).toLocaleString("pt-BR")}</div>
                </div>
                <div className="flex gap-1.5 shrink-0">
                  <Button variant="outline" size="sm" className="h-7 text-xs bg-white" onClick={() => onDownload(c.id, c.nomeArquivoOriginal)}><Download className="w-3.5 h-3.5" /> baixar</Button>
                  {!c.ativo && <Button size="sm" className="h-7 text-xs" onClick={() => onAtivo(c.id)}>marcar ativo</Button>}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
