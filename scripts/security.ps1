$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    $raw = & dotnet list OficinaBrasil.sln package --vulnerable --include-transitive --format json
    if ($LASTEXITCODE -ne 0) { throw 'A consulta de vulnerabilidades falhou; não é possível afirmar ausência de alertas.' }
    $json = $raw -join "`n"
    $audit = $json | ConvertFrom-Json
    if ($audit.problems) { throw 'A auditoria retornou problemas. Verifique a restauração e as fontes NuGet.' }
    $evidence = Join-Path $projectRoot 'docs/evidencias'
    New-Item -ItemType Directory -Path $evidence -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $evidence 'nuget-audit.json'), $json, [Text.UTF8Encoding]::new($false))
    $alerts = @($audit.projects | ForEach-Object { $_.frameworks } | ForEach-Object { @($_.topLevelPackages) + @($_.transitivePackages) } | Where-Object { $_.vulnerabilities })
    $report = @('# Análise de dependências', '', "Execução: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz').", '', 'Comando: `dotnet list OficinaBrasil.sln package --vulnerable --include-transitive --format json`.', '', "Resultado: $($alerts.Count) referências com avisos conhecidos nas fontes consultadas. Evidência bruta: [nuget-audit.json](nuget-audit.json).", '', 'Correção aplicada: MySqlConnector 2.3.5 → 2.6.1, que contém a correção do [GHSA-473q-m89c-ghf8](https://github.com/advisories/GHSA-473q-m89c-ghf8).', '', 'Escopo: dependências NuGet diretas e transitivas. Esta verificação não substitui teste de invasão, análise do código, das imagens Docker ou da configuração do ambiente. Novos alertas podem surgir; execute novamente na versão final.', '', 'Configurações de exemplo e credenciais DEMO são apenas locais. Não usar em produção. Defina segredos por variáveis de ambiente ou user-secrets; nunca comite credenciais de serviços reais.')
    [IO.File]::WriteAllLines((Join-Path $evidence 'seguranca.md'), $report, [Text.UTF8Encoding]::new($false))
    $report | Write-Output
    if ($alerts.Count -gt 0) { throw 'Há dependências vulneráveis.' }
} finally { Pop-Location }
