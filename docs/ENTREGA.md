# Conferência da entrega — Fase 1

Este arquivo separa evidências técnicas existentes de providências externas. Não declara aprovação acadêmica nem inventa links ou identificação do grupo.

## Identificação da entrega

- Curso: SOAT — Software Architecture.
- Fase: 1.
- Grupo: **87**.
- Linguagem e tecnologias: C#, .NET 8, ASP.NET Core, EF Core, MySQL 8, JWT, Swagger/OpenAPI, Docker e xUnit.

| Integrante informado | RM | Discord |
|---|---|---|
| Bruno de Souza Gonçalves | Pendente | Pendente |

Confirmar se a lista de integrantes está completa. Repositório GitHub e vídeo: links ainda não informados.

## Requisitos técnicos e evidências

| Requisito | Evidência |
|---|---|
| Monólito em camadas | API, Application, Domain e Infrastructure na solução |
| REST e Swagger | Controllers e `/swagger` em Development |
| CRUDs e identificação por documento | Customers, Vehicles, Services e Parts controllers |
| OS, orçamento e decisão | Orders e Estimates controllers; testes HTTP com MySQL |
| Consulta do cliente | TrackingController com token limitado à OS |
| Estoque e transações | Concorrência EF no saldo e estado; testes de conflito e rollback |
| JWT administrativo | AuthController; testes de 401/403 e papéis |
| CPF/CNPJ e placa | Objetos de valor e Checks; testes de entradas válidas/inválidas |
| Cobertura mínima dos domínios críticos | `scripts/verify.ps1` valida a meta por projeto; [resultado SonarQube](evidencias/sonar.md) registra a cobertura da análise |
| Testes de integração | ApiIntegrationTests: HTTP + JWT + EF + MySQL real; SMTP local capturado |
| Vulnerabilidades de dependências | `scripts/security.ps1`; resultados locais em `.artifacts/security/` |
| Análise do código com SonarQube | [Resultado da análise](evidencias/sonar.md), com métricas, cobertura e achados; [reprodução local](SONAR.md) |
| Docker e execução local | Dockerfile, compose e [instruções de execução](../README.md) |
| DDD e exportações locais | [Documentação](ddd/README.md), SVGs, diagramas e glossário |
| Arquitetura C4 | [Níveis 1 a 3 e link do diagrama disponibilizado pelo grupo](arquitetura/README.md); conferir acesso público e exportação final |

## Providências do grupo antes da submissão

- Completar RM e Discord de Bruno de Souza Gonçalves e confirmar se há outros integrantes no Grupo 87.
- Publicar o repositório no GitHub, verificar acesso público e proteger a branch principal, exigindo PR.
- Identificar a versão final por branch e hash/tag; repetir os scripts de testes e auditoria nessa versão.
- Garantir que os arquivos de evidência e DDD estão no commit entregue, sem credenciais reais ou arquivos temporários.
- Validar o acesso ao Miro sem solicitação manual de permissão; a documentação local serve como cópia versionada da solução implementada.
- Validar o acesso ao C4 no Google Drive sem login e disponibilizar uma exportação em PDF das três abas do [arquivo C4](arquitetura/OficinaBrasil-C4.drawio).
- Gravar e publicar vídeo público no YouTube, de até 15 minutos, demonstrando a mesma versão do código.
- Acrescentar ao README os links definitivos do repositório e do vídeo.
- Montar o PDF único com identificação, tecnologias, repositório, branch/hash, vídeo, DDD e links para as evidências. Conferir todos os links em sessão não autenticada.

O diretório atual não contém repositório Git inicializado; não há hash de entrega verificável neste workspace. O workflow de CI está preparado, mas execução hospedada e regras de proteção só podem ser confirmadas após publicação no GitHub.

## Roteiro sugerido do vídeo

1. Identificação e problema da oficina (1 min).
2. Arquitetura, MySQL e DDD/Event Storming (3 min).
3. Login, cliente por documento, veículo e criação da OS (2 min).
4. Diagnóstico, orçamento no e-mail, aprovação e acompanhamento (3 min).
5. Execução, baixa de estoque, finalização, entrega e tempo médio (2 min).
6. Reprovação/falta de estoque e regras de permissão (1 min).
7. Testes, cobertura e auditoria de dependências (2 min).

Total sugerido: 14 minutos, reservando margem para transições. Identificar todos os integrantes e mostrar o commit utilizado.
