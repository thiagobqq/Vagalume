import test from "node:test"
import assert from "node:assert/strict"

function setupBrowserEnv() {
  const store = new Map<string, string>()
  const events: string[] = []

  const localStorageMock = {
    getItem: (k: string) => store.get(k) ?? null,
    setItem: (k: string, v: string) => { store.set(k, v) },
    removeItem: (k: string) => { store.delete(k) },
    clear: () => { store.clear() },
  }

  const windowMock = {
    location: { origin: "http://localhost:3000" },
    dispatchEvent: (ev: Event) => {
      events.push(ev.type)
      return true
    },
  }

  Object.defineProperty(globalThis, "localStorage", { value: localStorageMock, configurable: true })
  Object.defineProperty(globalThis, "window", { value: windowMock, configurable: true })

  return { store, events }
}

test("clearTokens dispara AUTH_CLEARED_EVENT na mesma aba e limpa storage", async () => {
  const { store, events } = setupBrowserEnv()
  const { clearTokens, AUTH_CLEARED_EVENT } = await import("./api.ts")

  store.set("vagalume:accessToken", "at-1")
  store.set("vagalume:refreshToken", "rt-1")
  store.set("vagalume:user", JSON.stringify({ id: "1" }))

  clearTokens()

  assert.equal(store.size, 0)
  assert.ok(events.includes(AUTH_CLEARED_EVENT))
})

test("apiJson retorna null corretamente quando envelope traz data: null", async () => {
  setupBrowserEnv()
  const { apiJson } = await import("./api.ts")

  const origFetch = globalThis.fetch
  globalThis.fetch = (async () =>
    new Response(JSON.stringify({ success: true, message: "ok", data: null }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    })) as typeof fetch

  try {
    const res = await apiJson<null>("/api/ia/configuracao", { auth: false })
    assert.equal(res, null)
  } finally {
    globalThis.fetch = origFetch
  }
})

test("doRefresh preserva tokens quando ocorre falha de rede temporaria", async () => {
  const { store, events } = setupBrowserEnv()
  const { apiFetch, AUTH_CLEARED_EVENT } = await import("./api.ts")

  store.set("vagalume:accessToken", "expirado")
  store.set("vagalume:refreshToken", "rt-valido")

  let calls = 0
  const origFetch = globalThis.fetch
  globalThis.fetch = (async (input: string | URL | Request) => {
    calls++
    const url = String(input)
    if (url.includes("/api/auth/refresh")) {
      throw new TypeError("Failed to fetch")
    }
    return new Response(JSON.stringify({ success: false, message: "Não autenticado", data: null }), { status: 401 })
  }) as typeof fetch

  try {
    const res = await apiFetch("/api/candidaturas")
    assert.equal(res.status, 401)
    assert.equal(calls, 2)
    assert.equal(store.get("vagalume:refreshToken"), "rt-valido")
    assert.ok(!events.includes(AUTH_CLEARED_EVENT))
  } finally {
    globalThis.fetch = origFetch
  }
})
