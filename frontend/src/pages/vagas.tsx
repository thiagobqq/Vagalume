import { useEffect, useMemo, useState } from "react"
import { PageHeader } from "@/components/app/page-header"
import { Panel } from "@/components/app/panel"
import { NotebookCard, NotebookPanel } from "@/components/app/notebook-panel"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { cn } from "@/lib/utils"
import { obterConexaoMcp } from "@/features/vagas/api"
import { useVagas } from "@/hooks/use-vagas"
import { useBuscaConfig } from "@/hooks/use-busca"
import { useAplicacaoPendente } from "@/hooks/use-aplicacao-pendente"
import { CompatibilidadeDialog } from "@/features/vagas/compatibilidade-dialog"
import { ConfirmarAplicacaoDialog } from "@/features/vagas/confirmar-aplicacao-dialog"
import { VerificacaoDialog } from "@/components/app/verificacao-dialog"
import { useAuth } from "@/features/auth/auth-context"
import { PERMISSOES } from "@/features/auth/permissoes"
import {
  DIAS_LABEL,
  FONTE_HINT,
  FONTE_LABEL,
  FONTE_TIPOS,
  MODALIDADE_LABEL,
  MODALIDADES,
  NIVEL_LABEL,
  NIVEIS,
  type ExecucaoBuscaDto,
  type SalvarPerfilBuscaRequest,
  type VagaDto,
} from "@/features/vagas/types"
import {
  Building2,
  Check,
  ExternalLink,
  Inbox,
  Loader2,
  Lock,
  MapPin,
  Plus,
  RefreshCw,
  Search,
  Trash2,
  Wand2,
  X,
  Zap,
} from "lucide-react"

function parseLista(valor: string): string[] {
  return valor
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean)
}

function fonteRestrita(tipo: string): boolean {
  return tipo !== "Trampos" && tipo !== "Agente"
}

const FONTE_COLOR: Record<string, string> = {
  Agente: "var(--crayon-purple)",
  Gupy: "var(--crayon-blue)",
  Trampos: "var(--crayon-green)",
  Programathor: "var(--crayon-purple)",
  InfoJobs: "var(--crayon-orange)",
  Catho: "var(--crayon-pink)",
  LinkedIn: "var(--crayon-yellow)",
  Rss: "var(--crayon-blue)",
}

function Metricas({ execucao }: { execucao: ExecucaoBuscaDto | null }) {
  const itens = [
    { label: "Encontradas", valor: execucao?.encontradas ?? 0, color: "var(--crayon-blue)" },
    { label: "Aprovadas", valor: execucao?.aprovadas ?? 0, color: "var(--crayon-green)" },
    { label: "Duplicadas", valor: execucao?.duplicadas ?? 0, color: "var(--crayon-orange)" },
    { label: "Novas", valor: execucao?.adicionadas ?? 0, color: "var(--crayon-purple)" },
  ]
  return (
    <NotebookPanel
      title="Última busca"
      badge={
        execucao?.finalizadaEm ? (
          <span className="text-xs text-muted-foreground">
            {new Date(execucao.finalizadaEm).toLocaleString("pt-BR")}
          </span>
        ) : undefined
      }
    >
      <div className="flex flex-wrap items-center gap-x-6 gap-y-2">
        {itens.map((i, idx) => (
          <div
            key={i.label}
            className={cn(
              "flex items-center gap-2",
              idx > 0 && "sm:border-l sm:border-ink/10 sm:pl-6"
            )}
          >
            <span
              className="h-1.5 w-1.5 shrink-0 rounded-full"
              style={{ background: `color-mix(in srgb, ${i.color} 55%, var(--muted-foreground))` }}
              aria-hidden="true"
            />
            <span className="text-sm font-bold tabular-nums">{i.valor}</span>
            <span className="text-[11px] uppercase tracking-wide text-muted-foreground">{i.label}</span>
          </div>
        ))}
      </div>
      {execucao?.detalhesPorFonte?.some((d) => !d.coletando && !d.aguardandoAgente && !d.sucesso) && (
        <div className="mt-3 space-y-1">
          {execucao.detalhesPorFonte.filter((d) => !d.coletando && !d.aguardandoAgente && !d.sucesso).map((d) => (
            <div key={d.fonteNome} className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">
              {d.fonteNome}: {d.erro ?? "falha na coleta"}
            </div>
          ))}
        </div>
      )}
    </NotebookPanel>
  )
}

function VagaCard({ vaga, onAplicar, onDescartar, onCompatibilidade, compatBloqueada }: {
  vaga: VagaDto
  onAplicar: () => void
  onDescartar: () => void
  onCompatibilidade: () => void
  compatBloqueada?: boolean
}) {
  const [descartando, setDescartando] = useState(false)

  return (
    <NotebookCard
      headerColor={FONTE_COLOR[vaga.fonteTipo] ?? "var(--crayon-blue)"}
      ruled
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <a
            href={vaga.url}
            target="_blank"
            rel="noreferrer"
            className="line-clamp-2 text-sm font-semibold hover:underline"
          >
            {vaga.titulo}
          </a>
          <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground">
            <span className="inline-flex items-center gap-1">
              <Building2 className="h-3 w-3" /> {vaga.empresa}
            </span>
            {vaga.localizacao && (
              <span className="inline-flex items-center gap-1">
                <MapPin className="h-3 w-3" /> {vaga.localizacao}
              </span>
            )}
            <span className="text-muted-foreground/80">{vaga.fonteNome}</span>
          </div>
        </div>
        <a href={vaga.url} target="_blank" rel="noreferrer" className="shrink-0 text-muted-foreground hover:text-foreground">
          <ExternalLink className="h-4 w-4" />
        </a>
      </div>

      <div className="mt-3 flex flex-wrap items-center gap-1.5">
        <Badge variant="secondary">{MODALIDADE_LABEL[vaga.modalidade] ?? vaga.modalidade}</Badge>
        {vaga.nivel !== "NaoInformado" && <Badge variant="outline">{NIVEL_LABEL[vaga.nivel] ?? vaga.nivel}</Badge>}
        <Badge variant="outline">{vaga.fonteTipo}</Badge>
      </div>

      <div className="mt-3 flex items-center gap-2">
        <Button
          variant="outline"
          size="sm"
          className="gap-1.5"
          disabled={descartando}
          onClick={onAplicar}
        >
          <Check className="h-3.5 w-3.5" /> aplicar
        </Button>
        <Button
          variant="ghost"
          size="sm"
          className="gap-1.5 text-muted-foreground hover:text-foreground"
          disabled={descartando}
          onClick={async () => {
            setDescartando(true)
            try {
              onDescartar()
            } finally {
              setDescartando(false)
            }
          }}
        >
          {descartando ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <X className="h-3.5 w-3.5" />} descartar
        </Button>
        <Button
          variant="outline"
          size="sm"
          className="ml-auto gap-1.5"
          disabled={descartando}
          onClick={onCompatibilidade}
        >
          {compatBloqueada ? <Lock className="h-3.5 w-3.5" /> : <Wand2 className="h-3.5 w-3.5" />} compatibilidade
        </Button>
      </div>
    </NotebookCard>
  )
}

function InboxTab() {
  const { vagas, loading, erro, executando, execucaoAtual, ultima, refresh, buscarAgora, aplicar, descartar } = useVagas()
  const { hasPermissao } = useAuth()
  const verificado = hasPermissao(PERMISSOES.verifyUser)
  const [q, setQ] = useState("")
  const [compat, setCompat] = useState<VagaDto | null>(null)
  const [verificacaoOpen, setVerificacaoOpen] = useState(false)

  const { pendente, iniciar, limpar } = useAplicacaoPendente()

  // abre a vaga em outra aba e agenda a confirmação ("você aplicou?")
  function abrirParaAplicar(v: VagaDto) {
    iniciar(v)
    const win = window.open(v.url, "_blank")
    if (win) win.opener = null
  }

  async function confirmarAplicacao() {
    if (!pendente) return
    await aplicar(pendente.id)
    limpar()
  }

  const aguardandoAgente = execucaoAtual?.detalhesPorFonte.some(d => d.aguardandoAgente)
  const filtradas = useMemo(() => {
    if (!q) return vagas
    const s = q.toLowerCase()
    return vagas.filter((v) => v.titulo.toLowerCase().includes(s) || v.empresa.toLowerCase().includes(s))
  }, [vagas, q])

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        <Button
          className="gap-1.5"
          disabled={executando}
          onClick={() => {
            void buscarAgora().catch(() => {
              /* erro já exibido pelo hook */
            })
          }}
        >
          {executando ? <Loader2 className="h-4 w-4 animate-spin" /> : <Zap className="h-4 w-4" />}
          {aguardandoAgente ? "aguardando agente…" : executando ? "buscando..." : "buscar agora"}
        </Button>
        <Button variant="outline" className="gap-1.5 text-muted-foreground hover:text-foreground" onClick={refresh} disabled={executando}>
          <RefreshCw className="h-4 w-4" /> atualizar
        </Button>
      </div>

      {executando && (
        <div className="rounded-lg border bg-muted/40 px-3 py-2 text-xs text-muted-foreground">
          {aguardandoAgente
            ? "O agente tem até 30 minutos a partir do pedido para devolver as vagas. Você pode fechar esta página."
            : "Buscando vagas… A lista e as métricas atualizam sozinhas."}
        </div>
      )}
      {!executando && ultima?.sucesso === false && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">
          {ultima.erro ?? "Uma ou mais fontes falharam. Consulte os detalhes abaixo."}
        </div>
      )}

      <Metricas execucao={execucaoAtual ?? ultima} />

      <div className="relative max-w-sm">
        <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input placeholder="Buscar por título ou empresa..." value={q} onChange={(e) => setQ(e.target.value)} className="pl-9" />
      </div>

      {erro && (
        <div className="flex items-center justify-between gap-3 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          <span>{erro}</span>
          <Button size="sm" variant="outline" onClick={refresh}>tentar novamente</Button>
        </div>
      )}

      {loading ? (
        <div className="grid place-items-center gap-2 py-12 text-sm text-muted-foreground">
          <Loader2 className="h-5 w-5 animate-spin" /> carregando vagas...
        </div>
      ) : filtradas.length === 0 ? (
        <NotebookPanel>
          <div className="py-8 text-center">
            <Inbox className="mx-auto h-6 w-6 text-muted-foreground" />
            <p className="mt-2 text-sm font-medium">nenhuma vaga nova</p>
            <p className="text-xs text-muted-foreground">
              configure suas fontes e critérios na aba Configuração e clique em "buscar agora".
            </p>
          </div>
        </NotebookPanel>
      ) : (
        <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
          {filtradas.map((v) => (
            <VagaCard
              key={v.id}
              vaga={v}
              onAplicar={() => abrirParaAplicar(v)}
              onDescartar={() => void descartar(v.id)}
              onCompatibilidade={() => {
                if (!verificado) {
                  setVerificacaoOpen(true)
                  return
                }
                setCompat(v)
              }}
              compatBloqueada={!verificado}
            />
          ))}
        </div>
      )}

      <ConfirmarAplicacaoDialog
        pendente={pendente}
        onConfirm={confirmarAplicacao}
        onCancel={limpar}
      />

      <CompatibilidadeDialog
        vaga={compat}
        open={!!compat}
        onOpenChange={(open) => {
          if (!open) setCompat(null)
        }}
      />

      <VerificacaoDialog open={verificacaoOpen} onOpenChange={setVerificacaoOpen} />
    </div>
  )
}

function ConfigTab() {
  const { perfil, fontes, loading, erro, salvarPerfil, adicionarFonte, alternarFonte, removerFonte } = useBuscaConfig()

  const [palavras, setPalavras] = useState("")
  const [tecnologias, setTecnologias] = useState("")
  const [excluir, setExcluir] = useState("")
  const [form, setForm] = useState<SalvarPerfilBuscaRequest>({
    ativo: true,
    palavrasChave: [],
    tecnologias: [],
    niveis: [],
    modalidades: [],
    excluirPalavras: [],
    localizacao: null,
    combina: "Qualquer",
    horaExecucao: "08:00",
    diasSemana: "0123456",
    fusoHorario: "America/Sao_Paulo",
    discordWebhookUrl: null,
  })
  const [salvando, setSalvando] = useState(false)
  const [msg, setMsg] = useState<string | null>(null)
  const [erroForm, setErroForm] = useState<string | null>(null)

  useEffect(() => {
    if (!perfil) return
    setPalavras(perfil.palavrasChave.join(", "))
    setTecnologias(perfil.tecnologias.join(", "))
    setExcluir(perfil.excluirPalavras.join(", "))
    setForm({
      ativo: perfil.ativo,
      palavrasChave: perfil.palavrasChave,
      tecnologias: perfil.tecnologias,
      niveis: perfil.niveis,
      modalidades: perfil.modalidades,
      excluirPalavras: perfil.excluirPalavras,
      localizacao: perfil.localizacao,
      combina: perfil.combina,
      horaExecucao: perfil.horaExecucao,
      diasSemana: perfil.diasSemana,
      fusoHorario: perfil.fusoHorario,
      discordWebhookUrl: perfil.discordWebhookUrl,
    })
  }, [perfil])

  const toggleArray = (campo: "niveis" | "modalidades", valor: string) => {
    setForm((f) => {
      const atual = f[campo]
      const novo = atual.includes(valor) ? atual.filter((x) => x !== valor) : [...atual, valor]
      return { ...f, [campo]: novo }
    })
  }

  const toggleDia = (dia: number) => {
    setForm((f) => {
      const ch = String(dia)
      const novo = f.diasSemana.includes(ch)
        ? f.diasSemana.replace(ch, "")
        : (f.diasSemana + ch).split("").sort().join("")
      return { ...f, diasSemana: novo }
    })
  }

  const salvar = async () => {
    setSalvando(true)
    setMsg(null)
    setErroForm(null)
    try {
      await salvarPerfil({
        ...form,
        palavrasChave: parseLista(palavras),
        tecnologias: parseLista(tecnologias),
        excluirPalavras: parseLista(excluir),
      })
      setMsg("Configuração salva")
    } catch (e) {
      setErroForm((e as Error).message)
    } finally {
      setSalvando(false)
    }
  }

  if (loading) {
    return (
      <div className="grid place-items-center gap-2 py-12 text-sm text-muted-foreground">
        <Loader2 className="h-5 w-5 animate-spin" /> carregando configuração...
      </div>
    )
  }

  return (
    <div className="space-y-5">
      {erro && <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">{erro}</div>}

      <Panel className="p-5">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-sm font-semibold">Critérios</h3>
            <p className="text-xs text-muted-foreground">Vagas que não casam são descartadas automaticamente.</p>
          </div>
          <Button
            variant={form.ativo ? "default" : "outline"}
            size="sm"
            onClick={() => setForm((f) => ({ ...f, ativo: !f.ativo }))}
          >
            {form.ativo ? "busca ativa" : "busca pausada"}
          </Button>
        </div>

        <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <label className="space-y-1 text-xs font-medium">
            Palavras-chave (separe por vírgula)
            <Input value={palavras} onChange={(e) => setPalavras(e.target.value)} placeholder="Backend, Full Stack, Júnior" />
          </label>
          <label className="space-y-1 text-xs font-medium">
            Tecnologias (separe por vírgula)
            <Input value={tecnologias} onChange={(e) => setTecnologias(e.target.value)} placeholder=".NET, C#, React, SQL Server" />
          </label>
          <label className="space-y-1 text-xs font-medium">
            Excluir (separe por vírgula)
            <Input value={excluir} onChange={(e) => setExcluir(e.target.value)} placeholder="vendas, estágio" />
          </label>
          <label className="space-y-1 text-xs font-medium">
            Localização
            <Input
              value={form.localizacao ?? ""}
              onChange={(e) => setForm((f) => ({ ...f, localizacao: e.target.value || null }))}
              placeholder="Brasil, São Paulo"
            />
          </label>
        </div>

        <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div className="space-y-1.5">
            <div className="text-xs font-medium">Nível</div>
            <div className="flex flex-wrap gap-1.5">
              {NIVEIS.map((n) => (
                <Button
                  key={n}
                  type="button"
                  size="sm"
                  variant={form.niveis.includes(n) ? "default" : "outline"}
                  className="h-7 text-xs"
                  onClick={() => toggleArray("niveis", n)}
                >
                  {NIVEL_LABEL[n] ?? n}
                </Button>
              ))}
            </div>
          </div>
          <div className="space-y-1.5">
            <div className="text-xs font-medium">Modalidade</div>
            <div className="flex flex-wrap gap-1.5">
              {MODALIDADES.map((m) => (
                <Button
                  key={m}
                  type="button"
                  size="sm"
                  variant={form.modalidades.includes(m) ? "default" : "outline"}
                  className="h-7 text-xs"
                  onClick={() => toggleArray("modalidades", m)}
                >
                  {MODALIDADE_LABEL[m] ?? m}
                </Button>
              ))}
            </div>
          </div>
        </div>

        <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-3">
          <div className="space-y-1 text-xs font-medium">
            Combinar termos
            <Select value={form.combina} onValueChange={(v) => setForm((f) => ({ ...f, combina: v }))}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Qualquer">Qualquer termo (OR)</SelectItem>
                <SelectItem value="Todos">Todos os termos (AND)</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <label className="space-y-1 text-xs font-medium">
            Hora da busca
            <Input
              type="time"
              value={form.horaExecucao}
              onChange={(e) => setForm((f) => ({ ...f, horaExecucao: e.target.value }))}
            />
          </label>
          <label className="space-y-1 text-xs font-medium">
            Webhook do Discord (opcional)
            <Input
              value={form.discordWebhookUrl ?? ""}
              onChange={(e) => setForm((f) => ({ ...f, discordWebhookUrl: e.target.value || null }))}
              placeholder="https://discord.com/api/webhooks/..."
            />
          </label>
        </div>

        <div className="mt-4 space-y-1.5">
          <div className="text-xs font-medium">Dias da semana</div>
          <div className="flex flex-wrap gap-1.5">
            {DIAS_LABEL.map((d, i) => (
              <Button
                key={d}
                type="button"
                size="sm"
                variant={form.diasSemana.includes(String(i)) ? "default" : "outline"}
                className="h-7 w-12 text-xs"
                onClick={() => toggleDia(i)}
              >
                {d}
              </Button>
            ))}
          </div>
        </div>

        {msg && <div className="mt-3 rounded-lg border bg-muted/40 px-3 py-2 text-xs">{msg}</div>}
        {erroForm && <div className="mt-3 rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">{erroForm}</div>}

        <div className="mt-4 flex justify-end">
          <Button onClick={salvar} disabled={salvando} className="gap-1.5">
            {salvando && <Loader2 className="h-4 w-4 animate-spin" />} salvar configuração
          </Button>
        </div>
      </Panel>

      <FontesPanel
        fontes={fontes}
        onAdicionar={adicionarFonte}
        onAlternar={alternarFonte}
        onRemover={removerFonte}
      />
    </div>
  )
}

function ConexaoMcp({ fonteId }: { fonteId: string }) {
  const [conexao, setConexao] = useState<Awaited<ReturnType<typeof obterConexaoMcp>> | null>(null)
  const [erro, setErro] = useState("")
  const [carregando, setCarregando] = useState(false)
  const [copiado, setCopiado] = useState(false)
  async function abrir() {
    setErro("")
    setCopiado(false)
    setCarregando(true)
    try { setConexao(await obterConexaoMcp(fonteId)) }
    catch (e) { setErro((e as Error).message) }
    finally { setCarregando(false) }
  }
  return <div className="w-full space-y-2 text-xs">
    <Button size="sm" variant="outline" disabled={carregando}
      onClick={() => conexao ? setConexao(null) : void abrir()}>
      {conexao ? "Fechar configuração" : "Configurar retorno MCP"}
    </Button>
    {conexao && <div className="space-y-2">
      <p>No OpenBot, adicione um app MCP HTTP e conceda ao agente acesso à ferramenta entregar_vagas.</p>
      <label className="block">URL do servidor MCP<Input readOnly value={conexao.url} onFocus={e => e.target.select()} /></label>
      <Button size="sm" variant="outline" onClick={async () => {
        try { await navigator.clipboard.writeText(conexao.authorization); setCopiado(true); setErro("") }
        catch { setErro("Selecione o valor do cabeçalho e copie manualmente.") }
      }}>{copiado ? "Authorization copiado" : "Copiar valor de Authorization"}</Button>
      <Input aria-label="Valor do cabeçalho Authorization" type="password" readOnly value={conexao.authorization} onFocus={e => e.target.select()} />
    </div>}
    {erro && <p role="alert">{erro}</p>}
  </div>
}

function FontesPanel({
  fontes,
  onAdicionar,
  onAlternar,
  onRemover,
}: {
  fontes: ReturnType<typeof useBuscaConfig>["fontes"]
  onAdicionar: ReturnType<typeof useBuscaConfig>["adicionarFonte"]
  onAlternar: ReturnType<typeof useBuscaConfig>["alternarFonte"]
  onRemover: ReturnType<typeof useBuscaConfig>["removerFonte"]
}) {
  const { hasPermissao } = useAuth()
  const verificado = hasPermissao(PERMISSOES.verifyUser)
  const [tipo, setTipo] = useState<string>(verificado ? "Gupy" : "Trampos")
  const [identificador, setIdentificador] = useState("")
  const [cabecalhos, setCabecalhos] = useState<{ nome: string; valor: string; id: string }[]>([])
  const [assinarWebhook, setAssinarWebhook] = useState(false)
  const [segredoOpenBot, setSegredoOpenBot] = useState("")
  const [webhookUrl, setWebhookUrl] = useState("")
  const [instrucoes, setInstrucoes] = useState("")
  const [nomeEmpresa, setNomeEmpresa] = useState("")
  const [adicionando, setAdicionando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)
  const [verificacaoOpen, setVerificacaoOpen] = useState(false)

  const bloquear = () => {
    setVerificacaoOpen(true)
  }

  const adicionar = async () => {
    if (!verificado && fonteRestrita(tipo)) {
      bloquear()
      return
    }
    if (tipo === "Rss" && !identificador.trim()) {
      setErro("Informe a URL do feed RSS")
      return
    }
    if (tipo === "Agente" && !/^https:\/\//i.test(webhookUrl.trim())) {
      setErro("Informe a URL HTTPS do webhook")
      return
    }
    if (tipo === "Agente" && cabecalhos.some(c => !c.nome.trim() || !c.valor.trim())) {
      setErro("Preencha o nome e o valor de cada cabeçalho de autenticação")
      return
    }
    if (tipo === "Agente" && assinarWebhook && !segredoOpenBot.trim()) {
      setErro("Informe o segredo para assinar o webhook")
      return
    }
    setAdicionando(true)
    setErro(null)
    try {
      await onAdicionar({
        tipo,
        ...(tipo === "Agente"
          ? { webhookUrl: webhookUrl.trim(), cabecalhos: cabecalhos.map(c => ({ nome: c.nome.trim(), valor: c.valor.trim() })), segredoOpenBot: segredoOpenBot.trim() || undefined, instrucoes: instrucoes.trim(), nome: nomeEmpresa.trim() || null }
          : { identificador: identificador.trim() || null, nomeEmpresa: nomeEmpresa.trim() || null }),
      })
      setCabecalhos([])
      setAssinarWebhook(false)
      setSegredoOpenBot("")
      setWebhookUrl("")
      setInstrucoes("")
      setIdentificador("")
      setNomeEmpresa("")
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setAdicionando(false)
    }
  }

  return (
    <Panel className="p-5">
      <h3 className="text-sm font-semibold">Fontes de vagas</h3>
      <p className="text-xs text-muted-foreground">
        Escolha as fontes que vão buscar vagas com os critérios do seu perfil.
      </p>

      <div className="mt-4 grid grid-cols-1 items-end gap-3 sm:grid-cols-[170px_1fr_1fr_auto]">
        <div className="space-y-1 text-xs font-medium">
          Tipo
          <Select
            value={tipo}
            onValueChange={(valor) => {
              if (!verificado && fonteRestrita(valor)) {
                bloquear()
                return
              }
              setIdentificador("")
              setNomeEmpresa("")
              setTipo(valor)
            }}
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {FONTE_TIPOS.map((t) => {
                const bloqueada = !verificado && fonteRestrita(t)
                return (
                  <SelectItem key={t} value={t}>
                    <span className="flex items-center gap-1.5">
                      {bloqueada && <Lock className="h-3 w-3" />}
                      {FONTE_LABEL[t] ?? t}
                      {bloqueada && <span className="text-muted-foreground">· verificados</span>}
                    </span>
                  </SelectItem>
                )
              })}
            </SelectContent>
          </Select>
        </div>

        {tipo === "Agente" && (
          <label className="space-y-1 text-xs font-medium">
            URL do webhook
            <Input type="password" autoComplete="off" maxLength={2048} value={webhookUrl}
              onChange={(e) => setWebhookUrl(e.target.value)} placeholder="https://…" />
          </label>
        )}

        {tipo === "Rss" && (
          <label className="space-y-1 text-xs font-medium sm:col-span-1">
            URL do feed
            <Input value={identificador} onChange={(e) => setIdentificador(e.target.value)} placeholder="https://empresa.com/vagas/feed" />
          </label>
        )}

        {tipo === "Gupy" && (
          <label className="space-y-1 text-xs font-medium sm:col-span-1">
            Slug da empresa (opcional)
            <Input value={identificador} onChange={(e) => setIdentificador(e.target.value)} placeholder="vazio = todo o portal" />
          </label>
        )}

        {tipo === "LinkedIn" && (
          <label className="space-y-1 text-xs font-medium sm:col-span-1">
            Localização (opcional)
            <Input value={identificador} onChange={(e) => setIdentificador(e.target.value)} placeholder="vazio = Brasil" />
          </label>
        )}

        <label className="space-y-1 text-xs font-medium">
          {tipo === "Agente" ? "Nome do agente (opcional)" : "Nome da empresa (opcional)"}
          <Input value={nomeEmpresa} onChange={(e) => setNomeEmpresa(e.target.value)} placeholder={tipo === "Agente" ? "Meu workflow" : "ex.: GitLab"} />
        </label>

        <Button onClick={adicionar} disabled={adicionando} className="gap-1.5">
          {adicionando ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />} adicionar
        </Button>
      </div>

      {tipo === "Agente" && (
        <div className="mt-3 space-y-2">
          <fieldset className="space-y-2">
            <legend className="text-xs font-medium">Cabeçalhos de autenticação (opcional)</legend>
            <p className="text-xs text-muted-foreground">Use os nomes exigidos pelo seu workflow, como api-key, api-secret ou Authorization.</p>
            {cabecalhos.map(c => (
              <div key={c.id} className="flex flex-wrap gap-2">
                <Input className="min-w-32 flex-1" aria-label="Nome do cabeçalho" placeholder="api-key" maxLength={128}
                  value={c.nome} onChange={e => setCabecalhos(lista => lista.map(item => item.id === c.id ? { ...item, nome: e.target.value } : item))} />
                <Input className="min-w-32 flex-1" aria-label="Valor do cabeçalho" placeholder="Valor" type="password" autoComplete="new-password" maxLength={4096}
                  value={c.valor} onChange={e => setCabecalhos(lista => lista.map(item => item.id === c.id ? { ...item, valor: e.target.value } : item))} />
                <Button variant="ghost" onClick={() => setCabecalhos(lista => lista.filter(item => item.id !== c.id))}>Remover</Button>
              </div>
            ))}
            <Button variant="outline" size="sm" disabled={cabecalhos.length >= 16}
              onClick={() => setCabecalhos(lista => [...lista, { id: crypto.randomUUID(), nome: "", valor: "" }])}>Adicionar cabeçalho</Button>
          </fieldset>
          <label className="block space-y-1 text-xs font-medium">
            Assinatura do webhook
            <select className="block h-9 w-full rounded-md border bg-background px-3 text-sm"
              value={assinarWebhook ? "openbot" : "nenhuma"}
              onChange={e => {
                setAssinarWebhook(e.target.value === "openbot")
                setSegredoOpenBot("")
              }}>
              <option value="nenhuma">Sem assinatura</option>
              <option value="openbot">HMAC-SHA256</option>
            </select>
          </label>
          {assinarWebhook && (
            <label className="block space-y-1 text-xs font-medium">
              Segredo de assinatura
              <Input type="password" autoComplete="new-password" maxLength={1024} value={segredoOpenBot}
                onChange={e => setSegredoOpenBot(e.target.value)} />
            </label>
          )}
          <label className="block space-y-1 text-xs font-medium">
            Instruções ao agente (opcional)
            <Input maxLength={2000} value={instrucoes} onChange={(e) => setInstrucoes(e.target.value)}
              placeholder="Priorize vagas com salário publicado" />
          </label>
          <details className="text-xs text-muted-foreground">
            <summary className="cursor-pointer">Como configurar o retorno do workflow</summary>
            {assinarWebhook ? <p className="mt-2">Após salvar a fonte, abra “Configurar retorno MCP”. O agente recebe o pedidoId e entrega as vagas pela ferramenta entregar_vagas dentro de 30 minutos.</p> : <>
            <p className="mt-2">Confirme o recebimento com uma resposta HTTP 2xx. Quando a busca terminar, envie um POST para
              <code> callback.url</code>, com o cabeçalho <code>Authorization: Bearer &lt;callback.token&gt;</code>
              {" "}e o corpo em JSON. Envie até 25 vagas em um único lote, dentro de 30 minutos.</p>
            <pre className="mt-2 overflow-x-auto rounded bg-muted p-2">{'{"vagas":[{"titulo":"Analista","url":"https://empresa.com/vagas/123"}]}'}</pre>
            <p className="mt-1">Se nenhuma vaga atender ao perfil, envie <code>{'{"vagas":[]}'}</code>.</p></>}
          </details>
        </div>
      )}

      {FONTE_HINT[tipo] && (
        <p className="mt-2 text-[11px] text-muted-foreground">{FONTE_HINT[tipo]}</p>
      )}

      {erro && <div className="mt-3 rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-xs text-destructive">{erro}</div>}

      <div className="mt-4 space-y-2">
        {fontes.length === 0 ? (
          <div className="rounded-lg border border-dashed px-4 py-6 text-center text-xs text-muted-foreground">
            nenhuma fonte cadastrada ainda.
          </div>
        ) : (
          fontes.map((f) => (
            <div key={f.id} className="flex flex-wrap items-center gap-3 rounded-lg border bg-muted/30 px-3 py-2.5">
              <Badge variant="outline">{FONTE_LABEL[f.tipo] ?? f.tipo}</Badge>
              <div className="min-w-0 flex-1">
                <div className="truncate text-sm font-medium">
                  {f.nome ?? f.nomeEmpresa ?? FONTE_LABEL[f.tipo] ?? f.tipo}
                </div>
                {f.tipo === "Agente" && f.instrucoes && (
                  <div className="text-xs text-muted-foreground">{f.instrucoes}</div>
                )}
                {f.identificador && (
                  <div className="truncate text-xs text-muted-foreground">{f.identificador}</div>
                )}
              </div>
              <Button
                size="sm"
                variant={f.ativo ? "default" : "outline"}
                className="h-7 gap-1.5 text-xs"
                onClick={() => {
                  if (!verificado && fonteRestrita(f.tipo)) {
                    bloquear()
                    return
                  }
                  void onAlternar(f.id, !f.ativo)
                }}
              >
                {!verificado && fonteRestrita(f.tipo) && <Lock className="h-3 w-3" />}
                {f.ativo ? "ativa" : "pausada"}
              </Button>
              <Button
                size="sm"
                variant="ghost"
                className="h-7 gap-1.5 text-xs text-muted-foreground hover:text-destructive"
                onClick={() => void onRemover(f.id)}
              >
                <Trash2 className="h-3.5 w-3.5" /> remover
              </Button>
              {f.retornoMcp && <ConexaoMcp fonteId={f.id} />}
            </div>
          ))
        )}
      </div>

      <VerificacaoDialog open={verificacaoOpen} onOpenChange={setVerificacaoOpen} />
    </Panel>
  )
}

export function VagasPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Busca de vagas"
        description="O sistema procura vagas periodicamente nas suas fontes e mostra só as que passam nos seus critérios."
      />
      <Tabs defaultValue="inbox">
        <TabsList>
          <TabsTrigger value="inbox">Novas vagas</TabsTrigger>
          <TabsTrigger value="config">Configuração</TabsTrigger>
        </TabsList>
        <TabsContent value="inbox">
          <InboxTab />
        </TabsContent>
        <TabsContent value="config">
          <ConfigTab />
        </TabsContent>
      </Tabs>
    </div>
  )
}
