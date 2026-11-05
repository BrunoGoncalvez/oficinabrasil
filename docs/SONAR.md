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

Depois do processamento, o script publica somente `docs/evidencias/sonar.md`, reunindo as métricas, a cobertura, o Quality Gate e os apontamentos de segurança, inclusive quando o Quality Gate reprova a análise. Os JSONs técnicos ficam em `.artifacts/sonar-export/`, ignorado pelo Git.

Os bancos de testes usam nomes `oficina_test_<GUID>` e são removidos ao final da suíte. Os arquivos intermediários ficam em `.artifacts/` e `.sonarqube/`, excluídos do Git e do contexto de build do Docker. Não publique tokens, credenciais locais ou o diretório interno do scanner.

## Evidências da entrega

No painel `http://localhost:9000/dashboard?id=oficinabrasil`, confira o Quality Gate, os problemas de segurança, os Security Hotspots e a cobertura. Um hotspot exige revisão e não equivale automaticamente a uma vulnerabilidade confirmada.

O [resultado da análise](evidencias/sonar.md) reúne as métricas, os testes, a cobertura e a avaliação dos achados em um único documento. O status do Quality Gate deve ser lido junto com as condições efetivamente avaliadas e com a lista de problemas.

O relatório registra a versão analisada, a data, as métricas e os achados. Uma instalação local não é acessível aos professores pelo endereço localhost; o documento permite consultar o resultado no repositório.

Para encerrar o SonarQube preservando os dados:

```powershell
docker compose -f docker-compose.sonar.yml down
```

## Referências

- [SonarScanner for .NET: execução](https://docs.sonarsource.com/sonarqube-server/analyzing-source-code/scanners/dotnet/using).
- [Cobertura de testes .NET](https://docs.sonarsource.com/sonarqube-server/analyzing-source-code/test-coverage/dotnet-test-coverage).
- [SonarQube Community Build local](https://docs.sonarsource.com/sonarqube-community-build/try-out-sonarqube).
