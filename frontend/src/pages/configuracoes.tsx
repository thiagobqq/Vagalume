import { PageHeader } from "@/components/app/page-header"
import { ConfiguracaoIaPanel } from "@/features/ia/configuracao-ia-panel"

export function ConfiguracoesPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Configurações"
        description="Conecte seu provedor de IA para analisar a compatibilidade entre seu currículo e as vagas."
      />
      <ConfiguracaoIaPanel />
    </div>
  )
}
