# Feature 002 — Regras oficiais de valores e consistência contábil (PVA ECD)

**Status:** especificação  
**Leiaute alvo:** ECD Leiaute 9  
**Fonte normativa primária:** Manual de Orientação da ECD – Leiaute 9, atualização **janeiro/2026** (`docs/Manual_de_Orientação_da_ECD_Leiaute_9_janeiro_2026.pdf`)  
**Fonte de mensagens e comportamento:** PVA da ECD **Sped Contábil 10.4.1** (JVM 21.0.3), mesma versão usada nas evidências de paridade

## Objetivo

Ampliar o validador para reproduzir **somente** as validações oficiais da Receita Federal relacionadas a **valores e consistências contábeis** (não reescrever nem substituir as validações estruturais já entregues na feature 001).

## Escopo funcional

### Inclui

- Levantamento completo, no manual e no PVA, das regras oficiais nas categorias:
  - Valores de lançamentos; débitos e créditos; partidas.
  - Saldos contábeis e saldos periódicos.
  - Encerramento de contas de resultado e apuração do resultado.
  - Demonstrações contábeis e consistência entre registros.
  - Comparações de valores entre registros (cruzamentos oficiais).
- Implementação determinística por regra oficial, com:
  - Código **oficial** da regra (como no PVA).
  - Registro(s), campos, condição, fórmula (se houver), mensagem oficial, severidade (erro / advertência / sem ocorrência).
  - Referência de página (ou localização estrutural) no manual janeiro/2026.
- Fixtures `ecds-teste/` (ou pasta dedicada `ecds-teste-pva/`) com **uma divergência intencional por regra** implementada.
- Evidência de paridade: mesmo arquivo validado no **PVA ECD** e no agente; diff documentado.
- Matriz de rastreabilidade (artefato `rule-matrix.md`) como entrega obrigatória.

### Exclui (restrições explícitas)

- Criar regras de auditoria ou “boas práticas” sem código oficial.
- Inventar códigos, mensagens ou fórmulas não presentes no manual/PVA.
- Tratar situação contabilmente incomum como erro oficial sem amparo PVA.
- **Alterar** implementação ou severidade das validações estruturais e regras `ECD-CONTENT-*` já ativas (podem coexistir; eventual desativação só após mapeamento 1:1 com regra oficial homologada).

## Requisitos

| ID | Requisito |
|----|-----------|
| FR-201 | Cada regra implementada **MUST** usar identificador e mensagem **oficiais do PVA**; `OfficialReference` no catálogo **MUST** apontar para manual janeiro/2026. |
| FR-202 | Regras oficiais **MUST** registrar-se em categoria `Accounting` (ou subcategoria documentada); **MUST NOT** reclassificar regras estruturais existentes. |
| FR-203 | Resultado **MUST** mapear para taxonomia do projeto (erro ↔ `RuleOutcome.Error`, advertência ↔ `RuleOutcome.Alert` conforme PVA). |
| FR-204 | Implementação **MUST** ser plugin `IValidationRule` com `ImplementationKey` estável; catálogo Postgres via seeder **versionado** (novo conjunto `ECD-PVA-*` ou código literal do PVA). |
| FR-205 | Matriz **MUST** listar todas as regras oficiais do escopo; linhas `Não implementado` **MUST** permanecer visíveis no relatório como `NotVerified` / pendência. |
| FR-206 | Testes **MUST** incluir comparação documentada com PVA (versão do instalador, data, captura ou log exportado). |

## Critérios de aceite (épico)

1. `rule-matrix.md` cobre 100% das regras oficiais de **valores/consistência contábil** do leiaute 9 conforme levantamento assinado na spec.
2. Cada regra marcada “Implementada” possui fixture + teste de integração + evidência PVA.
3. Nenhuma regra nova sem linha na matriz com referência de página do manual.
4. Relatório e SOFIA exibem mensagem **oficial** (não paráfrase inventada).

## Dependências

| ID | Dependência |
|----|-------------|
| DEP-PVA-001 | Instalação do PVA ECD compatível com leiaute 9 e manual janeiro/2026 para geração de evidências. |
| DEP-PVA-002 | Exportação ou documentação da lista de regras/mensagens do PVA (se não estiver integralmente no manual). |
| DEP-ECD-001 | Parser de registros I/J/K… existente; pode exigir índices adicionais (saldos por conta/período) sem mudar regras estruturais. |

## Riscos

- Volume elevado de regras oficiais (centenas) — entrega incremental por bloco/registro.
- Divergência manual × PVA — **prevalece o PVA** para mensagem e severidade; manual para fundamentação e página.
- Regras `ECD-CONTENT-*` atuais **não** são códigos PVA — exigem auditoria de equivalência antes de desligamento.

