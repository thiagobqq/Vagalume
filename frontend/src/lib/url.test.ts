import test from "node:test"
import assert from "node:assert/strict"
import { safeExternalUrl, inferOrigemFromUrl } from "./url.ts"

test("safeExternalUrl aceita http/https e bloqueia esquemas perigosos", () => {
  assert.equal(safeExternalUrl("https://www.linkedin.com/jobs/view/123"), "https://www.linkedin.com/jobs/view/123")
  assert.equal(safeExternalUrl("http://empresa.com/vaga"), "http://empresa.com/vaga")
  assert.equal(safeExternalUrl("javascript:alert(1)"), null)
  assert.equal(safeExternalUrl("data:text/html,<script>alert(1)</script>"), null)
  assert.equal(safeExternalUrl("   "), null)
  assert.equal(safeExternalUrl(null), null)
})

test("inferOrigemFromUrl detecta LinkedIn, Gupy e Catho corretamente", () => {
  assert.equal(inferOrigemFromUrl("https://www.linkedin.com/jobs/view/999"), "LinkedIn")
  assert.equal(inferOrigemFromUrl("https://nubank.gupy.io/jobs/123"), "Gupy")
  assert.equal(inferOrigemFromUrl("https://www.catho.com.br/vagas/dev/123"), "Catho")
  assert.equal(inferOrigemFromUrl("https://carreiras.empresa.com/vaga/1"), null)
  assert.equal(inferOrigemFromUrl("javascript:alert(1)"), null)
})
