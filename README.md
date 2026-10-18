# Oficina Mecânica — Sistema Integrado de Atendimento e Execução de Serviços

Back-end (MVP) para uma oficina mecânica de médio porte gerenciar **ordens de serviço**, **clientes e veículos**, **catálogo de serviços** e **peças/insumos com estoque**, permitindo que o **cliente acompanhe e aprove o orçamento pela API**.

Tech Challenge — Fase 1 · FIAP PosTech Software Architecture (SOAT) · Grupo 68

## Objetivo

Substituir anotações manuais e planilhas por um sistema que:

- conduz a OS por todo o ciclo `Recebida → EmDiagnostico → AguardandoAprovacao → EmExecucao → Finalizada → Entregue`, com regras de transição garantidas pelo domínio;
- gera o **orçamento automaticamente** a partir dos serviços e peças incluídos;
- registra a **aprovação ou recusa** do cliente e **baixa o estoque** na mesma transação da aprovação;
- mede o **tempo médio de execução** dos serviços.

## Descrição da solução

| Fluxo | Como funciona |
|---|---|
| Criação da OS | O atendente identifica o cliente por **CPF/CNPJ** e informa a **placa**. Se o veículo ainda não existir, ele é cadastrado na hora (marca, modelo, ano). O mecânico inicia o diagnóstico e inclui serviços e peças; o orçamento é recalculado a cada item. |
| Orçamento | O atendente envia o orçamento (a OS vai para `AguardandoAprovacao`). O cliente faz login e **aprova** (estoque baixado, OS em execução) ou **recusa** (OS finalizada como recusada, estoque intocado). |
| Acompanhamento | O status muda **somente** pelas ações de negócio; não existe endpoint de "mudar status". Cada transição fica no histórico da OS. O cliente consulta as próprias OS em `/api/v1/me/ordens-servico`. |
| Gestão | CRUDs de clientes, veículos, serviços e peças; entrada de estoque; listagem de OS priorizada (`EmExecucao` > `AguardandoAprovacao` > `EmDiagnostico` > `Recebida`); detalhe da OS; relatório de tempo médio de execução. |

## Arquitetura

**Monólito em camadas** (Clean Architecture + DDD), com dependências apontando para o domínio:

```
Oficina.Api ──► Oficina.Application ──► Oficina.Domain
     │                  ▲                     ▲
     └──► Oficina.Infrastructure ─────────────┘
```

| Camada | Contém |
|---|---|
| `Oficina.Domain` | Agregados (`OrdemDeServico`, `Peca`, `Cliente`, `Veiculo`, `Servico`, `Usuario`), objetos de valor (`Documento`, `Placa`, `Dinheiro`), eventos de domínio, interfaces de repositório |
| `Oficina.Application` | Casos de uso (um serviço por contexto), DTOs, portas (`IUnitOfWork`, `ITokenService`, `ISenhaHasher`) |
| `Oficina.Infrastructure` | EF Core + PostgreSQL, migrations, repositórios, seed, JWT, hash de senha |
| `Oficina.Api` | Minimal APIs, autenticação/autorização, OpenAPI/Swagger, ProblemDetails, rate limit, health check |

Documentação de arquitetura:

- [Diagramas C4 (contexto, contêiner, componente)](docs/arquitetura/c4.md)
- [Máquina de estados da OS](docs/arquitetura/maquina-de-estados.md)
- [Requisitos funcionais e não funcionais](docs/arquitetura/requisitos.md)
- ADRs: [001 Monólito em camadas](docs/arquitetura/adr/ADR-001-monolito-em-camadas.md) · [002 PostgreSQL](docs/arquitetura/adr/ADR-002-postgresql.md) · [003 JWT](docs/arquitetura/adr/ADR-003-autenticacao-jwt.md) · [004 Transições de status](docs/arquitetura/adr/ADR-004-transicoes-de-status.md) · [005 Estoque e transações](docs/arquitetura/adr/ADR-005-baixa-de-estoque-e-transacoes.md)

## Tecnologias

- C# 14 / **.NET 10**, ASP.NET Core 10 (Minimal APIs)
- **EF Core 10** + Npgsql, **PostgreSQL 17**
- JWT Bearer, PBKDF2 (`PasswordHasher`), rate limiting nativo
- OpenAPI 3.1 nativo + **Swagger UI**
- **xUnit**, NSubstitute, WebApplicationFactory, **Testcontainers**, coverlet + ReportGenerator
- **Docker** + Docker Compose, GitHub Actions

## Banco de dados: PostgreSQL

Escolhido porque o ponto crítico do sistema é a **aprovação do orçamento com baixa de estoque atômica**, mesmo com duas OS disputando a mesma peça:

- transações ACID e constraints fortes (unicidade de CPF/CNPJ, placa, SKU e número da OS; `CHECK (saldo >= 0)`);
- concorrência otimista com a coluna de sistema `xmin`, suportada diretamente pelo Npgsql;
- `SEQUENCE` nativa para a numeração `OS-2026-000123`;
- open source, imagem Docker leve, oferta gerenciada nas principais nuvens.

SQL Server (imagem pesada, licença) e MongoDB (estoque compartilhado pede relações e transações entre agregados) foram descartados. Detalhes no [ADR-002](docs/arquitetura/adr/ADR-002-postgresql.md).

## Pré-requisitos

- [Docker](https://www.docker.com/) com Docker Compose (para rodar a aplicação e os testes de integração)
- [.NET SDK 10](https://dotnet.microsoft.com/download) (só para rodar os testes ou executar sem Docker)

## Execução

```bash
git clone <url-do-repositorio>
cd oficina-mecanica-soat
cp .env.example .env        # ajuste a senha do banco e a chave JWT
docker compose up -d --build
```

A API sobe em `http://localhost:8080`. As migrations e o seed de demonstração são aplicados automaticamente no start.

- Health check: `http://localhost:8080/health`
- Para parar: `docker compose down` (use `-v` para apagar também os dados)

<details>
<summary>Executar sem Docker (API local + banco no Docker)</summary>

```bash
docker compose up -d db
export ConnectionStrings__Oficina="Host=localhost;Port=5432;Database=oficina;Username=oficina;Password=<POSTGRES_PASSWORD do .env>"
export Jwt__Key="<JWT_KEY do .env>"
dotnet run --project src/Oficina.Api
```

A API sobe em `http://localhost:5166`.
</details>

## Swagger / documentação da API

Abra **`http://localhost:8080/swagger`**.

1. Faça login em `POST /api/v1/auth/login` (funcionário) ou `POST /api/v1/auth/clientes/login` (cliente).
2. Copie o `accessToken` da resposta.
3. Clique em **Authorize** e cole o token.

O documento OpenAPI fica em `http://localhost:8080/openapi/v1.json`.

## Usuários de demonstração

> Criados pelo **seed local** só para avaliação e demonstração. **Não são credenciais reais.**

| Papel | Login | Senha | Pode |
|---|---|---|---|
| Administrador | `admin@oficina.local` | `Admin@123` | Tudo, inclusive cadastro de serviços/peças, estoque e relatórios |
| Atendente | `atendente@oficina.local` | `Atendente@123` | Clientes, veículos, abrir OS, enviar orçamento, registrar entrega |
| Mecânico | `mecanico@oficina.local` | `Mecanico@123` | Diagnóstico, incluir serviços/peças, finalizar execução |
| Cliente | CPF `529.982.247-25` | `Cliente@123` | Consultar as próprias OS e aprovar/recusar o orçamento |

O seed também cria 4 serviços, 3 peças/insumos e o veículo `ABC1D23` do cliente de demonstração. Novos clientes recebem acesso à API quando cadastrados com o campo `senha`.

## Testes

Pré-requisito: Docker em execução (os testes de integração sobem um PostgreSQL com Testcontainers).

```bash
dotnet test
```

Cobertura (medida em `Oficina.Domain` + `Oficina.Application`, os domínios críticos):

```bash
dotnet tool restore
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults
dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:TestResults/relatorio \
  -reporttypes:"Html;MarkdownSummaryGithub" -assemblyfilters:"+Oficina.Domain;+Oficina.Application"
```

Resultado atual: **98,8% de linhas** e **96,5% de branches**, com 131 testes unitários e 31 de integração. Evidência em [`docs/testes/cobertura.md`](docs/testes/cobertura.md).

## Segurança

- JWT HS256 com chave via variável de ambiente, expiração de 60 min e validação de issuer, audience e lifetime
- Autorização por papel em todos os grupos de rotas; rotas do cliente sempre filtradas pelo `cliente_id` do token (OS de outro cliente → 404)
- Senhas com PBKDF2; rate limit de 5 tentativas de login por minuto por IP
- Erros em ProblemDetails (RFC 9457) com `traceId` e sem stack trace
- Contêiner da API roda como usuário não-root; segredos fora do repositório (`.env` no `.gitignore`)
- Relatório de análise de vulnerabilidades: [`docs/seguranca/relatorio-vulnerabilidades.md`](docs/seguranca/relatorio-vulnerabilidades.md)

## Estrutura de diretórios

```
oficina-mecanica-soat/
├─ .github/workflows/ci.yml        # build + testes + cobertura + pacotes vulneráveis
├─ docs/
│  ├─ arquitetura/                 # C4, ADRs, máquina de estados, requisitos
│  ├─ ddd/                         # linguagem ubíqua, modelo, matriz doc ↔ código, Event Storming
│  ├─ seguranca/                   # relatório de vulnerabilidades
│  └─ testes/                      # evidência de cobertura
├─ src/
│  ├─ Oficina.Domain/              # Comum, Clientes, Veiculos, Catalogo, Estoque, OrdensDeServico, Identidade
│  ├─ Oficina.Application/         # casos de uso por contexto
│  ├─ Oficina.Infrastructure/      # Persistencia (DbContext, Configuracoes, Migrations, Repositorios), Seguranca
│  └─ Oficina.Api/                 # Endpoints, Configuracao, Program.cs
├─ tests/
│  ├─ Oficina.UnitTests/           # Domain + Application
│  └─ Oficina.IntegrationTests/    # API + PostgreSQL (Testcontainers)
├─ Dockerfile
├─ docker-compose.yml
└─ .env.example
```

## Documentação DDD

- Event Storming (Miro): [https://miro.com/app/board/uXjVEbecZK4=/?share_link_id=246853268170](https://miro.com/app/board/uXjVEbecZK4=/?share_link_id=246853268170)
- Exportações do board: [`event-storming.pdf`](docs/ddd/event-storming.pdf) (fluxos de criação e acompanhamento da OS e gestão de peças e insumos) · [`diagramas.pdf`](docs/ddd/diagramas.pdf) (máquina de estados, mapa de contextos, agregados e linguagem ubíqua)
- Exportação e modelo de domínio: [`docs/ddd/`](docs/ddd/), incluindo [linguagem ubíqua, bounded contexts, agregados, eventos e matriz documentação ↔ código](docs/ddd/linguagem-ubiqua.md)

## Vídeo da entrega

- YouTube: _link a preencher_
