namespace Api.Agente.Domain.Entities;

public sealed class RuleLayoutBinding
{
    public required string RuleId { get; set; }

    public ValidationRule? Rule { get; set; }

    public required string LayoutVersion { get; set; }
}
