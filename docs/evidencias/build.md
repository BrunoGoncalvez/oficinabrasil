# Build e publicação verificados

Verificação em 2026-11-02.

| Verificação | Resultado |
|---|---|
| Solução .NET local | Compilação concluída; 0 erros e 0 avisos |
| Docker Compose (configuração combinada) | `docker compose -f docker-compose.yml -f docker-compose.demo.yml config --quiet` concluído |
| Dockerfile Linux / Release | Build e publicação da API concluídos, código de saída 0 |
| Imagem local | `oficinabrasil-corrigido-api:latest`, Linux/amd64 |
| Identificação da imagem | `sha256:3f29d9ad04f545d9303fac174fb45b8e66635738ce3cd8fc1ab4c7f27e4e1409` |
| Runtime da imagem | Microsoft.NETCore.App e Microsoft.AspNetCore.App 8.0.33 |

Comando do build Docker:

```text
docker compose -f docker-compose.yml -f docker-compose.demo.yml build api
```

[Log completo do build Linux](docker-build.log). A imagem foi construída localmente; não foi publicada em registry. Esta evidência comprova compilação/publicação em Linux. O fluxo HTTP com MySQL e o envio SMTP são exercitados nos [testes automatizados](resultados.trx), cuja [cobertura](cobertura.md) inclui todos os projetos de produção.

O hash de imagem não substitui o hash Git exigido pela faculdade. Registrar branch e commit final após a publicação do repositório, conforme [conferência da entrega](../ENTREGA.md).
