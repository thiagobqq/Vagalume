import { useState } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Sparkles, Link2, Loader2 } from "lucide-react"
import type { Origem } from "@/features/candidaturas/types"
import { ORIGEM_VALUES } from "@/features/candidaturas/types"

type Props = {
  open: boolean
  onOpenChange: (o: boolean) => void
  onCreate: (data: { empresa: string; cargo: string; link?: string | null; origem: Origem; observacoes?: string | null }) => Promise<unknown>
}

export function AddByLinkDialog({ open, onOpenChange, onCreate }: Props) {
  const [empresa, setEmpresa] = useState("")
  const [cargo, setCargo] = useState("")
  const [link, setLink] = useState("")
  const [origem, setOrigem] = useState<Origem>("Outro")
  const [obs, setObs] = useState("")
  const [saving, setSaving] = useState(false)
  const [err, setErr] = useState<string | null>(null)

  function reset() {
    setEmpresa("")
    setCargo("")
    setLink("")
    setOrigem("Outro")
    setObs("")
    setErr(null)
  }

  async function handleSave() {
    if (!empresa.trim() || !cargo.trim()) {
      setErr("Empresa e cargo são obrigatórios")
      return
    }
    if (link && (() => { try { new URL(link); return false } catch { return true } })()) {
      setErr("Link deve ser uma URL válida (https://...)")
      return
    }
    setSaving(true)
    setErr(null)
    try {
      await onCreate({
        empresa: empresa.trim(),
        cargo: cargo.trim(),
        link: link.trim() || null,
        origem,
        observacoes: obs.trim() || null,
      })
      reset()
      onOpenChange(false)
    } catch (e) {
      setErr((e as Error).message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) reset(); onOpenChange(o) }}>
      <DialogContent className="sm:max-w-[560px]" aria-describedby={undefined}>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <span className="grid h-7 w-7 place-items-center rounded-lg bg-primary text-primary-foreground">
              <Sparkles className="h-4 w-4" />
            </span>
            Nova candidatura
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label>Empresa *</Label>
              <Input value={empresa} onChange={(e) => setEmpresa(e.target.value)} placeholder="Ex: Nubank" maxLength={200} />
            </div>
            <div className="space-y-2">
              <Label>Origem *</Label>
              <Select value={origem} onValueChange={(v) => setOrigem(v as Origem)}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {ORIGEM_VALUES.map((o) => (
                    <SelectItem key={o} value={o}>{o}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="space-y-2">
            <Label>Cargo *</Label>
            <Input value={cargo} onChange={(e) => setCargo(e.target.value)} placeholder="Ex: Frontend Pleno" maxLength={200} />
          </div>

          <div className="space-y-2">
            <Label className="flex items-center gap-1.5"><Link2 className="h-3.5 w-3.5" /> Link (opcional)</Label>
            <Input value={link} onChange={(e) => setLink(e.target.value)} placeholder="https://..." className="text-xs" />
          </div>

          <div className="space-y-2">
            <Label>Observações (opcional)</Label>
            <Textarea placeholder="Ex: indiquei via recrutadora Ana..." value={obs} onChange={(e) => setObs(e.target.value)} rows={2} maxLength={2000} />
          </div>

          {err && (
            <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">
              {err}
            </div>
          )}

          <div className="flex justify-end gap-2 pt-1">
            <Button variant="outline" onClick={() => onOpenChange(false)} disabled={saving}>Cancelar</Button>
            <Button onClick={handleSave} disabled={saving} className="gap-2">
              {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Sparkles className="h-4 w-4" />}
              {saving ? "Salvando..." : "Salvar candidatura"}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
