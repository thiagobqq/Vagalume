import { AppShell } from "./components/app/app-shell"
import { Fireflies } from "./components/app/fireflies"
import { Landing } from "./pages/landing"
import { Dashboard } from "./pages/dashboard"
import { CurriculosPage } from "./pages/curriculos"
import { VagasPage } from "./pages/vagas"
import { AdminPage } from "./pages/admin"
import { Login } from "./pages/login"
import { Register } from "./pages/register"
import { AuthProvider } from "./features/auth/auth-context"
import { PERMISSOES } from "./features/auth/permissoes"
import { GOOGLE_CLIENT_ID } from "./features/auth/google"
import { ThemeProvider } from "./features/theme/theme-provider"
import { ProtectedRoute } from "./components/auth/protected-route"
import { GoogleOAuthProvider } from "@react-oauth/google"
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom"

export default function App() {
  const content = (
    <ThemeProvider>
      <Fireflies />
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/" element={<Landing />} />
            <Route
              path="/app"
              element={
                <ProtectedRoute>
                  <AppShell />
                </ProtectedRoute>
              }
            >
              <Route index element={<Dashboard />} />
              <Route
                path="curriculos"
                element={
                  <ProtectedRoute>
                    <CurriculosPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="vagas"
                element={
                  <ProtectedRoute>
                    <VagasPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="admin"
                element={
                  <ProtectedRoute permissao={PERMISSOES.admin}>
                    <AdminPage />
                  </ProtectedRoute>
                }
              />
            </Route>
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </ThemeProvider>
  )

  if (!GOOGLE_CLIENT_ID) return content

  return <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>{content}</GoogleOAuthProvider>
}
