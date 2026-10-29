<!--
Sync Impact Report
- Version change: (nenhuma) → 1.0.0
- Adoção inicial da constitution AgenteValidacaoECD
- Artefatos impactados (quando existirem): templates Spec Kit em `.specify/templates/`,
  futuras `spec.md`, `plan.md`, `tasks.md`
- Decisões pendentes registradas na seção 9 (D1–D12)
-->

# Constitution — AgenteValidacaoECD

**Versão:** 1.0.0  
**Ratificada em:** 2026-10-29  
**Última alteração:** 2026-10-29  

Documento normativo que orienta especificações, arquitetura, planos técnicos, tarefas,
implementações e revisões do **AgenteValidacaoECD**. Em caso de conflito com práticas
ad hoc ou preferências individuais, **esta constitution prevalece**.

---

## 1. Propósito e escopo

### 1.1 Propósito

O **AgenteValidacaoECD** é um agente de Inteligência Artificial responsável por
**analisar, validar a estrutura e a consistência** das informações contidas em arquivos
da **Escrituração Contábil Digital (ECD)**, identificando inconsistências, divergências
e possíveis erros de preenchimento ou estruturação dos registros.

A solução **DEVE** automatizar e otimizar a validação da ECD, aplicando regras de
negócio e critérios técnicos relacionados ao leiaute e às exigências do **Sistema Público
de Escrituração Digital (SPED)**. O agente **DEVE** interpretar registros, verificar
integridade, cruzar dados entre registros relacionados e apresentar resultados de forma
clara e organizada.

Ao final do processamento, o agente **DEVE** gerar um **relatório** com inconsistências
identificadas, indicando registros envolvidos, descrição dos problemas e, quando possível,
orientações para correção.

### 1.2 Escopo da solução

A solução **DEVE** combinar, de forma complementar e rastreável:

| Capacidade | Papel |
|------------|--------|
| Validação determinística | Arquivo, estrutura, registros e regras verificáveis |
| RAG sobre manuais | Fundamentação normativa recuperável |
| Ferramentas externas | Execução controlada e autorizada |
| LLM (OpenAI) | Síntese, classificação contextual e explicações |
| Saída estruturada | Contrato JSON versionado e validado |
| A2A (futuro) | Exposição para orquestradores e outros agentes |

### 1.3 Fora de escopo desta constitution

Esta constitution **NÃO** define leiautes ECD, regras fiscais, schemas XSD, tabelas de
domínio SPED nem critérios de conformidade legal. Esses elementos **DEVEM** ser obtidos
de **documentos oficiais** e registrados nas **especificações** e na base de conhecimento
(RAG), com origem, identificação e versão.

**NÃO DEVE** ser tratado como requisito aprovado qualquer suposição sobre layout, versão
de manual ou regra de negócio não documentada em fonte oficial ou spec aprovada.

### 1.4 Verificação (seção 1)

| Critério | Como verificar |
|----------|----------------|
| Escopo respeitado | PR referencia spec; não há validadores “inventados” sem fonte |
| Relatório exigido | Critérios de aceite da feature de análise incluem artefato de saída |
| Separação norma/código | Manuais e regras citam documento oficial na spec ou metadados RAG |

---

## 2. Princípios obrigatórios

Cada princípio apresenta **regra**, **justificativa** e **verificação** (aplicável em PR).

### P1 — Desenvolvimento orientado por especificação (SDD)

**Regra:** Todo trabalho de produto e engenharia **DEVE** seguir SDD. A sequência
obrigatória é: **constitution → especificação → esclarecimentos → plano técnico →
tarefas → implementação → validação**. Mudanças de comportamento **DEVEM** ser
refletidas primeiro nos artefatos de especificação; só então no código. Implementação
**NÃO DEVE** contradizer requisitos documentados e aprovados.

**Justificativa:** Domínio SPED/ECD exige rastreabilidade entre norma, regra implementada
e evidência de teste; SDD evita deriva entre intenção e código.

**Verificação:** PR cita spec/tarefa; diff alinha-se aos critérios de aceite; revisores
confirmam ausência de escopo não especificado.

---

### P2 — Verdade normativa e proibição de invenção

**Regra:** Layouts, versões de leiaute, schemas, regras estruturais e de negócio
**DEVEM** derivar de fontes oficiais ou specs aprovadas. O LLM e o RAG **NÃO DEVEM**
inventar artigos de manual, trechos normativos, resultados de ferramentas, referências
ou evidências. Na ausência de base suficiente, o sistema **DEVE** declarar **limitação**
ou **ausência de evidências**.

**Justificativa:** Conformidade contábil e responsabilidade técnica exigem fundamentação
auditável, não narrativa plausível.

**Verificação:** Achados com referência RAG possuem `chunk_id`/fonte recuperável; testes
reproduzem casos com manual conhecido; respostas sem retrieval **NÃO** afirmam norma
como fato.

---

### P3 — Validação determinística antes e independente do LLM

**Regra:** Regras estruturais e de negócio verificáveis **DEVEM** ser implementadas em
**validadores**, **ferramentas** ou serviços determinísticos com testes. O LLM **NÃO DEVE**
substituir validação obrigatória de estrutura, schema ou cruzamento exigido pela spec.
Validação por **XSD** **DEVE** ser aplicada **somente** a documentos XML para os quais
exista schema correspondente na spec; demais formatos **DEVEM** usar validadores
compatíveis com o leiaute ECD aplicável.

**Justificativa:** Resultados de conformidade estrutural devem ser reproduzíveis e
independentes de modelo probabilístico.

**Verificação:** Pipeline executa validadores antes da etapa LLM; testes unitários cobrem
regras determinísticas; PR que remove validador exige atualização de spec.

---

### P4 — Classificação obrigatória dos achados

**Regra:** Todo item reportado **DEVE** ser classificado em exatamente uma categoria
principal (com severidade quando aplicável):

1. **Erro estrutural** — violação de formato, schema ou hierarquia de registros  
2. **Inconsistência de negócio** — violação de regra determinística de domínio  
3. **Indício identificado pela IA** — hipótese ou interpretação assistida por LLM  
4. **Ausência de evidências** — não foi possível fundamentar ou confirmar  
5. **Falha técnica ou dependência indisponível** — timeout, erro de ferramenta, RAG/LLM indisponível  

O agente **NÃO DEVE** declarar **conformidade** ou “sem pendências” quando validações
obrigatórias da spec **não** tiverem sido concluídas; **DEVE** listar **validações não
realizadas** e impacto na conclusão.

**Justificativa:** Usuários e auditores precisam distinguir fato verificado de sugestão
de IA e de lacuna operacional.

**Verificação:** Contrato JSON inclui `tipo`/`categoria` por achado; testes de integração
cobrem cenários por categoria; relatório final explicita validações pendentes.

---

### P5 — Fluxo de validação documental (pipeline canônico)

**Regra:** O processamento de um arquivo ECD **DEVE** seguir, no mínimo, as etapas abaixo
(ordem lógica preservada; paralelismo **PODE** ser definido no plano técnico se não
alterar garantias):

1. Receber arquivo e identificar **usuário**, **contexto** e **identificador de execução**  
2. Validar **tamanho**, **formato** e condições de processamento  
3. Identificar **tipo de documento** e **versão do layout**  
4. Validar **estrutura**, **registros** e **schema** aplicável  
5. Consultar chunks do manual correspondente via **MCP** (Api.RAG)  
6. Executar **ferramentas externas** quando necessário e **autorizado**  
7. Produzir **análise com LLM** (Semantic Kernel + OpenAI)  
8. **Validar saída estruturada** (schema e regras semânticas)  
9. Retornar resultado com **evidências** e **rastreabilidade**  

**Justificativa:** Pipeline uniforme garante repetibilidade, observabilidade e defesa
em revisão de PR.

**Verificação:** Diagrama ou descrição no `plan.md`; logs/traces correlacionados por
`executionId`; testes de integração percorrem etapas 1–9.

---

### P6 — Stack oficial .NET 10 e serviços nomeados

**Regra:** A solução **DEVE** utilizar **.NET 10** com **ASP.NET Core**. Os serviços
principais **DEVEM** ser nomeados **Api.Agente** e **Api.RAG**. Desvios de stack ou
adição de projetos **DEVEM** ser justificados em ADR e spec antes da implementação.

**Justificativa:** Stack única e nomes estáveis reduzem ambiguidade operacional e de
integração.

**Verificação:** Solution contém projetos alinhados; CI usa SDK .NET 10; PRs novos
projetos referenciam ADR.

---

### P7 — Separação Api.Agente e Api.RAG com MCP

**Regra:** A comunicação funcional entre **Api.Agente** e **Api.RAG** **DEVE** ocorrer
via **MCP** (cliente MCP na Api.Agente; servidor MCP na Api.RAG). Cada serviço **DEVE**
ser **proprietário** dos seus dados. A Api.Agente **NÃO DEVE** acessar diretamente
tabelas, coleções ou vetores internos da Api.RAG (incluindo PostgreSQL/PgVector da RAG).

**Justificativa:** Limites de bounded context, evolução independente e segurança de dados
de conhecimento.

**Verificação:** Ausência de connection string da RAG na Api.Agente; chamadas de
recuperação passam por contrato MCP; testes de contrato MCP.

---

### P8 — Responsabilidades da Api.Agente

**Regra:** A Api.Agente **DEVE**:

- Expor **API REST** para execução do agente  
- Receber arquivos e solicitações de análise  
- **Orquestrar** o pipeline de validação (P5)  
- Integrar-se à **OpenAI**  
- Consultar a Api.RAG como **cliente MCP**  
- Utilizar **Semantic Kernel** para integração e **Function Calling**  
- Gerenciar **memória conversacional**  
- Registrar **consumo de tokens e custos**  
- **Validar** a resposta estruturada do LLM  
- Expor o agente via **servidor A2A** (quando feature aprovada na spec)  

**Justificativa:** Ponto único de orquestração do agente ECD e fronteira com usuários
e orquestradores.

**Verificação:** Checklist de arquitetura no plano; endpoints documentados; SK plugins
registrados; métricas de token persistidas.

---

### P9 — Responsabilidades da Api.RAG

**Regra:** A Api.RAG **DEVE**:

- Expor **API REST** para administração da base de conhecimento  
- Expor endpoint **POST** de **ingestão**  
- Processar documentos, gerar **chunks** e **embeddings**  
- Armazenar e consultar vetores em **PostgreSQL com PgVector**  
- Expor ferramentas de recuperação via **servidor MCP**  

**Justificativa:** Conhecimento normativo isolado, versionável e administrável.

**Verificação:** APIs de ingestão e metadados testadas; embeddings dimensionais
consistentes; ferramentas MCP documentadas.

---

### P10 — Camadas e simplicidade arquitetural

**Regra:** Cada serviço **DEVE** separar **Endpoints (API)**, **Aplicação**, **Domínio**
e **Infraestrutura**. Bibliotecas ou projetos adicionais **SÓ DEVEM** ser criados quando
justificados na spec ou ADR (reuso real, testabilidade ou fronteira clara). **NÃO DEVE**
haver complexidade sem necessidade documentada.

**Justificativa:** Manutenção e onboarding; alinhamento a Clean Architecture em .NET.

**Verificação:** Estrutura de pastas/projetos reflete camadas; PR sem justificativa
rejeita novo projeto “organizacional”.

---

### P11 — Conhecimento RAG: proveniência e recuperação

**Regra:**

- Manuais e documentos **DEVEM** possuir **origem**, **identificação** e **versão**  
- Cada chunk **DEVE** permitir recuperar **fonte** e **localização** (quando disponível)  
- Ingestão **DEVE** definir na spec: segmentação, deduplicação, reprocessamento e
  atualização  
- Modelo de embeddings, dimensão e estratégia de busca **DEVEM** ser **compatíveis** e
  **documentados** no plano técnico  
- Recuperação **DEVE** respeitar **tipo de documento** e **versão do manual** aplicável  
- Respostas fundamentadas no RAG **DEVEM** incluir **referências recuperáveis**  
- Conteúdo de arquivos ECD e chunks **DEVE** ser tratado como **dado não confiável**
  (prompt injection): **NÃO DEVE** alterar regras, políticas ou instruções do sistema  

Parâmetros como tamanho de chunk, topK e limiares **NÃO DEVEM** ser fixados arbitrariamente
nesta constitution; **DEVEM** ser **especificados e avaliados** com exemplos representativos
na spec/plano.

**Justificativa:** RAG auditável e seguro contra contaminação por conteúdo do arquivo
analisado.

**Verificação:** Metadados de chunk em respostas MCP; testes de ingestão; testes de
filtragem por versão; revisão de prompt contra injection.

---

### P12 — LLM OpenAI e saída estruturada

**Regra:**

- Modelo e parâmetros de execução **DEVEM** ser **configuração externa** (não hardcoded)  
- **DEVE** existir contrato **JSON explícito e versionado** para a resposta do agente  
- **DEVE** usar mecanismos de saída estruturada compatíveis com o modelo escolhido  
- **DEVE** validar estrutura e regras semânticas **antes** de aceitar a resposta  
- **DEVE** haver política **limitada** de retentativas e tratamento de respostas inválidas  
- **NÃO DEVE** fabricar evidências, resultados de ferramentas ou referências  

O contrato **DEVE** contemplar, conforme especificação: identificador de execução, status,
resultado da análise, inconsistências e severidades, evidências e referências, limitações,
validações não realizadas e necessidade de revisão humana.

**Envelope** retornado pela API REST **DEVE** ser distinto do JSON produzido pelo LLM:
identificadores operacionais, consumo, custos, tempos e metadados **DEVEM** ser preenchidos
pelo **sistema**, não pelo modelo.

**Justificativa:** Contrato estável para clientes e orquestradores; redução de alucinação
estrutural.

**Verificação:** JSON Schema ou equivalente versionado; testes de validação de payload;
campos operacionais ausentes no prompt de saída do LLM.

---

### P13 — Ferramentas (Semantic Kernel) e autorização

**Regra:** Cada ferramenta **DEVE** possuir: nome e responsabilidade claros, contratos de
entrada/saída, validação de argumentos, autenticação/autorização, timeout e tratamento de
falhas, política de retentativa e idempotência quando aplicável, testes e registro de
execução.

A **seleção** de ferramenta pelo LLM **NÃO DEVE** conceder **autorização** para executá-la;
autorização **DEVE** ser decidida pela aplicação conforme política (usuário, escopo, feature
flag, etc.) definida na spec.

**Justificativa:** Function calling não substitui controle de acesso nem limites operacionais.

**Verificação:** Testes negativos (LLM “pede” ferramenta sem permissão); logs de decisão
de autorização; contratos tipados nos plugins SK.

---

### P14 — Memória conversacional

**Regra:** Memória conversacional **DEVE** usar **MongoDB** ou **Redis**; a escolha
**DEVE** ser registrada em **ADR antes da implementação** (ver D1). **DEVE** haver:
isolamento por usuário/conversa e por organização quando aplicável; política de retenção,
expiração e exclusão; limite de histórico enviado ao modelo; proteção de dados sensíveis;
**separação** entre histórico conversacional e conhecimento oficial (RAG).

**Justificativa:** Contexto multi-turno sem confundir chat com base normativa.

**Verificação:** ADR publicado; testes de isolamento; configuração de TTL documentada.

---

### P15 — Consumo, tokens e custos

**Regra:** Controle de consumo **DEVE** persistir em **PostgreSQL** (base da Api.Agente
ou serviço dedicado documentado no plano). Por chamada, quando disponível do provedor,
**DEVE** registrar: execução e correlação, provedor e modelo, tokens entrada/saída,
consumo de embeddings quando aplicável, data, duração, status, estimativa de custo e
referência da tabela de preços utilizada.

Chamadas adicionais e retentativas **DEVEM** ser registradas. Valores **estimados**
**DEVEM** ser distinguidos dos **informados pelo provedor**. Consumo desconhecido
**NÃO DEVE** ser registrado como zero.

**Justificativa:** Governança financeira e capacidade de auditoria de uso de IA.

**Verificação:** Migrações/schema de consumo; testes inserem registros em retry; dashboards
ou queries documentadas.

---

### P16 — Integração A2A e independência operacional

**Regra:** A Api.Agente **DEVE** ser capaz de compilar, executar e entregar valor **sem**
orquestrador externo. Integração A2A **DEVE** tratar orquestradores como consumidores
externos. Agent Card e skills **NÃO DEVEM** declarar capacidades inexistentes no código.

**Justificativa:** Desenvolvimento paralelo e deploy isolado.

**Verificação:** README executa agente standalone; Agent Card alinhado a endpoints reais.

---

### P17 — Testes, observabilidade e Definition of Done

**Regra:** Features **DEVEM** incluir testes proporcionais ao risco: unitários para
domínio e validadores; integração para pipeline, MCP, LLM (com mocks/fakes onde
necessário) e contratos JSON. Logs **DEVEM** ser estruturados e correlacionados por
`executionId`. **NÃO DEVE** logar segredos, conteúdo integral de ECD em produção sem
política explícita na spec.

**Justificativa:** Domínio regulado e pipeline multi-etapa exigem evidência automatizada.

**Verificação:** CI executa testes; cobertura mínima definida na spec por feature; revisão
de logs em PR.

---

### P18 — Rastreabilidade requisito → tarefa → teste

**Regra:** Cada funcionalidade **DEVE** ter critérios de aceite **verificáveis** e ligação
explícita entre requisitos (spec), tarefas (`tasks.md`) e testes (nome ou ID).

**Justificativa:** SDD completo e auditoria de entrega.

**Verificação:** Matriz ou links na spec; tasks referenciam IDs de requisito; testes
nomeados ou anotados com mesmo ID.

---

## 3. Restrições arquiteturais

### 3.1 Topologia obrigatória

```
                    ┌─────────────────┐
                    │  Clientes / A2A │
                    └────────┬────────┘
                             │ REST / A2A
                    ┌────────▼────────┐
                    │   Api.Agente    │
                    │  SK + OpenAI    │
                    │  Orquestração   │
                    └────────┬────────┘
                             │ MCP (único canal funcional)
                    ┌────────▼────────┐
                    │    Api.RAG      │
                    │  REST admin     │
                    │  PgVector       │
                    └─────────────────┘
```

### 3.2 Restrições

| ID | Restrição |
|----|-----------|
| AR-01 | Api.Agente **NÃO DEVE** acessar datastore interno da Api.RAG |
| AR-02 | Integração Agente↔RAG **DEVE** ser MCP para recuperação/administração funcional |
| AR-03 | OpenAI **DEVE** ser configurável; credenciais **NÃO DEVEM** estar no repositório |
| AR-04 | Semantic Kernel **DEVE** ser o mecanismo de registro de ferramentas para Function Calling na Api.Agente |
| AR-05 | PostgreSQL com PgVector **DEVE** ser usado na Api.RAG para vetores |
| AR-06 | PostgreSQL **DEVE** ser usado para consumo/custos conforme P15 |
| AR-07 | Endpoints **NÃO DEVEM** conter lógica de domínio pesada; **DEVEM** delegar à aplicação |
| AR-08 | Validadores ECD **DEVEM** residir em domínio ou biblioteca de validação testável, não em prompts |

### 3.3 Verificação (seção 3)

Revisão de arquitetura no `plan.md`, diagrama de deployment, análise de dependências
entre projetos e scan de connection strings cruzadas.

---

## 4. Contratos e governança dos dados

### 4.1 Contrato de resposta do agente (LLM)

- **DEVE** ser versionado (`schemaVersion` ou equivalente na spec).  
- **DEVE** ser validado após geração; falha **DEVE** seguir política de retry limitada
  e, se persistir, status de falha com categoria P4 (5).  
- Campos mínimos orientativos (detalhamento na spec): `executionId`, `status`, `summary`,
  `findings[]` (categoria, severidade, registros, mensagem, evidências, referências),
  `limitations[]`, `skippedValidations[]`, `requiresHumanReview`.

### 4.2 Envelope da API REST (Api.Agente)

- **DEVE** incluir metadados operacionais: timestamps, duração, `correlationId`, consumo
  de tokens, custo estimado, versão da API.  
- **NÃO DEVE** confiar no LLM para preencher metadados operacionais.

### 4.3 Contratos MCP (Api.RAG)

- Ferramentas MCP **DEVEM** ter schemas de entrada/saída documentados.  
- Respostas de retrieval **DEVEM** incluir identificadores de fonte para citação.

### 4.4 Dados ECD e PII

- Arquivos ECD **DEVEM** ser tratados como dados sensíveis; retenção e armazenamento
  **DEVEM** ser definidos na spec (ver D5).  
- Exportação de relatórios **DEVE** respeitar mesma classificação.

### 4.5 Verificação (seção 4)

Contratos publicados em `specs/` ou OpenAPI; testes de contrato; validação JSON Schema
em CI quando aplicável.

---

## 5. Segurança e qualidade

### 5.1 Segurança

| Regra | Detalhe |
|-------|---------|
| Segredos | **NÃO DEVEM** ser commitados; usar user secrets / variáveis de ambiente |
| Autenticação API | **DEVE** ser definida na spec (ver D4); endpoints admin RAG **DEVEM** ser protegidos |
| Autorização ferramentas | Conforme P13; negação padrão |
| Conteúdo não confiável | ECD e chunks **NÃO DEVEM** sobrescrever system instructions |
| Dependências | Vulnerabilidades conhecidas **DEVEM** ser tratadas conforme política do repositório |
| Rate limiting | **PODE** ser exigido na spec para APIs públicas |

### 5.2 Qualidade de código

- **DEVE** seguir convenções .NET documentadas no repositório (editorconfig, analyzers).  
- Análise estática e formatação **DEVEM** passar no CI antes de merge.  
- **NÃO DEVE** mergear código sem testes exigidos pela spec da feature.

### 5.3 Revisão de pull request

Revisores **DEVEM** verificar, quando aplicável:

1. Aderência à constitution e spec  
2. Classificação P4 nos achados  
3. Ausência de norma/layout inventado (P2)  
4. Validadores determinísticos para regras obrigatórias (P3)  
5. MCP como fronteira Agente/RAG (P7)  
6. Contrato JSON validado (P12)  
7. Registro de consumo (P15)  
8. Rastreabilidade P18  

Desvios temporários **DEVEM** referenciar registro de exceção (seção 8).

---

## 6. Fluxo de desenvolvimento SDD

### 6.1 Artefatos Spec Kit (referência)

| Etapa | Artefato típico | Responsável |
|-------|-----------------|-------------|
| Constitution | `.specify/memory/constitution.md` | Arquitetura |
| Especificação | `spec.md` / features | Produto + domínio SPED |
| Esclarecimentos | Q&A registrados na spec | Time |
| Plano técnico | `plan.md` | Engenharia |
| Tarefas | `tasks.md` | Engenharia |
| Implementação | Código + testes | Desenvolvimento |
| Validação | Testes, checklist, demo | QA + revisores |

Comandos Spec Kit (quando configurados): `$speckit-specify`, `$speckit-plan`,
`$speckit-tasks`, `$speckit-implement`, `$speckit-analyze`.

### 6.2 Decisões arquiteturais (ADR)

Decisões relevantes (store de memória, auth, formato ECD prioritário, estratégia de
chunking, modelo de embedding, política de retenção) **DEVEM** ser registradas em
**ADRs** antes ou durante o plano técnico, conforme indicado na seção 9.

### 6.3 Exceções à constitution

Desvios **DEVEM** documentar: justificativa, impacto, responsável, aprovação, prazo de
revisão (se temporário). Merge **NÃO DEVE** ocorrer sem referência à exceção aprovada.

---

## 7. Definition of Done (DoD)

Uma feature ou tarefa **SÓ DEVE** ser considerada **concluída** quando **todos** os
itens aplicáveis abaixo forem atendidos:

| # | Critério |
|---|----------|
| DOD-01 | Spec aprovada e critérios de aceite verificados |
| DOD-02 | Plano/tarefas atualizados e vinculados à spec |
| DOD-03 | Implementação respeita P1–P18 e restrições AR-* |
| DOD-04 | Validadores determinísticos e testes automatizados executando no CI |
| DOD-05 | Contrato de saída (se aplicável) versionado e validado |
| DOD-06 | Integração MCP Agente↔RAG testada (se aplicável à feature) |
| DOD-07 | Consumo/custos registrados para chamadas LLM/embeddings (se aplicável) |
| DOD-08 | Logs correlacionados por execução, sem vazamento de segredos |
| DOD-09 | Documentação de operação/config atualizada (README ou docs/) |
| DOD-10 | Revisão de PR com checklist constitucional |
| DOD-11 | ADRs exigidos na seção 9 resolvidos ou explicitamente deferidos com exceção |
| DOD-12 | Nenhuma declaração de conformidade plena com validações pendentes (P4) |

---

## 8. Governança e alterações

### 8.1 Hierarquia normativa

1. Constitution (este documento)  
2. Especificações aprovadas e ADRs  
3. Planos e tarefas  
4. Código e configuração  

### 8.2 Emenda da constitution

Alterações **DEVEM** incluir:

- Incremento de **versão semântica**  
- Data de alteração  
- **Avaliação de impacto** nas especificações e planos existentes  
- Bloco *Sync Impact Report* no topo do arquivo (como comentário HTML)  

| Tipo | Quando usar |
|------|-------------|
| **MAJOR** | Remoção ou redefinição incompatível de princípio |
| **MINOR** | Novo princípio ou expansão material de obrigações |
| **PATCH** | Clarificações sem mudança de obrigação |

### 8.3 Conformidade em PR

Toda PR **DEVE** declarar aderência aos princípios aplicáveis (template de descrição
**PODE** incluir checklist P1–P18). Revisores **DEVEM** bloquear merge em violação
conhecida não coberta por exceção aprovada.

### 8.4 Orientação complementar

Templates em `.specify/templates/` e skills do projeto **PODEM** complementar esta
constitution; em conflito, **prevalece** esta constitution.

---

## 9. Decisões pendentes

Itens **não definidos** nesta constitution **NÃO** são requisitos aprovados. Devem ser
resolvidos na etapa indicada.

| ID | Decisão | Etapa de resolução | Notas |
|----|---------|-------------------|-------|
| D1 | **MongoDB vs Redis** para memória conversacional | ADR + plano técnico (antes de implementar P14) | Critérios: operação, TTL, cluster, custo |
| D2 | Versões de layout ECD **in scope** na V1 | Especificação | Derivar de escopo de produto e manuais oficiais |
| D3 | Biblioteca/pacote de parsing e validação estrutural ECD (interno vs existente Infis) | Plano técnico + ADR | Deve ser testável e rastreável a layout oficial |
| D4 | Modelo de **autenticação/autorização** das APIs (JWT, API Key, mTLS, etc.) | Especificação de segurança | Afeta P13 e admin RAG |
| D5 | **Retenção** de arquivos ECD enviados e relatórios gerados | Especificação + ADR | LGPD e política corporativa |
| D6 | Modelo de **embedding**, dimensão e **estratégia de busca** (híbrida, rerank, etc.) | Plano Api.RAG + avaliação com corpus representativo | Documentar métricas de recall |
| D7 | Parâmetros de chunking, topK e limiares de similaridade | Spec/plano RAG com experimentos | Não fixar na constitution |
| D8 | Política de **retry** LLM (contagem, backoff, fallback de modelo) | Plano Api.Agente | Alinhado a P12 |
| D9 | Escopo mínimo do **servidor A2A** na primeira entrega (Agent Card, skills) | Especificação integração | P16 |
| D10 | **Ferramentas externas** permitidas (lista, credenciais, SLAs) | Especificação | Etapa 6 do pipeline |
| D11 | Tabela de **preços** e moeda para estimativa de custo | Plano operacional | P15; versionar tabela |
| D12 | Estratégia de **validação XSD** vs validador customizado por versão ECD | Especificação técnica ECD | P3; só XSD quando schema existir |

---

**Version**: 1.0.0 | **Ratified**: 2026-10-29 | **Last Amended**: 2026-10-29
