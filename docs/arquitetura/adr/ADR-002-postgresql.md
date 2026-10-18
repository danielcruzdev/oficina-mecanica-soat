# ADR-002 — PostgreSQL como banco de dados

- **Status:** Aceita
- **Data:** 2026-10-02

## Contexto

O enunciado deixa o banco à escolha, desde que justificado. O sistema tem dados fortemente relacionais (cliente → veículos → OS → itens → peças/serviços), unicidade obrigatória (CPF/CNPJ, placa, SKU) e um fluxo crítico em que **aprovar o orçamento e baixar o estoque precisam ser atômicos**, inclusive quando duas OS disputam a mesma peça.

## Decisão

Usar **PostgreSQL 17** (imagem `postgres:17-alpine`) com EF Core 10 + Npgsql.

| Critério | Por que pesa | PostgreSQL |
|---|---|---|
| Transações ACID | Aprovação + baixa de estoque atômicas | Nativo |
| Modelo relacional | FKs e constraints de unicidade (documento, placa, SKU, número da OS) | Constraints fortes + `CHECK (saldo >= 0)` |
| Concorrência | Duas aprovações na mesma peça | Concorrência otimista com a coluna de sistema `xmin`, suportada diretamente pelo Npgsql |
| Numeração da OS | `OS-2026-000123` sem colisão | `SEQUENCE` nativa (`numero_os_seq`) |
| Custo e licença | MVP de oficina de médio porte | Open source, sem custo |
| Docker | Ambiente local com `docker compose` | Imagem oficial leve |
| Evolução | Nuvem gerenciada no futuro | Azure Database for PostgreSQL, Amazon RDS, Cloud SQL |

## Alternativas consideradas

- **SQL Server:** familiar no ecossistema .NET, mas imagem pesada e licença paga fora da edição Developer.
- **MongoDB:** o documento da OS combina com o modelo de agregado, mas o estoque é compartilhado entre OS e pede constraints e transações entre agregados, que são o ponto forte do modelo relacional.

## Consequências

- Migrations versionadas em `src/Oficina.Infrastructure/Persistencia/Migrations`, aplicadas automaticamente no start.
- Nomes de tabelas e colunas em `snake_case` (convenção do PostgreSQL), via `EFCore.NamingConventions`.
- Preços congelados (snapshot) em colunas próprias dos itens da OS.
