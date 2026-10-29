# Pre-Implement Checklist: AgenteValidacaoECD — Validação ECD

**Purpose**: Validar qualidade, clareza e completude dos **requisitos** antes de `$speckit-implement` (não valida código).

**Created**: 2026-10-29

**Feature**: [spec.md](../spec.md) · [plan.md](../plan.md) · [constitution.md](../../../.specify/memory/constitution.md)

**Note**: Gerado por `$speckit-checklist`. Itens são “unit tests” da escrita de requisitos.

**Review Ownership**: Marque `[x]` somente quando o revisor concluir que o critério de qualidade do requisito está atendido. `[x]` **não** significa implementação concluída.

**Marker Semantics**: `$speckit-implement` pode ler este checklist como gate; agentes **não** devem marcar checkboxes automaticamente.

---

## Requirement Completeness

- [ ] CHK001 Estão documentados requisitos para **todas** as etapas do pipeline (recebimento → relatório → status), alinhados ao plano e à constitution P5? [Completeness, Spec US1 · Plan §Pipeline]
- [ ] CHK002 Existe requisito explícito de **preservação do arquivo original** imutável em todo o ciclo? [Completeness, Spec FR-003]
- [ ] CHK003 Os requisitos do **motor de regras** cobrem metadados obrigatórios (ID, versão, referência oficial, severidade, condições)? [Completeness, Spec FR-009]
- [ ] CHK004 Estão especificados requisitos para **todas** as categorias de resultado (Erro, Alerta, N/A, Não verificado, etc.)? [Completeness, Spec §Taxonomia]
- [ ] CHK005 Os requisitos de **ingestão RAG** cobrem os 10 passos do plano (importação → histórico de versões)? [Completeness, Spec FR-028..036 · Plan §RAG]
- [ ] CHK006 Está definido requisito de **rastreio** da versão do manual e referências documentais por execução? [Completeness, Spec FR-035, FR-038, SC-007]
- [ ] CHK007 Existe artefato de requisitos para **Matriz de Leiautes V1** ou dependência DEP documentada até D2 ser fechada? [Gap, Spec §Pendência D2 · DEP-002]
- [ ] CHK008 Está previsto requisito de **catálogo de regras V1** vinculado a layouts e fontes oficiais (mesmo que vazio inicialmente)? [Gap, Spec DEP-002 · Plan §Motor]

## Requirement Clarity

- [ ] CHK009 O termo **“validação concluída”** está definido de forma que **proíbe** conformidade plena com validações obrigatórias pendentes? [Clarity, Spec FR-019, SC-006]
- [ ] CHK010 Está claro que **hipóteses de IA** não podem ser classificadas como **Erro** confirmado? [Clarity, Spec FR-013 · Constitution P4]
- [ ] CHK011 Os limites de **escopo fiscal** (somente ECD; exclui ECF) estão inequívocos em spec e plano? [Clarity, Spec FR-026]
- [ ] CHK012 A distinção entre **JSON do LLM** e **envelope operacional da API** está documentada nos requisitos/contratos? [Clarity, Constitution P12 · contracts/agent-response-schema.json]
- [ ] CHK013 Está especificado o comportamento quando **leiaute/período** não puder ser identificado (campos explícitos vs. inferência)? [Clarity, Spec FR-004 · Edge Cases]
- [ ] CHK014 Os requisitos de **deduplicação** na indexação RAG definem critério verificável (ex.: hash/chave)? [Clarity, Spec FR-034 · Plan §RAG]

## Requirement Consistency

- [ ] CHK015 Os requisitos de stack (.NET 10, Api.Agente/Api.RAG, MCP) são **consistentes** entre spec, plan e constitution (sem .NET 8 como norma)? [Consistency, Spec §Alinhamento · Plan · Constitution P6–P7]
- [ ] CHK016 A regra “**atualização RAG não altera regras determinísticas**” aparece de forma alinhada em spec, clarify e FR-039? [Consistency, Spec FR-039 · Clarifications RAG]
- [ ] CHK017 Os outcomes da taxonomia spec alinham-se às categorias constitution P4 (incl. falha técnica)? [Consistency, Spec §Taxonomia · Constitution P4]
- [ ] CHK018 Prioridades de user stories (US1/US2 vs US4 RAG) são coerentes com dependências descritas no plan e tasks? [Consistency, Plan · tasks.md §Dependencies]

## Acceptance Criteria Quality

- [ ] CHK019 Os **SC-001..SC-007** são mensuráveis e verificáveis sem conhecer implementação? [Measurability, Spec §Success Criteria]
- [ ] CHK020 Cada user story P1 possui **Independent Test** objetivo na spec? [Measurability, Spec §User Scenarios]
- [ ] CHK021 A matriz **T-01..T-22** liga cenários de aceite a FRs/US de forma rastreável? [Traceability, Spec §Traceability]
- [ ] CHK022 Os critérios de aceite distinguem **MVP mínimo** (só determinístico) vs **MVP P1** (com RAG+IA)? [Clarity, tasks.md §MVP scope]

## Scenario Coverage

- [ ] CHK023 Estão documentados requisitos para fluxo **primário** upload → processamento → relatório? [Coverage, Spec US1]
- [ ] CHK024 Estão documentados requisitos para **explicação IA** pós-validação e chat contextual? [Coverage, Spec US2]
- [ ] CHK025 Estão documentados requisitos para **histórico** e **exportação** tabular? [Coverage, Spec US3, FR-017–018]
- [ ] CHK026 Estão documentados requisitos para **administração** de manuais e status de ingestão? [Coverage, Spec US4]
- [ ] CHK027 Estão documentados requisitos para **interface Angular** ou deferimento explícito para incremento P3? [Coverage, Spec US5 · Assumptions]

## Edge Case Coverage

- [ ] CHK028 Os requisitos cobrem arquivo **corrompido/inválido/grande** sem falso “concluído”? [Edge Case, Spec §Edge Cases]
- [ ] CHK029 Estão definidos requisitos para **falha** de motor, RAG ou LLM com validações não concluídas explícitas? [Exception Flow, Spec Edge · FR-019]
- [ ] CHK030 Estão definidos requisitos para **prompt injection** / conteúdo ECD como dado não confiável? [Edge Case, Spec Edge · Constitution P11]
- [ ] CHK031 Estão definidos requisitos para **ingestão parcial/falha** e consulta de erros? [Exception Flow, Spec FR-036 · Edge Cases RAG]
- [ ] CHK032 Estão definidos requisitos para **execução duplicada** ou cancelamento (mesmo que detalhe fique no plano)? [Gap/Plan, Plan §Resiliência · tasks T060]

## Non-Functional Requirements

- [ ] CHK033 Existem metas de desempenho **quantificadas** ou marcadas como a definir no plano (SC-001, parsing)? [NFR, Spec SC-001 · Plan Technical Context]
- [ ] CHK034 Requisitos de **LGPD**, retenção e envio de dados contábeis a IA externa estão especificados ou registrados como ADR pendente? [NFR, Spec FR-023 · Assumptions · ADR D5]
- [ ] CHK035 Requisitos de **autenticação/autorização** estão identificados com decisão D4 explícita? [NFR, Spec FR-020 · DEP-006]
- [ ] CHK036 Requisitos de **auditoria** (execuções, regras, versões) estão completos o suficiente para revisão interna? [NFR, Spec FR-021]
- [ ] CHK037 Requisitos de **consumo/custo LLM** distinguem estimado vs. informado pelo provedor? [NFR, Constitution P15 · Spec FR-025]

## Dependencies & Assumptions

- [ ] CHK038 Cada **DEP-xxx** na spec indica impacto e o que permanece “Não verificado” até liberação? [Dependencies, Spec §Dependencies]
- [ ] CHK039 As **decisões pendentes D1–D12** da constitution estão refletidas no plan/research (sem virar requisito falso)? [Assumption, Constitution §9 · research.md]
- [ ] CHK040 A dependência de **manuais oficiais SPED** (DEP-001) está clara para regras e RAG? [Dependency, Spec DEP-001]
- [ ] CHK041 O plano documenta **dual database** e fronteira MCP sem contradizer spec? [Dependency, Plan · research.md · ADR-001 Proposed]

## Ambiguities & Conflicts

- [ ] CHK042 O marcador **[NEEDS CLARIFICATION] D2** (leiautes V1) foi resolvido ou explicitamente aceito como gate antes de regras normativas? [Ambiguity, Spec §Pendência]
- [ ] CHK043 Existe conflito residual entre briefing (.NET 8, SQL-only) e constitution/plano, com resolução documentada? [Conflict, Spec Clarifications · Plan Summary]
- [ ] CHK044 Parâmetros RAG (chunk size, topK, limiares) estão marcados como **a especificar no plano/piloto**, não inventados na spec? [Ambiguity, Constitution D7 · research.md]
- [ ] CHK045 Requisitos de **Angular** vs **API-first MVP** estão sem ambiguidade sobre o que é obrigatório na V1? [Ambiguity, Spec Assumptions · US5 P3]

## Constitution & SDD Gate

- [ ] CHK046 Os princípios P3 (determinístico antes LLM) e P4 (classificação) possuem requisitos espelhados na spec? [Traceability, Constitution P3–P4 · Spec]
- [ ] CHK047 A proibição de **inventar norma/layout** (P2) está refletida em FR-027 e critérios SC-004? [Traceability, Constitution P2]
- [ ] CHK048 O fluxo SDD (spec → plan → tasks) está completo o suficiente para iniciar implementação Phase 2+? [Process, Constitution §6 · tasks.md]
- [ ] CHK049 O checklist `requirements.md` (spec quality) foi revisado e gaps remanescentes estão listados nas Notes? [Process, checklists/requirements.md]

---

## Notes

- Revisar **CHK007, CHK008, CHK042** juntos após fechar D2 e publicar matriz/catálogo.
- Itens `[Gap]` podem exigir `$speckit-clarify` ou novo artefato em `specs/001-validacao-ecd/`, não código.
- Após ≥90% dos itens críticos (CHK001–CHK020, CHK042, CHK046–CHK048) marcados `[x]`, prosseguir com `$speckit-implement` Phase 2.
