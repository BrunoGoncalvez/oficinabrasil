# Requisitos do OficinaBrasil

O MVP organiza o atendimento da oficina, do cadastro à entrega do veículo. Esta página resume os requisitos da Fase 1 e como verificá-los na solução.

## Requisitos funcionais

Requisitos funcionais descrevem o que o sistema deve fazer.

| ID | Requisito | Como verificar |
|---|---|---|
| RF-01 | Cadastrar, consultar, atualizar e excluir clientes; localizar cliente por CPF/CNPJ | Executar o CRUD e a busca por documento em `/api/customers` |
| RF-02 | Cadastrar, consultar, atualizar e excluir veículos com cliente, placa, marca, modelo e ano | Executar o CRUD em `/api/vehicles` |
| RF-03 | Manter o catálogo de serviços e seus preços | Executar o CRUD em `/api/services` |
| RF-04 | Manter peças e insumos e controlar entradas e saídas de estoque | Executar o CRUD e as movimentações em `/api/parts`; verificar saldo e rejeição de saldo negativo |
| RF-05 | Abrir OS para um veículo e incluir serviços, peças e quantidades | Criar uma OS em `/api/orders`; itens preliminares podem ser informados na abertura e definidos no orçamento |
| RF-06 | Calcular o orçamento com os preços dos serviços e peças e disponibilizá-lo ao cliente | Criar orçamento em `/api/orders/{orderId}/estimate`; conferir total e e-mail enviado pelo SMTP configurado |
| RF-07 | Registrar aprovação ou reprovação do orçamento pelo cliente | Confirmar a decisão pelo link recebido; verificar OS aprovada ou cancelada |
| RF-08 | Controlar os estados da OS e impedir transições inválidas | Percorrer recebimento, diagnóstico, espera de aprovação, execução, finalização e entrega; tentar pular uma etapa |
| RF-09 | Permitir consulta do andamento da OS pelo cliente | Gerar o link e consultar `/api/tracking/{id}` com o token específico |
| RF-10 | Listar e detalhar OS e calcular o tempo médio de execução | Consultar `/api/orders`, uma OS por ID e `/api/orders/metrics/average-execution-hours` |

O orçamento também admite reemissão do link enquanto não respondido. O [guia Postman](postman/README.md) orienta a execução das requisições e suas dependências.

## Requisitos não funcionais e restrições técnicas

Estes requisitos descrevem qualidades esperadas e restrições de implementação do desafio.

| ID | Requisito | Como verificar |
|---|---|---|
| RNF-01 | Organizar o back-end como monólito em camadas | Conferir API, Application, Domain e Infrastructure e o [ADR da stack](ADRs/ADR-002-stack-tecnologica.md) |
| RNF-02 | Proteger APIs administrativas com JWT e autorização | Validar login, retorno 401 sem autenticação e 403 para papel sem permissão |
| RNF-03 | Validar CPF/CNPJ e placa antes de persistir cadastros | Enviar valores válidos e inválidos e conferir os testes de validação |
| RNF-04 | Garantir integridade transacional nos fluxos principais | Verificar que falhas não deixam alterações parciais de orçamento, decisão ou baixa de estoque; conferir testes de rollback e concorrência |
| RNF-05 | Expor API REST com documentação dos contratos | Acessar Swagger em Development e importar a coleção Postman |
| RNF-06 | Disponibilizar testes unitários e de integração, com ao menos 80% de cobertura nos domínios críticos | Executar `scripts/verify.ps1` e consultar as [evidências](evidencias/cobertura.md); o projeto aplica a meta também aos quatro projetos de produção e ao total |
| RNF-07 | Permitir execução local reproduzível com instruções, Dockerfile e Compose | Seguir o [README principal](../README.md#executar) e iniciar API e banco |
| RNF-08 | Justificar a escolha do banco e disponibilizar resultado da análise de vulnerabilidades | Consultar o [ADR do MySQL](ADRs/ADR-001-banco-de-dados-mysql.md) e executar `scripts/security.ps1` para atualizar o [relatório](evidencias/seguranca.md) |

Os critérios acima servem para validação, não declaram uma nova execução dos testes. Os relatórios registram as verificações já realizadas e devem corresponder ao commit final entregue.

## Limites do MVP

- O cliente acompanha a OS por consulta à API; não há atualização automática por push nem aplicativo próprio.
- A aprovação refere-se ao orçamento único da OS. Aprovação de reparos adicionais por versões de orçamento aparece na visão geral do problema, mas não está implementada nesta versão.
- Reserva de peças, fornecedores, pendência formal por falta de peças e notificações ao mecânico pertencem ao levantamento de negócio; o MVP usa saldo e movimentações e bloqueia a execução sem estoque suficiente.
- Não foram estabelecidas metas numéricas de latência, usuários simultâneos ou disponibilidade. Os exemplos da aula não são exigências específicas deste desafio.

## Referências e entrega

- **Fase 1 — Tech Challenge**, fornecido pelo grupo: funcionalidades nas páginas 3–4 e 9–10; requisitos técnicos nas páginas 4 e 10–11; entregáveis nas páginas 4–9.
- **14SOAT — Fase 1 — Arquiteturas de software — Aula 3**, fornecido pelo grupo: conceitos de requisitos funcionais e não funcionais e critérios de verificação nas páginas 5–6 e 10–12.

Os PDFs de aula não foram copiados para o repositório. A documentação de [DDD](ddd/README.md), os [ADRs](ADRs/README.md) e o [checklist de entrega](ENTREGA.md) complementam esta página. Vídeo, PDF único de submissão, identificação do grupo, acesso aos links e proteção da branch são providências de entrega, não funcionalidades da API.
