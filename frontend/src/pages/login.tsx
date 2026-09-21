import { useState } from "react"
import { Link, useNavigate } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Panel } from "@/components/app/panel"
import { LogoFull } from "@/components/app/logo"
import { Sparkles, Loader2 } from "lucide-react"
import { useAuth } from "@/features/auth/auth-context"

export function Login() {
  const nav = useNavigate()
  const { login } = useAuth()
  const [email, setEmail] = useState("")
  const [senha, setSenha] = useState("")
  const [err, setErr] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErr(null)
    setLoading(true)
    try {
      await login(email.trim(), senha)
      nav("/app")
    } catch (ex) {
      setErr((ex as Error).message || "Credenciais inválidas")
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="paper-texture grid min-h-screen place-items-center bg-background p-4">
      <div className="w-full max-w-[400px] space-y-5">
        <Link to="/" className="flex justify-center">
          <LogoFull className="max-w-[260px]" />
        </Link>

        <Panel notebook tape className="p-6 sm:p-7">
          <h1 className="text-base font-semibold tracking-tight">entrar no seu caderno</h1>
          <p className="mt-1 text-sm text-muted-foreground">Use e-mail e senha cadastrados.</p>

          <form className="mt-6 space-y-4" onSubmit={onSubmit}>
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
                placeholder="••••••••"
                value={senha}
                onChange={(e) => setSenha(e.target.value)}
                required
              />
            </div>
            {err && (
              <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">
                {err}
              </div>
            )}
            <Button type="submit" className="w-full gap-2" disabled={loading}>
              {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Sparkles className="h-4 w-4" />} entrar
            </Button>
          </form>

          <div className="mt-5 text-center text-xs text-muted-foreground">
            não tem conta?{" "}
            <Link to="/register" className="font-semibold text-foreground underline underline-offset-4">
              criar conta
            </Link>
          </div>
        </Panel>
      </div>
    </div>
  )
}
