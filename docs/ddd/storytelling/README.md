# Domain Storytelling

Os três diagramas descrevem o atendimento da oficina pela perspectiva dos atores e de suas atividades. A numeração nas conexões indica a sequência de leitura de cada história.

Foram mantidos os arquivos SVG originais, com nomes padronizados. As exportações PNG dos fluxos mecânico e de estoque representam as mesmas histórias e não foram adicionadas como documentos separados. Os arquivos de origem permanecem preservados fora do repositório.

## 1. Atendimento e orçamento

[Abrir o diagrama de atendimento](01-atendimento.svg).

O fluxo relaciona atendente, cliente e mecânico: cadastro, descrição da necessidade do serviço, abertura da OS, encaminhamento para diagnóstico, geração e envio do orçamento por e-mail e decisão do cliente. As anotações registram a espera por aprovação, a aprovação e a possibilidade de reprovação.

![Domain Storytelling: atendimento e orçamento](01-atendimento.svg)

Na aplicação, o orçamento coloca a OS em `AGUARDANDO_APROVACAO`. A decisão é registrada pela confirmação do link enviado ao cliente: aprovar leva a `APROVADA`; reprovar leva a `CANCELADA`.

## 2. Fluxo do mecânico

[Abrir o diagrama do fluxo mecânico](02-fluxo-mecanico.svg).

1. O mecânico consulta a OS aprovada.
2. Identifica e solicita as peças necessárias.
3. Inicia o serviço, colocando a OS em execução.
4. Conclui o serviço.
5. Atualiza a OS para finalizada.

![Domain Storytelling: fluxo do mecânico](02-fluxo-mecanico.svg)

Na aplicação, a execução exige aprovação e estoque suficiente. A entrada em `EM_EXECUCAO` registra a baixa das peças; a conclusão muda o estado para `FINALIZADA`. A entrega ao cliente é uma etapa posterior, realizada por atendente ou administrador.

## 3. Estoque

[Abrir o diagrama de estoque](03-estoque.svg).

1. O estoquista consulta o estoque.
2. Verifica a disponibilidade.
3. Reserva a peça.
4. Entrega a peça.
5. Registra a saída de estoque.

![Domain Storytelling: estoque](03-estoque.svg)

Esse diagrama descreve o processo de negócio descoberto. A implementação atual controla quantidade por SKU, ajustes de saldo e movimentações. A saída vinculada à OS ocorre automaticamente no início da execução. Não há uma operação própria de reserva nem registro da entrega física da peça; essas atividades não devem ser apresentadas como funcionalidades já implementadas.

## Relação com os demais documentos

O Domain Storytelling apresenta quem realiza as atividades e quais objetos participam. O [Event Storming](../README.md#event-storming) complementa essas histórias com comandos, eventos, políticas e estados. Os termos do domínio estão no [glossário de linguagem ubíqua](../linguagem-ubiqua.md).

As imagens foram preservadas como recebidas, incluindo o título genérico `<title>` no fluxo mecânico e o ator adicional sem conexões no fluxo de estoque. Os títulos e descrições desta página identificam cada história.

Retornar à [documentação DDD](../README.md) ou ao [README principal](../../../README.md).
