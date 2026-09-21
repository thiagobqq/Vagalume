import { Navigate } from "react-router-dom"
import { useAuth } from "@/features/auth/auth-context"

export function ProtectedRoute({
  children,
  permissao,
}: {
  children: React.ReactNode
  permissao?: string
}) {
  const { isAuthenticated, hasPermissao } = useAuth()
  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (permissao && !hasPermissao(permissao)) return <Navigate to="/app" replace />
  return <>{children}</>
}
