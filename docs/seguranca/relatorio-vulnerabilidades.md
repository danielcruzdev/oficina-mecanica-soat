# Relatório de análise de vulnerabilidades

| Campo | Valor |
|---|---|
| Data da análise | 18/10/2026 |
| Versão analisada | branch `main`, commit `66b955e` (+ correção do CI deste relatório). Vale também para a tag `v1.0.0-fase1`: depois dessa análise só mudou documentação, sem alteração de código, dependências ou imagem |
| Escopo | Dependências NuGet (SCA), imagem Docker da API, repositório (segredos e configuração), código-fonte (SAST) |
| Saídas completas das ferramentas | [`resultados/`](resultados/) |

## Resumo

| Tipo | Ferramenta | Críticas | Altas | Médias | Baixas | Situação |
|---|---|---|---|---|---|---|
| Dependências (SCA) | `dotnet list package --vulnerable --include-transitive` | 0 | 0 | 0 | 0 | Sem achados |
| Imagem Docker | Trivy 0.76.0 (`trivy image`) | 0 | 0 | 21 | 5 | Aceitas: pacotes do SO base sem correção publicada |
| Repositório: segredos | Trivy 0.76.0 (`trivy fs --scanners secret`) | 0 | 0 | 0 | 0 | Sem achados |
| Repositório: configuração | Trivy 0.76.0 (`trivy fs --scanners misconfig`) | 0 | 0 | 0 | 1 | Aceita (ver item 3) |
| Código (SAST) | Semgrep 1.180.0 (`p/csharp`, `p/owasp-top-ten`, `p/secrets`, `p/dockerfile`) | 0 | 0 | 0 | 0 | 3 achados no CI **corrigidos** |

**Não há achados críticos ou altos em aberto.**

## 1. Dependências (SCA)

```bash
dotnet list package --vulnerable --include-transitive
```

Nenhum pacote vulnerável, direto ou transitivo, nos 6 projetos. O NuGet Audit também roda em todo `dotnet restore`, e o CI executa esse comando a cada push e PR. Saída: [`resultados/dotnet-list-vulnerable.txt`](resultados/dotnet-list-vulnerable.txt).

## 2. Imagem Docker — Trivy

```bash
docker build -t oficina-api:fase1 .
trivy image --scanners vuln oficina-api:fase1
```

- **Bibliotecas .NET da aplicação e do runtime ASP.NET Core 10.0.13:** 0 vulnerabilidades.
- **Sistema operacional da imagem base** (`mcr.microsoft.com/dotnet/aspnet:10.0`, Ubuntu 24.04): 26 vulnerabilidades, 21 médias e 5 baixas, **nenhuma com versão corrigida publicada** no momento da análise.

| Pacote do SO | Qtd. | Severidade |
|---|---|---|
| libpcre2-8-0 | 7 | Média |
| libgnutls30t64 | 5 | Média |
| dash, libc-bin, libc6, tar | 2 cada | Média |
| zlib1g | 1 | Média |
| libicu74, libsystemd0, libudev1, login, passwd | 1 cada | Baixa |

**Tratamento: aceitas, com mitigação.**
- Não há versão corrigida para nenhum dos pacotes; a correção chega por uma nova imagem base da Microsoft, e basta refazer o build.
- A API não executa esses utilitários (shell, `tar`, `login`/`passwd`) nem processa entrada do usuário com eles.
- O contêiner roda como usuário **não-root** (`app`) e expõe só a porta 8080.
- Ação contínua: rebuild periódico da imagem e reexecução do `trivy image`.

Saída: [`resultados/trivy-image.txt`](resultados/trivy-image.txt).

## 3. Repositório — Trivy (segredos e configuração)

```bash
trivy fs --scanners vuln,secret,misconfig .
```

- **Segredos:** nenhum encontrado. A chave JWT e a senha do banco vêm de variáveis de ambiente (`.env`, que está no `.gitignore`). As credenciais do README e do seed são de demonstração local, como o próprio README informa.
- **Configuração:** 1 achado baixo:

| ID | Achado | Severidade | Tratamento |
|---|---|---|---|
| DS-0026 | Dockerfile sem instrução `HEALTHCHECK` | Baixa | **Aceito.** A imagem `aspnet` não traz `curl`/`wget`, e adicionar um só para o healthcheck aumentaria a superfície de ataque. A saúde é exposta em `GET /health` (inclui o banco) para o orquestrador consultar, e o `docker-compose.yml` já usa healthcheck no PostgreSQL para ordenar a subida |

Saída: [`resultados/trivy-fs.txt`](resultados/trivy-fs.txt).

## 4. Código (SAST) — Semgrep

```bash
semgrep scan --metrics=off --config p/csharp --config p/owasp-top-ten --config p/secrets --config p/dockerfile
```

101 regras em 99 arquivos.

| # | Achado | Severidade | Tratamento | Status |
|---|---|---|---|---|
| 1–3 | `github-actions-mutable-action-tag`: `actions/checkout@v4`, `actions/setup-dotnet@v4` e `actions/upload-artifact@v4` referenciados por tag mutável (risco de supply chain se a tag for reapontada) | Bloqueante | Actions fixadas pelo SHA completo do commit em `.github/workflows/ci.yml`, com a versão em comentário | **Corrigido** |

Nenhum achado no código C#. Reexecução após a correção: **0 achados**. Saída: [`resultados/semgrep.txt`](resultados/semgrep.txt).

## 5. Controles de segurança implementados

| Ameaça (OWASP API Top 10 2023) | Controle | Evidência |
|---|---|---|
| API1 — BOLA | Rotas `/me/*` filtram pelo `cliente_id` do token; OS de outro cliente → 404 | Teste `Cliente_nao_ve_nem_aprova_os_de_outro_cliente_404` |
| API2 — Autenticação quebrada | JWT HS256 com expiração de 60 min e validação de issuer/audience/lifetime/algoritmo; PBKDF2; rate limit de 5 logins/min por IP | `Seguranca.cs`, testes de 401 |
| API3 — Exposição de propriedades | DTOs de saída explícitos; `senha_hash` nunca é retornado | `*Response` na Application |
| API4 — Consumo irrestrito | Paginação com no máximo 100 itens; rate limit no login | `Paginacao` |
| API5 — Autorização por função | Políticas por papel em cada grupo de rotas | Testes de 403 |
| API8 — Configuração incorreta | ProblemDetails sem stack trace; segredos por variável de ambiente; contêiner não-root | Teste `Erro_nao_expoe_stack_trace` |

## 6. Como reproduzir

Sem instalar nada além do Docker:

```bash
docker build -t oficina-api:fase1 .
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock aquasec/trivy:0.76.0 image --scanners vuln oficina-api:fase1
docker run --rm -v "$PWD":/src aquasec/trivy:0.76.0 fs --scanners vuln,secret,misconfig /src
docker run --rm -v "$PWD":/src -w /src semgrep/semgrep:1.180.0 semgrep scan --metrics=off \
  --config p/csharp --config p/owasp-top-ten --config p/secrets --config p/dockerfile
dotnet list package --vulnerable --include-transitive
```
