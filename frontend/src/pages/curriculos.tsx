import { PageHeader } from "@/components/app/page-header"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { CurriculosPanel } from "@/features/curriculos/curriculos-panel"
import { ConfiguracaoIaPanel } from "@/features/ia/configuracao-ia-panel"

export function CurriculosPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Currículos"
        description="Envie PDFs e escolha qual fica ativo nas suas candidaturas."
      />
      <Tabs defaultValue="curriculos">
        <TabsList>
          <TabsTrigger value="curriculos">Currículos</TabsTrigger>
          <TabsTrigger value="config">Configurações</TabsTrigger>
        </TabsList>
        <TabsContent value="curriculos">
          <CurriculosPanel />
        </TabsContent>
        <TabsContent value="config">
          <ConfiguracaoIaPanel />
        </TabsContent>
      </Tabs>
    </div>
  )
}
