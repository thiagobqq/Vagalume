import { Lock } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

export function VerificacaoDialog({
  open,
  onOpenChange,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Lock className="h-4 w-4" /> Recurso exclusivo para usuários verificados
          </DialogTitle>
          <DialogDescription>
            Projeto em testes, uso exclusivo para usuários verificados. A verificação é manual, feita pelo mantenedor.
          </DialogDescription>
        </DialogHeader>

        <DialogFooter>
          <Button
            variant="ghost"
            onClick={() => onOpenChange(false)}
            className="gap-1.5 text-muted-foreground hover:text-foreground"
          >
            entendi
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
