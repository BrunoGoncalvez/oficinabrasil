param(
    [string]$HostUrl = 'http://localhost:9000',
    [string]$ProjectKey = 'oficinabrasil',
    [switch]$ReportOnly
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($env:SONAR_TOKEN)) {
    throw 'Defina SONAR_TOKEN no processo atual. Consulte docs/SONAR.md.'
}
if (!$ReportOnly -and [string]::IsNullOrWhiteSpace($env:OFICINA_TEST_MYSQL)) {
    throw 'Defina OFICINA_TEST_MYSQL para executar todos os testes com banco isolado.'
}
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    $taskFile = '.sonarqube/out/.sonar/report-task.txt'
    $scanExitCode = 0
    if (!$ReportOnly) {
        $tools = Join-Path $projectRoot '.artifacts/tools'
        $scanner = Join-Path $tools 'dotnet-sonarscanner'
        if ($env:OS -eq 'Windows_NT') { $scanner += '.exe' }
        if (!(Test-Path -LiteralPath $scanner)) {
            dotnet tool install dotnet-sonarscanner --version 12.0.0 --tool-path $tools
            if ($LASTEXITCODE -ne 0) { throw 'Falha ao instalar o SonarScanner for .NET.' }
        }
        $run = '.artifacts/sonar/' + [Guid]::NewGuid().ToString('N')
        if (Test-Path -LiteralPath $taskFile) { Remove-Item -LiteralPath $taskFile }
        $begin = @('begin', "/k:$ProjectKey", "/d:sonar.host.url=$HostUrl", "/d:sonar.token=$env:SONAR_TOKEN",
            '/d:sonar.scanner.scanAll=false', "/d:sonar.cs.opencover.reportsPaths=$run/**/coverage.opencover.xml",
            "/d:sonar.cs.vstest.reportsPaths=$run/*.trx", '/d:sonar.qualitygate.wait=true', '/d:sonar.qualitygate.timeout=300')
        & $scanner @begin
        if ($LASTEXITCODE -ne 0) { throw 'Falha na preparação da análise Sonar.' }
        dotnet build OficinaBrasil.sln --no-incremental -m:1 -nr:false -p:UseSharedCompilation=false --verbosity minimal
        if ($LASTEXITCODE -ne 0) { throw 'Falha no build durante a análise Sonar.' }
        dotnet test tests/OficinaBrasil.Domain.Tests/OficinaBrasil.Domain.Tests.csproj --no-build --collect:'XPlat Code Coverage' --results-directory $run --logger 'trx;LogFileName=resultados.trx' --verbosity minimal -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
        if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes; a análise Sonar não foi enviada.' }
        if (!(Get-ChildItem -LiteralPath $run -Recurse -Filter coverage.opencover.xml)) {
            throw 'Relatório OpenCover ausente; a análise Sonar não foi enviada.'
        }
        & $scanner end "/d:sonar.token=$env:SONAR_TOKEN"
        $scanExitCode = $LASTEXITCODE
    }
    if (!(Test-Path -LiteralPath $taskFile)) { throw 'O scanner não produziu identificação da análise enviada.' }
    $taskProperties = ConvertFrom-StringData (Get-Content -LiteralPath $taskFile -Raw).Replace('\', '\\')
    if ($taskProperties.projectKey -ne $ProjectKey) { throw 'O projeto solicitado não corresponde à análise enviada.' }
    $headers = @{ Authorization = "Bearer $env:SONAR_TOKEN" }
    $task = Invoke-RestMethod -Uri "$HostUrl/api/ce/task?id=$($taskProperties.ceTaskId)" -Headers $headers -TimeoutSec 30
    if ($task.task.status -ne 'SUCCESS') { throw 'O processamento Sonar não concluiu com sucesso. Consulte o painel.' }
    $key = [Uri]::EscapeDataString($ProjectKey)
    $gate = Invoke-RestMethod -Uri "$HostUrl/api/qualitygates/project_status?analysisId=$($task.task.analysisId)" -Headers $headers -TimeoutSec 30
    $metrics = Invoke-RestMethod -Uri "$HostUrl/api/measures/component?component=$key&metricKeys=vulnerabilities,bugs,code_smells,software_quality_security_issues,software_quality_reliability_issues,software_quality_maintainability_issues,security_hotspots,security_hotspots_reviewed,coverage,duplicated_lines_density,ncloc,tests,test_errors,test_failures,skipped_tests" -Headers $headers -TimeoutSec 30
    $vulnerabilities = Invoke-RestMethod -Uri "$HostUrl/api/issues/search?componentKeys=$key&types=VULNERABILITY&resolved=false&ps=500" -Headers $headers -TimeoutSec 30
    $securityIssues = Invoke-RestMethod -Uri "$HostUrl/api/issues/search?componentKeys=$key&impactSoftwareQualities=SECURITY&resolved=false&ps=500" -Headers $headers -TimeoutSec 30
    $hotspots = Invoke-RestMethod -Uri "$HostUrl/api/hotspots/search?project=$key&ps=500" -Headers $headers -TimeoutSec 30
    $version = Invoke-RestMethod -Uri "$HostUrl/api/server/version" -Headers $headers -TimeoutSec 30
    $commit = & git rev-parse HEAD 2>$null
    if ($LASTEXITCODE -ne 0) { $commit = 'sem commit' }
    $workingTreeChanged = [bool](& git status --porcelain)
    $evidence = Join-Path $projectRoot 'docs/evidencias'
    New-Item -ItemType Directory -Path $evidence -Force | Out-Null
    foreach ($entry in @{
        'sonar-qualitygate.json' = $gate; 'sonar-metrics.json' = $metrics
        'sonar-vulnerabilities.json' = $vulnerabilities; 'sonar-hotspots.json' = $hotspots
        'sonar-security-issues.json' = $securityIssues
    }.GetEnumerator()) {
        [IO.File]::WriteAllText((Join-Path $evidence $entry.Key), ($entry.Value | ConvertTo-Json -Depth 50), [Text.UTF8Encoding]::new($false))
    }
    $summary = @('# Análise SonarQube', '', "Execução: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz').", '',
        "SonarQube: $version. Projeto: ``$ProjectKey``. Análise: ``$($task.task.analysisId)``.", '',
        "Commit base: ``$commit``. Alterações locais presentes na execução: $workingTreeChanged. A análise considera os arquivos de trabalho.", '',
        "Quality Gate: **$($gate.projectStatus.status)**.", '', '| Métrica | Valor |', '|---|---:|')
    foreach ($measure in $metrics.component.measures | Sort-Object metric) { $summary += "| $($measure.metric) | $($measure.value) |" }
    $summary += @('', '## Condições do Quality Gate', '')
    if (@($gate.projectStatus.conditions).Count -eq 0) {
        $summary += 'Esta análise não apresentou condições avaliadas no Quality Gate. O status OK não significa ausência de achados de segurança; consulte as métricas e as listas de problemas.'
    } else {
        $summary += @('| Métrica | Resultado | Valor | Limite |', '|---|---|---:|---:|')
        foreach ($condition in $gate.projectStatus.conditions) { $summary += "| $($condition.metricKey) | $($condition.status) | $($condition.actualValue) | $($condition.errorThreshold) |" }
    }
    $summary += @('', 'Evidências: [métricas](sonar-metrics.json), [Quality Gate](sonar-qualitygate.json), [vulnerabilidades abertas na classificação legada](sonar-vulnerabilities.json), [problemas com impacto em segurança no modo MQR](sonar-security-issues.json) e [Security Hotspots](sonar-hotspots.json). As consultas de achados incluem até 500 itens; confira o total e a paginação dos JSONs.', '',
        'Security Hotspots exigem revisão contextual; não são automaticamente vulnerabilidades confirmadas. O resultado descreve as regras disponíveis nesta edição do SonarQube e não substitui a auditoria de dependências.', '',
        'Reprodução: consulte [Análise com SonarQube](../SONAR.md). A cobertura desta execução vem dos testes completos com MySQL e SMTP local.', '',
        "[Painel local]($HostUrl/dashboard?id=$key). O endereço localhost é acessível somente no computador que executa o SonarQube.")
    [IO.File]::WriteAllLines((Join-Path $evidence 'sonar.md'), $summary, [Text.UTF8Encoding]::new($false))
    if ($scanExitCode -ne 0 -or $gate.projectStatus.status -ne 'OK') { throw 'Análise enviada, mas houve falha no scanner ou no Quality Gate. Evidências salvas em docs/evidencias/sonar.md.' }
    Write-Output "Análise concluída. Painel: $HostUrl/dashboard?id=$ProjectKey"
} finally { Pop-Location }
