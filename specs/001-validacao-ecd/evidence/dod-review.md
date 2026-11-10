# Revisão DoD constitucional — release interna (T063)

**Data**: 2026-11-05  
**Escopo**: feature `001-validacao-ecd`, backend (US1–US4) + Phase 8. UI Angular (US5, T055–T058) fora deste corte.  
**Referência**: [constitution §7](../../../.specify/memory/constitution.md) DOD-01..12  
**Evidências**: [smoke.md](./smoke.md), `dotnet test AgenteValidacaoECD.slnx` (36/36 com Docker ativo)

## Veredito

**Release interna de homologação (backend): aprovável com ressalvas.**  
**Release para piloto com usuários: não aprovada** até resolver os bloqueios B1–B4.

| Status | Itens |
|--------|-------|
| Atende | DOD-05, DOD-06, DOD-09 |
| Atende parcialmente | DOD-01, DOD-02, DOD-03, DOD-04, DOD-07, DOD-08, DOD-12 |
| Não atende | DOD-10, DOD-11 |

## Checklist

| # | Critério | Status | Evidência / lacuna |
|---|----------|--------|--------------------|
| DOD-01 | Spec aprovada e critérios de aceite verificados | Parcial | `checklists/requirements.md` 13/16 (abertos: detalhes de implementação na spec e marcadores `[NEEDS CLARIFICATION]`). `checklists/pre-implement.md` 0/49 revisados. SC-001/004/005 dependem de piloto e LLM real. |
| DOD-02 | Plano/tarefas atualizados e vinculados à spec | Parcial | T001–T054 e T059–T063 concluídas; T055–T058 (UI) abertas. Contrato DELETE e estado `Running → Cancelled` atualizados em `contracts/` e `data-model.md`. |
| DOD-03 | Implementação respeita P1–P18 e AR-* | Parcial | P7/AR-01 ok (Agente só fala MCP com RAG). P3 ok (regras antes do LLM). Lacunas: **P2/D2**, porque `BasicLayoutDetector` grava o campo 2 do `0000` (`LECD`) como versão de leiaute, sem fonte normativa; **P16/D9** A2A pós-MVP (spec §Escopo item 10). |
| DOD-04 | Validadores determinísticos e testes no CI | Parcial | 36 testes automatizados; regras SAMPLE testadas. **Não existe pipeline de CI** no repositório. Testes RAG exigem PostgreSQL e saem sem asserção quando ele não existe. |
| DOD-05 | Contrato de saída versionado e validado | Atende | `agent-response-schema.json` embutido; `AgentResponseSchemaValidatorTests`; `schemaVersion=1.0.0` no relatório do smoke. |
| DOD-06 | Integração MCP Agente↔RAG testada | Atende (após correção) | Defeito de contrato MCP encontrado no smoke e corrigido. `McpContractTests` valida schema plano e chamada real. Smoke 16/16 com `manualVersionUsed` preenchido. Lado Agente ainda usa MCP fake nos testes automatizados. |
| DOD-07 | Consumo/custos registrados para LLM/embeddings | Parcial | `LlmUsageRecorder` grava em PostgreSQL com tokens nulos (desconhecidos ≠ zero, conforme P15). Não exercitado sem `OpenAI:ApiKey`. Embeddings da RAG não registram consumo. Tabela de preços D11 indefinida. |
| DOD-08 | Logs correlacionados por execução, sem segredos | Parcial | Traces OpenTelemetry com `ecd.execution_id` em todas as fases (T059). Mensagens do worker trazem `{ExecutionId}` no template, mas o `ExecutionIdEnricher` só enriquece dentro de requisições HTTP. Chaves JWT **DEV-ONLY** versionadas em `appsettings.json`: aceitável em homologação, proibido em produção (§5.1). |
| DOD-09 | Documentação de operação/config atualizada | Atende | README raiz, `deploy/README.md`, quickstart alinhado a paths reais, `fixtures/README.md`, scripts `New-DevJwt.ps1` e `Invoke-Smoke.ps1`. |
| DOD-10 | Revisão de PR com checklist constitucional | Não atende | Mudanças ainda não commitadas nem revisadas em PR. |
| DOD-11 | ADRs exigidos resolvidos ou deferidos com exceção | Parcial | ADR-001 aceito em 2026-11-10 (PostgreSQL único, Neon/Supabase). ADR-002..005 continuam `Proposed`; D1 (Redis) foi implementado sem o ADR-002 aceito. |
| DOD-12 | Nenhuma conformidade plena com validações pendentes | Parcial | Narrativa declara "não constitui parecer de conformidade" e `limitations` cita regras SAMPLE. Porém `skippedValidations=[]` e `requiresHumanReview=false` num arquivo sem achados, enquanto o catálogo V1 normativo está vazio. Isso enfraquece SC-006. |

## Bloqueios para piloto

| ID | Bloqueio | DOD | Ação sugerida |
|----|----------|-----|---------------|
| B1 | Catálogo de regras é só SAMPLE e a versão de leiaute não tem base normativa | 03, 12 | Matriz de Leiautes V1 (D2) e regras oficiais; até lá, listar o catálogo V1 ausente em `skippedValidations` e marcar `requiresHumanReview=true` |
| B2 | Sem CI | 04 | Workflow `dotnet build` + `dotnet test` com serviço PostgreSQL |
| B3 | ADRs `Proposed` | 11 | Aceitar ou registrar exceção para ADR-002..005 (ADR-001 já aceito) |
| B4 | Vulnerabilidades NuGet conhecidas | 03 (§5.1) | `Microsoft.SemanticKernel.Core` 1.67.0 (crítica, GHSA-2ww3-72rp-wpp4) e `System.Security.Cryptography.Pkcs` 9.0.4 (alta, GHSA-846f-f764-76mx) — atualizar |

## Ressalvas não bloqueantes

- Enriquecimento de logs do worker com `ExecutionId` via `LogContext.PushProperty`.
- Smoke com LLM real para exercitar `LlmUsageRecord` e SC-004.
- Cancelamento de `Running` ponta a ponta com fixture maior.
- Teste automatizado do `RagMcpClient` contra o host RAG real (hoje coberto só pelo smoke).
