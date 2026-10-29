namespace Api.Agente.Domain.Enums;

public enum RuleOutcome
{
    Error = 0,
    Alert = 1,
    ProbableInconsistency = 2,
    ReviewRequired = 3,
    Validated = 4,
    NotApplicable = 5,
    NotVerified = 6,
}
