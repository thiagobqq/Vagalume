import { http, HttpResponse } from "msw"
import { mockApplications } from "@/features/applications/mock-data"
import type { Application } from "@/features/applications/types"

let apps: Application[] = [...mockApplications]

function fakeExtract(url: string) {
  const u = url.toLowerCase()
  if (u.includes("nubank")) return { empresa: "Nubank", cargo: "Frontend Engineer", requisitos: ["React", "TypeScript", "Next.js"] }
  if (u.includes("ifood")) return { empresa: "iFood", cargo: "Product Designer", requisitos: ["Figma", "Design System"] }
  if (u.includes("gupy")) return { empresa: "Empresa via Gupy", cargo: "Desenvolvedor Pleno", requisitos: ["Node.js", "React"] }
  if (u.includes("linkedin")) return { empresa: "Empresa via LinkedIn", cargo: "Vaga LinkedIn", requisitos: ["Comunicação", "Inglês"] }
  if (u.includes("greenhouse")) return { empresa: "Greenhouse Co", cargo: "Software Engineer", requisitos: ["Algoritmos"] }
  return { empresa: "Empresa detectada", cargo: "Cargo extraído", requisitos: ["Requisito A", "Requisito B"] }
}

export const handlers = [
  http.get("/api/applications", () => {
    return HttpResponse.json(apps)
  }),

  http.post("/api/applications", async ({ request }) => {
    const body = (await request.json()) as Partial<Application>
    const created: Application = {
      id: String(Date.now()),
      empresa: body.empresa ?? "Nova empresa",
      cargo: body.cargo ?? "Novo cargo",
      link: body.link ?? "https://example.com",
      status: (body.status as Application["status"]) ?? "aplicado",
      dataAplicacao: new Date().toISOString(),
      observacoes: body.observacoes,
      requisitos: body.requisitos ?? [],
    }
    apps = [created, ...apps]
    return HttpResponse.json(created, { status: 201 })
  }),

  http.patch("/api/applications/:id", async ({ params, request }) => {
    const body = (await request.json()) as Partial<Application>
    apps = apps.map((a) => (a.id === params.id ? { ...a, ...body } : a))
    const updated = apps.find((a) => a.id === params.id)
    return HttpResponse.json(updated)
  }),

  http.delete("/api/applications/:id", ({ params }) => {
    apps = apps.filter((a) => a.id !== params.id)
    return HttpResponse.json({ ok: true })
  }),

  http.post("/api/extract", async ({ request }) => {
    const { url } = (await request.json()) as { url: string }
    await new Promise((r) => setTimeout(r, 900))
    const ex = fakeExtract(url)
    return HttpResponse.json({ ...ex, link: url })
  }),

  http.post("/api/reset", () => {
    apps = [...mockApplications]
    return HttpResponse.json(apps)
  }),
]
