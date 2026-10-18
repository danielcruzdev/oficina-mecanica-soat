# ADR-004 — Regras de transição de status da OS

- **Status:** Aceita
- **Data:** 2026-10-04

## Contexto

O enunciado define os status `Recebida → Em diagnóstico → Aguardando aprovação → Em execução → Finalizada → Entregue` e exige que o sistema controle as mudanças, aplique regras de transição e altere o status **automaticamente conforme as ações**. Também é preciso decidir o que acontece quando o cliente recusa o orçamento.

## Decisão

1. **Não existe endpoint "mudar status".** O status muda só como consequência de um comando de negócio (`IniciarDiagnostico`, `EnviarOrcamento`, `AprovarOrcamento`, `RecusarOrcamento`, `FinalizarExecucao`, `RegistrarEntrega`).
2. A matriz de transições fica dentro do agregado `OrdemDeServico`. Qualquer transição fora dela lança `TransicaoDeStatusInvalidaException` → **HTTP 409**.
3. **Aprovação** leva automaticamente a `EmExecucao` e marca o início da execução.
4. **Recusa** leva automaticamente a `Finalizada` com `Resultado = OrcamentoRecusado`, sem novo status. O veículo fica liberado para retirada (`Entregue`).
5. Toda transição grava `HistoricoStatus` (de, para, data/hora UTC, usuário) e emite `StatusDaOSAlterado`.
6. Itens (serviços e peças) só podem ser alterados em `Recebida` ou `EmDiagnostico`, e o orçamento só pode ser enviado com pelo menos um item (senão **HTTP 422**).

Detalhes e diagrama em [`../maquina-de-estados.md`](../maquina-de-estados.md).

## Consequências

- (+) Impossível "pular etapa" pela API: a regra está no domínio e é coberta por testes unitários de todas as transições inválidas.
- (+) Mantém os 6 status do enunciado, sem status extra para a recusa.
- (–) Um serviço adicional durante a execução (novo orçamento) não faz parte do MVP.
