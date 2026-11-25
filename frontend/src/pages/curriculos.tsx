import { useState } from "react"
import { PageHeader } from "@/components/app/page-header"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { VerificacaoDialog } from "@/components/app/verificacao-dialog"
import { CurriculosPanel } from "@/features/curriculos/curriculos-panel"
import { ConfiguracaoIaPanel } from "@/features/ia/configuracao-ia-panel"
import { useAuth } from "@/features/auth/auth-context"
import { PERMISSOES } from "@/features/auth/permissoes"
import { Lock } from "lucide-react"

export function CurriculosPage() {
  const { hasPermissao } = useAuth()
  const verificado = hasPermissao(PERMISSOES.verifyUser)
  const [aba, setAba] = useState("curriculos")
  const [verificacaoOpen, setVerificacaoOpen] = useState(false)

  return (
    <div className="space-y-6">
      <PageHeader title="Currículos" />
      <Tabs
        value={aba}
        onValueChange={(valor) => {
          if (valor === "config" && !verificado) {
            setVerificacaoOpen(true)
            return
          }
          setAba(valor)
        }}
      >
        <TabsList>
          <TabsTrigger value="curriculos">Currículos</TabsTrigger>
          <TabsTrigger value="config" className="gap-1.5">
            {!verificado && <Lock className="h-3.5 w-3.5" />} Configurações
          </TabsTrigger>
        </TabsList>
        <TabsContent value="curriculos">
          <CurriculosPanel />
        </TabsContent>
        <TabsContent value="config">
          <ConfiguracaoIaPanel />
        </TabsContent>
      </Tabs>

      <VerificacaoDialog open={verificacaoOpen} onOpenChange={setVerificacaoOpen} />
    </div>
  )
}
