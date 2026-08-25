import { useEffect, useState } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Loader2, ExternalLink, Clock3 } from "lucide-react"
import type { CandidaturaDetalhadaDto, Origem } from "./types"
import { ORIGEM_VALUES, STATUS_VALUES } from "./types"
import { atualizarCandidatura, obterCandidatura } from "./api"

type Props = {
  id: string | null
  open: boolean
  onOpenChange: (o: boolean) => void
  onUpdated: () => void
}

export function CandidaturaDetailDialog({ id, open, onOpenChange, onUpdated }: Props) {
  const [data, setData] = useState<CandidaturaDetalhadaDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [err, setErr] = useState<string | null>(null)

  const [empresa, setEmpresa] = useState("")
  const [cargo, setCargo] = useState("")
  const [link, setLink] = useState("")
  const [origem, setOrigem] = useState<Origem>("Outro")
  const [obs, setObs] = useState("")

  useEffect(() => {
    if (!open || !id) return
    setLoading(true)
    setErr(null)
    obterCandidatura(id)
      .then((d) => {
        setData(d)
        setEmpresa(d.empresa)
        setCargo(d.cargo)
        setLink(d.link ?? "")
        setOrigem((d.origem as Origem) || "Outro")
        setObs(d.observacoes ?? "")
      })
      .catch((e) => setErr((e as Error).message))
      .finally(() => setLoading(false))
  }, [id, open])

  async function handleSave() {
    if (!id) return
    if (!empresa.trim() || !cargo.trim()) {
      setErr("Empresa e cargo não podem ser vazios")
      return
    }
    setSaving(true)
    setErr(null)
    try {
      await atualizarCandidatura(id, {
        empresa: empresa.trim(),
        cargo: cargo.trim(),
        link: link.trim() ? link.trim() : null,
        origem,
        observacoes: obs.trim() ? obs.trim() : null,
      })
      onUpdated()
      onOpenChange(false)
    } catch (e) {
      setErr((e as Error).message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[640px] bg-[#fef9f0] max-h-[90vh] overflow-auto">
        <DialogHeader>
          <DialogTitle className="text-lg">Detalhe da candidatura</DialogTitle>
          <DialogDescription className="text-xs">Edite os campos ou veja o histórico de status.</DialogDescription>
        </DialogHeader>

        {loading ? (
          <div className="py-10 grid place-items-center gap-2 text-sm"><Loader2 className="w-5 h-5 animate-spin" /> carregando...</div>
        ) : err && !data ? (
          <div className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700">{err}</div>
        ) : data ? (
          <div className="space-y-5">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Empresa</Label>
                <Input value={empresa} onChange={(e) => setEmpresa(e.target.value)} className="bg-white" />
              </div>
              <div className="space-y-1.5">
                <Label>Origem</Label>
                <Select value={origem} onValueChange={(v) => setOrigem(v as Origem)}>
                  <SelectTrigger className="bg-white"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {ORIGEM_VALUES.map((o) => <SelectItem key={o} value={o}>{o}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-1.5">
              <Label>Cargo</Label>
              <Input value={cargo} onChange={(e) => setCargo(e.target.value)} className="bg-white" />
            </div>

            <div className="space-y-1.5">
              <Label>Link</Label>
              <Input value={link} onChange={(e) => setLink(e.target.value)} className="bg-white text-xs" />
              {data.link && <a href={data.link} target="_blank" rel="noreferrer" className="text-xs inline-flex items-center gap-1 underline">abrir link <ExternalLink className="w-3 h-3" /></a>}
            </div>

            <div className="space-y-1.5">
              <Label>Observações</Label>
              <Textarea value={obs} onChange={(e) => setObs(e.target.value)} className="bg-white" rows={2} maxLength={2000} />
            </div>

            <div className="flex flex-wrap gap-2 text-xs">
              <span className="inline-flex items-center gap-1.5 rounded-full border bg-white px-3 py-1"><Clock3 className="w-3 h-3" /> status: <b>{data.status}</b></span>
              <span className="inline-flex items-center gap-1 rounded-full border bg-white px-3 py-1">criado: {new Date(data.criadoEm).toLocaleString("pt-BR")}</span>
              <span className="inline-flex items-center gap-1 rounded-full border bg-white px-3 py-1">atualizado: {new Date(data.atualizadoEm).toLocaleString("pt-BR")}</span>
            </div>

            {err && <div className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700">{err}</div>}

            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => onOpenChange(false)}>Fechar</Button>
              <Button onClick={handleSave} disabled={saving} className="gap-2">{saving ? <Loader2 className="w-4 h-4 animate-spin" /> : null} salvar</Button>
            </div>

            <div className="border-t pt-4">
              <h4 className="font-extrabold text-sm mb-2">Histórico de status</h4>
              {data.eventos.length === 0 ? (
                <p className="text-xs text-muted-foreground">sem eventos</p>
              ) : (
                <div className="space-y-2">
                  {data.eventos.map((ev) => (
                    <div key={ev.id} className="rounded-xl border bg-white px-3 py-2 text-xs flex flex-col gap-1">
                      <div className="flex items-center gap-2 font-bold">
                        <span className="px-2 py-0.5 rounded-full border bg-[#f8f5ec]">{ev.statusAnterior ?? "—"}</span>
                        <span>→</span>
                        <span className="px-2 py-0.5 rounded-full border bg-primary">{ev.statusNovo}</span>
                        <span className="ml-auto font-normal text-muted-foreground">{new Date(ev.criadoEm).toLocaleString("pt-BR")}</span>
                      </div>
                      {ev.observacao && <p className="text-foreground/70 border-l-2 border-amber-200 pl-2">{ev.observacao}</p>}
                    </div>
                  ))}
                </div>
              )}
              <p className="text-[11px] text-muted-foreground mt-2">Status disponíveis: {STATUS_VALUES.join(", ")} (transição livre, conforme backend).</p>
            </div>
          </div>
        ) : null}
      </DialogContent>
    </Dialog>
  )
}
