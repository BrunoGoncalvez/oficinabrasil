# Cobertura verificada

Todos os tipos dos quatro projetos de produção são medidos, incluindo DTOs, validações e seed; sem exclusões de classes. Domain e Application são os domínios críticos.

| Projeto | Linhas | Ramos |
|---|---:|---:|
| OficinaBrasil.Domain | 99.50% | 90.81% |
| OficinaBrasil.Application | 96.25% | 82.69% |
| OficinaBrasil.Infrastructure | 96.80% | 100.00% |
| OficinaBrasil.API | 80.68% | 62.06% |

Cobertura global de linhas: 89.10%.

Meta: mínimo de 80% de linhas em CADA projeto de produção e no total. O comando falha se qualquer um ficar abaixo da meta.

Reprodução: `pwsh ./scripts/verify.ps1`. Os testes HTTP usam MySQL real em banco isolado `oficina_test_*`, removido ao final. Configure `OFICINA_TEST_MYSQL` para escolher a conexão. O worker é exercitado contra um SMTP local sem retransmissão.

Verificação: 2026-11-04 22:18:01 -03:00.
