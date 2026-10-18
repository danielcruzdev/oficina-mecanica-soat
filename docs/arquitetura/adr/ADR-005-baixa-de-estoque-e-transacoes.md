# ADR-005 — Baixa de estoque na aprovação, na mesma transação, com concorrência otimista

- **Status:** Aceita
- **Data:** 2026-10-06

## Contexto

As peças e insumos incluídos na OS consomem estoque. É preciso decidir **quando** o estoque é baixado e como evitar saldo negativo quando duas OS disputam a mesma peça.

Opções:
1. Reservar ao incluir a peça na OS.
2. Baixar só na aprovação do orçamento.

Reservar na inclusão prende estoque em orçamentos que podem ser recusados e exige expiração de reservas.

## Decisão

- **Na inclusão:** só se valida a disponibilidade no momento (saldo ≥ quantidade já incluída + nova), sem reservar.
- **Na aprovação:** o caso de uso `AprovarOrcamento` carrega a OS e as peças, muda o status (`EmExecucao`), baixa o saldo de cada peça e grava uma `MovimentacaoEstoque(SaidaOS)`, tudo em **um único `SaveChanges`**, ou seja, uma única transação.
  - Falta de saldo de qualquer peça → `DomainException` antes de salvar → **HTTP 422** e nada é gravado.
- **Concorrência:** a `Peca` usa a coluna de sistema `xmin` do PostgreSQL como token de concorrência otimista. Se duas aprovações alteram a mesma peça ao mesmo tempo, a segunda recebe `DbUpdateConcurrencyException` → **HTTP 409** e pode tentar de novo.
- Defesa extra no banco: `CHECK (saldo >= 0)`.
- **Unit of Work:** um `DbContext` por request; cada caso de uso chama `IUnitOfWork.SalvarAsync` uma única vez.

## Consequências

- (+) O estoque só é consumido quando o cliente aprova.
- (+) Os testes de integração cobrem o rollback (falta de estoque) e a aprovação simultânea de duas OS na mesma peça.
- (–) Entre a inclusão e a aprovação o saldo pode mudar. Nesse caso a aprovação falha com 422 e a oficina ajusta o orçamento ou dá entrada no estoque.
