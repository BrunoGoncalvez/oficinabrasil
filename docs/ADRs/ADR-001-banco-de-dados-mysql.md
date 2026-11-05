# ADR-001 — Utilizar MySQL para persistência relacional

- **Status:** Aceito
- **Data do registro:** 03/11/2026
- **Escopo:** persistência dos dados do OficinaBrasil
- **Decisão relacionada:** [ADR-002 — Stack tecnológica](ADR-002-stack-tecnologica.md)

## Contexto

A oficina precisa relacionar clientes, veículos, ordens de serviço, orçamentos, serviços, peças e movimentações de estoque. Documento do cliente, placa, SKU e e-mail do funcionário devem ser únicos. Um orçamento pertence a uma OS, e seus valores precisam preservar os preços utilizados no momento de sua criação.

Operações de negócio alteram mais de um registro. A criação do orçamento grava itens, diagnóstico, estado da OS e e-mail pendente. O início da execução altera o estado da OS, baixa peças e registra movimentações. Uma falha não pode deixar apenas parte dessas alterações persistida.

Também é necessário tratar alterações concorrentes de saldo, estado e decisão do orçamento. O sistema atual é um monólito com uma base de dados, sem requisito demonstrado de distribuição geográfica ou particionamento entre bancos.

## Critérios de escolha

O **SKU (Stock Keeping Unit)** é o código interno do tipo de peça/insumo (`Part.Sku`). Um cadastro com SKU `FIL-OLEO-BOSCH-X` pode representar dez unidades iguais por meio do saldo `10`. O índice único impede cadastros com o mesmo SKU; referências de OS e movimentações utilizam a chave `Part.Id`. A definição completa está no [glossário do domínio](../ddd/linguagem-ubiqua.md).

- Representar relacionamentos e impor integridade referencial e unicidade.
- Gravar alterações relacionadas em transações.
- Integrar com o EF Core utilizado na aplicação.
- Permitir reprodução do ambiente de desenvolvimento e de testes com Docker.
- Manter uma operação compatível com o escopo do MVP, sem introduzir bancos adicionais.

## Alternativas analisadas

| Alternativa | Adequação ao contexto | Motivo da escolha ou não adoção |
|---|---|---|
| **MySQL 8** | Modelo relacional, transações e integração com EF Core por meio do Pomelo | Escolhido por atender às regras de integridade e à operação em uma única base. Já compõe o ambiente reproduzível da aplicação e dos testes. |
| **PostgreSQL** | Alternativa relacional compatível com o tipo de dados e as transações necessárias | Seria uma escolha viável. Não foi identificado requisito que justifique trocar o provedor e revalidar o esquema, o SQL e os testes da implementação atual. |
| **SQL Server** | Alternativa relacional com integração ao ecossistema .NET | Também atenderia ao domínio. Não há dependência de recursos específicos desse banco que justifique adotá-lo no lugar da base atual. |
| **SQLite** | Opção para execução embarcada e persistência em arquivo | Não adotado como banco principal: o projeto usa um serviço MySQL compartilhado e testa a persistência no mesmo mecanismo. Usar SQLite nos testes não verificaria o comportamento específico do provedor MySQL. |
| **MongoDB** | Modelo orientado a documentos | Não adotado porque os relacionamentos, restrições e atualizações entre registros são centrais neste domínio. Não foi identificado benefício que justifique mudar a modelagem e a estratégia de integridade para documentos. |

Essa comparação não estabelece superioridade geral do MySQL. PostgreSQL e SQL Server também poderiam atender ao sistema. Não foram realizados benchmarks comparativos que sustentem uma decisão por desempenho ou custo.

## Decisão

Utilizar **MySQL 8**, com **EF Core 8**, o provedor **Pomelo.EntityFrameworkCore.MySql** e o driver **MySqlConnector**, em uma única base relacional para o monólito.

Os mapeamentos devem manter chaves estrangeiras, índices únicos e precisão decimal para valores monetários. Alterações pertencentes à mesma operação de negócio devem ser salvas na mesma unidade transacional de `SaveChanges`.

O saldo da peça, o estado da OS e a versão do orçamento usam controle otimista de concorrência no EF Core. Um conflito deve produzir HTTP 409 para que o consumidor consulte os dados novamente, sem repetição automática da intenção de negócio.

O envio SMTP ocorre após a gravação do orçamento. A transação do banco protege o registro do e-mail pendente, mas não inclui a entrega ao servidor SMTP.

## Consequências

### Benefícios

- O esquema representa diretamente as relações cliente–veículo–OS–itens.
- Restrições do banco complementam as validações de aplicação e domínio.
- Transações evitam gravações parciais de orçamento, decisão e baixa de estoque.
- Docker Compose e testes de integração com MySQL permitem reproduzir o ambiente de persistência.

### Custos e limitações

- É necessário operar um serviço MySQL, com credenciais, volumes, backups e recuperação definidos conforme o ambiente.
- A aplicação depende do comportamento do provedor Pomelo e do SQL específico mantido no repositório. Uma troca de banco exige revisão e testes.
- Testes de integração precisam de MySQL e permissão para criar e remover bancos isolados.
- `EnsureCreated` atende à criação inicial do MVP, mas não atualiza bancos existentes. O projeto mantém um script SQL para a atualização de orçamentos; evolução de esquema em produção exige uma estratégia de migrations versionadas.
- Não existe transação distribuída com SMTP. O envio do e-mail pode ser repetido após uma interrupção.

## Condições para revisão

Reavaliar a decisão se houver requisito de infraestrutura que imponha outro banco, necessidade de recursos específicos não atendidos pelo desenho atual ou evidência de carga que exija mudanças. Uma revisão deve incluir testes de integridade, concorrência, migração e recuperação de dados.

## Evidências na implementação

- [Configuração do EF Core e do provedor MySQL](../../src/OficinaBrasil.API/Program.cs).
- [Mapeamentos, índices e tokens de concorrência](../../src/OficinaBrasil.Infrastructure/Persistence/WorkshopDbContext.cs).
- [Dependências de persistência](../../src/OficinaBrasil.Infrastructure/OficinaBrasil.Infrastructure.csproj).
- [Ambiente Docker e volume do banco](../../docker-compose.yml).
- [SQL para bancos existentes](../../database/001_estimates.sql).
- [Testes e evidências de cobertura](../evidencias/sonar.md).
