import { useEffect, useRef, useState, type ReactNode } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Badge } from "@/components/ui/badge"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { VerificacaoDialog } from "@/components/app/verificacao-dialog"
import { useAuth } from "@/features/auth/auth-context"
import { PERMISSOES } from "@/features/auth/permissoes"
import {
  ArrowRight,
  Check,
  CheckCircle2,
  Copy,
  ExternalLink,
  Loader2,
  Lock,
  MessageSquareText,
  Mic,
  Pencil,
  Sparkles,
  Trash2,
  Wand2,
  XCircle,
} from "lucide-react"
import type { CandidaturaDetalhadaDto, Origem, PrepEntrevistaDto, RascunhoFollowUpDto, Status } from "./types"
import { ORIGEM_LABEL, ORIGEM_VALUES, STATUS_LABEL, STATUS_ORDER } from "./types"
import { MODALIDADE_LABEL, MODALIDADES, type AnaliseCompatibilidadeDto } from "@/features/vagas/types"
import { STATUS_STYLE } from "./status-style"
import {
  analisarCompatibilidadeCandidatura,
  atualizarCandidatura,
  gerarFollowUpCandidatura,
  gerarPrepEntrevista,
  obterCandidatura,
  obterPrepEntrevista,
} from "./api"
import { safeExternalUrl } from "@/lib/url"

const STATUS_BADGE: Record<Status, "applied" | "triage" | "interview" | "offer" | "closed"> = {
  Aplicado: "applied",
  Triagem: "triage",
  Entrevista: "interview",
  Oferta: "offer",
  Encerrado: "closed",
}

const MOTIVOS_ENCERRAMENTO = [
  { value: "", label: "Não informado" },
  { value: "SemRetorno", label: "Sem retorno (ghosting)" },
  { value: "RecusaTriagem", label: "Recusa na triagem" },
  { value: "RecusaTecnica", label: "Recusa técnica" },
  { value: "RecusaComportamental", label: "Fit cultural / comportamental" },
  { value: "Salario", label: "Alinhamento salarial" },
  { value: "VagaCongelada", label: "Vaga congelada / cancelada" },
  { value: "Desistencia", label: "Desistência minha" },
]

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
  onDelete?: (id: string) => Promise<void>
  onStatusChange: (status: Status, observacao?: string) => Promise<void> | void
}

export function CandidaturaDetailDialog({ id, open, onOpenChange, onUpdated, onDelete, onStatusChange }: Props) {
  const { hasPermissao } = useAuth()
  const verificado = hasPermissao(PERMISSOES.verifyUser) || hasPermissao(PERMISSOES.admin)

  const [data, setData] = useState<CandidaturaDetalhadaDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [movingStatus, setMovingStatus] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [editing, setEditing] = useState(false)
  const [err, setErr] = useState<string | null>(null)
  const [verificacaoOpen, setVerificacaoOpen] = useState(false)

  const [empresa, setEmpresa] = useState("")
  const [cargo, setCargo] = useState("")
  const [link, setLink] = useState("")
  const [origem, setOrigem] = useState<Origem>("Outro")
  const [modalidade, setModalidade] = useState("NaoInformado")
  const [faixaSalarial, setFaixaSalarial] = useState("")
  const [motivoEncerramento, setMotivoEncerramento] = useState("")
  const [descricaoVaga, setDescricaoVaga] = useState("")
  const [obs, setObs] = useState("")
  const [notaTransicao, setNotaTransicao] = useState("")

  // Assistente IA
  const [iaErro, setIaErro] = useState<string | null>(null)
  const [compatLoading, setCompatLoading] = useState(false)
  const [compatRes, setCompatRes] = useState<AnaliseCompatibilidadeDto | null>(null)
  const [followLoading, setFollowLoading] = useState(false)
  const [followUp, setFollowUp] = useState<RascunhoFollowUpDto | null>(null)
  const [copiadoFollow, setCopiadoFollow] = useState(false)
  const [prepLoading, setPrepLoading] = useState(false)
  const [prep, setPrep] = useState<PrepEntrevistaDto | null>(null)

  const reqIdRef = useRef(0)

  useEffect(() => {
    const reqId = ++reqIdRef.current
    if (!open || !id) {
      setConfirmDelete(false)
      setNotaTransicao("")
      setCompatRes(null)
      setFollowUp(null)
      setPrep(null)
      setIaErro(null)
      return
    }
    setLoading(true)
    setEditing(false)
    setConfirmDelete(false)
    setNotaTransicao("")
    setCompatRes(null)
    setFollowUp(null)
    setPrep(null)
    setIaErro(null)
    setErr(null)

    obterCandidatura(id)
      .then((d) => {
        if (reqId !== reqIdRef.current) return
        setData(d)
        setEmpresa(d.empresa)
        setCargo(d.cargo)
        setLink(d.link ?? "")
        setOrigem((d.origem as Origem) || "Outro")
        setModalidade(d.modalidade ?? "NaoInformado")
        setFaixaSalarial(d.faixaSalarial ?? "")
        setMotivoEncerramento(d.motivoEncerramento ?? "")
        setDescricaoVaga(d.descricaoVaga ?? "")
        setObs(d.observacoes ?? "")
        if (verificado) {
          obterPrepEntrevista(id)
            .then((p) => {
              if (reqId === reqIdRef.current && p) setPrep(p)
            })
            .catch(() => {})
        }
      })
      .catch((e) => {
        if (reqId === reqIdRef.current) setErr((e as Error).message)
      })
      .finally(() => {
        if (reqId === reqIdRef.current) setLoading(false)
      })
  }, [id, open, verificado])

  function enterEdit() {
    if (!data) return
    setEmpresa(data.empresa)
    setCargo(data.cargo)
    setLink(data.link ?? "")
    setOrigem((data.origem as Origem) || "Outro")
    setModalidade(data.modalidade ?? "NaoInformado")
    setFaixaSalarial(data.faixaSalarial ?? "")
    setMotivoEncerramento(data.motivoEncerramento ?? "")
    setDescricaoVaga(data.descricaoVaga ?? "")
    setObs(data.observacoes ?? "")
    setErr(null)
    setEditing(true)
  }

  function cancelEdit() {
    setErr(null)
    setEditing(false)
  }

  async function handleSave(e: React.FormEvent) {
    e.preventDefault()
    if (!id) return
    if (!empresa.trim() || !cargo.trim()) {
      setErr("Empresa e cargo não podem ser vazios")
      return
    }
    const linkTrimmed = link.trim()
    if (linkTrimmed && !safeExternalUrl(linkTrimmed)) {
      setErr("Link deve ser uma URL HTTP ou HTTPS válida")
      return
    }
    setSaving(true)
    setErr(null)
    try {
      const atualizada = await atualizarCandidatura(id, {
        empresa: empresa.trim(),
        cargo: cargo.trim(),
        link: linkTrimmed,
        origem,
        modalidade: modalidade === "NaoInformado" ? "" : modalidade,
        faixaSalarial: faixaSalarial.trim(),
        motivoEncerramento: motivoEncerramento.trim(),
        descricaoVaga: descricaoVaga.trim(),
        observacoes: obs.trim(),
      })
      onUpdated()
      setData((d) => (d ? { ...d, ...atualizada } : d))
      setEditing(false)
    } catch (ex) {
      setErr((ex as Error).message)
    } finally {
      setSaving(false)
    }
  }

  async function handleStatusChange(next: Status) {
    if (!id || !data || next === data.status) return
    setMovingStatus(true)
    setErr(null)
    try {
      await onStatusChange(next, notaTransicao.trim() || undefined)
      const atualizada = await obterCandidatura(id)
      setData(atualizada)
      setNotaTransicao("")
    } catch (e) {
      setErr((e as Error).message)
    } finally {
      setMovingStatus(false)
    }
  }

  async function handleDelete() {
    if (!id || !onDelete) return
    setDeleting(true)
    setErr(null)
    try {
      await onDelete(id)
    } catch (e) {
      setErr((e as Error).message)
      setDeleting(false)
    }
  }

  async function handleAnalisarCompat() {
    if (!id) return
    if (!verificado) {
      setVerificacaoOpen(true)
      return
    }
    setCompatLoading(true)
    setIaErro(null)
    try {
      setCompatRes(await analisarCompatibilidadeCandidatura(id))
    } catch (e) {
      setIaErro((e as Error).message)
    } finally {
      setCompatLoading(false)
    }
  }

  async function handleGerarFollowUp() {
    if (!id) return
    if (!verificado) {
      setVerificacaoOpen(true)
      return
    }
    setFollowLoading(true)
    setIaErro(null)
    setCopiadoFollow(false)
    try {
      setFollowUp(await gerarFollowUpCandidatura(id))
    } catch (e) {
      setIaErro((e as Error).message)
    } finally {
      setFollowLoading(false)
    }
  }

  async function handleGerarPrep() {
    if (!id) return
    if (!verificado) {
      setVerificacaoOpen(true)
      return
    }
    setPrepLoading(true)
    setIaErro(null)
    try {
      setPrep(await gerarPrepEntrevista(id))
    } catch (e) {
      setIaErro((e as Error).message)
    } finally {
      setPrepLoading(false)
    }
  }

  const safeLink = safeExternalUrl(data?.link)
  const motivoLabel = MOTIVOS_ENCERRAMENTO.find((m) => m.value === (data?.motivoEncerramento ?? ""))?.label

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent
          className="max-h-[90vh] overflow-auto sm:max-w-[680px]"
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
              <form onSubmit={handleSave} className="min-w-0 space-y-4">
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label htmlFor="edit-empresa">Empresa</Label>
                    <Input id="edit-empresa" value={empresa} onChange={(e) => setEmpresa(e.target.value)} maxLength={200} required />
                  </div>
                  <div className="space-y-1.5">
                    <Label htmlFor="edit-origem">Origem</Label>
                    <Select value={origem} onValueChange={(v) => setOrigem(v as Origem)}>
                      <SelectTrigger id="edit-origem"><SelectValue /></SelectTrigger>
                      <SelectContent>
                        {ORIGEM_VALUES.map((o) => <SelectItem key={o} value={o}>{ORIGEM_LABEL[o]}</SelectItem>)}
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="space-y-1.5">
                  <Label htmlFor="edit-cargo">Cargo</Label>
                  <Input id="edit-cargo" value={cargo} onChange={(e) => setCargo(e.target.value)} maxLength={200} required />
                </div>

                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label htmlFor="edit-modalidade">Modalidade</Label>
                    <Select value={modalidade} onValueChange={setModalidade}>
                      <SelectTrigger id="edit-modalidade"><SelectValue /></SelectTrigger>
                      <SelectContent>
                        <SelectItem value="NaoInformado">Não informado</SelectItem>
                        {MODALIDADES.map((m) => <SelectItem key={m} value={m}>{MODALIDADE_LABEL[m]}</SelectItem>)}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-1.5">
                    <Label htmlFor="edit-salario">Faixa salarial</Label>
                    <Input id="edit-salario" value={faixaSalarial} onChange={(e) => setFaixaSalarial(e.target.value)} maxLength={120} placeholder="Ex: R$ 8k - 10k" />
                  </div>
                </div>

                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label htmlFor="edit-link">Link</Label>
                    <Input id="edit-link" value={link} onChange={(e) => setLink(e.target.value)} maxLength={2048} placeholder="https://..." className="text-xs" />
                  </div>
                  <div className="space-y-1.5">
                    <Label htmlFor="edit-motivo">Motivo de encerramento</Label>
                    <Select value={motivoEncerramento || "__none__"} onValueChange={(v) => setMotivoEncerramento(v === "__none__" ? "" : v)}>
                      <SelectTrigger id="edit-motivo"><SelectValue /></SelectTrigger>
                      <SelectContent>
                        {MOTIVOS_ENCERRAMENTO.map((m) => (
                          <SelectItem key={m.value || "__none__"} value={m.value || "__none__"}>{m.label}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="space-y-1.5">
                  <Label htmlFor="edit-desc">Descrição / requisitos da vaga</Label>
                  <Textarea id="edit-desc" value={descricaoVaga} onChange={(e) => setDescricaoVaga(e.target.value)} rows={3} maxLength={20000} />
                </div>

                <div className="space-y-1.5">
                  <Label htmlFor="edit-obs">Observações</Label>
                  <Textarea id="edit-obs" value={obs} onChange={(e) => setObs(e.target.value)} rows={2} maxLength={2000} />
                </div>

                <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
                  <Badge variant={STATUS_BADGE[(data.status as Status) || "Aplicado"]}>status: {data.status}</Badge>
                </div>

                {err && (
                  <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">{err}</div>
                )}

                <div className="flex justify-end gap-2">
                  <Button type="button" variant="outline" onClick={cancelEdit} disabled={saving}>cancelar</Button>
                  <Button type="submit" disabled={saving} className="gap-2">
                    {saving && <Loader2 className="h-4 w-4 animate-spin" />} salvar
                  </Button>
                </div>
              </form>
            ) : (
              <div className="min-w-0 space-y-5">
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <Field label="Empresa">{data.empresa}</Field>
                  <Field label="Cargo">{data.cargo}</Field>
                  <Field label="Origem">{ORIGEM_LABEL[data.origem as Origem] ?? data.origem}</Field>
                  <Field label="Status">
                    <div className="space-y-1.5">
                      <Select
                        value={(data.status as Status) || "Aplicado"}
                        onValueChange={(v) => void handleStatusChange(v as Status)}
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
                      <Input
                        value={notaTransicao}
                        onChange={(e) => setNotaTransicao(e.target.value)}
                        placeholder="Nota opcional antes de mudar o status..."
                        maxLength={300}
                        className="h-7 text-xs"
                      />
                    </div>
                  </Field>
                  {(data.modalidade && data.modalidade !== "NaoInformado") && (
                    <Field label="Modalidade">{MODALIDADE_LABEL[data.modalidade] ?? data.modalidade}</Field>
                  )}
                  {data.faixaSalarial && <Field label="Faixa salarial">{data.faixaSalarial}</Field>}
                  {data.motivoEncerramento && (
                    <Field label="Motivo de encerramento">{motivoLabel || data.motivoEncerramento}</Field>
                  )}
                </div>

                {/* Assistente IA (Compatibilidade, Follow-up, Prep de Entrevista) */}
                <div className="space-y-3 rounded-xl border border-ink/20 bg-muted/20 p-3.5">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <span className="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wide text-muted-foreground">
                      <Sparkles className="h-3.5 w-3.5 text-primary" /> Assistente IA (BYOK)
                    </span>
                    <div className="flex flex-wrap gap-1.5">
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        className="h-7 gap-1.5 text-xs"
                        disabled={compatLoading}
                        onClick={() => void handleAnalisarCompat()}
                      >
                        {compatLoading ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : !verificado ? <Lock className="h-3.5 w-3.5" /> : <Wand2 className="h-3.5 w-3.5" />}
                        compatibilidade
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        className="h-7 gap-1.5 text-xs"
                        disabled={followLoading}
                        onClick={() => void handleGerarFollowUp()}
                      >
                        {followLoading ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : !verificado ? <Lock className="h-3.5 w-3.5" /> : <MessageSquareText className="h-3.5 w-3.5" />}
                        gerar follow-up
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant={data.status === "Entrevista" ? "default" : "outline"}
                        className="h-7 gap-1.5 text-xs"
                        disabled={prepLoading}
                        onClick={() => void handleGerarPrep()}
                      >
                        {prepLoading ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : !verificado ? <Lock className="h-3.5 w-3.5" /> : <Mic className="h-3.5 w-3.5" />}
                        {prep ? "regerar prep entrevista" : "prep entrevista"}
                      </Button>
                    </div>
                  </div>

                  {iaErro && (
                    <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">
                      {iaErro}
                    </div>
                  )}

                  {followUp && (
                    <div className="space-y-2 rounded-lg border bg-card p-3 text-xs">
                      <div className="flex items-center justify-between gap-2">
                        <span className="font-bold">Rascunho de follow-up ({followUp.canalSugerido})</span>
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          className="h-7 gap-1 text-xs"
                          onClick={async () => {
                            const texto = followUp.assunto
                              ? `Assunto: ${followUp.assunto}\n\n${followUp.mensagem}`
                              : followUp.mensagem
                            await navigator.clipboard.writeText(texto).catch(() => {})
                            setCopiadoFollow(true)
                          }}
                        >
                          {copiadoFollow ? <Check className="h-3 w-3" /> : <Copy className="h-3 w-3" />}
                          {copiadoFollow ? "copiado" : "copiar"}
                        </Button>
                      </div>
                      {followUp.assunto && (
                        <div className="text-muted-foreground">
                          <span className="font-semibold text-foreground">Assunto:</span> {followUp.assunto}
                        </div>
                      )}
                      <Textarea
                        rows={4}
                        value={followUp.mensagem}
                        onChange={(e) => setFollowUp({ ...followUp, mensagem: e.target.value })}
                        className="text-xs"
                      />
                    </div>
                  )}

                  {compatRes && (
                    <div className="space-y-2 rounded-lg border bg-card p-3 text-xs">
                      <div className="flex items-center gap-2">
                        <Badge variant="offer" className="tabular-nums">Match {compatRes.score}/100</Badge>
                        <span className="text-muted-foreground">{compatRes.curriculoNome}</span>
                      </div>
                      <p className="text-sm">{compatRes.resumo}</p>
                      {compatRes.requisitos.length > 0 && (
                        <ul className="space-y-1">
                          {compatRes.requisitos.slice(0, 8).map((r, idx) => (
                            <li key={idx} className="flex items-start gap-1.5">
                              {r.atendido ? (
                                <CheckCircle2 className="mt-0.5 h-3.5 w-3.5 shrink-0 text-emerald-600" />
                              ) : (
                                <XCircle className="mt-0.5 h-3.5 w-3.5 shrink-0 text-destructive" />
                              )}
                              <span>
                                <strong>{r.requisito}</strong>
                                {r.gap && <span className="text-destructive"> — {r.gap}</span>}
                              </span>
                            </li>
                          ))}
                        </ul>
                      )}
                    </div>
                  )}

                  {prep && (
                    <div className="space-y-2.5 rounded-lg border bg-card p-3 text-xs">
                      <div className="flex items-center justify-between gap-2">
                        <span className="font-bold">Roteiro de Preparação para Entrevista</span>
                        <span className="text-[11px] text-muted-foreground">
                          {new Date(prep.geradoEm).toLocaleDateString("pt-BR")}
                        </span>
                      </div>
                      {prep.resumo && <p className="text-muted-foreground">{prep.resumo}</p>}
                      {prep.perguntasTecnicas.length > 0 && (
                        <div>
                          <div className="font-semibold">Perguntas técnicas prováveis</div>
                          <ul className="mt-1 list-disc space-y-0.5 pl-4">
                            {prep.perguntasTecnicas.map((p, i) => <li key={i}>{p}</li>)}
                          </ul>
                        </div>
                      )}
                      {prep.perguntasComportamentais.length > 0 && (
                        <div>
                          <div className="font-semibold">Perguntas comportamentais</div>
                          <ul className="mt-1 list-disc space-y-0.5 pl-4">
                            {prep.perguntasComportamentais.map((p, i) => <li key={i}>{p}</li>)}
                          </ul>
                        </div>
                      )}
                      {prep.pontosParaDestacar.length > 0 && (
                        <div>
                          <div className="font-semibold">Pontos fortes para destacar</div>
                          <ul className="mt-1 list-disc space-y-0.5 pl-4">
                            {prep.pontosParaDestacar.map((p, i) => <li key={i}>{p}</li>)}
                          </ul>
                        </div>
                      )}
                      {prep.perguntasParaEntrevistador.length > 0 && (
                        <div>
                          <div className="font-semibold">Boas perguntas para fazer ao entrevistador</div>
                          <ul className="mt-1 list-disc space-y-0.5 pl-4">
                            {prep.perguntasParaEntrevistador.map((p, i) => <li key={i}>{p}</li>)}
                          </ul>
                        </div>
                      )}
                    </div>
                  )}
                </div>

                <Field label="Link">
                  {safeLink ? (
                    <a
                      href={safeLink}
                      target="_blank"
                      rel="noreferrer"
                      className="break-all underline underline-offset-4 hover:opacity-80"
                    >
                      {safeLink} <ExternalLink className="inline h-3.5 w-3.5 align-[-2px]" />
                    </a>
                  ) : (
                    <span className="text-muted-foreground">sem link</span>
                  )}
                </Field>

                {data.descricaoVaga && (
                  <details className="rounded-lg border bg-muted/20 px-3 py-2 text-xs">
                    <summary className="cursor-pointer font-semibold text-muted-foreground">
                      Ver descrição da vaga salva
                    </summary>
                    <p className="mt-2 whitespace-pre-wrap break-words text-sm leading-relaxed">{data.descricaoVaga}</p>
                  </details>
                )}

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

                <div className="flex flex-wrap items-center justify-between gap-2">
                  {onDelete ? (
                    confirmDelete ? (
                      <div className="flex items-center gap-2">
                        <Button
                          type="button"
                          variant="destructive"
                          size="sm"
                          disabled={deleting}
                          onClick={() => void handleDelete()}
                          className="gap-1.5"
                        >
                          {deleting && <Loader2 className="h-3.5 w-3.5 animate-spin" />} confirmar exclusão
                        </Button>
                        <Button type="button" variant="ghost" size="sm" onClick={() => setConfirmDelete(false)} disabled={deleting}>
                          cancelar
                        </Button>
                      </div>
                    ) : (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => setConfirmDelete(true)}
                        className="gap-1.5 text-destructive hover:text-destructive"
                      >
                        <Trash2 className="h-4 w-4" /> excluir
                      </Button>
                    )
                  ) : (
                    <span />
                  )}

                  <div className="flex gap-2">
                    <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>fechar</Button>
                    <Button type="button" onClick={enterEdit} className="gap-2">
                      <Pencil className="h-4 w-4" /> editar
                    </Button>
                  </div>
                </div>
              </div>
            )
          ) : null}
        </DialogContent>
      </Dialog>

      <VerificacaoDialog open={verificacaoOpen} onOpenChange={setVerificacaoOpen} />
    </>
  )
}
