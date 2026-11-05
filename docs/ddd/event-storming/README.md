# Event Storming

O [quadro de Event Storming no Miro](https://miro.com/app/board/uXjVGE0G3xk=/) reúne o levantamento do atendimento da oficina: eventos de negócio, comandos, atores, políticas e agregados.

## Fluxo do atendimento

1. **Cadastro e abertura:** o atendente identifica o cliente e o veículo, realiza os cadastros necessários e abre a ordem de serviço.
2. **Diagnóstico e orçamento:** o mecânico registra o diagnóstico; o orçamento reúne os serviços, as peças e seus valores e é enviado ao cliente.
3. **Decisão do cliente:** a aprovação permite seguir para execução; a reprovação cancela a OS.
4. **Disponibilidade de peças:** o estoque é consultado antes do início do serviço. Quando o saldo é insuficiente, a reposição é necessária para prosseguir.
5. **Execução e entrega:** o mecânico executa e conclui o serviço; o atendente registra a entrega do veículo.

## Comandos, eventos e regras

| Comando | Evento de negócio | Regra na aplicação |
|---|---|---|
| Abrir ordem de serviço | OS recebida | Cliente e veículo devem estar cadastrados |
| Iniciar diagnóstico | Diagnóstico iniciado | A OS deve estar em `RECEBIDA` |
| Gerar orçamento | Orçamento gerado | Os preços são registrados nos itens; a OS passa a `AGUARDANDO_APROVACAO` |
| Enviar orçamento | Orçamento enviado | O envio ocorre por e-mail, em segundo plano |
| Aprovar ou reprovar orçamento | Decisão registrada | O link deve estar válido e sem decisão anterior; a OS passa a `APROVADA` ou `CANCELADA` |
| Iniciar serviço | Execução iniciada | Exige aprovação e estoque suficiente; a baixa das peças acompanha a mudança para `EM_EXECUCAO` |
| Concluir serviço | Serviço finalizado | A OS passa de `EM_EXECUCAO` para `FINALIZADA` |
| Registrar entrega | Veículo entregue | A OS passa de `FINALIZADA` para `ENTREGUE` |
| Registrar entrada ou saída de peças | Estoque movimentado | A movimentação exige motivo e preserva o saldo não negativo |

## Agregados e responsabilidades

| Elemento do domínio | Responsabilidade |
|---|---|
| Cliente | Identificação e dados cadastrais |
| Veículo | Identificação do veículo e vínculo com o cliente |
| Ordem de serviço | Diagnóstico, itens, orçamento, decisão e evolução do atendimento |
| Estoque | Disponibilidade de peças e histórico de movimentações |

Na implementação, `ServiceOrder` coordena o atendimento, seus itens e o orçamento. O estoque é representado por `Part` e `StockMovement`. Os limites e as regras desses elementos estão detalhados em [contextos e agregados](../README.md#contextos-e-agregados).

## Escopo implementado

O quadro também contempla reserva de peças, solicitação de reposição, participação de fornecedores e notificações ao mecânico. Na aplicação atual, a reposição é registrada por movimentação manual, e a baixa vinculada à OS ocorre no início da execução. Reserva, fluxo de fornecedores e notificações automáticas ao mecânico permanecem fora do escopo.

Os eventos descrevem fatos de negócio; o sistema os realiza por casos de uso e persistência transacional. Não há barramento de eventos ou Event Sourcing.

Consulte também o [Domain Storytelling](../storytelling/README.md), a [linguagem ubíqua](../linguagem-ubiqua.md) e a [documentação DDD](../README.md).
