import { useState } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Sparkles, Link2, Wand2, Loader2, Check } from "lucide-react"
import type { Status } from "./types"

type Extracted = { empresa: string; cargo: string; requisitos: string[]; link: string }

export function AddByLinkDialog({
  open,
  onOpenChange,
  onCreate,
}: {
  open: boolean
  onOpenChange: (o: boolean) => void
  onCreate: (data: { empresa: string; cargo: string; link: string; status: Status; observacoes?: string }) => void
}) {
  const [url, setUrl] = useState("")
  const [phase, setPhase] = useState<"idle" | "extracting" | "preview">("idle")
  const [data, setData] = useState<Extracted | null>(null)
  const [status, setStatus] = useState<Status>("aplicado")
  const [obs, setObs] = useState("")

  async function handleExtract() {
    if (!url) return
    setPhase("extracting")
    // calls MSW mocked endpoint
    try {
      const res = await fetch("/api/extract", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ url }),
      })
      const json = (await res.json()) as Extracted
      setData(json)
      setPhase("preview")
    } catch {
      // fallback fake
      setData({
        empresa: "Empresa detectada",
        cargo: "Cargo extraído via IA",
        requisitos: ["React", "TypeScript"],
        link: url,
      })
      setPhase("preview")
    }
  }

  function handleSave() {
    if (!data) return
    onCreate({ empresa: data.empresa, cargo: data.cargo, link: data.link, status, observacoes: obs || undefined })
    setUrl("")
    setData(null)
    setPhase("idle")
    setObs("")
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[560px] bg-[#fef9f0]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-xl">
            <span className="w-8 h-8 rounded-lg bg-primary border border-foreground grid place-items-center">
              <Wand2 className="w-4 h-4" />
            </span>
            Cole o link da vaga
            <span className="hand font-normal text-sm text-muted-foreground rotate-[-2deg]">IA preenche pra você</span>
          </DialogTitle>
          <DialogDescription>Cole a URL do LinkedIn / Gupy / Greenhouse. A gente lê e já anota no seu caderno.</DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="url">Link da vaga</Label>
            <div className="flex gap-2">
              <div className="relative flex-1">
                <Link2 className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                <Input id="url" placeholder="https://..." value={url} onChange={(e) => setUrl(e.target.value)} className="pl-9 bg-white" />
              </div>
              <Button onClick={handleExtract} disabled={!url || phase === "extracting"} className="gap-2 shrink-0">
                {phase === "extracting" ? <Loader2 className="w-4 h-4 animate-spin" /> : <Sparkles className="w-4 h-4" />}
                {phase === "extracting" ? "Lendo..." : "Extrair"}
              </Button>
            </div>
            <p className="text-xs text-muted-foreground hand text-[13px]">✎ dica: funciona melhor com links públicos sem login</p>
          </div>

          {phase === "extracting" && (
            <div className="rounded-xl border-[1.5px] border-foreground bg-white p-4 shadow-[4px_4px_0_var(--foreground)] flex items-center gap-3">
              <Loader2 className="w-5 h-5 animate-spin" />
              <div>
                <div className="font-bold text-sm">Lendo a vaga com IA...</div>
                <div className="text-xs text-muted-foreground">extraindo empresa, cargo e requisitos</div>
              </div>
            </div>
          )}

          {phase === "preview" && data && (
            <div className="space-y-4 animate-in fade-in">
              <div className="rounded-xl border-[1.5px] border-foreground bg-[#fef3c7] p-3 flex gap-2 items-start shadow-[3px_3px_0_var(--foreground)]">
                <Check className="w-4 h-4 mt-0.5" />
                <div className="text-xs">
                  <span className="font-extrabold">Pronto!</span> Campos preenchidos automaticamente — revise e salve no caderno.
                </div>
              </div>

              <div className="grid gap-3">
                <div className="grid grid-cols-2 gap-3">
                  <div className="space-y-1.5">
                    <Label>Empresa</Label>
                    <Input value={data.empresa} onChange={(e) => setData({ ...data, empresa: e.target.value })} className="bg-white" />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Status inicial</Label>
                    <Select value={status} onValueChange={(v) => setStatus(v as Status)}>
                      <SelectTrigger className="bg-white">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="aplicado">Aplicado</SelectItem>
                        <SelectItem value="triagem">Triagem</SelectItem>
                        <SelectItem value="entrevista">Entrevista</SelectItem>
                        <SelectItem value="oferta">Oferta</SelectItem>
                        <SelectItem value="recusado">Recusado</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>
                <div className="space-y-1.5">
                  <Label>Cargo</Label>
                  <Input value={data.cargo} onChange={(e) => setData({ ...data, cargo: e.target.value })} className="bg-white" />
                </div>
                <div className="space-y-1.5">
                  <Label>Link</Label>
                  <Input value={data.link} onChange={(e) => setData({ ...data, link: e.target.value })} className="bg-white text-xs" />
                </div>
                <div className="space-y-1.5">
                  <Label>Requisitos detectados</Label>
                  <div className="flex flex-wrap gap-1.5">
                    {data.requisitos.map((r) => (
                      <span key={r} className="text-xs font-bold bg-white border border-foreground rounded-full px-2.5 py-1">
                        {r}
                      </span>
                    ))}
                  </div>
                </div>
                <div className="space-y-1.5">
                  <Label>Anotação (opcional)</Label>
                  <Textarea placeholder="Ex: indiquei via recrutadora Ana..." value={obs} onChange={(e) => setObs(e.target.value)} className="bg-white" rows={2} />
                </div>
              </div>

              <div className="flex justify-end gap-2 pt-2">
                <Button variant="outline" onClick={() => setPhase("idle")}>
                  Voltar
                </Button>
                <Button onClick={handleSave} className="gap-2">
                  <span>Salvar no caderno</span> ✦
                </Button>
              </div>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
