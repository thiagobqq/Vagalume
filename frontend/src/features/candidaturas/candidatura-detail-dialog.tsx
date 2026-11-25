import { useEffect, useState, type ReactNode } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Badge } from "@/components/ui/badge"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Loader2, ExternalLink, Pencil, ArrowRight } from "lucide-react"
import type { CandidaturaDetalhadaDto, Origem, Status } from "./types"
import { ORIGEM_VALUES, STATUS_LABEL, STATUS_ORDER } from "./types"
import { STATUS_STYLE } from "./status-style"
import { atualizarCandidatura, obterCandidatura } from "./api"

const STATUS_BADGE: Record<Status, "applied" | "triage" | "interview" | "offer" | "closed"> = {
  Aplicado: "applied",
  Triagem: "triage",
  Entrevista: "interview",
  Oferta: "offer",
  Encerrado: "closed",
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="min-w-0 space-y-1">
      <div className="text-xs font-medium text-muted-foreground">{label}</div>
      <div className="min-w-0 break-words text-sm">{children}</div>
    </div>
  )
}

type Props = {
  id: string | null
  open: boolean
  onOpenChange: (o: boolean) => void
  onUpdated: () => void
  onStatusChange: (status: Status) => Promise<void> | void
}

export function CandidaturaDetailDialog({ id, open, onOpenChange, onUpdated, onStatusChange }: Props) {
  const [data, setData] = useState<CandidaturaDetalhadaDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [movingStatus, setMovingStatus] = useState(false)
  const [editing, setEditing] = useState(false)
  const [err, setErr] = useState<string | null>(null)

  const [empresa, setEmpresa] = useState("")
  const [cargo, setCargo] = useState("")
  const [link, setLink] = useState("")
  const [origem, setOrigem] = useState<Origem>("Outro")
  const [obs, setObs] = useState("")

  useEffect(() => {
    if (!open || !id) return
    setLoading(true)
    setEditing(false)
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

  function enterEdit() {
    if (!data) return
    setEmpresa(data.empresa)
    setCargo(data.cargo)
    setLink(data.link ?? "")
    setOrigem((data.origem as Origem) || "Outro")
    setObs(data.observacoes ?? "")
    setErr(null)
    setEditing(true)
  }

  function cancelEdit() {
    setErr(null)
    setEditing(false)
  }

  async function handleSave() {
    if (!id) return
    if (!empresa.trim() || !cargo.trim()) {
      setErr("Empresa e cargo não podem ser vazios")
      return
    }
    setSaving(true)
    setErr(null)
    try {
      const atualizada = await atualizarCandidatura(id, {
        empresa: empresa.trim(),
        cargo: cargo.trim(),
        link: link.trim() ? link.trim() : null,
        origem,
        observacoes: obs.trim() ? obs.trim() : null,
      })
      onUpdated()
      setData((d) => (d ? { ...d, ...atualizada } : d))
      setEditing(false)
    } catch (e) {
      setErr((e as Error).message)
    } finally {
      setSaving(false)
    }
  }

  async function handleStatusChange(next: Status) {
    if (!id || !data || next === data.status) return
    setMovingStatus(true)
    setErr(null)
    try {
      await onStatusChange(next)
      const atualizada = await obterCandidatura(id)
      setData(atualizada)
    } catch (e) {
      setErr((e as Error).message)
    } finally {
      setMovingStatus(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="max-h-[90vh] overflow-auto sm:max-w-[640px]"
        {...(!data || editing ? { "aria-describedby": undefined } : {})}
      >
        <DialogHeader className="min-w-0">
          <DialogTitle className="break-words">{data ? (editing ? "Editar candidatura" : data.cargo) : "Detalhe da candidatura"}</DialogTitle>
          {data && !editing && (
            <DialogDescription className="break-words text-xs">{data.empresa}</DialogDescription>
          )}
        </DialogHeader>

        {loading ? (
          <div className="grid place-items-center gap-2 py-10 text-sm text-muted-foreground">
            <Loader2 className="h-5 w-5 animate-spin" /> carregando...
          </div>
        ) : err && !data ? (
          <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">{err}</div>
        ) : data ? (
          editing ? (
            <div className="min-w-0 space-y-5">
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
                <Button variant="outline" onClick={cancelEdit} disabled={saving}>cancelar</Button>
                <Button onClick={handleSave} disabled={saving} className="gap-2">
                  {saving && <Loader2 className="h-4 w-4 animate-spin" />} salvar
                </Button>
              </div>
            </div>
          ) : (
            <div className="min-w-0 space-y-5">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <Field label="Empresa">{data.empresa}</Field>
                <Field label="Cargo">{data.cargo}</Field>
                <Field label="Origem">{data.origem}</Field>
                <Field label="Status">
                  <Select
                    value={(data.status as Status) || "Aplicado"}
                    onValueChange={(v) => handleStatusChange(v as Status)}
                    disabled={movingStatus}
                  >
                    <SelectTrigger className="h-8 w-full max-w-[220px] gap-1.5">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {STATUS_ORDER.map((s) => (
                        <SelectItem key={s} value={s}>
                          <span className="inline-flex items-center gap-1.5">
                            <span
                              className="h-1.5 w-1.5 rounded-full"
                              style={{ background: STATUS_STYLE[s].dot }}
                              aria-hidden="true"
                            />
                            {STATUS_LABEL[s]}
                          </span>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </Field>
              </div>

              <Field label="Link">
                {data.link ? (
                  <a
                    href={data.link}
                    target="_blank"
                    rel="noreferrer"
                    className="break-all underline underline-offset-4 hover:opacity-80"
                  >
                    {data.link} <ExternalLink className="inline h-3.5 w-3.5 align-[-2px]" />
                  </a>
                ) : (
                  <span className="text-muted-foreground">sem link</span>
                )}
              </Field>

              <div className="min-w-0 space-y-1">
                <div className="text-xs font-medium text-muted-foreground">Observações</div>
                {data.observacoes ? (
                  <p className="whitespace-pre-wrap break-words rounded-lg border bg-muted/30 px-3 py-2 text-sm">{data.observacoes}</p>
                ) : (
                  <p className="text-sm text-muted-foreground">sem observações</p>
                )}
              </div>

              {data.eventos && data.eventos.length > 0 && (
                <div className="min-w-0 space-y-2">
                  <div className="text-xs font-medium text-muted-foreground">Histórico de transições</div>
                  <ol className="space-y-1.5 rounded-lg border bg-muted/20 p-3">
                    {data.eventos.map((ev) => {
                      const novoStyle = STATUS_STYLE[(ev.statusNovo as Status) || "Aplicado"]
                      const antStyle = ev.statusAnterior ? STATUS_STYLE[ev.statusAnterior as Status] : null
                      return (
                        <li key={ev.id} className="flex flex-wrap items-center justify-between gap-2 text-xs">
                          <div className="flex flex-wrap items-center gap-1.5 font-medium">
                            {ev.statusAnterior ? (
                              <>
                                <span className="inline-flex items-center gap-1">
                                  <span
                                    className="h-2 w-2 rounded-full"
                                    style={{ background: antStyle?.dot ?? "var(--muted-foreground)" }}
                                  />
                                  {STATUS_LABEL[ev.statusAnterior as Status] ?? ev.statusAnterior}
                                </span>
                                <ArrowRight className="h-3 w-3 text-muted-foreground" />
                              </>
                            ) : (
                              <span className="text-muted-foreground">Criada em</span>
                            )}
                            <span className="inline-flex items-center gap-1 font-semibold">
                              <span
                                className="h-2 w-2 rounded-full"
                                style={{ background: novoStyle?.dot ?? "var(--muted-foreground)" }}
                              />
                              {STATUS_LABEL[ev.statusNovo as Status] ?? ev.statusNovo}
                            </span>
                            {ev.observacao && (
                              <span className="text-muted-foreground">— {ev.observacao}</span>
                            )}
                          </div>
                          <span className="text-[11px] text-muted-foreground">
                            {new Date(ev.criadoEm).toLocaleString("pt-BR")}
                          </span>
                        </li>
                      )
                    })}
                  </ol>
                </div>
              )}

              <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
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
                <Button variant="outline" onClick={() => onOpenChange(false)}>fechar</Button>
                <Button onClick={enterEdit} className="gap-2">
                  <Pencil className="h-4 w-4" /> editar
                </Button>
              </div>
            </div>
          )
        ) : null}
      </DialogContent>
    </Dialog>
  )
}
