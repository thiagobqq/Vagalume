import { useState } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Link2, Loader2, Sparkles, Wand2 } from "lucide-react"
import type { CriarCandidaturaRequest, Origem } from "@/features/candidaturas/types"
import { ORIGEM_LABEL, ORIGEM_VALUES } from "@/features/candidaturas/types"
import { MODALIDADE_LABEL, MODALIDADES } from "@/features/vagas/types"
import { extrairVagaPorLink } from "@/features/candidaturas/api"
import { inferOrigemFromUrl, safeExternalUrl } from "@/lib/url"

type Props = {
  open: boolean
  onOpenChange: (o: boolean) => void
  onCreate: (data: CriarCandidaturaRequest) => Promise<unknown>
}

export function AddByLinkDialog({ open, onOpenChange, onCreate }: Props) {
  const [empresa, setEmpresa] = useState("")
  const [cargo, setCargo] = useState("")
  const [link, setLink] = useState("")
  const [origem, setOrigem] = useState<Origem>("Outro")
  const [origemManual, setOrigemManual] = useState(false)
  const [modalidade, setModalidade] = useState<string>("NaoInformado")
  const [faixaSalarial, setFaixaSalarial] = useState("")
  const [descricaoVaga, setDescricaoVaga] = useState("")
  const [obs, setObs] = useState("")
  const [extraindo, setExtraindo] = useState(false)
  const [infoExtracao, setInfoExtracao] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [err, setErr] = useState<string | null>(null)

  function reset() {
    setEmpresa("")
    setCargo("")
    setLink("")
    setOrigem("Outro")
    setOrigemManual(false)
    setModalidade("NaoInformado")
    setFaixaSalarial("")
    setDescricaoVaga("")
    setObs("")
    setInfoExtracao(null)
    setErr(null)
  }

  function closeDialog() {
    reset()
    onOpenChange(false)
  }

  function handleLinkChange(valor: string) {
    setLink(valor)
    setInfoExtracao(null)
    if (!origemManual) {
      const inferida = inferOrigemFromUrl(valor)
      if (inferida) setOrigem(inferida)
    }
  }

  async function handleExtrairLink() {
    const limpo = link.trim()
    if (!limpo || !safeExternalUrl(limpo)) {
      setErr("Cole uma URL HTTP/HTTPS válida antes de extrair.")
      return
    }
    setExtraindo(true)
    setErr(null)
    setInfoExtracao(null)
    try {
      const res = await extrairVagaPorLink(limpo)
      if (res.empresa) setEmpresa(res.empresa)
      if (res.cargo) setCargo(res.cargo)
      if (res.origem && (ORIGEM_VALUES as readonly string[]).includes(res.origem)) {
        setOrigem(res.origem as Origem)
      }
      if (res.modalidade && res.modalidade !== "NaoInformado") {
        setModalidade(res.modalidade)
      }
      if (res.descricao) setDescricaoVaga(res.descricao)
      setInfoExtracao(
        res.extracaoParcial
          ? "Alguns sites restringem leitura automática — complete os campos restantes abaixo."
          : "Dados preenchidos automaticamente! Revise antes de salvar."
      )
    } catch (ex) {
      setErr((ex as Error).message)
    } finally {
      setExtraindo(false)
    }
  }

  async function handleSave(e: React.FormEvent) {
    e.preventDefault()
    if (!empresa.trim() || !cargo.trim()) {
      setErr("Empresa e cargo são obrigatórios")
      return
    }
    const linkTrimmed = link.trim()
    if (linkTrimmed && !safeExternalUrl(linkTrimmed)) {
      setErr("Link deve ser uma URL HTTP ou HTTPS válida (https://...)")
      return
    }
    setSaving(true)
    setErr(null)
    try {
      await onCreate({
        empresa: empresa.trim(),
        cargo: cargo.trim(),
        link: linkTrimmed || null,
        origem,
        modalidade: modalidade !== "NaoInformado" ? modalidade : null,
        faixaSalarial: faixaSalarial.trim() || null,
        descricaoVaga: descricaoVaga.trim() || null,
        observacoes: obs.trim() || null,
      })
      closeDialog()
    } catch (ex) {
      setErr((ex as Error).message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) reset(); onOpenChange(o) }}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-[580px]" aria-describedby={undefined}>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <span className="grid h-7 w-7 place-items-center rounded-lg bg-primary text-primary-foreground">
              <Sparkles className="h-4 w-4" />
            </span>
            Nova candidatura
          </DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSave} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="add-link" className="flex items-center gap-1.5">
              <Link2 className="h-3.5 w-3.5" /> Link da vaga (opcional)
            </Label>
            <div className="flex gap-2">
              <Input
                id="add-link"
                value={link}
                onChange={(e) => handleLinkChange(e.target.value)}
                placeholder="https://www.linkedin.com/jobs/..."
                maxLength={2048}
                className="text-xs"
              />
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="shrink-0 gap-1.5"
                disabled={extraindo || !link.trim()}
                onClick={() => void handleExtrairLink()}
              >
                {extraindo ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Wand2 className="h-3.5 w-3.5" />}
                preencher
              </Button>
            </div>
            {infoExtracao && (
              <p className="text-[11px] text-muted-foreground">{infoExtracao}</p>
            )}
          </div>

          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="add-empresa">Empresa *</Label>
              <Input
                id="add-empresa"
                value={empresa}
                onChange={(e) => setEmpresa(e.target.value)}
                placeholder="Ex: Nubank"
                maxLength={200}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="add-origem">Origem *</Label>
              <Select
                value={origem}
                onValueChange={(v) => {
                  setOrigem(v as Origem)
                  setOrigemManual(true)
                }}
              >
                <SelectTrigger id="add-origem">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {ORIGEM_VALUES.map((o) => (
                    <SelectItem key={o} value={o}>{ORIGEM_LABEL[o]}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="add-cargo">Cargo *</Label>
            <Input
              id="add-cargo"
              value={cargo}
              onChange={(e) => setCargo(e.target.value)}
              placeholder="Ex: Frontend Pleno"
              maxLength={200}
              required
            />
          </div>

          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="add-modalidade">Modalidade (opcional)</Label>
              <Select value={modalidade} onValueChange={setModalidade}>
                <SelectTrigger id="add-modalidade">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="NaoInformado">Não informado</SelectItem>
                  {MODALIDADES.map((m) => (
                    <SelectItem key={m} value={m}>{MODALIDADE_LABEL[m]}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="add-salario">Faixa salarial (opcional)</Label>
              <Input
                id="add-salario"
                value={faixaSalarial}
                onChange={(e) => setFaixaSalarial(e.target.value)}
                placeholder="Ex: R$ 8k - 10k CLT"
                maxLength={120}
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="add-descricao">Descrição / requisitos da vaga (opcional — usada pela IA)</Label>
            <Textarea
              id="add-descricao"
              placeholder="Cole os requisitos principais para liberar compatibilidade e prep de entrevista..."
              value={descricaoVaga}
              onChange={(e) => setDescricaoVaga(e.target.value)}
              rows={3}
              maxLength={20000}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="add-obs">Observações pessoais (opcional)</Label>
            <Textarea
              id="add-obs"
              placeholder="Ex: indiquei via recrutadora Ana..."
              value={obs}
              onChange={(e) => setObs(e.target.value)}
              rows={2}
              maxLength={2000}
            />
          </div>

          {err && (
            <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">
              {err}
            </div>
          )}

          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" variant="outline" onClick={closeDialog} disabled={saving}>
              Cancelar
            </Button>
            <Button type="submit" disabled={saving} className="gap-2">
              {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Sparkles className="h-4 w-4" />}
              {saving ? "Salvando..." : "Salvar candidatura"}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
