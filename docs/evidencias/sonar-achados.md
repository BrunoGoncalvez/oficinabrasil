# Avaliação dos achados do SonarQube

Análise executada em 2026-11-04 com SonarQube Community Build 26.10.0.133078 e SonarScanner for .NET 12.0. O [resultado exportado](sonar.md) registra o identificador da análise, as métricas e o commit base. A execução incluiu alterações locais ainda não commitadas.

Foram identificados **8 problemas com impacto em segurança**, também apresentados como vulnerabilidades na classificação legada. Todos foram classificados pelo Sonar como severidade legada `MINOR`. Os achados permanecem abertos; não foram suprimidos nem marcados como resolvidos.

| Regra | Quantidade | Local | Avaliação e correção recomendada |
|---|---:|---|---|
| S6444 | 5 | [Checks.cs](../../src/OficinaBrasil.Domain/Validation/Checks.cs) | As validações e normalizações de CPF/CNPJ e placa usam expressões regulares sem timeout explícito. Definir um limite de execução, limitar o tamanho das entradas e tratar a falha de validação por timeout. |
| S6444 | 1 | [DemoSeeder.cs](../../src/OficinaBrasil.API/Services/DemoSeeder.cs) | A normalização da massa de demonstração usa expressão regular sem timeout. A execução é restrita a Development e os dados são locais; ainda assim, aplicar timeout explícito mantém a proteção consistente. |
| S5332 | 1 | [EstimateEmailWorker.cs](../../src/OficinaBrasil.API/Services/EstimateEmailWorker.cs) | TLS é habilitado por padrão, mas pode ser desligado por configuração. O SMTP local de demonstração e testes usa essa opção. Exigir TLS em produção e restringir a exceção ao ambiente de desenvolvimento. |
| docker:S6471 | 1 | [Dockerfile](../../src/OficinaBrasil.API/Dockerfile) | A imagem de execução herda o usuário root. Configurar um usuário sem privilégios e as permissões necessárias no diretório de chaves persistentes; validar a inicialização e o acesso ao volume após a mudança. |

Os padrões de CPF/CNPJ e placa não contêm quantificadores aninhados. O relatório identifica a ausência de limite de execução; esta revisão não demonstrou uma exploração de negação de serviço.

## Resultado das verificações

- **62 testes aprovados**, incluindo HTTP, JWT, MySQL real, concorrência e SMTP local.
- **85,7% de cobertura** reportada pelo SonarQube. A métrica do Sonar combina linhas e condições; não deve ser comparada diretamente ao percentual de linhas do relatório Cobertura.
- **0 dependências com alertas conhecidos** na [auditoria NuGet](seguranca.md) executada na mesma data.
- **Quality Gate OK**, sem condições avaliadas nesta análise inicial. Esse status não elimina os oito problemas de segurança abertos.
- **0 Security Hotspots** na execução. Os oito achados acima estão classificados como problemas de segurança, não como hotspots.

Também foram reportados problemas de manutenibilidade e confiabilidade, registrados nas métricas. Esta execução analisa o código e importa os testes; não representa um teste de invasão ou uma auditoria das imagens de containers.

## Pendências antes da entrega final

Aplicar as correções dos achados, repetir a análise e atualizar as evidências para a versão final do código. Preservar o relatório desta execução como referência do diagnóstico inicial, caso seja necessário demonstrar a evolução.
