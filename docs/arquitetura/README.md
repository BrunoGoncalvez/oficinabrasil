# Arquitetura C4 — OficinaBrasil

**Diagrama disponibilizado pelo grupo:** [acessar C4 no Google Drive](https://drive.google.com/file/d/1H27tvh5jXve668DbhFRELxEavZPblo2P/view?usp=sharing).

**Arquivo completo no repositório:** [abrir ou baixar OficinaBrasil-C4.drawio](OficinaBrasil-C4.drawio). Esta é a versão fornecida pelo grupo, com as três abas verificadas no arquivo: **C4 - Context**, **C4 - Container** e **C4 - Components**. Ela substitui o desenho inicial usado como referência.

A prévia do Google Drive pode mostrar apenas uma página. Para consultar todas as abas, baixe o arquivo completo e abra-o no draw.io. As permissões do link externo ainda precisam ser conferidas sem login.

## Abrir no draw.io

1. Abra o diagrams.net/draw.io.
2. Use **Arquivo → Abrir de → Dispositivo** e selecione `OficinaBrasil-C4.drawio`.
3. Navegue pelas abas **C4 - Context**, **C4 - Container** e **C4 - Components**.
4. Ajuste a disposição e os conectores conforme necessário. Para compartilhar, exporte cada página como SVG, PNG ou PDF e mantenha também o `.drawio` editável no repositório.

## Nível 1 — Contexto

**Pergunta:** quem usa o sistema e com quais sistemas externos ele se relaciona?

- À esquerda: funcionários e cliente.
- No centro: OficinaBrasil como um único sistema.
- À direita: serviço externo de e-mail.
- Relações: funcionários operam o atendimento; cliente acompanha e decide o orçamento; sistema solicita envio de e-mail; serviço de e-mail entrega a mensagem ao cliente.

Não apresente banco, controllers ou frameworks nesta página. Ela explica o sistema para uma pessoa de negócio.

### Elementos definidos para o nível 1

| Elemento | Tipo | Descrição |
|---|---|---|
| Funcionário | Pessoa | Atendente, mecânico, estoquista ou administrador que realiza as atividades da oficina conforme seu papel. |
| Cliente | Pessoa | Proprietário do veículo que acompanha a ordem de serviço e aprova ou reprova o orçamento. |
| OficinaBrasil | Sistema de software | Gerencia cadastros, OS, orçamento, estoque, execução e entrega. |
| Serviço de e-mail | Sistema de software externo | Serviço externo responsável pela entrega dos e-mails. |

| Origem | Destino | Texto da relação |
|---|---|---|
| Funcionário | OficinaBrasil | Gerencia cadastros e atendimento conforme suas permissões. |
| Cliente | OficinaBrasil | Consulta o andamento da OS e aprova ou reprova o orçamento. |
| OficinaBrasil | Serviço de e-mail | Solicita envio de orçamento. |
| Serviço de e-mail | Cliente | Entrega orçamento com links de decisão. |

Cada relação deve ter um conector independente e sua própria descrição. O serviço SMTP é externo ao OficinaBrasil; o worker que solicita o envio pertence à API e aparece no nível 3.

Os três níveis estão disponíveis no arquivo completo fornecido pelo grupo, vinculado no início desta página. As tabelas servem como apoio à leitura dos diagramas.

## Nível 2 — Contêineres

**Pergunta:** quais aplicações e armazenamentos formam o sistema?

Dentro da fronteira OficinaBrasil, coloque **API ASP.NET Core 8** e **banco MySQL 8**. Fora dela, mantenha as pessoas e o serviço de e-mail. Identifique os protocolos nas relações: HTTP/JSON, tokens de acesso, protocolo MySQL e SMTP.

Um contêiner C4 representa uma aplicação ou armazenamento. Não corresponde obrigatoriamente a um contêiner Docker. Os quatro projetos de produção compõem a mesma API; o worker e o Swagger não são aplicações implantadas separadamente. Swagger e Postman são ferramentas de acesso, sem um frontend próprio no MVP.

O Compose local usa HTTP. O diagrama diferencia esse ambiente do uso de HTTPS em uma publicação. O servidor SMTP é externo ao sistema lógico; na demonstração ele é representado pelo Mailpit, mesmo quando iniciado pelo mesmo Compose.

### Modelo para replicar o nível 2 no draw.io

Use o artigo indicado pelo grupo como referência de detalhamento, adaptando o exemplo ao monólito existente. Desenhe um retângulo tracejado **OficinaBrasil [Sistema de software]**. Dentro dele, coloque apenas os dois contêineres abaixo; pessoas e SMTP ficam fora.

| Bloco | Tipo e tecnologia | Descrição para digitar |
|---|---|---|
| API OficinaBrasil | Contêiner: C# / ASP.NET Core 8 | Processa cadastros, ordens de serviço, orçamentos, estoque e autenticação. Envia orçamentos por um worker no mesmo processo. |
| Banco de dados OficinaBrasil | Contêiner: MySQL 8 | Armazena clientes, veículos, funcionários, serviços, peças, ordens de serviço, orçamentos e movimentações de estoque. |
| Serviço de e-mail | Sistema de software externo: SMTP | Recebe mensagens da API e entrega os orçamentos ao cliente. |

Use um cilindro para o banco. Posicione os funcionários e o cliente acima ou à esquerda, a API no centro, o banco abaixo da API e o serviço de e-mail à direita, fora da fronteira.

| Origem | Destino | Texto da seta |
|---|---|---|
| Funcionário | API OficinaBrasil | Gerencia cadastros e atendimento — HTTP/JSON; JWT nas rotas protegidas |
| Cliente | API OficinaBrasil | Consulta a OS e confirma a decisão do orçamento — HTTP/JSON ou HTML/formulário; token específico |
| API OficinaBrasil | Banco de dados OficinaBrasil | Consulta e grava dados — EF Core/Pomelo; protocolo MySQL |
| API OficinaBrasil | Serviço de e-mail | Envia orçamento — SMTP |
| Serviço de e-mail | Cliente | Entrega orçamento com links de decisão — e-mail |

Use HTTPS no ambiente publicado; o Compose de demonstração utiliza HTTP. TLS no SMTP depende da configuração do ambiente. Cada relação deve ter seu próprio conector, sem junção das setas de pessoas.

Referências: [artigo indicado pelo grupo](https://medium.com/@thiagocapaverde/documenta%C3%A7%C3%A3o-c4-model-uma-abordagem-pr%C3%A1tica-e-aprofundada-no-n%C3%ADvel-de-container-d63911555619) e [definição oficial do diagrama de contêineres C4](https://c4model.com/diagrams/container).

## Nível 3 — Componentes

**Pergunta:** como a API se divide internamente e quem utiliza cada responsabilidade?

Amplie o contêiner da API. O banco e o servidor SMTP ficam fora dessa fronteira.

| Componente | Elementos do código |
|---|---|
| Autenticação e autorização | `AuthController`, JWT Bearer, `PasswordHasher` e papéis |
| Controllers de cadastro | `CustomersController`, `VehiclesController`, `ServicesController`, `PartsController` |
| Controllers de atendimento | `OrdersController`, `EstimatesController`, `TrackingController` |
| Casos de uso de cadastro | `CustomerUseCases`, `VehicleUseCases`, `ServiceCatalogUseCases`, `InventoryUseCases` |
| Casos de uso de atendimento | `ServiceOrderUseCases`, `EstimateUseCases` |
| Modelo de domínio | Entidades, objetos de valor, estados e validações |
| Persistência | Contrato `IWorkshopData`, implementação `WorkshopData`, repositórios e `WorkshopDbContext` |
| Acesso de acompanhamento | `TrackingAccess` e Data Protection |
| Processador de e-mails | `EstimateEmailWorker`, executado como `BackgroundService` |

Esses agrupamentos evitam desenhar cada classe isoladamente. As setas mostram chamadas ou uso durante a execução, e não o grafo de referências dos projetos. `IWorkshopData` é definido em Application e implementado em Infrastructure; sua inclusão no agrupamento de persistência identifica o contrato de acesso, sem mudar sua localização no código.

Para replicar, organize controllers no topo, casos de uso no meio e domínio e persistência abaixo. Coloque `TrackingAccess` e o worker à direita. Conecte cada componente apenas aos elementos que ele utiliza, usando as legendas do arquivo.

### Pontos que precisam permanecer fiéis ao código

- Controllers também consultam e atualizam dados por `IWorkshopData`; nem toda chamada passa por um caso de uso.
- O domínio não acessa o banco: os casos de uso coordenam as regras e a gravação.
- O orçamento grava a mensagem pendente no banco; o worker consulta o `WorkshopDbContext` diretamente e envia por SMTP após o commit.
- O worker pertence à API. Não existe broker, microsserviço de notificação ou execução distribuída da fila.
- JWT é utilizado por funcionários. Acompanhamento e decisão usam seus tokens específicos.

## Referências do projeto

- [Composição e configuração da API](../../src/OficinaBrasil.API/Program.cs).
- [Contrato de persistência](../../src/OficinaBrasil.Application/Interfaces/IWorkshopData.cs).
- [Worker de e-mail](../../src/OficinaBrasil.API/Services/EstimateEmailWorker.cs).
- [Modelo DDD](../ddd/README.md), [requisitos](../REQUISITOS.md) e [ADRs](../ADRs/README.md).

O arquivo fornecido foi conferido quanto à estrutura XML e à presença das três abas. Para uma visualização sem editor, exporte um PDF com a opção de todas as páginas e confira as três páginas antes de publicar.
