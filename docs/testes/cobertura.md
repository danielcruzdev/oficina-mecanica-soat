# Testes e cobertura

## Domínios críticos

A meta de **≥ 80%** é medida nos projetos que concentram as regras de negócio:

- `Oficina.Domain`: agregados, objetos de valor, máquina de estados, estoque
- `Oficina.Application`: casos de uso (abertura → entrega da OS, aprovação transacional, login, relatórios)

`Oficina.Infrastructure` e `Oficina.Api` (EF Core, endpoints, configuração) são exercitados pelos testes de integração, mas ficam fora da meta por serem código de framework/composição.

## Resultado

| Métrica (Domain + Application) | Valor |
|---|---|
| Cobertura de linhas | **98,8%** (945 de 956) |
| Cobertura de branches | **96,5%** (224 de 232) |
| Testes unitários | 131 (xUnit + NSubstitute) |
| Testes de integração | 31 (WebApplicationFactory + Testcontainers PostgreSQL) |

Relatório completo por classe: [`resumo-cobertura.md`](resumo-cobertura.md).

## O que cada suíte cobre

| Suíte | Projeto | Cobre |
|---|---|---|
| Unitários — domínio | `tests/Oficina.UnitTests/Domain` | CPF/CNPJ (incl. alfanumérico), placa, dinheiro, todas as transições válidas e inválidas da OS, cálculo e versão do orçamento, saldo e baixa de estoque, cadastros |
| Unitários — aplicação | `tests/Oficina.UnitTests/Application` | Casos de uso com repositórios dublados: abertura com veículo novo/existente, disponibilidade de peça, aprovação sem estoque (nada é salvo), recusa, BOLA, login, tempo médio, listagem |
| Integração | `tests/Oficina.IntegrationTests` | Fluxo completo abrir → entregar, recusa, falta de estoque com rollback (422), aprovações simultâneas na mesma peça, 401/403, BOLA (404), validações 400/409, CRUDs, listagem priorizada |

## Como reproduzir

Pré-requisito: Docker em execução (os testes de integração sobem um PostgreSQL com Testcontainers).

```bash
dotnet tool restore
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults
dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:TestResults/relatorio \
  -reporttypes:"Html;MarkdownSummaryGithub" -assemblyfilters:"+Oficina.Domain;+Oficina.Application"
```

Abra `TestResults/relatorio/index.html` no navegador para ver o relatório HTML.
