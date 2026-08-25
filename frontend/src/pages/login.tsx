import { Link, useNavigate } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { PaperSheet } from "@/components/app/paper-sheet"
import { Sparkles } from "lucide-react"

export function Login() {
  const nav = useNavigate()
  return (
    <div className="min-h-screen bg-[#fef9f0] grid place-items-center p-4">
      <div className="w-full max-w-[420px] space-y-4">
        <Link to="/" className="flex items-center justify-center gap-2">
          <div className="w-8 h-8 rounded-xl bg-primary border border-foreground grid place-items-center shadow-[2px_2px_0_var(--foreground)] rotate-[-3deg]">✦</div>
          <span className="font-extrabold">vagalume</span>
          <span className="hand text-sm">caderno de vagas</span>
        </Link>

        <PaperSheet tape="top" className="p-6 sm:p-8" withSpiral={false}>
          <h1 className="text-xl font-extrabold">entrar no seu caderno</h1>
          <p className="text-sm text-muted-foreground mt-1">Use e-mail e senha — OAuth Google vem depois.</p>

          <form
            className="mt-6 space-y-4"
            onSubmit={(e) => {
              e.preventDefault()
              nav("/app")
            }}
          >
            <div className="space-y-2">
              <Label htmlFor="email">e-mail</Label>
              <Input id="email" type="email" placeholder="voce@email.com" defaultValue="demo@vagalume.local" className="bg-white" required />
            </div>
            <div className="space-y-2">
              <Label htmlFor="pass">senha</Label>
              <Input id="pass" type="password" placeholder="••••••••" defaultValue="demo123" className="bg-white" required />
            </div>
            <Button type="submit" className="w-full gap-2">
              <Sparkles className="w-4 h-4" /> entrar
            </Button>
            <Button type="button" variant="outline" className="w-full" onClick={() => nav("/app")}>
              entrar como demo (sem senha)
            </Button>
          </form>

          <div className="mt-6 rounded-xl bg-[#fef3c7] border border-foreground p-3 text-xs">
            <span className="font-bold">Protótipo:</span> qualquer e-mail/senha entra. Dados ficam no MSW em memória.
          </div>

          <div className="mt-4 text-center text-xs text-muted-foreground">
            não tem conta? <Link to="/app" className="underline font-bold text-foreground">começar grátis</Link>
          </div>
        </PaperSheet>
      </div>
    </div>
  )
}
