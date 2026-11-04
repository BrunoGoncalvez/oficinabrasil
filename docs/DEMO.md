# Massa de demonstração

Dados fictícios com nomes, preços e cenários plausíveis. Os CPFs têm dígitos verificadores válidos apenas para testar as validações; não representam identidades verificadas. Os e-mails usam o domínio reservado `example.test`.

## Popular o MySQL configurado na aplicação

Pare a execução da API antes do build. No terminal da pasta da solução:

```powershell
dotnet run --project src/OficinaBrasil.API -- --seed-demo
```

O perfil de execução já define `Development`. O comando cria o banco quando necessário, adiciona a tabela de orçamentos caso ela ainda não exista, grava a massa em transação e encerra. Depois, inicie a API normalmente. A seed é exclusiva de Development, não apaga dados, não altera senhas de usuários existentes e não roda automaticamente a cada inicialização. Repetir o comando preserva a massa e as decisões já realizadas. Para recomeçar do zero, use outro banco de desenvolvimento.

São criados 4 clientes, 4 veículos, 4 serviços, 4 peças e 8 OS. Os cadastros coincidentes são reaproveitados. Há estoque suficiente para as OS de demonstração e um filtro de ar sem saldo para testar falta de peças. As baixas das OS em execução/finalizadas/entregues possuem movimentações correspondentes.

## Logins de demonstração

Use `POST /api/auth/login` com `email` e `password`. A senha inicial de todos é `Demo@12345` (pode ser definida por `SeedDemo__Password` antes da primeira execução).

| E-mail | Papel |
|---|---|
| `admin@oficina.demo` | Administrador |
| `atendente@oficina.demo` | Atendente |
| `mecanico@oficina.demo` | Mecânico |
| `estoquista@oficina.demo` | Estoquista |

No Swagger, copie o `accessToken` para **Authorize**.

## OS prontas para explorar

| ID da OS | Estado inicial |
|---|---|
| `de000000-0000-0000-0004-000000000001` | RECEBIDA — começar o fluxo aqui |
| `de000000-0000-0000-0004-000000000002` | EM_DIAGNOSTICO — pronta para cadastrar orçamento |
| `de000000-0000-0000-0004-000000000003` | AGUARDANDO_APROVACAO — e-mail pendente para a caixa local |
| `de000000-0000-0000-0004-000000000004` | APROVADA — pronta para iniciar execução |
| `de000000-0000-0000-0004-000000000005` | EM_EXECUCAO |
| `de000000-0000-0000-0004-000000000006` | FINALIZADA |
| `de000000-0000-0000-0004-000000000007` | ENTREGUE |
| `de000000-0000-0000-0004-000000000008` | CANCELADA por reprovação do orçamento |

Os cenários respondidos possuem histórico simulado; apenas a OS em espera possui e-mail pendente. O token dela vale 72 horas a partir da seed. A seed não renova links ao ser repetida.

Se o orçamento pendente expirar, use `POST /api/orders/{id}/estimate/reissue` como atendente/mecânico/administrador. O novo link invalida o anterior e preserva os valores; orçamento já respondido não pode ser reemitido.

## Percorrer o fluxo completo

Antes de abrir a OS, o atendente pode localizar o cliente por CPF ou CNPJ:

```http
GET /api/customers/by-document?document=90000000175
```

Esse é o CPF da primeira cliente da seed. A rota exige login de atendente ou administrador. Aceita documento com ou sem pontuação (no CNPJ, use o parâmetro de query para permitir a barra). A validação existente confere tamanho, sequências repetidas e dígitos verificadores, normalizando para apenas números antes da busca. Retorna `200` com o cliente, `404` quando o documento é válido mas não está cadastrado, ou `400` quando é inválido/ausente.

1. Entre como mecânico. Na primeira OS, chame `PATCH /api/orders/de000000-0000-0000-0004-000000000001/status`:

```json
{ "status": 1 }
```

2. Cadastre o orçamento com `POST /api/orders/de000000-0000-0000-0004-000000000001/estimate`:

```json
{
  "diagnosis": "Pastilhas dianteiras no limite de desgaste. Discos em boas condições.",
  "services": [{ "serviceId": "de000000-0000-0000-0003-000000000002" }],
  "parts": [{ "partId": "de000000-0000-0000-0005-000000000003", "quantity": 1 }]
}
```

O total é R$ 400,00 (R$ 180,00 de mão de obra + R$ 220,00 de peças). A OS passa para `AGUARDANDO_APROVACAO`.

3. Abra o e-mail no Mailpit e escolha aprovar ou reprovar. Confirme na página aberta pelo link. Aprovar muda para `APROVADA`; reprovar muda para `CANCELADA`.
4. Se aprovada, como mecânico, envie `{ "status": 3 }` para iniciar execução e baixar uma unidade do estoque; depois `{ "status": 4 }` para finalizar.
5. Entre como atendente ou administrador e envie `{ "status": 5 }` para registrar a entrega.

Para o cliente acompanhar por API, o atendente chama `POST /api/orders/{id}/tracking-link` e compartilha o `path` retornado. O cliente usa esse caminho sem JWT de funcionário; ele contém token com validade de 30 dias. Documento + ID da OS não autorizam mais a consulta pública.

## Ver os e-mails localmente

Com Docker disponível, suba apenas a caixa local:

```powershell
docker compose -f docker-compose.yml -f docker-compose.demo.yml up -d mailpit
$env:Smtp__Host = 'localhost'
$env:Smtp__Port = '1025'
$env:Smtp__EnableSsl = 'false'
$env:Smtp__From = 'oficina@example.test'
$env:Estimates__PublicBaseUrl = 'https://localhost:65371'
dotnet run --project src/OficinaBrasil.API
```

Abra [a caixa local](http://localhost:8025). O worker busca mensagens a cada 30 segundos. Os e-mails de teste ficam capturados no Mailpit. Use a porta efetiva da API em `Estimates__PublicBaseUrl` se ela for diferente no seu ambiente.

O `appsettings.Development.json` já configura SMTP local em `localhost:1025`, sem TLS, para funcionar também quando a API é iniciada pela IDE. As variáveis do exemplo são opcionais para esse cenário e valem apenas para o terminal/processo em que foram definidas. Se o envio continuar pendente, confirme que a API usa `Development`, reinicie a execução pela IDE e confira se há variáveis `Smtp__*` ou user-secrets sobrescrevendo o arquivo. No Docker, o overlay troca o host para `mailpit`, pois `localhost` dentro do contêiner aponta para a própria API.

Para executar todo o ambiente por Docker, incluindo banco e API:

```powershell
docker compose -f docker-compose.yml -f docker-compose.demo.yml up -d mysql mailpit
docker compose -f docker-compose.yml -f docker-compose.demo.yml run --build --rm api --seed-demo
docker compose -f docker-compose.yml -f docker-compose.demo.yml up -d api
```

Nesse caso a API fica em `http://localhost:8080/swagger`, a caixa em `http://localhost:8025`, e a massa é criada no banco do Docker (independente do MySQL local).
