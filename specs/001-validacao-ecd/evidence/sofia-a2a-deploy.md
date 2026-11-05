# Evidência — integração SOFIA/A2A (homolog)

## Endpoints

| Recurso | URL dev |
|---------|---------|
| Agent Card | `GET http://localhost:5100/.well-known/agent-card.json` |
| A2A JSON-RPC | `POST http://localhost:5100/a2a` (`message/send`) |
| REST legado | `POST http://localhost:5100/api/v1/validations` |

## Configuração

```json
{
  "Sofia": {
    "PublicBaseUrl": "http://localhost:5100",
    "A2aServiceKey": "<segredo-producao>"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200", "http://localhost:5173"]
  }
}
```

- **SOFIA backend → agente**: header `X-Sofia-Service-Key` (ou JWT `Validator`/`Admin`).
- **Browser UI SOFIA**: CORS `SofiaBrowser` só quando `Cors:AllowedOrigins` estiver preenchido; integração server-server **não depende** de CORS.

## Smoke manual

```powershell
# Card
Invoke-RestMethod http://localhost:5100/.well-known/agent-card.json

# A2A (Development sem chave ou com X-Sofia-Service-Key)
$body = @{
  jsonrpc = "2.0"
  id = "smoke-1"
  method = "message/send"
  params = @{
    message = @{
      role = "user"
      messageId = [guid]::NewGuid().ToString()
      parts = @(@{
        kind = "text"
        text = (@{
          versaoContrato = "1.0"
          idExecucao = [guid]::NewGuid()
          idSolicitacao = [guid]::NewGuid()
          usuario = @{ nome = "Smoke SOFIA" }
          pergunta = "Validar ECD"
          documentos = @(@{
            idDocumento = "doc-1"
            nome = "ecd.txt"
            texto = "|0000|LECD|9.0|01012025|31122025|EMP|`r`n|0990|2|`r`n"
          })
        } | ConvertTo-Json -Compress)
      })
    }
  }
} | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Post -Uri http://localhost:5100/a2a -ContentType "application/json" -Body $body -Headers @{ "X-Sofia-Service-Key" = "<opcional>" }
```

## Testes automatizados

- `tests/Api.Agente.Tests/Unit/SofiaContractValidatorTests.cs`
- `tests/Api.Agente.Tests/Integration/SofiaA2aContractTests.cs`

## Backlog

- T069: resposta A2A assíncrona (`working` + polling) para validações longas (>3 min).
