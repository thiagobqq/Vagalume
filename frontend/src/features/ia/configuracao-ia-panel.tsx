import { useEffect, useMemo, useState } from "react"
import { NotebookPanel } from "@/components/app/notebook-panel"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { CheckCircle2, KeyRound, Loader2, ShieldAlert, Trash2, XCircle } from "lucide-react"
import { listarProvedoresIa, obterConfiguracaoIa, removerConfiguracaoIa, salvarConfiguracaoIa, testarIa } from "./api"
import type { ConfiguracaoIaDto, ProvedorIaDto, TestarIaResponse } from "./types"

export function ConfiguracaoIaPanel() {
  const [provedores, setProvedores] = useState<ProvedorIaDto[]>([])
  const [config, setConfig] = useState<ConfiguracaoIaDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState<string | null>(null)

  const [provedor, setProvedor] = useState("")
  const [modelo, setModelo] = useState("")
  const [apiKey, setApiKey] = useState("")
  const [ativo, setAtivo] = useState(true)

  const [salvando, setSalvando] = useState(false)
  const [testando, setTestando] = useState(false)
  const [removendo, setRemovendo] = useState(false)
  const [msg, setMsg] = useState<string | null>(null)
  const [teste, setTeste] = useState<TestarIaResponse | null>(null)

  useEffect(() => {
    let ativoReq = true
    ;(async () => {
      setLoading(true)
      setErro(null)
      try {
        const [lista, cfg] = await Promise.all([listarProvedoresIa(), obterConfiguracaoIa()])
        if (!ativoReq) return
        setProvedores(lista)
        setConfig(cfg)
        if (cfg) {
          setProvedor(cfg.provedor)
          setModelo(cfg.modelo)
          setAtivo(cfg.ativo)
        } else if (lista.length > 0) {
          setProvedor(lista[0].valor)
          setModelo(lista[0].modeloPadrao ?? "")
        }
      } catch (e) {
        if (ativoReq) setErro((e as Error).message)
      } finally {
        if (ativoReq) setLoading(false)
      }
    })()
    return () => {
      ativoReq = false
    }
  }, [])

  const provedorAtual = useMemo(
    () => provedores.find((p) => p.valor === provedor) ?? null,
    [provedores, provedor]
  )

  const mesmoProvedorSalvo = !!config && config.provedor === provedor
  const temChaveSalvaNesteProvedor = mesmoProvedorSalvo && config.temChave

  const trocarProvedor = (valor: string) => {
    setProvedor(valor)
    setTeste(null)
    setErro(null)
    setMsg(null)
    setApiKey("")
    const preset = provedores.find((p) => p.valor === valor)
    if (preset?.modeloPadrao) setModelo(preset.modeloPadrao)
  }

  const salvar = async () => {
    if (!provedor) return
    if (!modelo.trim()) {
      setErro("Informe o modelo do provedor")
      return
    }
    if (!temChaveSalvaNesteProvedor && !apiKey.trim() && provedorAtual?.requerChave) {
      setErro("Informe a API key do provedor selecionado")
      return
    }
    setSalvando(true)
    setMsg(null)
    setErro(null)
    try {
      const salvo = await salvarConfiguracaoIa({
        provedor,
        modelo: modelo.trim(),
        apiKey: apiKey.trim() || null,
        ativo,
      })
      setConfig(salvo)
      setApiKey("")
      setMsg("Configuração de IA salva")
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setSalvando(false)
    }
  }

  const testar = async () => {
    if (!provedor || !modelo.trim()) {
      setErro("Informe provedor e modelo para testar")
      return
    }
    if (!temChaveSalvaNesteProvedor && !apiKey.trim() && provedorAtual?.requerChave) {
      setErro("Informe a API key deste provedor para testar a conexão")
      return
    }
    setTestando(true)
    setErro(null)
    setTeste(null)
    try {
      const resultado = await testarIa({
        provedor,
        modelo: modelo.trim(),
        apiKey: apiKey.trim() || null,
      })
      setTeste(resultado)
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setTestando(false)
    }
  }

  const remover = async () => {
    if (!config) return
    setRemovendo(true)
    setErro(null)
    setMsg(null)
    setTeste(null)
    try {
      await removerConfiguracaoIa()
      setConfig(null)
      setApiKey("")
      setAtivo(true)
      setMsg("Configuração e API key removidas do servidor")
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setRemovendo(false)
    }
  }

  if (loading) {
    return (
      <div className="grid place-items-center gap-2 py-12 text-sm text-muted-foreground">
        <Loader2 className="h-5 w-5 animate-spin" /> carregando configuração de IA...
      </div>
    )
  }

  const badgeVariant = !config
    ? "outline"
    : !config.ativo
      ? "secondary"
      : config.temChave || provedorAtual?.requerChave === false
        ? "offer"
        : "outline"

  const badgeLabel = !config
    ? "não configurado"
    : !config.ativo
      ? "pausada"
      : config.temChave || provedorAtual?.requerChave === false
        ? "configurado"
        : "sem chave"

  return (
    <div className="space-y-5">
      {erro && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">{erro}</div>
      )}

      <NotebookPanel
        title={
          <span className="flex items-center gap-2">
            <KeyRound className="h-4 w-4" /> Provedor de IA
          </span>
        }
        badge={<Badge variant={badgeVariant}>{badgeLabel}</Badge>}
      >
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div className="space-y-1 text-xs font-medium">
            Provedor
            <Select value={provedor} onValueChange={trocarProvedor}>
              <SelectTrigger>
                <SelectValue placeholder="Selecione o provedor" />
              </SelectTrigger>
              <SelectContent>
                {provedores.map((p) => (
                  <SelectItem key={p.valor} value={p.valor}>
                    {p.nome}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <label className="space-y-1 text-xs font-medium">
            Modelo
            <Input
              value={modelo}
              onChange={(e) => setModelo(e.target.value)}
              placeholder={provedorAtual?.modeloPadrao ?? "ex.: gpt-4o-mini"}
            />
          </label>
        </div>

        {provedorAtual && provedorAtual.modelosSugeridos.length > 0 && (
          <div className="mt-2 flex flex-wrap items-center gap-1.5">
            <span className="text-[11px] text-muted-foreground">sugestões:</span>
            {provedorAtual.modelosSugeridos.map((m) => (
              <Button
                key={m}
                type="button"
                size="sm"
                variant={modelo === m ? "secondary" : "outline"}
                className={
                  modelo === m
                    ? "h-6 px-2 text-[11px] ring-1 ring-ink/30"
                    : "h-6 px-2 text-[11px]"
                }
                onClick={() => setModelo(m)}
              >
                {m}
              </Button>
            ))}
          </div>
        )}

        <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <label className="space-y-1 text-xs font-medium">
            API key {provedorAtual?.requerChave === false && <span className="text-muted-foreground">(opcional)</span>}
            <Input
              type="password"
              autoComplete="off"
              value={apiKey}
              onChange={(e) => setApiKey(e.target.value)}
              placeholder={
                temChaveSalvaNesteProvedor
                  ? `salva: ${config.chaveMascarada} — deixe vazio para manter`
                  : "cole a API key deste provedor"
              }
            />
          </label>
          <div className="flex items-end">
            <Button
              type="button"
              variant={ativo ? "secondary" : "outline"}
              size="sm"
              className="gap-1.5"
              onClick={() => setAtivo((v) => !v)}
            >
              {ativo && <span className="h-1.5 w-1.5 rounded-full bg-[var(--st-offer)]" aria-hidden="true" />}
              {ativo ? "IA ativa" : "IA pausada"}
            </Button>
          </div>
        </div>

        {msg && <div className="mt-3 rounded-lg border bg-muted/40 px-3 py-2 text-xs">{msg}</div>}

        {teste && (
          <div
            className={
              teste.ok
                ? "mt-3 flex items-center gap-2 rounded-lg border border-emerald-500/30 bg-emerald-500/5 px-3 py-2 text-xs text-emerald-700 dark:text-emerald-400"
                : "mt-3 flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive"
            }
          >
            {teste.ok ? <CheckCircle2 className="h-4 w-4" /> : <XCircle className="h-4 w-4" />}
            {teste.mensagem}
          </div>
        )}

        <div className="mt-4 flex flex-wrap items-center justify-between gap-2">
          {config ? (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={remover}
              disabled={removendo || salvando || testando}
              className="gap-1.5 text-destructive hover:text-destructive"
            >
              {removendo ? <Loader2 className="h-4 w-4 animate-spin" /> : <Trash2 className="h-4 w-4" />}
              remover chave
            </Button>
          ) : (
            <span />
          )}
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" onClick={testar} disabled={testando || salvando || removendo} className="gap-1.5">
              {testando && <Loader2 className="h-4 w-4 animate-spin" />} testar conexão
            </Button>
            <Button onClick={salvar} disabled={salvando || testando || removendo} className="gap-1.5">
              {salvando && <Loader2 className="h-4 w-4 animate-spin" />} salvar
            </Button>
          </div>
        </div>
      </NotebookPanel>

      <div className="flex items-start gap-2 rounded-lg border bg-muted/30 px-3 py-2 text-[11px] text-muted-foreground">
        <ShieldAlert className="mt-0.5 h-3.5 w-3.5 shrink-0" />
        <span>
          Ao gerar uma análise, o texto do seu currículo ativo e a descrição da vaga são enviados ao provedor
          escolhido por você. Use um provedor no qual você confia e verifique os custos da sua conta.
        </span>
      </div>
    </div>
  )
}
