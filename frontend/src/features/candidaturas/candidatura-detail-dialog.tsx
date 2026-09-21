import { useEffect, useState } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Badge } from "@/components/ui/badge"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Loader2, ExternalLink } from "lucide-react"
import type { CandidaturaDetalhadaDto, Origem, Status } from "./types"
import { ORIGEM_VALUES, STATUS_VALUES } from "./types"
import { atualizarCandidatura, obterCandidatura } from "./api"

const STATUS_BADGE: Record<Status, "applied" | "triage" | "interview" | "offer" | "closed"> = {
  Aplicado: "applied",
  Triagem: "triage",
  Entrevista: "interview",
  Oferta: "offer",
  Encerrado: "closed",
}

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
      <DialogContent className="max-h-[90vh] overflow-auto sm:max-w-[640px]">
        <DialogHeader>
          <DialogTitle>Detalhe da candidatura</DialogTitle>
          <DialogDescription className="text-xs">Edite os campos ou veja o histórico de status.</DialogDescription>
        </DialogHeader>

        {loading ? (
          <div className="grid place-items-center gap-2 py-10 text-sm text-muted-foreground">
            <Loader2 className="h-5 w-5 animate-spin" /> carregando...
          </div>
        ) : err && !data ? (
          <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">{err}</div>
        ) : data ? (
          <div className="space-y-5">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label>Empresa</Label>
                <Input value={empresa} onChange={(e) => setEmpresa(e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label>Origem</Label>
                <Select value={origem} onValueChange={(v) => setOrigem(v as Origem)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {ORIGEM_VALUES.map((o) => <SelectItem key={o} value={o}>{o}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-2">
              <Label>Cargo</Label>
              <Input value={cargo} onChange={(e) => setCargo(e.target.value)} />
            </div>

            <div className="space-y-2">
              <Label>Link</Label>
              <Input value={link} onChange={(e) => setLink(e.target.value)} className="text-xs" />
              {data.link && (
                <a href={data.link} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-xs text-muted-foreground underline underline-offset-4 hover:text-foreground">
                  abrir link <ExternalLink className="h-3 w-3" />
                </a>
              )}
            </div>

            <div className="space-y-2">
              <Label>Observações</Label>
              <Textarea value={obs} onChange={(e) => setObs(e.target.value)} rows={2} maxLength={2000} />
            </div>

            <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
              <Badge variant={STATUS_BADGE[(data.status as Status) || "Aplicado"]}>status: {data.status}</Badge>
              <span className="inline-flex items-center rounded-full border bg-muted/40 px-3 py-1">
                criado: {new Date(data.criadoEm).toLocaleString("pt-BR")}
              </span>
              <span className="inline-flex items-center rounded-full border bg-muted/40 px-3 py-1">
                atualizado: {new Date(data.atualizadoEm).toLocaleString("pt-BR")}
              </span>
            </div>

            {err && (
              <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">{err}</div>
            )}

            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => onOpenChange(false)}>Fechar</Button>
              <Button onClick={handleSave} disabled={saving} className="gap-2">
                {saving && <Loader2 className="h-4 w-4 animate-spin" />} salvar
              </Button>
            </div>

            <div className="space-y-3 border-t pt-4">
              <h4 className="text-sm font-semibold">Histórico de status</h4>
              {data.eventos.length === 0 ? (
                <p className="text-xs text-muted-foreground">sem eventos</p>
              ) : (
                <div className="space-y-2">
                  {data.eventos.map((ev) => (
                    <div key={ev.id} className="flex flex-col gap-1.5 rounded-lg border bg-muted/30 px-3 py-2 text-xs">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="rounded-md border bg-card px-2 py-0.5 text-muted-foreground">
                          {ev.statusAnterior ?? "—"}
                        </span>
                        <span className="text-muted-foreground">→</span>
                        <Badge variant={STATUS_BADGE[(ev.statusNovo as Status) || "Aplicado"]}>{ev.statusNovo}</Badge>
                        <span className="ml-auto text-muted-foreground">{new Date(ev.criadoEm).toLocaleString("pt-BR")}</span>
                      </div>
                      {ev.observacao && <p className="border-l-2 border-border pl-2 text-muted-foreground">{ev.observacao}</p>}
                    </div>
                  ))}
                </div>
              )}
              <p className="text-[11px] text-muted-foreground">
                Status disponíveis: {STATUS_VALUES.join(", ")} (transição livre, conforme backend).
              </p>
            </div>
          </div>
        ) : null}
      </DialogContent>
    </Dialog>
  )
}
