import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom"
import { Landing } from "./pages/landing"
import { Dashboard } from "./pages/dashboard"
import { Login } from "./pages/login"

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Landing />} />
        <Route path="/app" element={<Dashboard />} />
        <Route path="/login" element={<Login />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
