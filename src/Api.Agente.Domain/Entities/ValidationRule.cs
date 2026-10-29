using Api.Agente.Domain.Enums;

namespace Api.Agente.Domain.Entities;

public sealed class ValidationRule
{
    public required string Id { get; set; }

    public required string Version { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public RuleCategory Category { get; set; }

    public FindingSeverity SeverityDefault { get; set; }

    public string? OfficialReference { get; set; }

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public required string ImplementationKey { get; set; }

    public bool IsActive { get; set; }

    public ICollection<RuleLayoutBinding> LayoutBindings { get; set; } = [];
}
