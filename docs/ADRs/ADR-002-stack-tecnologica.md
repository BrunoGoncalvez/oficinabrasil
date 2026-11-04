# ADR-002 — Utilizar C#/.NET e monólito em camadas

- **Status:** Aceito
- **Data do registro:** 03/11/2026
- **Escopo:** linguagem, framework, organização da aplicação e ferramentas de suporte
- **Decisão relacionada:** [ADR-001 — Banco de dados MySQL](ADR-001-banco-de-dados-mysql.md)

## Contexto

O OficinaBrasil precisa expor uma API HTTP para cadastro, atendimento, orçamento, decisão do cliente, execução e estoque. As regras incluem transições de estado, autorização por papel, validações de documento e controle de concorrência.

O projeto precisa permitir execução local, demonstração reproduzível, documentação de contratos e verificação automatizada. O escopo atual não exige serviços implantados de forma independente nem uma interface gráfica própria.

As decisões devem favorecer a separação entre regras de negócio, casos de uso, transporte HTTP e persistência, mantendo um único processo de aplicação e uma única base de dados.

## Critérios de escolha

- Expressar regras de domínio com tipos e validações explícitas.
- Disponibilizar recursos para API HTTP, autenticação e autorização.
- Integrar persistência relacional, transações e tratamento de concorrência.
- Permitir testes unitários e de integração HTTP com banco real.
- Reproduzir build, execução e verificações sem depender exclusivamente da IDE.
- Evitar a complexidade operacional de distribuição que não seja necessária ao escopo.

## Alternativas analisadas

| Decisão | Alternativas | Justificativa no contexto |
|---|---|---|
| **C#/.NET 8 e ASP.NET Core** | Java/Spring Boot; TypeScript/Node.js com NestJS | Todas são opções para uma API de negócio. A stack adotada reúne linguagem tipada, controllers, injeção de dependências, autenticação e integração com EF Core. Não há benchmark ou avaliação de produtividade comparativa registrada. |
| **Monólito em camadas** | Microsserviços; aplicação concentrada nos controllers | Um único processo permite tratar o fluxo transacional sem coordenação entre serviços. A separação em camadas mantém as regras fora dos controllers. Microsserviços adicionariam comunicação remota, implantação e consistência entre serviços sem uma necessidade atual demonstrada. |
| **EF Core com repositórios** | Dapper; acesso direto com ADO.NET | EF Core já oferece mapeamento, rastreamento de alterações e controle de concorrência usados no projeto. Dapper ou ADO.NET exigiriam implementar explicitamente parte dessa coordenação. Não há evidência de desempenho que justifique substituir o mecanismo atual. |
| **JWT para funcionários** | Sessões com cookie; provedor externo de identidade | O token Bearer atende ao consumo da API por Swagger e Postman e à autorização por papel. A implementação atual não depende de login federado. Um provedor externo passa a ser relevante se houver requisitos de SSO ou gestão centralizada de identidade. |
| **SMTP com mensagem pendente no banco** | Envio síncrono na requisição; broker e serviço de notificação separado | Persistir a pendência permite nova tentativa após falha de envio, sem manter a requisição aberta até a entrega. O escopo atual não exige infraestrutura de mensageria dedicada. |
| **Docker Compose** | Instalação manual; orquestração com Kubernetes | Compose descreve API, banco, volumes e caixa de e-mail de demonstração. A execução manual continua disponível; não há necessidade atual de um cluster para a entrega. |
| **xUnit e testes HTTP com MySQL** | Apenas testes unitários; persistência simulada em todos os testes | O conjunto adotado verifica regras isoladas e também contratos HTTP, JWT, persistência e concorrência no banco utilizado pela aplicação. |

## Decisão

Adotar a seguinte composição, correspondente à implementação atual:

| Responsabilidade | Tecnologia ou organização |
|---|---|
| Linguagem e runtime | C# e .NET 8 |
| API HTTP | ASP.NET Core com controllers |
| Organização | Monólito em camadas: API, Application, Domain e Infrastructure |
| Persistência | EF Core 8, Pomelo e MySQL 8, conforme ADR-001 |
| Identidade interna | JWT Bearer com papéis e senha armazenada por hash via `PasswordHasher` |
| Acesso público limitado | Token de decisão do orçamento e token de acompanhamento específico da OS, separados do JWT |
| Documentação dos contratos | Swagger/OpenAPI com Swashbuckle e coleção Postman versionada |
| E-mail | SMTP com processamento em segundo plano por `BackgroundService` |
| Testes | xUnit, `Microsoft.AspNetCore.Mvc.Testing` e Coverlet |
| Ambiente reproduzível | Docker e Docker Compose; Mailpit para demonstração local de e-mail |
| Integração contínua | GitHub Actions com build, testes, cobertura e auditoria de dependências |

Os controllers recebem e traduzem requisições HTTP; a Application coordena casos de uso e contratos de persistência; o Domain concentra entidades, objetos de valor e regras; a Infrastructure implementa o acesso ao banco. A API compõe as dependências na inicialização. Os contextos de negócio descritos no DDD são limites conceituais, sem implantação separada.

## Consequências

### Benefícios

- A separação de responsabilidades facilita localizar e testar regras de negócio.
- Um único processo e banco simplificam implantação e transações do fluxo de atendimento.
- Swagger e Postman permitem explorar os contratos e demonstrar a API.
- Os testes de integração exercitam o caminho HTTP até o MySQL, incluindo autenticação e autorização.
- O workflow pode repetir as verificações de qualidade a cada alteração publicada.

### Custos e limitações

- A aplicação é implantada e escalada como uma unidade. Os projetos da solução não são serviços independentes.
- A separação por projetos exige manter as dependências e impedir que regras se concentrem nos controllers ou no acesso a dados.
- Repositórios e EF Core acrescentam abstrações e exigem atenção ao rastreamento de entidades e aos limites transacionais.
- O JWT atual expira em duas horas; não há fluxo de refresh token nem mecanismo dedicado de revogação imediata dos tokens já emitidos.
- O worker SMTP deve ter uma única instância responsável pela fila atual. O envio é pelo menos uma vez e pode ser duplicado após falha; não existe garantia de entrega exatamente uma vez.
- Docker Compose descreve o ambiente de desenvolvimento e demonstração; a operação em produção exige configuração própria de segredos, disponibilidade e observabilidade.
- Versões de runtime, bibliotecas e imagens precisam de atualização e auditoria periódicas. Este ADR registra a stack adotada, sem presumir suporte indefinido.

## Condições para revisão

Reavaliar a stack ou decomposição se surgirem necessidades comprovadas de escala independente, identidade federada, processamento de mensagens por múltiplas instâncias ou operação distribuída. Mudanças de runtime e dependências devem considerar compatibilidade e execução dos testes. A revisão não deve pressupor que migrar para microsserviços, por si só, melhora o sistema.

## Evidências na implementação

- [Composição da aplicação, JWT, Swagger e worker](../../OficinaBrasil.API/Program.cs).
- [Dependências da API](../../OficinaBrasil.API/OficinaBrasil.API.csproj).
- [Dependências e ferramentas de teste](../../OficinaBrasil.Domain.Tests/OficinaBrasil.Domain.Tests.csproj).
- [Modelagem e limites dos contextos](../ddd/README.md).
- [Docker Compose](../../docker-compose.yml) e [Mailpit de demonstração](../../docker-compose.demo.yml).
- [Workflow de qualidade](../../.github/workflows/quality.yml).
- [Coleção e guia Postman](../postman/README.md).
