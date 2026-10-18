# Requisitos

Prioridade MoSCoW: **M** = obrigatório (enunciado), **S** = desejável, **C** = opcional. O MVP entrega todos os itens **M**.

## Requisitos funcionais

| ID | Requisito | Prior. | Status | Onde |
|---|---|---|---|---|
| RF-01 | CRUD de clientes (PF/PJ), CPF/CNPJ válido e único, remoção lógica | M | Entregue | `ClienteService`, `/clientes` |
| RF-02 | CRUD de veículos (placa, marca, modelo, ano), placa válida e única | M | Entregue | `VeiculoService`, `/veiculos` |
| RF-03 | Histórico de OS por veículo | S | Parcial: filtro `?placa=` na listagem de OS | `/ordens-servico?placa=` |
| RF-04 | CRUD de serviços (preço de mão de obra, tempo estimado, ativo) | M | Entregue | `ServicoService`, `/servicos` |
| RF-05 | CRUD de peças e insumos (SKU único, tipo, unidade, preço, saldo, mínimo) | M | Entregue | `PecaService`, `/pecas` |
| RF-06 | Entrada de estoque (gera movimentação) | M | Entregue | `POST /pecas/{id}/entradas` |
| RF-07 | Ajuste de estoque (inventário) | S | Fora do MVP | — |
| RF-08 | Consultar movimentações de uma peça | S | Fora do MVP (movimentações são gravadas) | — |
| RF-09 | Alerta de estoque mínimo | C | Evento `EstoqueAbaixoDoMinimo` no log | `Peca.BaixarPorOS` |
| RF-10 | Abrir OS por CPF/CNPJ, com veículo existente ou cadastrado na hora | M | Entregue | `POST /ordens-servico` |
| RF-11 | Iniciar diagnóstico | M | Entregue | `POST /ordens-servico/{id}/diagnostico` |
| RF-12 | Incluir/remover serviços (preço congelado, rejeita inativo) | M | Entregue | `/ordens-servico/{id}/servicos` |
| RF-13 | Incluir/remover peças (valida disponibilidade, sem reservar) | M | Entregue | `/ordens-servico/{id}/pecas` |
| RF-14 | Orçamento automático (recalculado a cada item, com versão) | M | Entregue | `Orcamento.Recalcular` |
| RF-15 | Enviar orçamento (≥ 1 item → AguardandoAprovacao) | M | Entregue | `POST /ordens-servico/{id}/orcamento/envio` |
| RF-16 | Cliente aprova: baixa de estoque + EmExecucao na mesma transação | M | Entregue | `POST /me/ordens-servico/{id}/orcamento/aprovacao` |
| RF-17 | Cliente recusa: Finalizada (recusado), estoque intocado | M | Entregue | `POST /me/ordens-servico/{id}/orcamento/recusa` |
| RF-18 | Finalizar execução | M | Entregue | `POST /ordens-servico/{id}/execucao/finalizacao` |
| RF-19 | Registrar entrega | M | Entregue | `POST /ordens-servico/{id}/entrega` |
| RF-20 | Histórico de status (de/para, data UTC, usuário) | M | Entregue | `HistoricoStatus` |
| RF-21 | Login do cliente com CPF/CNPJ + senha (JWT `Cliente`) | M | Entregue | `POST /auth/clientes/login` |
| RF-22 | Cliente consulta só as próprias OS (404 para OS alheia) | M | Entregue | `/me/ordens-servico` |
| RF-23 | Serviço adicional durante a execução | C | Fora do MVP | — |
| RF-24 | Listar OS com filtros, paginação e ordem de prioridade | M | Entregue | `GET /ordens-servico` |
| RF-25 | Detalhar OS | M | Entregue | `GET /ordens-servico/{id}` |
| RF-26 | Tempo médio de execução (média, mínimo, máximo, quantidade; filtros período e serviço) | M | Entregue | `GET /relatorios/tempo-medio-execucao` |
| RF-27 | Gestão de usuários funcionários | C | Fora do MVP (usuários de seed) | — |

## Requisitos não funcionais

| ID | Categoria | Requisito | Como foi atendido |
|---|---|---|---|
| RNF-01 | Arquitetura | Monólito em camadas | 4 projetos com dependências para dentro (ADR-001) |
| RNF-02 | Segurança | JWT HS256, chave ≥ 256 bits por variável de ambiente, expiração 60 min | `Seguranca.cs`, `JwtTokenService` (ADR-003) |
| RNF-03 | Segurança | Autorização por papel + checagem de dono (BOLA) | Políticas por grupo de rota; `CarregarDoCliente` |
| RNF-04 | Segurança | Hash de senha PBKDF2; rate limit no login | `SenhaHasher`; 5/min por IP |
| RNF-05 | Segurança / LGPD | Erros em ProblemDetails sem stack trace | `TratamentoDeErros` |
| RNF-06 | Consistência | Um caso de uso = uma transação; concorrência otimista na peça | `IUnitOfWork`, `xmin` (ADR-005) |
| RNF-07 | Qualidade | Cobertura ≥ 80% em Domain + Application | 98,8% de linhas ([cobertura](../testes/cobertura.md)) |
| RNF-08 | Documentação | OpenAPI + Swagger UI com Authorize | `/swagger` |
| RNF-09 | Portabilidade | `docker compose up` sobe API + banco com migrations e seed | `docker-compose.yml` |
| RNF-10 | Observabilidade | Logs estruturados, health check do banco, `traceId` nos erros | `/health`, `ILogger` |
| RNF-11 | Desempenho | Listagens paginadas (máx. 100 por página) | `Paginacao` |
| RNF-12 | Cadeia de suprimentos | Análise de vulnerabilidades | NuGet Audit no restore + `dotnet list package --vulnerable` no CI |
