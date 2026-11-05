# Relatório de segurança

## Dependências NuGet

Execução: 2026-11-04 21:21:24 -03:00.

Comando: `dotnet list OficinaBrasil.sln package --vulnerable --include-transitive --format json`.

Resultado: 0 referências com avisos conhecidos nas fontes consultadas. Evidência bruta: [nuget-audit.json](nuget-audit.json).

Correção aplicada: MySqlConnector 2.3.5 → 2.6.1, que contém a correção do [GHSA-473q-m89c-ghf8](https://github.com/advisories/GHSA-473q-m89c-ghf8).

Escopo: dependências NuGet diretas e transitivas. Esta verificação não substitui teste de invasão, análise do código, das imagens Docker ou da configuração do ambiente. Novos alertas podem surgir; execute novamente na versão final.

Configurações de exemplo e credenciais DEMO são apenas locais. Não usar em produção. Defina segredos por variáveis de ambiente ou user-secrets; nunca comite credenciais de serviços reais.

## Análise do código com SonarQube

Análise realizada em 2026-11-04 com SonarQube Community Build 26.10.0.133078 e SonarScanner for .NET 12.0. A versão analisada e o identificador da execução estão no [relatório SonarQube](sonar.md).

| Verificação | Resultado |
|---|---:|
| Problemas com impacto em segurança | 8 abertos |
| Security Hotspots | 0 |
| Testes executados | 62 |
| Falhas nos testes | 0 |
| Erros nos testes | 0 |
| Testes ignorados | 0 |
| Cobertura registrada no SonarQube | 85,7% |

Os oito apontamentos correspondem a seis expressões regulares sem timeout explícito, uma configuração de TLS do SMTP e a execução da imagem Docker como root. A [avaliação dos achados](sonar-achados.md) apresenta os locais afetados, o contexto e as correções recomendadas. Os problemas permanecem abertos.

O Quality Gate retornou **OK**, sem condições avaliadas nesta análise inicial. Esse status não elimina os oito problemas de segurança identificados.

### Evidências da análise

- [Resumo da execução e métricas](sonar.md).
- [Métricas exportadas em JSON](sonar-metrics.json), incluindo cobertura e resultados dos testes.
- [Problemas com impacto em segurança no modo MQR](sonar-security-issues.json).
- [Vulnerabilidades na classificação legada](sonar-vulnerabilities.json).
- [Security Hotspots](sonar-hotspots.json).
- [Resultado e condições do Quality Gate](sonar-qualitygate.json).
- [Instruções para reproduzir a análise](../SONAR.md).

### Cobertura de testes

O SonarQube importou a cobertura OpenCover gerada pelo Coverlet e os resultados TRX dos testes .NET. A execução incluiu os testes unitários e de integração HTTP, MySQL e SMTP local.

A métrica `coverage` registrada foi **85,7%**. Ela combina cobertura de linhas e condições e tem definição diferente do percentual de linhas do [relatório Cobertura](cobertura.md).
