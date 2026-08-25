import { StrictMode } from "react"
import { createRoot } from "react-dom/client"
import "./index.css"
import App from "./App.tsx"

async function enableMocking() {
  // MSW desabilitado por padrão — backend real é a fonte da verdade
  // Ative com VITE_USE_MSW=true se precisar rodar sem backend
  if (import.meta.env.PROD) return
  if (import.meta.env.VITE_USE_MSW !== "true") return
  const { worker } = await import("./mocks/browser.ts")
  return worker.start({ onUnhandledRequest: "bypass" })
}

enableMocking().then(() => {
  createRoot(document.getElementById("root")!).render(
    <StrictMode>
      <App />
    </StrictMode>
  )
})
