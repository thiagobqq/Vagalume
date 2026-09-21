import { AppShell } from "./components/app/app-shell"
import { Landing } from "./pages/landing"
import { Dashboard } from "./pages/dashboard"
import { CurriculosPage } from "./pages/curriculos"
import { ConfiguracoesPage } from "./pages/configuracoes"
import { VagasPage } from "./pages/vagas"
import { AdminPage } from "./pages/admin"
import { Login } from "./pages/login"
import { Register } from "./pages/register"
import { AuthProvider } from "./features/auth/auth-context"
import { PERMISSOES } from "./features/auth/permissoes"
import { ThemeProvider } from "./features/theme/theme-provider"
import { ProtectedRoute } from "./components/auth/protected-route"
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom"

export default function App() {
  return (
    <ThemeProvider>
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
              <Route path="curriculos" element={<CurriculosPage />} />
              <Route
                path="configuracoes"
                element={
                  <ProtectedRoute permissao={PERMISSOES.compatibilidade}>
                    <ConfiguracoesPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="vagas"
                element={
                  <ProtectedRoute permissao={PERMISSOES.buscaVagas}>
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
}
