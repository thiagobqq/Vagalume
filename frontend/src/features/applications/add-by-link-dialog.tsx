import { useState } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Sparkles, Link2, Wand2, Loader2 } from "lucide-react"
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
      <DialogContent className="sm:max-w-[560px] bg-[#fef9f0]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-xl">
            <span className="w-8 h-8 rounded-lg bg-primary border border-foreground grid place-items-center">
              <Wand2 className="w-4 h-4" />
            </span>
            Nova candidatura
            <span className="hand font-normal text-sm text-muted-foreground rotate-[-2deg]">seu caderno</span>
          </DialogTitle>
          <DialogDescription>Preencha os dados da vaga. Origem é obrigatória (backend).</DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label>Empresa *</Label>
              <Input value={empresa} onChange={(e) => setEmpresa(e.target.value)} placeholder="Ex: Nubank" className="bg-white" maxLength={200} />
            </div>
            <div className="space-y-1.5">
              <Label>Origem *</Label>
              <Select value={origem} onValueChange={(v) => setOrigem(v as Origem)}>
                <SelectTrigger className="bg-white">
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

          <div className="space-y-1.5">
            <Label>Cargo *</Label>
            <Input value={cargo} onChange={(e) => setCargo(e.target.value)} placeholder="Ex: Frontend Pleno" className="bg-white" maxLength={200} />
          </div>

          <div className="space-y-1.5">
            <Label className="flex items-center gap-1.5"><Link2 className="w-3.5 h-3.5" /> Link (opcional)</Label>
            <Input value={link} onChange={(e) => setLink(e.target.value)} placeholder="https://..." className="bg-white text-xs" />
          </div>

          <div className="space-y-1.5">
            <Label>Observações (opcional)</Label>
            <Textarea placeholder="Ex: indiquei via recrutadora Ana..." value={obs} onChange={(e) => setObs(e.target.value)} className="bg-white" rows={2} maxLength={2000} />
          </div>

          {err && <div className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700">{err}</div>}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" onClick={() => onOpenChange(false)} disabled={saving}>Cancelar</Button>
            <Button onClick={handleSave} disabled={saving} className="gap-2">
              {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Sparkles className="w-4 h-4" />}
              {saving ? "Salvando..." : "Salvar no caderno ✦"}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
