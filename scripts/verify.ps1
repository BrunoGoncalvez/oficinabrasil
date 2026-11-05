param([double]$MinimumCoverage = 80)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    $runDirectory = Join-Path $projectRoot ('.artifacts/tests/' + [Guid]::NewGuid().ToString('N'))
    dotnet test tests/OficinaBrasil.Domain.Tests/OficinaBrasil.Domain.Tests.csproj -m:1 -nr:false --collect:'XPlat Code Coverage' --results-directory $runDirectory --logger 'trx;LogFileName=resultados.trx' --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes. Consulte os resultados acima.' }
    $report = Get-ChildItem -LiteralPath $runDirectory -Recurse -Filter coverage.cobertura.xml | Select-Object -First 1
    if (!$report) { throw 'Relatório de cobertura não encontrado.' }
    [xml]$coverage = Get-Content -LiteralPath $report.FullName
    $evidence = $runDirectory
    $lines = @('# Cobertura verificada', '', 'Todos os tipos dos quatro projetos de produção são medidos, incluindo DTOs, validações e seed; sem exclusões de classes. Domain e Application são os domínios críticos.', '', '| Projeto | Linhas | Ramos |', '|---|---:|---:|')
    $failed = $false
    foreach ($name in @('OficinaBrasil.Domain', 'OficinaBrasil.Application', 'OficinaBrasil.Infrastructure', 'OficinaBrasil.API')) {
        $package = $coverage.coverage.packages.package | Where-Object { $_.name -eq $name }
        if (!$package) { throw "Projeto ausente no relatório: $name" }
        $rate = [double]::Parse($package.'line-rate', [Globalization.CultureInfo]::InvariantCulture) * 100
        $branches = [double]::Parse($package.'branch-rate', [Globalization.CultureInfo]::InvariantCulture) * 100
        $lines += "| $name | $($rate.ToString('F2', [Globalization.CultureInfo]::InvariantCulture))% | $($branches.ToString('F2', [Globalization.CultureInfo]::InvariantCulture))% |"
        if ($rate -lt $MinimumCoverage) { $failed = $true }
    }
    $global = [double]::Parse($coverage.coverage.'line-rate', [Globalization.CultureInfo]::InvariantCulture) * 100
    if ($global -lt $MinimumCoverage) { $failed = $true }
    $lines += @('', "Cobertura global de linhas: $($global.ToString('F2', [Globalization.CultureInfo]::InvariantCulture))%.", '', "Meta: mínimo de $MinimumCoverage% de linhas em CADA projeto de produção e no total. O comando falha se qualquer um ficar abaixo da meta.", '', 'Reprodução: `pwsh ./scripts/verify.ps1`. Os testes HTTP usam MySQL real em banco isolado `oficina_test_*`, removido ao final. Configure `OFICINA_TEST_MYSQL` para escolher a conexão. O worker é exercitado contra um SMTP local sem retransmissão.', '', "Verificação: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz').")
    [IO.File]::WriteAllLines((Join-Path $evidence 'cobertura.md'), $lines, [Text.UTF8Encoding]::new($false))
    $lines | Write-Output
    if ($failed) { throw 'Cobertura abaixo da meta.' }
} finally { Pop-Location }
