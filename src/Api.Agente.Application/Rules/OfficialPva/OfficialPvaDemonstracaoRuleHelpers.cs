using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

internal static class OfficialPvaDemonstracaoRuleHelpers
{
    public static RuleEvaluationResult Validated(string ruleId, string version, string message) => new()
    {
        RuleId = ruleId,
        RuleVersion = version,
        Outcome = RuleOutcome.Validated,
        Message = message,
    };

    public static RuleEvaluationResult Build(
        List<RuleEvidence> evidence,
        string ruleId,
        string version,
        string okMessage,
        string errorMessage) =>
        evidence.Count == 0
            ? Validated(ruleId, version, okMessage)
            : new RuleEvaluationResult
            {
                RuleId = ruleId,
                RuleVersion = version,
                Outcome = RuleOutcome.Error,
                Message = errorMessage,
                Evidence = evidence,
            };

    public static RuleEvaluationResult BuildAlert(
        List<RuleEvidence> evidence,
        string ruleId,
        string version,
        string okMessage,
        string alertMessage) =>
        evidence.Count == 0
            ? Validated(ruleId, version, okMessage)
            : new RuleEvaluationResult
            {
                RuleId = ruleId,
                RuleVersion = version,
                Outcome = RuleOutcome.Alert,
                Message = alertMessage,
                Evidence = evidence,
            };
}
