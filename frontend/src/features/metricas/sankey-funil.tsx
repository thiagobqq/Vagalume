import { useMemo, useState } from "react"
import type { FluxoSankeyDto, SelecaoSankey } from "./types"
import { SANKEY_NODE_META, type SankeyNodeMeta } from "./sankey-meta"

const OUTGOING_PRIORITY: Record<string, number> = {
  Triagem: 0,
  Entrevista: 1,
  Oferta: 2,
  EmAndamento: 3,
  Aguardando: 4,
  Encerrado: 5,
}

const INCOMING_PRIORITY: Record<string, number> = {
  Oferta: 0,
  Entrevista: 1,
  Triagem: 2,
  Aplicado: 3,
}

function getNodeMeta(id: string): SankeyNodeMeta {
  return (
    SANKEY_NODE_META[id] ?? {
      label: id,
      shortHint: id,
      color: "var(--muted-foreground)",
      soft: "var(--muted)",
      column: 4,
      orderInColumn: 9,
    }
  )
}

type LayoutNode = {
  id: string
  meta: SankeyNodeMeta
  colIndex: number
  value: number
  x: number
  y: number
  width: number
  height: number
  flowHeight: number
  flowOffsetY: number
  isLeftmost: boolean
  isRightmost: boolean
}

type LayoutLink = {
  key: string
  flow: FluxoSankeyDto
  source: LayoutNode
  target: LayoutNode
  sy0: number
  sy1: number
  ty0: number
  ty1: number
  path: string
  midX: number
  midY: number
  gradientId: string
}

type HoverState =
  | { tipo: "fluxo"; link: LayoutLink }
  | { tipo: "no"; node: LayoutNode }
  | null

const SVG_WIDTH = 960
const NODE_WIDTH = 22
const PAD_LEFT = 148
const PAD_RIGHT = 166
const PAD_TOP = 64
const PAD_BOTTOM = 28
const NODE_GAP = 26
const MIN_RIGHT_NODE_HEIGHT = 36
const MIN_STAGE_NODE_HEIGHT = 18

function computeLayout(fluxos: FluxoSankeyDto[], totalCandidaturas: number) {
  if (fluxos.length === 0 || totalCandidaturas <= 0) {
    return { nodes: [] as LayoutNode[], links: [] as LayoutLink[], height: 320 }
  }

  const inflow = new Map<string, number>()
  const outflow = new Map<string, number>()
  const nodeIds = new Set<string>()

  for (const f of fluxos) {
    nodeIds.add(f.origem)
    nodeIds.add(f.destino)
    outflow.set(f.origem, (outflow.get(f.origem) ?? 0) + f.quantidade)
    inflow.set(f.destino, (inflow.get(f.destino) ?? 0) + f.quantidade)
  }

  // Agrupa nós por coluna lógica e compacta apenas colunas presentes
  const colsMap = new Map<number, string[]>()
  for (const id of nodeIds) {
    const col = getNodeMeta(id).column
    const arr = colsMap.get(col) ?? []
    arr.push(id)
    colsMap.set(col, arr)
  }

  const sortedLogicalCols = Array.from(colsMap.keys()).sort((a, b) => a - b)
  const numCols = sortedLogicalCols.length
  const usableWidth = SVG_WIDTH - PAD_LEFT - PAD_RIGHT - NODE_WIDTH

  const colX = (colIdx: number) => {
    if (numCols <= 1) return PAD_LEFT
    return PAD_LEFT + (usableWidth * colIdx) / (numCols - 1)
  }

  const nodeValue = (id: string) => Math.max(inflow.get(id) ?? 0, outflow.get(id) ?? 0)

  // Estima a altura necessária com base na coluna mais alta (normalmente Aplicado ou coluna final)
  const baseChartHeight = 400
  const maxUsableHeight = baseChartHeight - PAD_TOP - PAD_BOTTOM

  // Calcula escala vertical conservando espaço para gaps na coluna com mais nós
  let maxColCount = 1
  for (const ids of colsMap.values()) {
    if (ids.length > maxColCount) maxColCount = ids.length
  }
  const availableForBars = Math.max(160, maxUsableHeight - (maxColCount - 1) * NODE_GAP)
  const pxPerUnit = availableForBars / totalCandidaturas

  const nodesById = new Map<string, LayoutNode>()
  let maxBottom = baseChartHeight

  sortedLogicalCols.forEach((logicalCol, colIdx) => {
    const ids = (colsMap.get(logicalCol) ?? []).sort(
      (a, b) => getNodeMeta(a).orderInColumn - getNodeMeta(b).orderInColumn
    )
    const isLeftmost = colIdx === 0
    const isRightmost = colIdx === numCols - 1
    const minH = isRightmost ? MIN_RIGHT_NODE_HEIGHT : MIN_STAGE_NODE_HEIGHT

    let cursorY = PAD_TOP
    for (const id of ids) {
      const value = nodeValue(id)
      const flowHeight = Math.max(3, value * pxPerUnit)
      const height = Math.max(flowHeight, minH)
      const flowOffsetY = (height - flowHeight) / 2

      const node: LayoutNode = {
        id,
        meta: getNodeMeta(id),
        colIndex: colIdx,
        value,
        x: colX(colIdx),
        y: cursorY,
        width: NODE_WIDTH,
        height,
        flowHeight,
        flowOffsetY,
        isLeftmost,
        isRightmost,
      }
      nodesById.set(id, node)
      cursorY += height + NODE_GAP
    }

    const colBottom = cursorY - NODE_GAP + PAD_BOTTOM
    if (colBottom > maxBottom) maxBottom = colBottom
  })

  // Ordena saídas de cada nó para minimizar cruzamentos
  const outgoingCursor = new Map<string, number>()
  const incomingCursor = new Map<string, number>()
  for (const n of nodesById.values()) {
    outgoingCursor.set(n.id, n.y + n.flowOffsetY)
    incomingCursor.set(n.id, n.y + n.flowOffsetY)
  }

  // Pré-calcula portas de saída por nó de origem
  const sourcePort = new Map<string, { y0: number; y1: number }>()
  for (const source of nodesById.values()) {
    const outLinks = fluxos
      .filter((f) => f.origem === source.id)
      .sort((a, b) => (OUTGOING_PRIORITY[a.destino] ?? 99) - (OUTGOING_PRIORITY[b.destino] ?? 99))

    const totalOut = outLinks.reduce((acc, f) => acc + f.quantidade, 0)
    let cur = source.y + source.flowOffsetY
    for (const f of outLinks) {
      const h = totalOut > 0 ? (f.quantidade / totalOut) * source.flowHeight : 0
      sourcePort.set(`${f.origem}->${f.destino}`, { y0: cur, y1: cur + h })
      cur += h
    }
  }

  // Pré-calcula portas de entrada por nó de destino
  const targetPort = new Map<string, { y0: number; y1: number }>()
  for (const target of nodesById.values()) {
    const inLinks = fluxos
      .filter((f) => f.destino === target.id)
      .sort((a, b) => (INCOMING_PRIORITY[a.origem] ?? 99) - (INCOMING_PRIORITY[b.origem] ?? 99))

    const totalIn = inLinks.reduce((acc, f) => acc + f.quantidade, 0)
    let cur = target.y + target.flowOffsetY
    for (const f of inLinks) {
      const h = totalIn > 0 ? (f.quantidade / totalIn) * target.flowHeight : 0
      targetPort.set(`${f.origem}->${f.destino}`, { y0: cur, y1: cur + h })
      cur += h
    }
  }

  const links: LayoutLink[] = fluxos.map((flow) => {
    const key = `${flow.origem}->${flow.destino}`
    const source = nodesById.get(flow.origem)!
    const target = nodesById.get(flow.destino)!
    const sp = sourcePort.get(key)!
    const tp = targetPort.get(key)!

    const x0 = source.x + source.width
    const x1 = target.x
    const dx = x1 - x0
    const cx0 = x0 + dx * 0.45
    const cx1 = x1 - dx * 0.45

    // Garante espessura visual mínima de 2.5px para fluxos unitários em grandes volumes
    const syMid = (sp.y0 + sp.y1) / 2
    const tyMid = (tp.y0 + tp.y1) / 2
    const halfH = Math.max((sp.y1 - sp.y0) / 2, 1.5)
    const sy0 = syMid - halfH
    const sy1 = syMid + halfH
    const ty0 = tyMid - halfH
    const ty1 = tyMid + halfH

    const path = [
      `M ${x0} ${sy0}`,
      `C ${cx0} ${sy0}, ${cx1} ${ty0}, ${x1} ${ty0}`,
      `L ${x1} ${ty1}`,
      `C ${cx1} ${ty1}, ${cx0} ${sy1}, ${x0} ${sy1}`,
      "Z",
    ].join(" ")

    return {
      key,
      flow,
      source,
      target,
      sy0,
      sy1,
      ty0,
      ty1,
      path,
      midX: (x0 + x1) / 2,
      midY: (syMid + tyMid) / 2,
      gradientId: `sankey-grad-${flow.origem}-${flow.destino}`,
    }
  })

  return {
    nodes: Array.from(nodesById.values()),
    links,
    height: Math.ceil(maxBottom),
  }
}

function formatPct(value: number) {
  return `${value.toLocaleString("pt-BR", { maximumFractionDigits: 1 })}%`
}

type Props = {
  fluxos: FluxoSankeyDto[]
  totalCandidaturas: number
  selecao: SelecaoSankey
  onSelect: (selecao: SelecaoSankey) => void
}

export function SankeyFunil({ fluxos, totalCandidaturas, selecao, onSelect }: Props) {
  const [hover, setHover] = useState<HoverState>(null)

  const layout = useMemo(
    () => computeLayout(fluxos, totalCandidaturas),
    [fluxos, totalCandidaturas]
  )

  function isLinkHighlighted(link: LayoutLink): boolean {
    if (hover) {
      if (hover.tipo === "fluxo") return hover.link.key === link.key
      return link.source.id === hover.node.id || link.target.id === hover.node.id
    }
    if (selecao) {
      if (selecao.tipo === "fluxo") {
        return link.flow.origem === selecao.origem && link.flow.destino === selecao.destino
      }
      return link.source.id === selecao.id || link.target.id === selecao.id
    }
    return true
  }

  function isNodeHighlighted(node: LayoutNode): boolean {
    if (hover) {
      if (hover.tipo === "no") return hover.node.id === node.id
      return hover.link.source.id === node.id || hover.link.target.id === node.id
    }
    if (selecao) {
      if (selecao.tipo === "no") return selecao.id === node.id
      return selecao.origem === node.id || selecao.destino === node.id
    }
    return true
  }

  function toggleLink(flow: FluxoSankeyDto) {
    if (selecao?.tipo === "fluxo" && selecao.origem === flow.origem && selecao.destino === flow.destino) {
      onSelect(null)
    } else {
      onSelect({ tipo: "fluxo", origem: flow.origem, destino: flow.destino })
    }
  }

  function toggleNode(id: string) {
    if (selecao?.tipo === "no" && selecao.id === id) {
      onSelect(null)
    } else {
      onSelect({ tipo: "no", id })
    }
  }

  if (layout.nodes.length === 0) {
    return null
  }

  return (
    <div className="space-y-3">
      {/* Barra de inspeção ao passar o mouse (ou dica) */}
      <div className="flex min-h-[38px] flex-wrap items-center justify-between gap-2 rounded-xl border border-ink/15 bg-[var(--nb-paper)]/70 px-3.5 py-2 text-xs">
        {hover?.tipo === "fluxo" ? (
          <div className="flex flex-wrap items-center gap-2">
            <span className="inline-flex items-center gap-1.5 font-bold text-foreground">
              <span
                className="h-2.5 w-2.5 rounded-full border border-ink/30"
                style={{ background: hover.link.source.meta.color }}
              />
              {hover.link.source.meta.label}
              <span className="text-muted-foreground">→</span>
              <span
                className="h-2.5 w-2.5 rounded-full border border-ink/30"
                style={{ background: hover.link.target.meta.color }}
              />
              {hover.link.target.meta.label}
            </span>
            <span className="rounded-full border border-ink/20 bg-card px-2 py-0.5 font-bold">
              {hover.link.flow.quantidade} vaga{hover.link.flow.quantidade === 1 ? "" : "s"}
            </span>
            <span className="text-muted-foreground">
              {formatPct(hover.link.flow.percentualOrigem)} saindo de {hover.link.source.meta.label} ·{" "}
              {formatPct(hover.link.flow.percentualTotal)} do total
            </span>
          </div>
        ) : hover?.tipo === "no" ? (
          <div className="flex flex-wrap items-center gap-2">
            <span className="inline-flex items-center gap-1.5 font-bold text-foreground">
              <span
                className="h-2.5 w-2.5 rounded-full border border-ink/30"
                style={{ background: hover.node.meta.color }}
              />
              {hover.node.meta.label}
            </span>
            <span className="rounded-full border border-ink/20 bg-card px-2 py-0.5 font-bold">
              {hover.node.value} vaga{hover.node.value === 1 ? "" : "s"} (
              {formatPct((hover.node.value / totalCandidaturas) * 100)})
            </span>
            <span className="text-muted-foreground">{hover.node.meta.shortHint}</span>
          </div>
        ) : (
          <span className="text-muted-foreground">
            Passe o mouse ou clique em qualquer faixa ou etapa do diagrama para filtrar as vagas daquele caminho.
          </span>
        )}

        {selecao && (
          <button
            type="button"
            onClick={() => onSelect(null)}
            className="text-xs font-semibold text-foreground underline underline-offset-4 hover:opacity-75"
          >
            limpar seleção
          </button>
        )}
      </div>

      {/* Container responsivo do SVG */}
      <div className="w-full overflow-x-auto pb-1">
        <svg
          viewBox={`0 0 ${SVG_WIDTH} ${layout.height}`}
          className="h-auto w-full min-w-[680px] select-none"
          role="img"
          aria-label="Diagrama de Sankey do histórico de transição das candidaturas"
        >
          <defs>
            {layout.links.map((link) => (
              <linearGradient
                key={link.gradientId}
                id={link.gradientId}
                gradientUnits="userSpaceOnUse"
                x1={link.source.x + link.source.width}
                y1={0}
                x2={link.target.x}
                y2={0}
              >
                <stop offset="0%" stopColor={link.source.meta.color} />
                <stop offset="100%" stopColor={link.target.meta.color} />
              </linearGradient>
            ))}
          </defs>

          {/* Faixas (fluxos) */}
          <g>
            {layout.links.map((link) => {
              const active = isLinkHighlighted(link)
              const isSelected =
                selecao?.tipo === "fluxo" &&
                selecao.origem === link.flow.origem &&
                selecao.destino === link.flow.destino
              const isHovered = hover?.tipo === "fluxo" && hover.link.key === link.key
              const hasAnyFocus = Boolean(hover || selecao)

              const opacity = isSelected || isHovered ? 0.85 : active ? (hasAnyFocus ? 0.68 : 0.44) : 0.1

              return (
                <g
                  key={link.key}
                  className="cursor-pointer transition-opacity duration-150"
                  onMouseEnter={() => setHover({ tipo: "fluxo", link })}
                  onMouseLeave={() => setHover(null)}
                  onClick={() => toggleLink(link.flow)}
                >
                  <path
                    d={link.path}
                    fill={`url(#${link.gradientId})`}
                    fillOpacity={opacity}
                    stroke={isSelected || isHovered ? "var(--nb-ink)" : "none"}
                    strokeWidth={isSelected || isHovered ? 1.25 : 0}
                    strokeOpacity={0.55}
                    className="transition-[fill-opacity] duration-150"
                  />
                  {/* Rótulo discreto no meio da faixa quando ela tem altura suficiente */}
                  {link.sy1 - link.sy0 >= 22 && (
                    <text
                      x={link.midX}
                      y={link.midY + 4}
                      textAnchor="middle"
                      className="pointer-events-none fill-foreground text-[11px] font-bold"
                      opacity={active ? 0.9 : 0.25}
                      style={{ paintOrder: "stroke", stroke: "var(--card)", strokeWidth: 3.5 }}
                    >
                      {link.flow.quantidade}
                    </text>
                  )}
                </g>
              )
            })}
          </g>

          {/* Nós (etapas e destinos) */}
          <g>
            {layout.nodes.map((node) => {
              const active = isNodeHighlighted(node)
              const isSelected = selecao?.tipo === "no" && selecao.id === node.id
              const pct = Math.round((node.value / totalCandidaturas) * 1000) / 10

              // Posição dos rótulos sem sobrepor faixas
              const labelX = node.isLeftmost
                ? node.x - 12
                : node.isRightmost
                  ? node.x + node.width + 12
                  : node.x + node.width / 2

              const labelAnchor = node.isLeftmost ? "end" : node.isRightmost ? "start" : "middle"
              const labelTitleY = node.isLeftmost || node.isRightmost ? node.y + node.height / 2 - 4 : node.y - 26
              const labelSubY = node.isLeftmost || node.isRightmost ? node.y + node.height / 2 + 13 : node.y - 10

              return (
                <g
                  key={node.id}
                  className="cursor-pointer"
                  opacity={active ? 1 : 0.35}
                  onMouseEnter={() => setHover({ tipo: "no", node })}
                  onMouseLeave={() => setHover(null)}
                  onClick={() => toggleNode(node.id)}
                >
                  {/* Sombra dura estilo caderno */}
                  <rect
                    x={node.x + 2.5}
                    y={node.y + 2.5}
                    width={node.width}
                    height={node.height}
                    rx={6}
                    fill="var(--nb-shadow)"
                    opacity={isSelected ? 0.95 : 0.22}
                  />
                  <rect
                    x={node.x}
                    y={node.y}
                    width={node.width}
                    height={node.height}
                    rx={6}
                    fill={node.meta.color}
                    stroke="var(--nb-ink)"
                    strokeWidth={isSelected ? 2.2 : 1.5}
                  />

                  <text
                    x={labelX}
                    y={labelTitleY}
                    textAnchor={labelAnchor}
                    className="fill-foreground text-[13px] font-extrabold tracking-tight"
                  >
                    {node.meta.label}
                  </text>
                  <text
                    x={labelX}
                    y={labelSubY}
                    textAnchor={labelAnchor}
                    className="fill-muted-foreground text-[11px] font-semibold"
                  >
                    {node.value} ({formatPct(pct)})
                  </text>
                </g>
              )
            })}
          </g>
        </svg>
      </div>
    </div>
  )
}
