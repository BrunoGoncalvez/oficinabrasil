# Análise de dependências

Execução: 2026-11-02 21:16:39 -03:00.

Comando: `dotnet list OficinaBrasil.sln package --vulnerable --include-transitive --format json`.

Resultado: 0 referências com avisos conhecidos nas fontes consultadas. Evidência bruta: [nuget-audit.json](nuget-audit.json).

Correção aplicada: MySqlConnector 2.3.5 → 2.6.1, que contém a correção do [GHSA-473q-m89c-ghf8](https://github.com/advisories/GHSA-473q-m89c-ghf8).

Escopo: dependências NuGet diretas e transitivas. Esta verificação não substitui teste de invasão, análise do código, das imagens Docker ou da configuração do ambiente. Novos alertas podem surgir; execute novamente na versão final.

Configurações de exemplo e credenciais DEMO são apenas locais. Não usar em produção. Defina segredos por variáveis de ambiente ou user-secrets; nunca comite credenciais de serviços reais.
