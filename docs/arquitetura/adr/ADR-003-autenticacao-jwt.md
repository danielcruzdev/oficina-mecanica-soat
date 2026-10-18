# ADR-003 — Autenticação JWT para funcionários e clientes

- **Status:** Aceita
- **Data:** 2026-10-03

## Contexto

As APIs administrativas exigem autenticação JWT. Além disso, o cliente precisa consultar e aprovar o orçamento da própria OS pela API. Os dois públicos têm permissões muito diferentes, e o cliente nunca pode enxergar a OS de outro cliente (OWASP API1 — BOLA).

## Decisão

- Um único emissor de JWT (HS256, chave ≥ 256 bits vinda de variável de ambiente, expiração de 60 min, validação de issuer, audience, lifetime e algoritmo).
- Dois endpoints de login:
  - `POST /api/v1/auth/login` — funcionário (login + senha).
  - `POST /api/v1/auth/clientes/login` — cliente (CPF/CNPJ + senha).
- Claims: `sub` (usuário), `role` (`Administrador`, `Atendente`, `Mecanico`, `Cliente`) e, para clientes, `cliente_id`.
- Autorização por **políticas** em cada grupo de rotas (`Administrador`, `Atendimento`, `Oficina`, `Funcionario`, `Cliente`).
- Rotas do cliente em `/api/v1/me/...`: o `cliente_id` vem **sempre do token**. A OS de outro cliente responde **404**, como se não existisse.
- Senhas com PBKDF2 (`PasswordHasher` do ASP.NET Core Identity) e rate limit de 5 tentativas de login por minuto por IP.

## Consequências

- (+) Um mecanismo só, sem serviço de identidade externo.
- (+) O teste de integração de BOLA garante o isolamento entre clientes.
- (–) Sem refresh token nem revogação: ao expirar (60 min), é preciso logar de novo. Aceitável no MVP.
- (–) A senha inicial do cliente é definida no cadastro; um fluxo de "primeiro acesso" fica para fase futura.
