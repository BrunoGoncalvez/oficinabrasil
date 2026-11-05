# Análise com SonarQube

O SonarScanner for .NET analisa o código da solução e envia os resultados ao SonarQube local. A auditoria NuGet em `scripts/security.ps1` complementa essa verificação com alertas das dependências.

## Iniciar o ambiente local

Com Docker Desktop em execução, na raiz do repositório:

```powershell
docker compose -f docker-compose.sonar.yml up -d
docker compose up -d mysql
```

Aguarde o SonarQube iniciar e abra `http://localhost:9000`. Em uma instalação nova, entre com `admin` / `admin`, altere a senha e crie o projeto local com chave `oficinabrasil`. Em **My Account > Security**, gere um token de análise para esse projeto.

Esse Compose é independente do Compose da aplicação e usa volumes próprios. A instância é destinada à avaliação local, com banco embarcado; não é uma instalação de produção.

Se esta máquina já foi configurada e existir `.artifacts/sonar-local.json`, as credenciais locais estão nos campos `AdminUser` e `AdminPassword` desse arquivo ignorado pelo Git. Não o compartilhe ou inclua na entrega. Nesse caso, não se aplicam as credenciais iniciais de uma instalação nova.

## Executar a análise

Defina o token apenas no processo atual, sem gravá-lo no histórico do terminal:

```powershell
$secret = Read-Host 'Token do SonarQube' -AsSecureString
$env:SONAR_TOKEN = [System.Net.NetworkCredential]::new('', $secret).Password
$env:OFICINA_TEST_MYSQL = 'Server=127.0.0.1;Port=3307;User=root;Password=root_dev_only;'
try {
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sonar.ps1
} finally {
    Remove-Item Env:SONAR_TOKEN -ErrorAction SilentlyContinue
}
```

O script instala o scanner 12.0.0 em `.artifacts/tools`, inicia a análise, recompila a solução, executa todos os testes com MySQL real e gera cobertura OpenCover. Por fim, envia o resultado e aguarda o Quality Gate. A execução falha se o build, os testes, o envio ou o Quality Gate falharem.

Depois do processamento, o script salva `sonar.md` e os JSONs de métricas, Quality Gate, vulnerabilidades e hotspots em `docs/evidencias/`, inclusive quando o Quality Gate reprova a análise.

Os bancos de testes usam nomes `oficina_test_<GUID>` e são removidos ao final da suíte. Os arquivos intermediários ficam em `.artifacts/` e `.sonarqube/`, excluídos do Git e do contexto de build do Docker. Não publique tokens, credenciais locais ou o diretório interno do scanner.

## Evidências da entrega

No painel `http://localhost:9000/dashboard?id=oficinabrasil`, confira o Quality Gate, os problemas de segurança, os Security Hotspots e a cobertura. Um hotspot exige revisão e não equivale automaticamente a uma vulnerabilidade confirmada.

O [resultado da execução](evidencias/sonar.md) registra as métricas e os JSONs exportados. A [avaliação dos achados](evidencias/sonar-achados.md) apresenta o contexto e as correções recomendadas para os problemas identificados. O status do Quality Gate deve ser lido junto com as condições efetivamente avaliadas e com a lista de problemas.

Registre a versão analisada, a data, as métricas, os achados e as decisões de revisão. Inclua capturas do painel e dos problemas relevantes em `docs/evidencias/`. Uma instalação local não é acessível aos professores pelo endereço localhost; as evidências devem acompanhar o repositório.

Para encerrar o SonarQube preservando os dados:

```powershell
docker compose -f docker-compose.sonar.yml down
```

## Referências

- [SonarScanner for .NET: execução](https://docs.sonarsource.com/sonarqube-server/analyzing-source-code/scanners/dotnet/using).
- [Cobertura de testes .NET](https://docs.sonarsource.com/sonarqube-server/analyzing-source-code/test-coverage/dotnet-test-coverage).
- [SonarQube Community Build local](https://docs.sonarsource.com/sonarqube-community-build/try-out-sonarqube).
