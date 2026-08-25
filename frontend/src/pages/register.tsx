import { useState } from "react"
import { Link, useNavigate } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { PaperSheet } from "@/components/app/paper-sheet"
import { Sparkles, Loader2 } from "lucide-react"
import { useAuth } from "@/features/auth/auth-context"

export function Register() {
  const nav = useNavigate()
  const { register } = useAuth()
  const [nome, setNome] = useState("")
  const [email, setEmail] = useState("")
  const [senha, setSenha] = useState("")
  const [err, setErr] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErr(null)
    if (senha.length < 6) {
      setErr("Senha deve ter no mínimo 6 caracteres")
      return
    }
    setLoading(true)
    try {
      await register(nome.trim(), email.trim(), senha)
      nav("/app")
    } catch (ex) {
      setErr((ex as Error).message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-[#fef9f0] grid place-items-center p-4">
      <div className="w-full max-w-[420px] space-y-4">
        <Link to="/" className="flex items-center justify-center gap-2">
          <div className="w-8 h-8 rounded-xl bg-primary border border-foreground grid place-items-center shadow-[2px_2px_0_var(--foreground)] rotate-[-3deg]">✦</div>
          <span className="font-extrabold">vagalume</span>
          <span className="hand text-sm">caderno de vagas</span>
        </Link>

        <PaperSheet tape="top" className="p-6 sm:p-8" withSpiral={false}>
          <h1 className="text-xl font-extrabold">criar sua conta</h1>
          <p className="text-sm text-muted-foreground mt-1">Leva 10 segundos. Depois você já anota vagas.</p>

          <form className="mt-6 space-y-4" onSubmit={onSubmit}>
            <div className="space-y-2">
              <Label htmlFor="nome">nome</Label>
              <Input id="nome" placeholder="Seu nome" value={nome} onChange={(e) => setNome(e.target.value)} className="bg-white" required maxLength={150} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="email">e-mail</Label>
              <Input id="email" type="email" placeholder="voce@email.com" value={email} onChange={(e) => setEmail(e.target.value)} className="bg-white" required />
            </div>
            <div className="space-y-2">
              <Label htmlFor="pass">senha</Label>
              <Input id="pass" type="password" placeholder="mínimo 6 caracteres" value={senha} onChange={(e) => setSenha(e.target.value)} className="bg-white" required minLength={6} />
            </div>
            {err && <div className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700">{err}</div>}
            <Button type="submit" className="w-full gap-2" disabled={loading}>
              {loading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Sparkles className="w-4 h-4" />} criar conta
            </Button>
          </form>

          <div className="mt-4 text-center text-xs text-muted-foreground">
            já tem conta?{" "}
            <Link to="/login" className="underline font-bold text-foreground">
              entrar
            </Link>
          </div>
        </PaperSheet>
      </div>
    </div>
  )
}
