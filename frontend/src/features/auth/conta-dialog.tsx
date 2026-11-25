import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Badge } from "@/components/ui/badge"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { useAuth } from "@/features/auth/auth-context"
import { PERMISSOES } from "@/features/auth/permissoes"
import { CheckCircle2, KeyRound, Loader2, LogOut, ShieldCheck, User } from "lucide-react"

export function ContaDialog({
  open,
  onOpenChange,
  onSolicitarVerificacao,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSolicitarVerificacao: () => void
}) {
  const { user, updateProfile, changePassword, logout, hasPermissao } = useAuth()
  const verificado = hasPermissao(PERMISSOES.verifyUser) || hasPermissao(PERMISSOES.admin)

  const [nome, setNome] = useState(user?.nome ?? "")
  const [salvandoNome, setSalvandoNome] = useState(false)
  const [msgNome, setMsgNome] = useState<string | null>(null)
  const [erroNome, setErroNome] = useState<string | null>(null)

  const [senhaAtual, setSenhaAtual] = useState("")
  const [novaSenha, setNovaSenha] = useState("")
  const [confirmarSenha, setConfirmarSenha] = useState("")
  const [salvandoSenha, setSalvandoSenha] = useState(false)
  const [msgSenha, setMsgSenha] = useState<string | null>(null)
  const [erroSenha, setErroSenha] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    setNome(user?.nome ?? "")
    setMsgNome(null)
    setErroNome(null)
    setSenhaAtual("")
    setNovaSenha("")
    setConfirmarSenha("")
    setMsgSenha(null)
    setErroSenha(null)
  }, [open, user?.nome])

  async function handleSalvarNome(e: React.FormEvent) {
    e.preventDefault()
    if (!nome.trim()) {
      setErroNome("Informe seu nome")
      return
    }
    setSalvandoNome(true)
    setErroNome(null)
    setMsgNome(null)
    try {
      await updateProfile(nome.trim())
      setMsgNome("Nome atualizado com sucesso")
    } catch (err) {
      setErroNome((err as Error).message)
    } finally {
      setSalvandoNome(false)
    }
  }

  async function handleAlterarSenha(e: React.FormEvent) {
    e.preventDefault()
    setErroSenha(null)
    setMsgSenha(null)
    if (novaSenha.length < 6) {
      setErroSenha("A nova senha deve ter no mínimo 6 caracteres")
      return
    }
    if (novaSenha !== confirmarSenha) {
      setErroSenha("A confirmação de senha não confere")
      return
    }
    setSalvandoSenha(true)
    try {
      await changePassword(user?.temSenha ? senhaAtual : null, novaSenha)
      setSenhaAtual("")
      setNovaSenha("")
      setConfirmarSenha("")
      setMsgSenha("Senha atualizada com sucesso")
    } catch (err) {
      setErroSenha((err as Error).message)
    } finally {
      setSalvandoSenha(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[88vh] max-w-lg overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <User className="h-4 w-4" /> Minha conta
          </DialogTitle>
          <DialogDescription>{user?.email}</DialogDescription>
        </DialogHeader>

        <div className="space-y-5">
          {/* Status da verificação */}
          <div className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-ink/20 bg-muted/30 px-3.5 py-2.5 text-xs">
            <div className="flex items-center gap-2">
              <ShieldCheck className="h-4 w-4 text-muted-foreground" />
              <span>Status da conta:</span>
              <Badge variant={verificado ? "offer" : "outline"}>
                {verificado ? "verificada" : "acesso inicial"}
              </Badge>
            </div>
            {verificado ? (
              <Link
                to="/app/curriculos?aba=config"
                onClick={() => onOpenChange(false)}
                className="font-semibold underline underline-offset-2"
              >
                Configurar provedor de IA
              </Link>
            ) : (
              <Button
                type="button"
                size="sm"
                variant="outline"
                className="h-7 text-xs"
                onClick={() => {
                  onOpenChange(false)
                  onSolicitarVerificacao()
                }}
              >
                Solicitar verificação
              </Button>
            )}
          </div>

          {/* Perfil */}
          <form onSubmit={handleSalvarNome} className="space-y-2.5">
            <div className="text-xs font-bold uppercase tracking-wide text-muted-foreground">Perfil</div>
            <div className="space-y-1">
              <Label htmlFor="conta-nome">Nome</Label>
              <div className="flex gap-2">
                <Input
                  id="conta-nome"
                  value={nome}
                  onChange={(e) => setNome(e.target.value)}
                  maxLength={150}
                  autoComplete="name"
                />
                <Button type="submit" disabled={salvandoNome}>
                  {salvandoNome ? <Loader2 className="h-4 w-4 animate-spin" /> : "Salvar"}
                </Button>
              </div>
            </div>
            {msgNome && (
              <p className="flex items-center gap-1.5 text-xs text-emerald-700 dark:text-emerald-400">
                <CheckCircle2 className="h-3.5 w-3.5" /> {msgNome}
              </p>
            )}
            {erroNome && <p className="text-xs text-destructive">{erroNome}</p>}
          </form>

          {/* Senha */}
          <form onSubmit={handleAlterarSenha} className="space-y-2.5 border-t border-ink/15 pt-4">
            <div className="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wide text-muted-foreground">
              <KeyRound className="h-3.5 w-3.5" /> {user?.temSenha ? "Alterar senha" : "Definir senha local"}
            </div>
            {user?.temSenha && (
              <div className="space-y-1">
                <Label htmlFor="conta-senha-atual">Senha atual</Label>
                <Input
                  id="conta-senha-atual"
                  type="password"
                  autoComplete="current-password"
                  value={senhaAtual}
                  onChange={(e) => setSenhaAtual(e.target.value)}
                />
              </div>
            )}
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
              <div className="space-y-1">
                <Label htmlFor="conta-nova-senha">Nova senha</Label>
                <Input
                  id="conta-nova-senha"
                  type="password"
                  autoComplete="new-password"
                  value={novaSenha}
                  onChange={(e) => setNovaSenha(e.target.value)}
                  placeholder="mínimo 6 caracteres"
                />
              </div>
              <div className="space-y-1">
                <Label htmlFor="conta-conf-senha">Confirmar nova senha</Label>
                <Input
                  id="conta-conf-senha"
                  type="password"
                  autoComplete="new-password"
                  value={confirmarSenha}
                  onChange={(e) => setConfirmarSenha(e.target.value)}
                />
              </div>
            </div>
            {msgSenha && (
              <p className="flex items-center gap-1.5 text-xs text-emerald-700 dark:text-emerald-400">
                <CheckCircle2 className="h-3.5 w-3.5" /> {msgSenha}
              </p>
            )}
            {erroSenha && <p className="text-xs text-destructive">{erroSenha}</p>}
            <Button type="submit" variant="outline" size="sm" disabled={salvandoSenha} className="gap-1.5">
              {salvandoSenha && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
              {user?.temSenha ? "Atualizar senha" : "Criar senha"}
            </Button>
          </form>

          {/* Sessões */}
          <div className="flex items-center justify-between border-t border-ink/15 pt-4">
            <div className="text-xs text-muted-foreground">Encerrar sessões em todos os dispositivos</div>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="gap-1.5 text-destructive hover:text-destructive"
              onClick={() => {
                onOpenChange(false)
                logout(true)
              }}
            >
              <LogOut className="h-3.5 w-3.5" /> sair de tudo
            </Button>
          </div>
        </div>

        <DialogFooter>
          <Button variant="ghost" onClick={() => onOpenChange(false)}>
            fechar
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
