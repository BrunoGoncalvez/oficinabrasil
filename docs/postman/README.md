# Coleção Postman — OficinaBrasil

Importe [OficinaBrasil.postman_collection.json](OficinaBrasil.postman_collection.json) no Postman usando **Import → Files**. O arquivo inclui as variáveis necessárias; não é obrigatório importar um environment separado.

## Configuração

1. Inicie a API conforme o README da solução.
2. Nas variáveis da coleção, configure `baseUrl` (padrão `http://localhost:65372`; Docker: `http://localhost:8080`), `email` e `password` de um funcionário ativo.
3. Execute **01 | Autenticação e funcionários → Login e salvar JWT**. O script salva `accessToken`, herdado pelas requisições protegidas. O JWT expira em duas horas; faça login novamente quando necessário.

Para explorar todos os módulos, utilize administrador. O administrador inicial depende de `SeedAdmin__Email` e `SeedAdmin__Password`. Se optar pela [seed de demonstração](../DEMO.md), use as credenciais documentadas ali. Nenhuma senha real ou token está incluído na coleção. Configure `employeePassword` apenas se for cadastrar funcionário.

## Sequência sugerida

1. Crie cliente, veículo, serviço e peça, nessa ordem. Os scripts salvam `customerId`, `vehicleId`, `serviceId` e `partId` após respostas de sucesso. As listagens não sobrescrevem os IDs; para trabalhar com registros existentes, preencha as variáveis manualmente.
2. Abra uma OS: o script salva `orderId`. Inicie o diagnóstico no módulo de ordens.
3. Crie o orçamento: salva `estimateId` e coloca a OS em espera de aprovação. Os preços vêm do catálogo, sem serem informados no orçamento.
4. Com SMTP configurado, abra o e-mail do orçamento e copie o parâmetro `token` do link para `estimateToken`. A API não retorna esse segredo no cadastro nem na consulta da OS. Defina `approve` como `true` ou `false`, visualize a confirmação e envie a decisão. O POST usa formulário `application/x-www-form-urlencoded`.
5. Se aprovado, inicie a execução, finalize e registre a entrega. Se reprovado, a OS é cancelada. A reemissão é opcional para orçamentos não respondidos: invalida o token anterior e exige copiar o novo token do novo e-mail.
6. Gere um link de acompanhamento para salvar `trackingToken` e teste a consulta pública. Esse token é diferente do JWT e do token de decisão.

Os exemplos usam dados fictícios. Altere `customerDocument` (CPF/CNPJ válido), `vehiclePlate`, `partSku` e `employeeEmail` ao repetir cadastros para evitar duplicidades. O cliente de exemplo usa `example.test`: para receber o orçamento, ajuste seu e-mail para uma caixa de testes acessível ou utilize um capturador SMTP local.

A coleção foi organizada para execução manual. **Não execute toda a coleção linearmente no Runner**: há etapas que dependem de e-mail, caminhos alternativos e exclusões. A pasta final contém DELETEs opcionais; recursos vinculados podem retornar 409. A atualização de peça não altera seu saldo: use a movimentação de estoque.

## SKU no cadastro e na reposição de peças

**SKU (Stock Keeping Unit)** é o código interno único de um tipo de peça/insumo. Na coleção, a variável `partSku` preenche o campo `sku` das requisições de criação e atualização de peça. Exemplo: `FIL-OLEO-BOSCH-X` identifica um produto; dez unidades iguais usam esse mesmo SKU com `quantity: 10`.

Para criar outro produto, escolha outro SKU. Para repor unidades do mesmo produto, utilize `POST /api/parts/{{partId}}/stock` com `delta` positivo; não crie outro cadastro. Para saída manual, use `delta` negativo e informe o motivo. A atualização por PUT não altera o saldo. O campo SKU é obrigatório, e sua duplicidade é rejeitada; o banco também impõe unicidade.

O `partId` é o GUID usado nas URLs, nos itens da OS e nas movimentações; ele é diferente do SKU. Consulte também o [glossário do domínio](../ddd/linguagem-ubiqua.md).

## Cobertura e contratos

| Módulo | Rotas únicas |
|---|---:|
| Autenticação e funcionários | 2 |
| Clientes | 6 |
| Veículos | 5 |
| Serviços | 5 |
| Peças e estoque | 6 |
| Ordens de serviço | 6 |
| Orçamentos | 4 |
| Acompanhamento | 1 |
| **Total** | **35/35 (100%)** |

São 38 requisições: a rota PATCH de status possui quatro exemplos de transição. A cobertura considera pares método/caminho dos oito controllers; exclui Swagger e infraestrutura. Cada requisição informa permissões, pré-condições e código de sucesso, com teste de status e captura de variáveis quando aplicável.

Papéis numéricos: `0` Atendente, `1` Mecanico, `2` Estoquista, `3` Administrador. Status numéricos: `0` RECEBIDA, `1` EM_DIAGNOSTICO, `2` AGUARDANDO_APROVACAO, `3` EM_EXECUCAO, `4` FINALIZADA, `5` ENTREGUE, `6` APROVADA, `7` CANCELADA. As entradas usam números; as respostas de OS expõem o nome do status. Os estados 2 e 6 são definidos pelo fluxo de orçamento, não pelo PATCH genérico.

Erros habituais: 400 para validação/regra de negócio, 401 para autenticação, 403 para papel insuficiente, 404 para recurso inexistente e 409 para conflitos. A consulta pública de acompanhamento devolve 404 para token inválido. As decisões retornam HTML no GET e texto no POST; as demais operações documentadas retornam JSON ou 204 sem corpo.

Validação do artefato: JSON, corpos JSON, variáveis e correspondência das 35 rotas com os controllers foram conferidos estaticamente. Isso não substitui executar as requisições contra API, MySQL e SMTP configurados. Ao exportar uma cópia para compartilhar, limpe senhas e tokens salvos durante o uso.
