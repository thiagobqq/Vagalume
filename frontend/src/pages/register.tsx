import { useState } from "react"
import { Link, useNavigate } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Panel } from "@/components/app/panel"
import { LogoFull } from "@/components/app/logo"
import { GoogleButton } from "@/components/auth/google-button"
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
    <div className="paper-texture grid min-h-screen place-items-center p-4">
      <div className="w-full max-w-[400px] space-y-5">
        <Link to="/" className="flex justify-center">
          <LogoFull className="max-w-[260px]" />
        </Link>

        <Panel notebook tape className="p-6 sm:p-7">
          <h1 className="text-base font-semibold tracking-tight">criar sua conta</h1>
          <p className="mt-1 text-sm text-muted-foreground">Leva 10 segundos. Depois você já anota vagas.</p>

          <form className="mt-6 space-y-4" onSubmit={onSubmit}>
            <div className="space-y-2">
              <Label htmlFor="nome">nome</Label>
              <Input
                id="nome"
                placeholder="Seu nome"
                value={nome}
                onChange={(e) => setNome(e.target.value)}
                required
                maxLength={150}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="email">e-mail</Label>
              <Input
                id="email"
                type="email"
                placeholder="voce@email.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="pass">senha</Label>
              <Input
                id="pass"
                type="password"
                placeholder="mínimo 6 caracteres"
                value={senha}
                onChange={(e) => setSenha(e.target.value)}
                required
                minLength={6}
              />
            </div>
            {err && (
              <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">
                {err}
              </div>
            )}
            <Button type="submit" className="w-full gap-2" disabled={loading}>
              {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Sparkles className="h-4 w-4" />} criar conta
            </Button>
          </form>

          <div className="relative my-5 text-center text-xs text-muted-foreground">
            <div className="absolute inset-x-0 top-1/2 h-px bg-border" />
            <span className="relative z-10 bg-card px-2">ou</span>
          </div>

          <GoogleButton onError={setErr} />

          <div className="mt-5 text-center text-xs text-muted-foreground">
            já tem conta?{" "}
            <Link to="/login" className="font-semibold text-foreground underline underline-offset-4">
              entrar
            </Link>
          </div>
        </Panel>
      </div>
    </div>
  )
}
