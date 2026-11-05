# OficinaBrasil — FIAP Tech Challenge Fase 1

Monólito modular simples, em **.NET 8 / ASP.NET Core**, com **MySQL 8**, EF Core, Swagger e JWT para funcionários.

**Objetivo:** substituir o controle manual de atendimento por um back-end que registra clientes/veículos, diagnóstico, orçamento, autorização, execução, estoque e entrega. A solução usa um processo ASP.NET Core e um banco relacional; os contextos de negócio são descritos na [documentação DDD](docs/ddd/README.md).

## Apresentação e documentação

Este é o README principal do projeto. Os guias específicos ficam em `docs`.

| Material | Acesso |
|---|---|
| Vídeo de apresentação | **Pendente: inserir o link após a publicação** |
| Quadro Miro | [Domain Storytelling e Event Storming](https://miro.com/app/board/uXjVGE0G3xk=/) |
| Modelagem do domínio | [DDD, contextos e diagramas](docs/ddd/README.md) |
| Requisitos | [Funcionais, não funcionais e limites do MVP](docs/REQUISITOS.md) |
| Arquitetura C4 | [Arquivo completo — três abas](docs/arquitetura/OficinaBrasil-C4.drawio) · [Google Drive](https://drive.google.com/file/d/1H27tvh5jXve668DbhFRELxEavZPblo2P/view?usp=sharing) · [Guia dos níveis 1 a 3](docs/arquitetura/README.md) |
| Domain Storytelling | [Atendimento, fluxo mecânico e estoque](docs/ddd/storytelling/README.md) |
| Event Storming | [Brainstorming, linha do tempo, comandos, políticas e agregados](docs/ddd/event-storming/README.md) |
| Decisões arquiteturais | [ADRs: banco de dados e stack tecnológica](docs/ADRs/README.md) |
| Linguagem ubíqua | [Glossário do domínio](docs/ddd/linguagem-ubiqua.md) |
| Postman | [Importar coleção](docs/postman/OficinaBrasil.postman_collection.json) · [Como utilizar](docs/postman/README.md) |
| Guia de atendimento | [Fluxo de atendimento em PDF](docs/guias/OficinaBrasil-Fluxo-Atendimento.pdf) |
| Dados de demonstração | [Preparar e explorar os cenários](docs/DEMO.md) |
| Qualidade e segurança | [Resultado da análise SonarQube](docs/evidencias/sonar.md) |

## Funcionalidades

- Cadastro de clientes, veículos, serviços e peças.
- Identificação do cliente por CPF/CNPJ.
- Abertura da OS, diagnóstico e orçamento com preços do catálogo.
- Envio do orçamento por e-mail e aprovação ou reprovação pelo cliente.
- Execução com baixa de estoque, finalização e entrega.
- Acompanhamento público por link específico da OS e consulta do tempo médio de execução.
- Autenticação de funcionários e autorização por papel.

## Organização

```text
src/OficinaBrasil.API/                  Controllers, autenticação e serviços de execução
src/OficinaBrasil.Application/          Casos de uso, DTOs e interfaces
src/OficinaBrasil.Domain/               Entidades, objetos de valor e regras de negócio
src/OficinaBrasil.Infrastructure/       Persistência e repositórios
tests/OficinaBrasil.Domain.Tests/       Testes unitários e de integração
database/                      SQL para atualização de bancos existentes
docs/                          Modelagem, guias, Postman e evidências
scripts/                       Verificação de cobertura e auditoria NuGet
.github/workflows/                      Integração contínua
```

As pastas locais `tmp`, `.artifacts`, `.vs`, `bin` e `obj` são ignoradas pelo Git. Não fazem parte da entrega. Os scripts são usados tanto localmente quanto pelo workflow de qualidade.

- `src/OficinaBrasil.API/Controllers`: um controller por recurso; `AuthController` para funcionários.
- `src/OficinaBrasil.Application/UseCases`: casos de uso separados por funcionalidade.
- `src/OficinaBrasil.Application/DTOs`: contratos de entrada da API.
- `src/OficinaBrasil.Application/Interfaces`: contratos de persistência da aplicação.
- `src/OficinaBrasil.Domain/Entities`: uma entidade por arquivo; `Enums`, `Exceptions` e `Validation`.
- `src/OficinaBrasil.Infrastructure/Repositories`: repositórios individuais; `Persistence` contém o DbContext e consultas compostas.
- `tests/OficinaBrasil.Domain.Tests`: testes unitários e de integração HTTP com MySQL real e SMTP local capturado.

**Fluxo:** HTTP → Controller → UseCase → entidades/regras de domínio → contrato de repositório → implementação EF Core → MySQL.

## Executar

### Com Docker

Na raiz do projeto, com Docker e Docker Compose disponíveis:

```powershell
docker compose up --build -d
```

Acesse o Swagger em `http://localhost:8080/swagger`. O compose inicia a API e o MySQL; as credenciais de desenvolvimento estão em `docker-compose.yml`.

Para demonstrar também os e-mails de orçamento, utilize o compose com Mailpit:

```powershell
docker compose -f docker-compose.yml -f docker-compose.demo.yml up --build -d
```

Nesse modo, os e-mails ficam disponíveis em `http://localhost:8025`. Para encerrar os serviços, execute `docker compose -f docker-compose.yml -f docker-compose.demo.yml down`.

### Com .NET e MySQL locais

Para popular o banco com dados de demonstração e testar o fluxo completo, consulte [docs/DEMO.md](docs/DEMO.md). Com o MySQL configurado, execute `dotnet run --project src/OficinaBrasil.API -- --seed-demo`.

1. Tenha .NET 8 SDK e MySQL 8 disponíveis.
2. Configure as variáveis `ConnectionStrings__MySql`, `Jwt__Key`, `SeedAdmin__Email` e `SeedAdmin__Password`, ou crie `src/OficinaBrasil.API/appsettings.Local.json` para suas configurações privadas. Esse arquivo é carregado somente em Development, ignorado pelo Git e excluído da publicação. Variáveis de ambiente e argumentos de linha de comando têm prioridade sobre ele.
3. `dotnet restore OficinaBrasil.sln` e `dotnet build OficinaBrasil.sln`.
4. `dotnet run --project src/OficinaBrasil.API` (ou `docker compose up --build`).
5. Swagger no endereço fornecido pelo host em ambiente Development.

Exemplo de estrutura do arquivo privado (preencha os valores antes de executar):

```json
{
  "ConnectionStrings": { "MySql": "SUA_CONEXAO_MYSQL" },
  "Jwt": { "Key": "SUA_CHAVE_ALEATORIA_COM_PELO_MENOS_32_BYTES" },
  "SeedAdmin": { "Email": "admin@oficina.local", "Password": "SUA_SENHA_INICIAL" }
}
```

O MySQL do Compose está disponível em `localhost:3307`; para iniciá-lo separadamente, execute `docker compose up -d mysql`. Se estiver usando MySQL instalado na máquina, informe a porta e as credenciais dessa instalação. O administrador inicial é criado somente quando ainda não há usuários no banco.

O banco é criado via `EnsureCreated`, somente para facilitar o MVP. Para ambientes reais, substitua por migrations do EF Core.

### Decisão do banco de dados

A escolha do **MySQL 8**, o contexto, as alternativas e suas consequências estão no [ADR-001 — Banco de dados](docs/ADRs/ADR-001-banco-de-dados-mysql.md).

A escolha de **C#/.NET, ASP.NET Core e monólito em camadas**, com as ferramentas de persistência, autenticação, testes e execução, está no [ADR-002 — Stack tecnológica](docs/ADRs/ADR-002-stack-tecnologica.md). O [índice de ADRs](docs/ADRs/README.md) reúne os registros e a orientação para futuras revisões.

### Swagger e autenticação

A [coleção Postman](docs/postman/OficinaBrasil.postman_collection.json) cobre as 35 rotas da API, com autenticação automática, variáveis e exemplos organizados por módulo. Consulte o [guia de importação e execução](docs/postman/README.md).

Execução local pelo perfil: `https://localhost:65371/swagger` (confira a porta exibida no terminal). Com Docker: `http://localhost:8080/swagger`. `POST /api/auth/login` retorna `accessToken`; use-o no botão **Authorize**. O administrador inicial é configurado por `SeedAdmin__Email` e `SeedAdmin__Password`; a [seed de demonstração](docs/DEMO.md) cria contas por papel. Credenciais de exemplo são exclusivas de desenvolvimento.

O mecânico pode ler serviços e peças para montar o orçamento, mas só atendente/administrador alteram serviços e só estoquista/administrador alteram peças e estoque. APIs administrativas exigem JWT. As rotas públicas de decisão e acompanhamento exigem seus tokens específicos.

## Fluxo OS

### Diagnóstico, orçamento e decisão do cliente

1. `POST /api/orders` abre a OS em `RECEBIDA`; serviços e peças podem ficar vazios nesta etapa. Itens informados na abertura são preliminares.
2. O mecânico chama `PATCH /api/orders/{id}/status` com `{ "status": 1 }` para iniciar `EM_DIAGNOSTICO`.
3. O mecânico/administrador chama `POST /api/orders/{id}/estimate` com o diagnóstico e os itens definitivos:

```json
{
  "diagnosis": "Pastilhas dianteiras desgastadas",
  "services": [{ "serviceId": "GUID-DO-SERVICO" }],
  "parts": [{ "partId": "GUID-DA-PECA", "quantity": 2 }]
}
```

Os preços são obtidos do catálogo no servidor e registrados no orçamento. O cadastro substitui os itens preliminares, salva o diagnóstico, muda a OS para `AGUARDANDO_APROVACAO` e persiste o e-mail pendente na mesma transação. Há um orçamento por OS nesta versão.

4. O processamento em segundo plano envia o orçamento por SMTP, com tentativas a cada 30 segundos até a validade. O e-mail oferece **Aprovar** e **Reprovar**. Abrir o link por `GET /api/estimates/{id}/decision?token=...&approve=true|false` apenas mostra os valores e uma confirmação; não muda o status. A confirmação envia um formulário `POST` para a mesma rota, com `token` e `approve`.
5. A aprovação leva a `APROVADA`; a reprovação leva a `CANCELADA`. A decisão e o horário ficam registrados. Token inválido, expirado ou já utilizado não altera a OS. Decisões concorrentes são protegidas por controle de concorrência no orçamento. O endpoint genérico de status não permite substituir esse fluxo.

O `GET /api/orders/{id}` autenticado informa o orçamento, a decisão e `emailSentAt` (nulo enquanto pendente), sem expor token ou corpo do e-mail.

**Banco existente:** antes de iniciar a API atualizada, execute [database/001_estimates.sql](database/001_estimates.sql) no banco MySQL configurado. `EnsureCreated` cria as tabelas em bancos novos, mas não atualiza bancos existentes. Faça uma cópia do banco antes de aplicar alterações. OS antigas já em `AGUARDANDO_APROVACAO` sem orçamento precisam ser regularizadas antes de usar o novo fluxo.

**Configuração:** defina `Estimates__PublicBaseUrl` com a origem pública HTTPS da API (sem `/api`; o localhost do arquivo é apenas para desenvolvimento), `Estimates__ValidityHours` (padrão 72), `Smtp__Host`, `Smtp__Port`, `Smtp__EnableSsl`, `Smtp__From`, `Smtp__Username` e `Smtp__Password`. Use variáveis de ambiente ou user-secrets para credenciais. Para SMTP local de testes, pode-se desabilitar TLS. O cliente precisa de e-mail válido no cadastro.

O envio é assíncrono: a OS entra em espera quando o orçamento e a mensagem são gravados, e não após confirmação do SMTP. Falhas deixam a mensagem pendente. Execute um único processo responsável pela fila; há entrega pelo menos uma vez (uma interrupção após o envio pode causar repetição do e-mail). Orçamentos expirados não são enviados. Use `POST /api/orders/{id}/estimate/reissue` para renovar a validade e reenfileirar o orçamento não respondido, revogando o token anterior e preservando os valores. Configure SMTP real ou a caixa local Mailpit para observar o envio.

`RECEBIDA → EM_DIAGNOSTICO → AGUARDANDO_APROVACAO → APROVADA → EM_EXECUCAO → FINALIZADA → ENTREGUE`, com opção de cancelamento em estados permitidos. Os valores do enum original são preservados.

Os clientes possuem veículos; usuários são funcionários internos. Para disponibilizar acompanhamento ao cliente, atendente/administrador chama `POST /api/orders/{id}/tracking-link` e entrega o `path` retornado ao cliente. `GET /api/tracking/{id}?token=...` mostra o andamento sem dados pessoais. O token é limitado àquela OS e vale 30 dias; CPF/CNPJ deixou de ser credencial de consulta. Persistir as chaves de Data Protection é necessário para preservar esses links entre recriações do contêiner (o compose configura volume próprio).

## Testes, cobertura e segurança

Pré-requisitos adicionais: MySQL 8 acessível com permissão de criar/remover bancos de teste; PowerShell 7 (`pwsh`) para os scripts multiplataforma, ou Windows PowerShell com permissão de execução dos scripts locais. O usuário da aplicação no compose não tem essa permissão: para testes locais contra o MySQL do Docker, configure a conexão do usuário root na porta 3307. Não use banco de produção.

```powershell
$env:OFICINA_TEST_MYSQL = 'Server=127.0.0.1;Port=3307;User=root;Password=root_dev_only;'
dotnet test OficinaBrasil.sln
pwsh ./scripts/verify.ps1
pwsh ./scripts/security.ps1
```

Se a variável não for definida, os testes usam os parâmetros de conexão locais de `appsettings.json` + `appsettings.Development.json`, mas sempre substituem o nome do banco por `oficina_test_<GUID>`. O banco isolado é removido ao final. O worker SMTP é desativado no host de testes; um teste o inicia explicitamente contra um servidor SMTP local que não retransmite mensagens.

No Windows configurado para exigir scripts assinados, pode-se executar cada script revisado com uma exceção apenas naquele processo: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1` (ou `scripts/security.ps1`). Isso não altera a política persistente da máquina.

`verify.ps1` executa a suíte e exige **80% de cobertura de linhas em cada um dos quatro projetos de produção e no total**, considerando todos os tipos, inclusive DTOs e seed. Assim, Domain e Application (domínios críticos) também são avaliados individualmente. Gera XML Cobertura, TRX e resumo em `.artifacts/tests/`, fora do versionamento. Os testes verificam transições, cadastro, documentos, preços, aprovação/reprovação, token, reemissão, JWT/papéis, rollback, concorrência de saldo, entrega SMTP e idempotência da seed. Transações usam `SaveChanges`; saldo, status e decisão têm controle otimista de concorrência. Conflitos retornam 409.

`security.ps1` consulta alertas NuGet diretos/transitivos e falha se houver avisos. O relatório delimita o escopo da análise; não é uma declaração de ausência de qualquer vulnerabilidade. A pipeline em `.github/workflows/quality.yml` repete build, testes, cobertura e auditoria com MySQL 8 e publica evidências.

- [Resultado da análise SonarQube: segurança, testes e cobertura](docs/evidencias/sonar.md)
- [Análise do código com SonarQube local](docs/SONAR.md)
- [DDD, Event Storming, diagramas e glossário](docs/ddd/README.md)
- [Conferência dos requisitos e entregáveis](docs/ENTREGA.md)

## Limites e entrega acadêmica

O MVP não implementa reserva de peças, fornecedor, orçamento adicional/versionado ou microsserviços. A documentação DDD explicita as diferenças em relação ao brainstorming original. O orçamento é único por OS, com reemissão de link antes da decisão. O worker SMTP é de instância única; não há garantia de envio exatamente uma vez. Não há frontend obrigatório para esta fase.

Os exemplos de conexão e administrador locais estão apenas em `appsettings.Development.json` e no compose de desenvolvimento. Fora de Development, configure a conexão e uma chave JWT própria; a aplicação rejeita a chave de exemplo e não cria um administrador com senha padrão do arquivo de desenvolvimento.

Antes da submissão, preencha o link do vídeo na tabela de apresentação, confirme os integrantes e o grupo, registre a branch e o commit final e confira a lista em [docs/ENTREGA.md](docs/ENTREGA.md).
