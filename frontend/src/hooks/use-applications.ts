import { useEffect, useState } from "react"
import type { Application, Status } from "@/features/applications/types"

export function useApplications() {
  const [apps, setApps] = useState<Application[]>([])
  const [loading, setLoading] = useState(true)

  async function refresh() {
    setLoading(true)
    const res = await fetch("/api/applications")
    const data = (await res.json()) as Application[]
    setApps(data)
    setLoading(false)
  }

  useEffect(() => {
    refresh()
  }, [])

  async function move(id: string, status: Status) {
    setApps((prev) => prev.map((a) => (a.id === id ? { ...a, status } : a)))
    await fetch(`/api/applications/${id}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ status }),
    })
  }

  async function remove(id: string) {
    setApps((prev) => prev.filter((a) => a.id !== id))
    await fetch(`/api/applications/${id}`, { method: "DELETE" })
  }

  async function create(data: { empresa: string; cargo: string; link: string; status: Status; observacoes?: string }) {
    const res = await fetch("/api/applications", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    })
    const created = (await res.json()) as Application
    setApps((prev) => [created, ...prev])
    return created
  }

  return { apps, loading, move, remove, create, refresh }
}
