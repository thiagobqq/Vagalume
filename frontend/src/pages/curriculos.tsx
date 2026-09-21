import { PageHeader } from "@/components/app/page-header"
import { CurriculosPanel } from "@/features/curriculos/curriculos-panel"

export function CurriculosPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Currículos"
        description="Envie PDFs e escolha qual fica ativo nas suas candidaturas."
      />
      <CurriculosPanel />
    </div>
  )
}
