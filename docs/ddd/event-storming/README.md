# Event Storming — descoberta do domínio

Esta página organiza os recortes do quadro de descoberta na ordem de leitura. O recebimento está em andamento; foram identificadas oito imagens: brainstorming, linha do tempo, comandos e políticas e agregados. Os diagramas de síntese da implementação continuam disponíveis na [documentação DDD](../README.md#event-storming-e-exportações).

## Organização dos recortes

| Ordem | Conteúdo identificado | Nome previsto para o arquivo |
|---|---|---|
| 1 | Brainstorming: levantamento inicial de eventos | `01-brainstorming.png` |
| 2 | Linha do tempo: cadastro, diagnóstico, orçamento e decisão | `02-linha-do-tempo-parte-01.png` |
| 3 | Linha do tempo: disponibilidade, execução e entrega | `02-linha-do-tempo-parte-02.png` |
| 4 | Comandos e políticas: cadastro, diagnóstico e envio de orçamento | `03-comandos-politicas-parte-01.png` |
| 5 | Comandos e políticas: decisão do cliente e disponibilidade de peças | `03-comandos-politicas-parte-02.png` |
| 6 | Comandos e políticas: reposição, reserva e execução | `03-comandos-politicas-parte-03.png` |
| 7 | Comandos e políticas: conclusão e entrega | `03-comandos-politicas-parte-04.png` |
| 8 | Agregados: Cliente, Veículo, Ordem de Serviço e Estoque | `04-agregados.png` |

As imagens foram recebidas no chat, mas os arquivos correspondentes ainda não estão disponíveis no diretório do projeto. Os nomes acima reservam sua organização; não representam arquivos já adicionados. Não há links de imagem até que os originais sejam copiados para esta pasta.

## 1. Brainstorming

O levantamento inicial reúne fatos e atividades relacionados a:

- Atendimento: serviço solicitado, cliente cadastrado, veículo cadastrado e OS aberta.
- Diagnóstico: diagnóstico iniciado, diagnóstico realizado e mecânico designado.
- Orçamento: orçamento definido, enviado ao cliente, aprovado ou recusado.
- Execução: peças identificadas, serviço iniciado, serviço concluído e OS finalizada.
- Estoque: peça cadastrada, estoque consultado, indisponibilidade de peça identificada e estoque reabastecido.
- Encerramento: veículo entregue ao cliente e OS cancelada.

O quadro apresenta repetições de **Orçamento recusado** e **Ordem de Serviço cancelada**. Elas são preservadas no levantamento original; nesta síntese, cada conceito aparece uma única vez. A repetição não indica uma funcionalidade adicional.

## 2. Linha do tempo — parte 1

O fluxo começa pela pesquisa de cliente e veículo. Quando encontrados, segue para abertura da OS. Quando não encontrados, há um caminho de cadastro que retorna à abertura.

Na sequência, aparecem diagnóstico iniciado, diagnóstico concluído, orçamento gerado e notificação enviada ao cliente. A decisão divide o fluxo:

- Aprovação: a OS muda para aprovada e segue para solicitação e verificação de disponibilidade de peças e insumos.
- Recusa: a OS muda para cancelada e segue pelo caminho alternativo de notificação do cliente, visível na segunda parte.

Há uma pergunta em aberto no quadro: **como o cliente será notificado?** Na implementação atual, o envio do orçamento é realizado por e-mail, com link de confirmação da decisão.

## 3. Linha do tempo — parte 2

Este recorte continua a solicitação e a verificação de disponibilidade vistas na parte 1. Esses dois pontos reaparecem para conectar os recortes; não são outra etapa independente.

O caminho principal apresenta peças disponíveis, alocação, início da execução, conclusão do serviço e finalização da OS. Depois, aparecem notificação do cliente, entrega do veículo e alteração da OS para entregue.

O caminho de indisponibilidade apresenta solicitação de reabastecimento, peças reabastecidas, atualização do estoque e notificação do mecânico, retornando à disponibilidade. O caminho de cancelamento também chega à notificação do cliente.

O quadro mantém perguntas sobre o canal de notificação e se o cliente ou um guincho fará a retirada. São pontos de descoberta, não decisões técnicas automaticamente implementadas.

## 4. Comandos e políticas

### Legenda do quadro

| Identificação | Cor | Significado nesta leitura |
|---|---|---|
| CMD | Azul-turquesa | Comando: intenção de realizar uma ação |
| EV | Amarelo | Evento: resultado de negócio ocorrido |
| POL | Roxo | Política: regra que relaciona um acontecimento à próxima ação |
| AT | Amarelo-claro | Ator responsável pela atividade |
| ML | Verde | Consulta ou modelo de leitura que apoia uma ação |
| PA | Vermelho | Ponto de atenção ou condição destacada no quadro |

As siglas são mantidas como aparecem nos recortes. A descrição de ML e PA corresponde à função visual de seus cartões, sem pressupor uma legenda formal adicional do quadro original.

### Parte 1 — cadastro, diagnóstico e orçamento

O atendente consulta cliente e veículo por CPF/CNPJ. Se não cadastrados, realiza o cadastro antes de abrir a OS. A criação da ordem aciona a política de notificar o mecânico responsável.

O mecânico consulta ordens abertas, inicia o diagnóstico e o finaliza. Em seguida, o orçamento é gerado, e uma política determina seu envio ao cliente.

| Comando | Evento resultante no quadro |
|---|---|
| Cadastrar Ordem de Serviço | Ordem de Serviço criada |
| Notificar Mecânico | Mecânico notificado |
| Iniciar Diagnóstico | Diagnóstico iniciado |
| Finalizar Diagnóstico | Diagnóstico concluído |
| Gerar orçamento | Orçamento gerado |
| Enviar Notificação ao cliente | Orçamento enviado ao cliente |

### Parte 2 — decisão e solicitação de peças

O cliente pode aprovar ou reprovar o orçamento. Na aprovação, a política determina notificar o mecânico para executar o serviço. Na reprovação, a política determina cancelar a OS.

O fluxo aprovado segue por iniciar serviço, solicitar peças e insumos e verificar disponibilidade, com participação do mecânico e do estoquista. A consulta de disponibilidade em estoque apoia essa verificação.

Se as peças não estiverem disponíveis, a política determina registrar uma pendência na OS e solicitar reposição. Se estiverem disponíveis, o caminho segue para reserva e registro de saída.

### Parte 3 — reposição, reserva e execução

O início deste recorte repete a verificação de disponibilidade, a política de falta de estoque e os caminhos de pendência e reserva da parte 2. É uma sobreposição para continuidade, não uma segunda ocorrência obrigatória dessas ações.

O caminho de reposição mostra solicitação, entrega das peças pelo fornecedor, registro de entrada e peças recebidas no estoque. O caminho de atendimento mostra reserva, saída, entrega ao mecânico, retomada do serviço, conclusão e finalização da OS.

O evento de recebimento no estoque aparece no ramo superior. Os recortes não detalham toda a coordenação entre esse recebimento e a retomada do serviço; não foi acrescentada uma política que não esteja visível.

### Parte 4 — conclusão e entrega

Este recorte repete o trecho de retomada e conclusão do serviço para mostrar o encerramento completo:

1. Retomar serviço → Serviço em execução.
2. Concluir serviço → Serviço concluído.
3. Finalizar Ordem de Serviço → Ordem de Serviço finalizada.
4. Registrar Entrega do Veículo → Veículo entregue ao cliente.

O mecânico aparece associado à retomada; o atendente, ao registro de entrega. A sobreposição com a parte 3 deve ser lida uma única vez na história completa.

### Políticas identificadas

| Gatilho | Ação definida no quadro |
|---|---|
| OS criada | Notificar o mecânico responsável |
| Orçamento gerado | Enviar o orçamento ao cliente |
| Orçamento aprovado | Notificar o mecânico para executar o serviço |
| Orçamento reprovado | Alterar a OS para cancelada |
| Solicitação de peças não atendida por falta de estoque | Registrar pendência na OS |

## 5. Agregados

O quadro agrupa comandos, eventos, atores, consultas e políticas em torno de quatro elementos identificados como agregados: **Cliente**, **Veículo**, **Ordem de Serviço** e **Estoque**. É uma visão de organização da descoberta, complementar à sequência temporal dos recortes anteriores.

| Agrupamento no quadro | Responsabilidade identificada | Correspondência na implementação |
|---|---|---|
| Cliente | Cadastro e manutenção da identificação do cliente | `Customer`, `DocumentNumber` e `CustomerUseCases` |
| Veículo | Cadastro e manutenção do veículo relacionado ao cliente | `Vehicle`, `LicensePlate` e `VehicleUseCases` |
| Ordem de Serviço | Coordenação do atendimento, diagnóstico, orçamento, decisão, execução e entrega | `ServiceOrder`, seus itens, `Estimate`, `ServiceOrderUseCases` e `EstimateUseCases` |
| Estoque | Disponibilidade e movimentação das peças utilizadas no atendimento | `Part`, `StockMovement` e `InventoryUseCases` |

As conexões visíveis relacionam o cadastro de veículo à abertura da OS e a solicitação de peças da OS ao estoque. O quadro também reúne políticas e resultados ao redor desses agrupamentos. Os textos menores desta imagem não permitem uma transcrição integral confiável; a tabela registra os nomes e as relações gerais identificáveis.

### Limites de consistência e diferenças de nomenclatura

Na implementação, `ServiceOrder` é a raiz do agregado de atendimento; itens e orçamento pertencem a esse fluxo. A decisão do orçamento e a mudança de estado da OS são gravadas juntas. A execução também coordena a atualização das peças e o registro das movimentações na mesma transação.

**Estoque** é o nome do agrupamento de negócio no quadro, mas não existe uma entidade ou raiz chamada `Estoque` no código. A entidade `Part` representa cada cadastro de peça com saldo, e `StockMovement` registra suas movimentações. Uma transação envolvendo OS e peças não transforma automaticamente essas entidades em um único agregado.

O contexto de Cadastro reúne Cliente e Veículo, sem significar que ambos formem um único agregado. Da mesma forma, os quatro agrupamentos do desenho não devem ser interpretados como quatro microsserviços ou como a lista completa de entidades do sistema. Catálogo e identidade e acesso também aparecem no [modelo implementado](../README.md#contextos-e-agregados).

As reservas, notificações e demais atividades do quadro mantêm as mesmas diferenças de escopo descritas abaixo; seu agrupamento nesta etapa não comprova implementação.

## Relação com a aplicação

| Elemento do quadro | Comportamento atual |
|---|---|
| Cadastro, diagnóstico, orçamento e decisão | Atendidos pelos cadastros, pela OS e pelo fluxo de orçamento |
| Envio do orçamento ao cliente | SMTP em segundo plano, com validade e reemissão do link |
| Falta de peças | Impede o início da execução; reposição por movimentação manual de estoque |
| Alocação ou reserva de peças | Não há operação própria de reserva; a baixa ocorre no início da execução |
| Solicitação de reabastecimento e notificação ao mecânico | Não há fluxo automatizado implementado |
| Mecânico designado | Não há atribuição individual de mecânico à OS; o acesso é por papel |
| Notificação ao mecânico após criação ou aprovação | Não há envio automático específico; o mecânico consulta as OS pela API |
| OS aguardando peças e retomada de serviço | Não há estado específico de pendência nem comando próprio de retomada; a insuficiência de estoque bloqueia a entrada em execução |
| Entrega de peças pelo fornecedor | Não há cadastro ou fluxo de fornecedor; entradas são movimentações manuais de estoque |
| Notificação de conclusão ou cancelamento | Não há envio automático específico para essas situações |
| Entrega por cliente ou guincho | A aplicação registra a entrega, sem distinguir quem retirou o veículo |

O levantamento original mistura fatos já ocorridos com ações, como solicitar e verificar. Isso foi mantido nesta leitura dos recortes. A síntese do modelo implementado distingue [comandos, eventos e políticas](../README.md).

Em comandos e políticas, o desenho posiciona o início do serviço antes da verificação e retirada das peças. No código, a disponibilidade é validada antes da transição para `EM_EXECUCAO`, e a baixa é salva junto com a mudança de estado. O desenho foi documentado como descoberta, sem alterar a regra implementada.

## Continuação

Os próximos recortes serão incorporados nesta sequência. Sobreposições serão usadas para identificar a continuidade; recortes diferentes do mesmo fluxo não serão descartados como duplicatas.

Consulte também o [Domain Storytelling](../storytelling/README.md), o [glossário](../linguagem-ubiqua.md) e o [quadro original no Miro](https://miro.com/app/board/uXjVGE0G3xk=/).
