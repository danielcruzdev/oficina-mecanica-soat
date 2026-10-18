# Relatório de análise de vulnerabilidades

| Campo | Valor |
|---|---|
| Versão analisada | branch `main`, tag `v1.0.0-fase1` (commit: _preencher_) |
| Escopo | Dependências NuGet, imagem Docker da API, repositório (segredos), código-fonte |

## 1. Dependências (SCA) — `dotnet list package --vulnerable --include-transitive`

| Data | Resultado |
|---|---|
| 18/10/2026 | Nenhum pacote vulnerável (diretos e transitivos) em `Oficina.Domain`, `Oficina.Application`, `Oficina.Infrastructure`, `Oficina.Api`, `Oficina.UnitTests` e `Oficina.IntegrationTests` |

O NuGet Audit também roda em todo `dotnet restore` (local e CI), e o CI executa esse comando a cada push/PR.

## 2. Imagem e repositório — Trivy

```bash
docker build -t oficina-api:fase1 .
trivy image --severity HIGH,CRITICAL oficina-api:fase1
trivy fs --scanners vuln,secret,misconfig .
```

| Alvo | Data | Críticas | Altas | Médias | Observações |
|---|---|---|---|---|---|
| Imagem `oficina-api` | _preencher_ | | | | |
| Repositório (`fs`) | _preencher_ | | | | |

## 3. Código (SAST) — SonarQube Cloud ou CodeQL

| Ferramenta | Data | Bugs | Vulnerabilidades | Security hotspots | Observações |
|---|---|---|---|---|---|
| _preencher_ | | | | | |

## 4. Achados e tratamento

| # | Ferramenta | Achado | Severidade | Tratamento | Status |
|---|---|---|---|---|---|
| | | | | | |

## 5. Controles já implementados

| Ameaça (OWASP API Top 10 2023) | Controle |
|---|---|
| API1 — BOLA | Rotas `/me/*` filtram pelo `cliente_id` do token; OS de outro cliente → 404 (teste de integração dedicado) |
| API2 — Autenticação quebrada | JWT com expiração de 60 min, validação de issuer/audience/lifetime/algoritmo; PBKDF2; rate limit no login |
| API3 — Exposição de propriedades | DTOs de saída explícitos; `senha_hash` nunca é retornado |
| API4 — Consumo irrestrito | Paginação com máximo de 100 itens; rate limit no login |
| API5 — Autorização por função | Políticas por papel em cada grupo de rotas |
| API8 — Configuração incorreta | ProblemDetails sem stack trace; segredos por variável de ambiente; contêiner não-root |
