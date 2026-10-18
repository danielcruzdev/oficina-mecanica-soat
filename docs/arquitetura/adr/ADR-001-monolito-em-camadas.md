# ADR-001 — Monólito em camadas (Clean Architecture + DDD)

- **Status:** Aceita
- **Data:** 2026-10-02

## Contexto

O MVP precisa ser entregue por uma pessoa, em poucas semanas, com regras de negócio ricas (máquina de estados da OS, orçamento, estoque) e exigência explícita de um back-end **monolítico organizado em camadas**. Ao mesmo tempo, o domínio precisa ficar isolado de banco e HTTP para ser testável com cobertura alta.

## Decisão

Um único deployable (`Oficina.Api`) dividido em quatro projetos, com dependências apontando para o domínio:

| Projeto | Responsabilidade | Depende de |
|---|---|---|
| `Oficina.Domain` | Agregados, entidades, objetos de valor, eventos, interfaces de repositório | — |
| `Oficina.Application` | Casos de uso (um serviço por contexto), DTOs, portas (`IUnitOfWork`, `ITokenService`, `ISenhaHasher`) | Domain |
| `Oficina.Infrastructure` | EF Core/PostgreSQL, repositórios, migrations, seed, JWT, hash de senha | Application, Domain |
| `Oficina.Api` | Minimal APIs, autenticação/autorização, OpenAPI, tratamento de erros, composição (DI) | todos |

Os bounded contexts (Atendimento/OS, Estoque, Catálogo, Clientes e Veículos, Identidade) viram **pastas/namespaces** em cada camada.

Simplificações deliberadas:
- Sem MediatR e sem AutoMapper: cada caso de uso é um método de serviço e o mapeamento para DTO é explícito (`XResponse.De(...)`). Ambos passaram a ter licença comercial em 2025 e não trazem ganho no tamanho deste MVP.
- Linguagem ubíqua em português no domínio (`OrdemDeServico`, `Orcamento`), termos técnicos em inglês (`Repository`, `DbContext`).

## Consequências

- (+) Domínio sem dependência de framework: testável com testes unitários puros.
- (+) Um único processo e um único banco: deploy e transações simples.
- (–) Os contextos compartilham o mesmo banco; a separação é lógica, não física. Uma futura extração para serviços exigiria separar esquemas.
