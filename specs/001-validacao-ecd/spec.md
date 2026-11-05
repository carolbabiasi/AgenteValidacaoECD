# Feature Specification: AgenteValidacaoECD — Validação da Escrituração Contábil Digital

**Feature Branch**: `001-validacao-ecd`

**Created**: 2026-10-29

**Status**: Draft

**Constitution**: AgenteValidacaoECD v1.0.0 (`.specify/memory/constitution.md`)

**Input**: Agente de IA para analisar, validar e identificar inconsistências em arquivos
da ECD (SPED), uso interno Infis Consultoria, combinando motor de regras determinísticas,
relatórios rastreáveis, interpretação assistida por IA e evolução incremental (API, agente,
RAG, interface).

## Visão Geral

O **AgenteValidacaoECD** automatiza a conferência de arquivos da **Escrituração Contábil
Digital (ECD)**, reduzindo trabalho manual e aumentando a confiabilidade das análises.
Profissionais contábeis da Infis **DEVEM** poder enviar arquivos ECD, acompanhar o
processamento, consultar inconsistências com evidências objetivas e obter explicações
assistidas por IA **sem** que a IA substitua regras verificáveis ou invente normas SPED.

A solução **NÃO DEVE** incluir validação da **ECF** nem de outras escriturações fiscais
no escopo inicial. Regras oficiais **NÃO DEVEM** ser inventadas: cada regra implementada
**DEVE** referenciar documentação oficial ou registro explícito de dependência pendente.

### Objetivo da V1 (MVP funcional)

Entregar um fluxo ponta a ponta que permita:

1. Importar um arquivo ECD e preservar o original.
2. Identificar período e versão de leiaute quando os metadados do arquivo permitirem.
3. Interpretar registros e executar **validações determinísticas** (estruturais e contábeis
   inicialmente acordadas).
4. Consolidar resultados com classificação, severidade, evidências e regras executadas.
5. Gerar **relatório** consultável e exportável (formatos mínimos: estruturado na API e
   exportação tabular).
6. Acionar o **agente de IA** para explicar achados confirmados e diferenciar alertas,
   hipóteses e lacunas — conforme constitution (P3, P4, P12).

Interface web Angular e integrações avançadas **PODEM** seguir em incrementos posteriores
(P2/P3), desde que o MVP entregue valor via API e relatório.

## Alinhamento constitucional (normativo)

Em conflito entre esta spec e requisitos genéricos do briefing de produto, **prevalece**
a constitution v1.0.0. Pontos que **DEVEM** orientar o plano técnico:

| Tema | Obrigação constitucional |
|------|---------------------------|
| Runtime | **.NET 10**, ASP.NET Core (não .NET 8) |
| Serviços | **Api.Agente** (orquestração, LLM, SK) e **Api.RAG** (ingestão, embeddings) |
| Integração Agente↔RAG | **MCP**; Api.Agente **não** acessa datastore interno da RAG |
| LLM | OpenAI; saída **JSON versionada** validada; metadados operacionais pelo sistema |
| RAG | Manuais com origem/versão; citações recuperáveis; conteúdo não confiável |
| Pipeline | Nove etapas canônicas (constitution P5) |
| Conformidade | **Não** declarar arquivo válido com validações obrigatórias pendentes |

Persistência: **PostgreSQL + PgVector** na Api.RAG; **PostgreSQL** para consumo/custos
LLM (P15). Persistência adicional de execuções, regras e resultados operacionais **SERÁ**
detalhada no `$speckit-plan` (incluindo decisão sobre SQL Server corporativo vs.
consolidação em PostgreSQL — ver Assunções).

## Clarifications

### Session 2026-10-29

Decisões adotadas nesta sessão para reduzir ambiguidade (demais itens em Assunções ou
decisões pendentes):

- Q: A IA substitui o motor de regras? → A: **Não**. Regras determinísticas são fonte
  de achados confirmados; IA interpreta, agrupa, explica e marca hipóteses (P3).
- Q: Escopo fiscal inicial? → A: **Somente ECD**; ECF e demais escriturações **fora**
  de escopo.
- Q: Onde ficam manuais SPED? → A: Base de conhecimento administrada pela **Api.RAG**
  com ingestão documentada; consulta via **MCP** na validação.
- Q: Entrega mínima de interface? → A: **V1** prioriza **API REST** e exportação de
  relatório; **Angular** como incremento **P2** após MVP API estável.
- Q: Conflito briefing (.NET 8 / SQL Server) vs constitution? → A: **Constitution
  prevalece** para stack agente/RAG; plano técnico registra ADR se SQL Server for
  mantido para dados operacionais corporativos.

### Session 2026-10-29 — Gerenciamento RAG (Manual ECD)

Entrada do stakeholder incorporada nesta sessão `$speckit-clarify`:

- Q: Qual a fonte da base de conhecimento? → A: **Manual da Escrituração Contábil
  Digital (ECD)** (documento oficial SPED), administrado pela **Api.RAG**.
- Q: O que o processo de ingestão MUST fazer? → A: Importar novas versões; extrair e
  organizar texto; **preservar estrutura** (capítulos, seções, registros, referências)
  quando possível; segmentar em trechos para recuperação; gerar **embeddings** e
  indexar em busca vetorial; registrar **versão**, **data de publicação** e metadados.
- Q: Como tratar atualizações e duplicidade? → A: Atualizar base ao importar nova versão;
  **evitar duplicidades** na indexação; **manter histórico** de versões usadas nas análises.
- Q: Observabilidade da ingestão? → A: Registrar **erros** e permitir **consulta ao status**
  do processamento de ingestão.
- Q: Uso na análise ECD? → A: Agente **recupera trechos relevantes** do manual para
  fundamentar explicações de inconsistências; execução **MUST** permitir rastrear versão
  do manual e referências documentais (transparência e reprodutibilidade).
- Q: Atualização RAG altera regras determinísticas? → A: **Não**. Alterações no motor de
  regras exigem processo **separado** de revisão, implementação e testes (independente da
  ingestão de manual).

### Pendência registrada

- **[NEEDS CLARIFICATION: quais versões de leiaute ECD a V1 MUST suportar na primeira
  entrega operacional?]** — impacto direto no catálogo de regras e testes (constitution
  D2). Até resposta, regras **SÓ DEVEM** ser implementadas para versões listadas no
  artefato **Matriz de Leiautes V1** (a popular a partir de fontes oficiais SPED).

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Validar arquivo ECD e obter relatório (Priority: P1)

Como **contador da Infis**, quero **enviar um arquivo ECD para validação** e **receber
um relatório** com inconsistências, registros envolvidos, severidade e evidências das
regras executadas, para **substituir parte da conferência manual** com rastreabilidade.

**Why this priority**: Entrega o valor central do produto; demais capacidades dependem
deste fluxo.

**Independent Test**: Enviar arquivo ECD de teste (válido e inválido) via API, aguardar
conclusão e verificar relatório com contagem de regras executadas, achados classificados
e indicação explícita de validações **não verificadas** quando aplicável.

**Acceptance Scenarios**:

1. **Given** um arquivo ECD legível dentro dos limites de tamanho, **When** solicito
   validação, **Then** recebo identificador de execução, status final e relatório com
   período/leiaute identificados **ou** campo explícito de “não identificado”.
2. **Given** um arquivo com violação estrutural conhecida no dataset de teste, **When**
   a validação conclui, **Then** existe achado classificado como **Erro** (regra
   determinística) com registro, linha (se disponível), regra e mensagem objetiva.
3. **Given** validações obrigatórias da V1 não aplicáveis ao arquivo, **When** o
   relatório é gerado, **Then** essas regras aparecem como **Não aplicável** ou **Não
   verificado**, e o resumo **não** declara conformidade integral sem ressalvas.
4. **Given** processamento concluído, **When** consulto o relatório, **Then** o arquivo
   original permanece íntegro e identificável (hash ou referência de armazenamento).

---

### User Story 2 — Compreender achados com apoio do agente de IA (Priority: P1)

Como **contador**, quero **perguntas e explicações sobre os resultados da validação**
em linguagem clara, com distinção entre erro confirmado e alerta, para **priorizar
correções** sem confundir hipótese com fato.

**Why this priority**: Diferencia o produto de um validador “seco”; alinhado ao objetivo
de negócio, sem violar P3.

**Independent Test**: Após validação com achados mistos, solicitar explicação via agente;
verificar que explicações de **Erro** citam evidências de regra/valor esperado; alertas
e hipóteses estão rotulados; ausência de norma inventada quando RAG não retorna fonte.

**Acceptance Scenarios**:

1. **Given** achados determinísticos no relatório, **When** peço explicação de um item,
   **Then** a resposta referencia registro, regra e valores encontrados/esperados quando
   existirem no motor.
2. **Given** pergunta sobre requisito normativo, **When** RAG não recupera evidência
   suficiente, **Then** o agente declara limitação e **não** cita artigo ou regra
   inexistente como fato.
3. **Given** sugestão de correção, **When** apresentada, **Then** está rotulada como
   **orientação** sujeita a julgamento profissional, não como ordem automática de
   alteração do arquivo.

---

### User Story 3 — Consultar histórico e exportar relatório (Priority: P2)

Como **contador**, quero **consultar validações anteriores** e **exportar o relatório**
em formato tabular adequado à rotina interna, para **documentar trabalho e compartilhar
com a equipe**.

**Why this priority**: Suporta operação contínua após o MVP de validação pontual.

**Independent Test**: Listar execuções por usuário/período, reabrir relatório e baixar
exportação com mesmos achados da consulta API.

**Acceptance Scenarios**:

1. **Given** execuções anteriores do usuário autorizado, **When** consulto histórico,
   **Then** vejo data, arquivo, status, contagem de achados e versão de leiaute.
2. **Given** relatório concluído, **When** solicito exportação, **Then** recebo arquivo
   tabular contendo achados, severidade, registro, linha e identificador da regra.
3. **Given** usuário sem permissão, **When** tenta acessar execução alheia, **Then**
   acesso é negado e o evento é registrado para auditoria.

---

### User Story 4 — Administrar base de conhecimento SPED (Priority: P2)

Como **administrador técnico**, quero **ingestar e versionar manuais oficiais** aplicáveis
à ECD, para **fundamentar respostas RAG** usadas pelo agente.

**Why this priority**: Habilita explicações normativas auditáveis (P11); depende de Api.RAG.

**Independent Test**: Ingerir documento de teste com metadados de versão; recuperar chunk
via MCP filtrado por versão de leiaute; verificar citação na resposta do agente.

**Acceptance Scenarios**:

1. **Given** manual com metadados oficiais, **When** executo ingestão, **Then** chunks
   ficam consultáveis com origem, versão e localização.
2. **Given** leiaute V1 da matriz, **When** agente consulta RAG, **Then** recuperação
   respeita versão/tipo documental configurados.
3. **Given** nova versão do Manual ECD publicada, **When** importo via ingestão, **Then**
   versão anterior permanece no histórico, chunks da nova versão ficam disponíveis e
   status/erros da ingestão são consultáveis.
4. **Given** reimportação do mesmo manual sem mudança de conteúdo, **When** indexação
   executa deduplicação, **Then** não há chunks duplicados funcionalmente equivalentes
   na mesma versão lógica do manual.
5. **Given** ingestão concluída, **When** uma validação ECD usa RAG, **Then** o relatório
   ou metadados da execução registram **versão do manual** e referências documentais
   citadas.

---

### User Story 5 — Acompanhar validação na interface web (Priority: P3)

Como **contador**, quero **importar arquivo, acompanhar progresso e filtrar achados**
em interface web moderna, para **operar sem ferramentas de API manual**.

**Why this priority**: Melhora UX interna; não bloqueia MVP API (P1).

**Independent Test**: Fluxo UI espelha User Story 1–3 (upload, status, filtros por
severidade/categoria/registro, detalhe, exportação).

**Acceptance Scenarios**:

1. **Given** usuário autenticado, **When** envio ECD pela tela, **Then** vejo progresso
   até conclusão ou falha explicada.
2. **Given** relatório com múltiplos achados, **When** aplico filtros, **Then** lista
   reflete categoria, severidade e registro selecionados.

---

### Edge Cases

- Arquivo **corrompido**, vazio ou extensão/formato não suportado → rejeição na entrada
  com mensagem clara; **nenhuma** validação parcial apresentada como concluída.
- Arquivo **muito grande** → rejeição ou processamento assíncrono conforme limites
  definidos no plano; consumo de memória controlado.
- **Leiaute não reconhecido** → interpretação limitada; regras dependentes de versão
  marcadas **Não verificado**; relatório explicita impossibilidade de validação plena.
- **Falha** do motor de regras, RAG ou LLM → achado/falha técnica (P4); execução registra
  etapa falha; usuário informado de validações não concluídas.
- **Registros duplicados, fora de ordem ou malformados** → apontamentos estruturais com
  linha/registro quando parser disponibilizar.
- Tentativa de **prompt injection** via conteúdo ECD → conteúdo tratado como dado; políticas
  do agente inalteradas.
- Envio a **IA externa** sem autorização → bloqueado por política de segurança (configuração
  e trilha de auditoria).
- **Ingestão** do manual falha parcialmente → job registra erros por etapa; versão **não**
  é marcada como ativa para recuperação até conclusão bem-sucedida ou aceite explícito
  administrativo (política no plano).
- **Leiaute ECD indeterminado** na execução → recuperação RAG usa **versão ativa padrão**
  configurada pelo administrador para Manual ECD; versão efetiva **MUST** constar no
  registro da análise.

## Requirements *(mandatory)*

### Functional Requirements

#### Importação e interpretação

- **FR-001**: O sistema **MUST** aceitar upload de arquivo ECD via API REST, associando
  usuário, contexto organizacional (quando aplicável) e identificador único de execução.
- **FR-002**: O sistema **MUST** validar integridade, formato suportado e limites de
  tamanho antes do processamento profundo.
- **FR-003**: O sistema **MUST** preservar o arquivo original imutável durante todo o
  ciclo (referência ou cópia versionada).
- **FR-004**: O sistema **MUST** identificar período de escrituração e versão de leiaute
  quando metadados do arquivo permitirem; caso contrário **MUST** registrar explicitamente
  a indeterminação.
- **FR-005**: O sistema **MUST** interpretar registros, blocos, hierarquia e sequência
  conforme leiaute aplicável **documentado** (não inferido pelo LLM).
- **FR-006**: O sistema **MUST** detectar registros ausentes, duplicados, malformados ou
  fora da sequência esperada quando o leiaute V1 definir critérios verificáveis.

#### Validação estrutural e contábil (motor de regras)

- **FR-007**: O sistema **MUST** possuir motor de regras executando validações
  estruturais: campos, tipos, obrigatoriedade, formatos, hierarquia, blocos e registros
  obrigatórios condicionais — **por versão de leiaute catalogada**.
- **FR-008**: O sistema **MUST** executar validações contábeis determinísticas acordadas
  para V1 (ex.: coerência débito/crédito, saldos e relações entre registros contábeis)
  **somente** quando definidas com critério objetivo e referência documental na matriz
  de regras.
- **FR-009**: Cada regra **MUST** possuir, quando aplicável: ID único, nome, descrição,
  categoria, registros, condições de execução, critério, mensagem, severidade, referência
  oficial, versão e vigência.
- **FR-010**: Regras **MUST** ser versionadas independentemente do código e **MUST** ter
  testes automatizados com fixtures ECD representativos.
- **FR-011**: O motor **MUST** permitir evoluir regras sem reestruturação arquitetural
  completa (cadastro/configuração conforme plano).

#### Agente de IA e RAG

- **FR-012**: O agente **MUST** explicar achados, agrupar relacionados, sugerir causas
  prováveis e recomendar investigação **sem** alterar o arquivo fonte.
- **FR-013**: O agente **MUST NOT** apresentar hipótese de IA como **Erro** confirmado;
  classificações **MUST** respeitar P4 e a taxonomia de resultados (seção abaixo).
- **FR-014**: Explicações normativas **MUST** citar referências RAG recuperáveis ou
  declarar ausência de evidência.
- **FR-015**: A Api.Agente **MUST** consultar conhecimento via **MCP** na Api.RAG durante
  o pipeline quando a spec da execução exigir fundamentação documental.

#### Gerenciamento da base de conhecimento (Manual ECD — Api.RAG)

- **FR-028**: A Api.RAG **MUST** expor processo de **ingestão** (incl. POST de ingestão)
  para importar **novas versões** do Manual da ECD.
- **FR-029**: A ingestão **MUST** extrair e organizar conteúdo textual, **preservando**
  estrutura de capítulos, seções, registros e referências **sempre que possível** no
  formato fonte. O formato oficial de publicação do manual (**PDF**) **MUST** ser aceito,
  com o **número de página** preservado como localização documental dos trechos; `.txt`/`.md`
  permanecem aceitos.
- **FR-030**: O conteúdo **MUST** ser dividido em **trechos (chunks)** adequados à
  recuperação contextual; parâmetros de segmentação **MUST** ser definidos e avaliados
  no plano (constitution D7).
- **FR-031**: A Api.RAG **MUST** gerar **embeddings** e indexar trechos em **base de
  busca vetorial** (PgVector), com modelo e dimensão documentados.
- **FR-032**: Cada versão ingestada **MUST** possuir metadados: identificação da fonte,
  **versão do manual**, **tipo de documento** (`Manual`, `Faq`, `Norm`), **código da série**
  (`documentCode`, ex.: `MANUAL`, `FAQ-RFB`), **data de publicação** (quando disponível) e
  demais metadados oficiais aplicáveis. Ativar uma versão **MUST** arquivar somente a versão
  ativa anterior da mesma `(layoutFamily, documentCode)`. Além do Manual da ECD, a base **MAY** conter documentos oficiais
  complementares (perguntas frequentes da RFB, instruções normativas, normas do CFC); cada
  documento **MUST** declarar seu **tipo** (`Manual`, `Faq`, `Norm`) e um **código de série**
  estável (ex.: `MANUAL`, `IN-RFB-2003-2021`). Ativar uma versão **MUST** arquivar apenas a
  versão ativa anterior da **mesma série** (FR-033), e as referências documentais **MUST**
  indicar tipo e série do trecho citado (FR-038).
- **FR-033**: Importar nova versão **MUST** **atualizar** a base consultável sem apagar
  histórico de versões anteriores (versionamento lógico retido).
- **FR-034**: A indexação **MUST** **evitar duplicidades** (mesmo conteúdo/versão reindexado
  sem necessidade); estratégia de deduplicação **MUST** ser testável.
- **FR-035**: O sistema **MUST** **manter histórico** das versões do manual **utilizadas**
  em cada análise de ECD (vínculo execução ↔ versão manual ↔ referências recuperadas).
- **FR-036**: A ingestão **MUST** registrar **erros** por job e **MUST** permitir **consulta
  de status** do processamento (em progresso, concluído, falhou, parcial).
- **FR-037**: Durante análise ECD, o agente **MUST** recuperar trechos **relevantes** do
  manual (filtrados por versão/leiaute quando identificável) e usá-los como **referência**
  nas explicações, sem tratar conteúdo do manual como instrução de sistema (P11).
- **FR-038**: Cada execução **MUST** permitir **rastrear** versão do manual e **referências
  documentais** (localização/capítulo/seção/chunk recuperável) usadas nas respostas
  fundamentadas em RAG.
- **FR-039**: Atualização da base RAG **MUST NOT** alterar automaticamente **regras
  determinísticas** de validação; mudanças de regras **MUST** seguir fluxo próprio de
  revisão, implementação, versionamento e testes (FR-009, FR-010).

#### Relatório, histórico e exportação

- **FR-016**: O relatório **MUST** incluir: identificação do arquivo, período, leiaute,
  data/hora, quantidade de registros analisados, regras executadas, inconsistências,
  resumo por categoria/severidade, detalhe por achado (registro, linha, valores,
  regra, explicação, orientação), **versão do Manual ECD** referenciada na análise e
  **referências documentais RAG** quando utilizadas em explicações.
- **FR-017**: O sistema **MUST** permitir consulta detalhada de resultados por execução
  e histórico de processamentos autorizados.
- **FR-018**: O sistema **MUST** exportar relatório em formato tabular (Excel ou
  equivalente CSV/XLSX) e **MAY** adicionar PDF em incremento posterior.
- **FR-019**: O resultado **MUST** listar validações **pendentes/não verificadas** e
  **MUST NOT** afirmar validade integral do arquivo se houver pendências obrigatórias.

#### Segurança, auditoria e processamento

- **FR-020**: O sistema **MUST** implementar autenticação e autorização para APIs,
  arquivos e relatórios (detalhe de mecanismo no plano — D4).
- **FR-021**: O sistema **MUST** registrar trilha de auditoria: execuções, regras
  aplicadas, versões de regra, usuário, status e falhas.
- **FR-022**: O sistema **MUST** aplicar controles contra arquivos maliciosos
  (limites, validação de entrada, isolamento de processamento).
- **FR-023**: Dados contábeis **MUST NOT** ser enviados a provedores de IA externos
  sem autorização e controles definidos (política Infis + configuração).
- **FR-024**: Operações longas **MUST** suportar processamento assíncrono com status
  consultável.
- **FR-025**: O sistema **MUST** registrar consumo de tokens/custos de IA por execução
  quando houver chamadas ao provedor (P15).

#### Fora de escopo (V1)

- **FR-026**: O sistema **MUST NOT** validar ECF ou outras escriturações SPED fora ECD
  neste épico.
- **FR-027**: O sistema **MUST NOT** implementar regras sem referência oficial ou registro
  de dependência **DEP-xxx** explícita na matriz de regras.

### Taxonomia de resultados (qualidade)

Cada resultado de regra ou achado **MUST** mapear para:

| Resultado | Significado |
|-----------|-------------|
| **Erro** | Violação confirmada de regra determinística aplicável |
| **Alerta** | Situação relevante que não configura violação confirmada |
| **Inconsistência provável** | Divergência que exige confirmação (inclui indícios IA) |
| **Revisão necessária** | Depende de julgamento profissional |
| **Validado** | Regra executada sem violação |
| **Não aplicável** | Regra não se aplica ao arquivo/contexto |
| **Não verificado** | Regra não pôde ser executada (dependência, falha, leiaute) |

Falhas técnicas **MUST** ser distinguidas e **MUST** aparecer no relatório de execução
(constitution P4).

### Key Entities

- **Execução de validação**: Correlação única, usuário, timestamps, status, leiaute,
  período, métricas (registros, regras, achados), pendências.
- **Arquivo ECD**: Metadados, referência ao original, hash, tamanho, estado de retenção.
- **Registro interpretado**: Tipo, campos parseados, linha origem, bloco, relações pai/filho.
- **Regra de validação**: Definição versionada, vigência, severidade padrão, referência normativa.
- **Resultado de regra**: Vínculo execução+regra, outcome da taxonomia, evidências estruturadas.
- **Achado / inconsistência**: Agregação apresentável (pode combinar resultados de regras),
  severidade, registros, valores encontrado/esperado, explicação IA opcional.
- **Relatório**: Visão consolidada exportável e consultável por execução.
- **Documento de conhecimento**: Manual ECD oficial ingestado, metadados de publicação,
  versão lógica, estado (rascunho/ativo/arquivado).
- **Job de ingestão**: Identificador, status, timestamps, erros, versão alvo, operador.
- **Trecho (chunk)**: Texto segmentado, hierarquia estrutural (capítulo/seção/registro),
  embedding, referência de localização na fonte.
- **Referência documental RAG**: Ponteiro recuperável (chunk + localização + versão manual)
  vinculado a explicação ou achado.
- **Snapshot RAG da execução**: Conjunto de referências e versão do manual efetivamente
  usados naquela validação (reprodutibilidade).
- **Consumo de IA**: Tokens, modelo, custo estimado, correlação com execução.
- **Usuário / organização**: Identidade, permissões, escopo de acesso a execuções.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em ambiente de homologação, um contador **conclui** upload e obtém relatório
  completo de um arquivo ECD de teste padrão em **até 15 minutos** incluindo tempo de
  fila/processamento (excluindo arquivos acima do limite V1).
- **SC-002**: Para o **conjunto de regras V1** aprovado com fixtures oficiais, **100%**
  dos casos de teste automatizados passam com outcome esperado (Erro/Validado/N/A).
- **SC-003**: **95%** dos achados classificados como **Erro** no relatório possuem
  identificador de regra, registro (ou justificativa de indisponibilidade) e evidência
  objetiva registrada.
- **SC-004**: Em revisão amostral de **20** explicações IA pós-validação, **100%**
  diferenciam erro confirmado de alerta/hipótese; **0%** citam norma sem referência
  RAG recuperável ou declaração de limitação.
- **SC-005**: Usuários piloto Infis (**≥ 3**) completam o fluxo P1 (envio → relatório →
  interpretação) sem suporte técnico em **≥ 80%** das sessões de teste guiado.
- **SC-006**: Nenhum relatório de execução concluída declara “sem pendências” quando
  existir regra obrigatória V1 em estado **Não verificado** (verificação automatizada
  em testes de integração).
- **SC-007**: Em execuções que utilizem RAG para explicações, **100%** registram versão
  do Manual ECD e **≥ 1** referência documental recuperável **ou** declaração explícita
  de ausência de evidência normativa.

## Assumptions

- Usuários são **colaboradores internos** Infis Consultoria, com treinamento básico em SPED/ECD.
- Arquivos ECD V1 seguem formatos previstos na **Matriz de Leiautes V1** (a ser populada
  a partir de fontes oficiais antes de codificar regras específicas).
- Autenticação V1 **assume** integração com mecanismo corporativo Infis (SSO ou API Key
  interna) até decisão formal D4 no plano de segurança.
- Retenção padrão provisória de arquivos e relatórios: **12 meses**, sujeita a ADR D5/LGPD.
- Interface **Angular** é desejável para operação, mas **não** bloqueia aceite do MVP API.
- Processamento de arquivos grandes usa **streaming/chunked parsing**; limites numéricos
  exatos ficam para NFR no plano.
- Provedor de IA inicial: **OpenAI** (constitution); substituição exige ADR sem reescrever
  motor de regras.
- Ambiente inicial: **homologação interna**; produção e SLA corporativo definidos após piloto.
- Recuperação RAG durante análise **prioriza** manual cuja versão **corresponda** ao leiaute
  ECD identificado; se indeterminado, usa **versão ativa padrão** configurada pelo
  administrador (registrada na execução).

## Dependencies

| ID | Dependência | Impacto |
|----|-------------|---------|
| DEP-001 | Publicação/acesso a **manuais e leiautes oficiais** SPED/ECD aplicáveis | Regras, RAG, testes |
| DEP-002 | **Matriz de Leiautes V1** e **catálogo de regras V1** derivados de DEP-001 | Escopo MVP |
| DEP-003 | Credenciais **OpenAI** e política Infis para dados contábeis em IA | Agente |
| DEP-004 | Infra **PostgreSQL** (operacional + PgVector) | Api.RAG, consumo |
| DEP-005 | Decisão **D2** (versões ECD in scope) | Cronograma de regras |
| DEP-006 | Decisão **D4** (auth) | Endpoints e UI |
| DEP-007 | Pacote/biblioteca de parsing ECD (**D3**) | Interpretação registros |

Regras cujo critério dependa de documentação ainda não disponível **MUST** registrar
**DEP-xxx** e permanecer **Não verificado** até liberação.

## Risks

| Risco | Mitigação |
|-------|-----------|
| Escopo de leiautes ECD amplo demais na V1 | Matriz V1 mínima + clarificação D2; regras incrementais |
| Invenção de norma via LLM | P2, P11, testes SC-004, validação de saída estruturada |
| Performance em arquivos grandes | Processamento assíncrono, limites, testes de volume |
| Divergência briefing vs constitution | Plano referencia constitution; ADRs para exceções |
| LGPD / dados sensíveis | Retenção D5, criptografia em repouso no plano, mínimo envio a IA |

## Plano de implementação incremental (orientação)

Ordem sugerida para `$speckit-plan` / `$speckit-tasks` (não substitui tarefas detalhadas):

1. Requisitos e matriz de leiautes/regras V1 (com DEP-001/002).
2. Histórias P1 e critérios de aceite automatizados.
3. Arquitetura Api.Agente + Api.RAG (MCP), domínio e contratos API.
4. Leitura/interpretação ECD (D3) e motor de regras com testes.
5. Pipeline P5, relatório, histórico, exportação tabular.
6. Api.RAG ingestão + recuperação versionada.
7. Integração OpenAI/SK, saída JSON, consumo/custos.
8. Segurança, auditoria, async.
9. Interface Angular (P3).
10. **A2A SOFIA (D9)** — Phase 9: Agent Card + `message/send` + contrato v1.0 (homolog). PDF (FR-018) e ferramentas externas (D10) permanecem pós-MVP.
    A ingestão do manual em PDF (FR-029) faz parte do MVP.

## Traceability — Testes de aceitação (matriz inicial)

| ID | Verifica | Relacionado |
|----|----------|-------------|
| T-01 | Upload válido → execução concluída | FR-001, FR-002, US1 |
| T-02 | Upload inválido → rejeição clara | Edge cases, FR-002 |
| T-03 | Preservação do original | FR-003 |
| T-04 | Identificação período/leiaute | FR-004 |
| T-05 | Achado estrutural determinístico | FR-007, SC-002 |
| T-06 | Achado contábil determinístico V1 | FR-008, SC-002 |
| T-07 | Metadados completos de regra | FR-009 |
| T-08 | Regressão de regra versionada | FR-010 |
| T-09 | Explicação IA com distinção erro/alerta | FR-012, FR-013, SC-004 |
| T-10 | Citação RAG ou limitação | FR-014, FR-015 |
| T-11 | Campos obrigatórios do relatório | FR-016 |
| T-12 | Histórico e autorização | FR-017, FR-020, US3 |
| T-13 | Exportação tabular | FR-018 |
| T-14 | Pendências explícitas | FR-019, SC-006 |
| T-15 | Trilha de auditoria | FR-021 |
| T-16 | Arquivo grande / async | FR-024, Edge cases |
| T-17 | Registro de consumo IA | FR-025 |
| T-18 | Ausência de escopo ECF | FR-026 |
| T-19 | Ingestão manual: status e erros | FR-028, FR-036, US4 |
| T-20 | Deduplicação na reindexação | FR-034, US4 |
| T-21 | Rastreio versão manual + refs na execução | FR-035, FR-038, SC-007 |
| T-22 | Nova versão RAG não altera regras | FR-039 |

## Constitution cross-reference

Esta feature **MUST** satisfazer princípios P1–P18 e DoD constitucional no `$speckit-plan`
e revisões de PR. Decisões pendentes D1–D12 permanecem registradas na constitution até
resolução nos artefatos derivados.
