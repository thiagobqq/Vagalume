import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom"
import { Landing } from "./pages/landing"
import { Dashboard } from "./pages/dashboard"
import { Login } from "./pages/login"
import { Register } from "./pages/register"
import { AuthProvider } from "./features/auth/auth-context"
import { ThemeProvider } from "./features/theme/theme-provider"
import { ProtectedRoute } from "./components/auth/protected-route"

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
                  <Dashboard />
                </ProtectedRoute>
              }
            />
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </ThemeProvider>
  )
}
