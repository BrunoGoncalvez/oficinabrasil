# Análise SonarQube

Execução: 2026-11-04 21:45:37 -03:00.

SonarQube: 26.10.0.133078. Projeto: `oficinabrasil`. Análise: `b8e4574a-c914-4e95-9e87-9af7fccfc838`.

Commit base: `e2371ff21a66c980327346ff45d9457696f0a89d`. Alterações locais presentes na execução: True. A análise considera os arquivos de trabalho.

Quality Gate: **OK**.

| Métrica | Valor |
|---|---:|
| Defeitos (classificação legada) | 0 |
| Problemas de qualidade do código (classificação legada) | 17 |
| Cobertura de testes | 85,7% |
| Percentual de linhas duplicadas | 0,0% |
| Linhas de código sem comentários | 1935 |
| Pontos de atenção de segurança | 0 |
| Testes ignorados | 0 |
| Problemas de manutenibilidade (MQR) | 15 |
| Problemas de confiabilidade (MQR) | 2 |
| Problemas de segurança (MQR) | 8 |
| Erros nos testes | 0 |
| Falhas nos testes | 0 |
| Testes executados | 62 |
| Vulnerabilidades (classificação legada) | 8 |

## Condições do Quality Gate

Esta análise não apresentou condições avaliadas no Quality Gate. O status OK não significa ausência de achados de segurança; consulte as métricas e as listas de problemas.

## Apontamentos de segurança

| Regra | Quantidade | Local | Avaliação e correção recomendada |
|---|---:|---|---|
| S6444 | 5 | [Checks.cs](../../src/OficinaBrasil.Domain/Validation/Checks.cs) | As validações e normalizações de CPF/CNPJ e placa usam expressões regulares sem timeout explícito. Definir um limite de execução, limitar o tamanho das entradas e tratar a falha de validação por timeout. |
| S6444 | 1 | [DemoSeeder.cs](../../src/OficinaBrasil.API/Services/DemoSeeder.cs) | A normalização da massa de demonstração usa expressão regular sem timeout. A execução é restrita a Development e os dados são locais; ainda assim, aplicar timeout explícito mantém a proteção consistente. |
| S5332 | 1 | [EstimateEmailWorker.cs](../../src/OficinaBrasil.API/Services/EstimateEmailWorker.cs) | TLS é habilitado por padrão, mas pode ser desligado por configuração. O SMTP local de demonstração e testes usa essa opção. Exigir TLS em produção e restringir a exceção ao ambiente de desenvolvimento. |
| docker:S6471 | 1 | [Dockerfile](../../src/OficinaBrasil.API/Dockerfile) | A imagem de execução herda o usuário root. Configurar um usuário sem privilégios e as permissões necessárias no diretório de chaves persistentes; validar a inicialização e o acesso ao volume após a mudança. |

Os oito apontamentos permanecem abertos. Os dados brutos de apoio ficam em `.artifacts/sonar-export/`, fora do versionamento.

Security Hotspots exigem revisão contextual; não são automaticamente vulnerabilidades confirmadas. O resultado descreve as regras disponíveis nesta edição do SonarQube e não substitui a auditoria de dependências.

Reprodução: consulte [Análise com SonarQube](../SONAR.md). A cobertura foi importada do Coverlet em formato OpenCover, junto aos resultados TRX dos testes completos com MySQL e SMTP local. A métrica do Sonar combina cobertura de linhas e condições.

[Painel local](http://localhost:9000/dashboard?id=oficinabrasil). O endereço localhost é acessível somente no computador que executa o SonarQube.
