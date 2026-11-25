import { Component, type ErrorInfo, type ReactNode } from "react"
import { Button } from "@/components/ui/button"

type State = { hasError: boolean; message: string }

export class ErrorBoundary extends Component<{ children: ReactNode }, State> {
  state: State = { hasError: false, message: "" }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, message: error?.message || "Erro inesperado na interface." }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error("ErrorBoundary capturou erro:", error, info)
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="grid min-h-dvh place-items-center bg-background p-6">
          <div className="w-full max-w-md space-y-4 rounded-2xl border-[1.5px] border-ink bg-card p-6 text-center shadow-hard-sm">
            <h1 className="text-lg font-extrabold">Algo saiu do esperado</h1>
            <p className="text-sm text-muted-foreground">{this.state.message}</p>
            <div className="flex justify-center gap-2">
              <Button onClick={() => window.location.reload()}>Recarregar página</Button>
              <Button
                variant="outline"
                onClick={() => {
                  this.setState({ hasError: false, message: "" })
                  window.location.href = "/app"
                }}
              >
                Voltar ao início
              </Button>
            </div>
          </div>
        </div>
      )
    }
    return this.props.children
  }
}
