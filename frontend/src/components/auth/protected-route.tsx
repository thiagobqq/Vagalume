import { Navigate } from "react-router-dom"
import { Loader2 } from "lucide-react"
import { useAuth } from "@/features/auth/auth-context"

export function ProtectedRoute({
  children,
  permissao,
}: {
  children: React.ReactNode
  permissao?: string
}) {
  const { isAuthenticated, hasPermissao, loading } = useAuth()
  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (permissao && !hasPermissao(permissao)) {
    if (loading) {
      return (
        <div className="grid min-h-[40vh] place-items-center text-sm text-muted-foreground">
          <span className="inline-flex items-center gap-2">
            <Loader2 className="h-4 w-4 animate-spin" /> verificando permissões...
          </span>
        </div>
      )
    }
    return <Navigate to="/app" replace />
  }
  return <>{children}</>
}
