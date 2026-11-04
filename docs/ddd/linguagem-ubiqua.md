# Linguagem ubíqua e glossário

| Termo | Significado e nome no código |
|---|---|
| Cliente | Pessoa ou empresa proprietária do veículo (`Customer`); não é funcionário autenticado |
| Documento | CPF/CNPJ numérico, com dígitos verificadores válidos (`DocumentNumber`); não é credencial |
| Veículo | Cadastro por placa, marca, modelo, ano e cliente (`Vehicle`) |
| Placa | Identificador normalizado do veículo (`LicensePlate`) |
| Funcionário | Usuário interno com papel de atendente, mecânico, estoquista ou administrador (`User`) |
| OS | Ordem de serviço vinculada a um veículo (`ServiceOrder`) |
| Recebida | OS aberta, antes do início do diagnóstico |
| Diagnóstico | Avaliação do mecânico; conclusão registrada junto ao orçamento |
| Serviço | Trabalho do catálogo com preço atual (`WorkshopService`) |
| Item de serviço | Trabalho incluído na OS com preço capturado (`OrderServiceItem`) |
| Peça / insumo | Material com SKU, preço unitário e saldo (`Part`) |
| SKU | Stock Keeping Unit: código interno único do tipo de peça/insumo (`Part.Sku`); unidades iguais compartilham o mesmo código e são controladas pelo saldo |
| Item de peça | Material da OS com quantidade e preço capturado (`OrderPartItem`) |
| Orçamento | Diagnóstico, composição e total submetidos ao cliente (`Estimate`) |
| Aprovação | Consentimento sobre os valores apresentados; libera início do serviço |
| Reprovação | Recusa do orçamento; cancela a OS sem baixar peças |
| Reemissão | Novo token e validade para o mesmo orçamento ainda não respondido; revoga o anterior |
| Execução | Trabalho iniciado após aprovação; nessa ação ocorre a baixa de peças |
| Finalizada | Trabalho concluído, ainda aguardando entrega do veículo |
| Entregue | Entrega do veículo registrada pelo atendente ou administrador |
| Movimento de estoque | Entrada/saída com quantidade, motivo e instante (`StockMovement`) |
| Saldo | Quantidade disponível; nunca negativa; não equivale a uma reserva |
| Tempo médio | Média entre início da execução e finalização das OS com ambos os instantes registrados |
| Token de decisão | Segredo de uso único e validade configurável para decidir um orçamento |
| Token de acompanhamento | Acesso protegido, válido por 30 dias, apenas ao andamento de uma OS |
| E-mail pendente | Mensagem gravada com o orçamento; worker tenta entregar após o commit |
| Conflito | Dados mudaram desde a leitura; a gravação é revertida e a API retorna 409 |

A validação de placa aceita o padrão antigo `LLLNNNN` e o Mercosul `LLLNLNN`. A letra adicional não fica limitada a A–J (essa faixa é da conversão das placas antigas). Referência do formato: [especificação oficial do CONTRAN, item 1.2](https://www.gov.br/transportes/pt-br/assuntos/transito/conteudo-contran/resolucoes/resolucao9692022anexos.pdf). A validação confere sintaxe; não consulta o registro do veículo na Senatran.
