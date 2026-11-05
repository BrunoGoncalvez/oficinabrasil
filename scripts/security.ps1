$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    $raw = & dotnet list OficinaBrasil.sln package --vulnerable --include-transitive --format json
    if ($LASTEXITCODE -ne 0) { throw 'A consulta de vulnerabilidades falhou; não é possível afirmar ausência de alertas.' }
    $json = $raw -join "`n"
    $audit = $json | ConvertFrom-Json
    if ($audit.problems) { throw 'A auditoria retornou problemas. Verifique a restauração e as fontes NuGet.' }
    foreach ($project in $audit.projects) {
        $path = $project.path.Replace('\', '/')
        $prefix = $projectRoot.Replace('\', '/') + '/'
        if ($path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
            $project.path = $path.Substring($prefix.Length)
        }
    }
    $json = $audit | ConvertTo-Json -Depth 100
    $evidence = Join-Path $projectRoot 'docs/evidencias'
    New-Item -ItemType Directory -Path $evidence -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $evidence 'nuget-audit.json'), $json, [Text.UTF8Encoding]::new($false))
    $alerts = @($audit.projects | ForEach-Object { $_.frameworks } | ForEach-Object { @($_.topLevelPackages) + @($_.transitivePackages) } | Where-Object { $_.vulnerabilities })
    $report = @('# Análise de dependências', '', "Execução: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz').", '', 'Comando: `dotnet list OficinaBrasil.sln package --vulnerable --include-transitive --format json`.', '', "Resultado: $($alerts.Count) referências com avisos conhecidos nas fontes consultadas. Evidência bruta: [nuget-audit.json](nuget-audit.json).", '', 'Correção aplicada: MySqlConnector 2.3.5 → 2.6.1, que contém a correção do [GHSA-473q-m89c-ghf8](https://github.com/advisories/GHSA-473q-m89c-ghf8).', '', 'Escopo: dependências NuGet diretas e transitivas. Esta verificação não substitui teste de invasão, análise do código, das imagens Docker ou da configuração do ambiente. Novos alertas podem surgir; execute novamente na versão final.', '', 'Configurações de exemplo e credenciais DEMO são apenas locais. Não usar em produção. Defina segredos por variáveis de ambiente ou user-secrets; nunca comite credenciais de serviços reais.')
    $report[0] = '# Relatório de segurança'
    $report = @($report[0], '', '## Dependências NuGet') + $report[1..($report.Length - 1)]
    $sonarMetricsPath = Join-Path $evidence 'sonar-metrics.json'
    if (Test-Path -LiteralPath $sonarMetricsPath) {
        $sonar = Get-Content -LiteralPath $sonarMetricsPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $measures = @{}
        foreach ($measure in $sonar.component.measures) { $measures[$measure.metric] = $measure.value }
        $report += @('', '## Análise do código com SonarQube', '',
            'Resultados da análise SonarQube registrada no [relatório da execução](sonar.md), que identifica a versão analisada e a data. Este script atualiza a auditoria NuGet e consulta as evidências Sonar existentes; para executar novamente o Sonar, use `scripts/sonar.ps1`.', '',
            '| Verificação | Resultado |', '|---|---:|',
            "| Problemas com impacto em segurança | $($measures['software_quality_security_issues']) abertos |",
            "| Security Hotspots | $($measures['security_hotspots']) |",
            "| Testes executados | $($measures['tests']) |",
            "| Falhas nos testes | $($measures['test_failures']) |",
            "| Erros nos testes | $($measures['test_errors']) |",
            "| Testes ignorados | $($measures['skipped_tests']) |",
            "| Cobertura registrada no SonarQube | $($measures['coverage'])% |", '',
            'Consulte a [avaliação dos achados](sonar-achados.md) para o contexto e o tratamento dos problemas. O status do Quality Gate deve ser lido junto com as condições efetivamente avaliadas e com os problemas abertos.', '',
            '### Evidências da análise', '',
            '- [Resumo da execução](sonar.md) e [métricas em JSON](sonar-metrics.json).',
            '- [Problemas de segurança no modo MQR](sonar-security-issues.json) e [vulnerabilidades na classificação legada](sonar-vulnerabilities.json).',
            '- [Security Hotspots](sonar-hotspots.json) e [Quality Gate](sonar-qualitygate.json).',
            '- [Instruções de reprodução](../SONAR.md).', '',
            '### Cobertura de testes', '',
            'O SonarQube importa a cobertura OpenCover gerada pelo Coverlet e os resultados TRX dos testes .NET, incluindo a suíte de integração com MySQL e SMTP local. A métrica `coverage` combina cobertura de linhas e condições; sua definição difere do percentual de linhas do [relatório Cobertura](cobertura.md).')
    } else {
        $report += @('', '## Análise do código com SonarQube', '', 'Evidências SonarQube ainda não disponíveis. Execute a [análise local](../SONAR.md) e atualize este relatório.')
    }
    [IO.File]::WriteAllLines((Join-Path $evidence 'seguranca.md'), $report, [Text.UTF8Encoding]::new($false))
    $report | Write-Output
    if ($alerts.Count -gt 0) { throw 'Há dependências vulneráveis.' }
} finally { Pop-Location }
