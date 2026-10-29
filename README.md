# AgenteValidacaoECD

Agente de IA para validação da Escrituração Contábil Digital (ECD) — Infis Consultoria.

## Documentação SDD

- Constitution: `.specify/memory/constitution.md`
- Feature: `specs/001-validacao-ecd/` (spec, plan, tasks)

## Build

```powershell
dotnet build AgenteValidacaoECD.slnx
dotnet test AgenteValidacaoECD.slnx
```

**NuGet**: `nuget.config` na raiz usa apenas nuget.org. Para feed Infis, configure credenciais localmente.

## Execução local (após implementação)

Ver `specs/001-validacao-ecd/quickstart.md` e `deploy/README.md`.

| Host | URL dev |
|------|---------|
| Api.Agente | http://localhost:5100 |
| Api.RAG | http://localhost:5201 |
