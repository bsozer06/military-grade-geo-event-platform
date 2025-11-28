namespace GeoEvents.Application.SpatialRules;

/// <summary>
/// Represents the result of a spatial rule evaluation.
/// </summary>
public sealed class RuleResult
{
    public bool Violated { get; }
    public string RuleName { get; }
    public string? Reason { get; }
    public double? DistanceMeters { get; }
    public string? ZoneIdentifier { get; }

    private RuleResult(bool violated, string ruleName, string? reason, double? distanceMeters, string? zoneIdentifier)
    {
        Violated = violated;
        RuleName = ruleName;
        Reason = reason;
        DistanceMeters = distanceMeters;
        ZoneIdentifier = zoneIdentifier;
    }

    public static RuleResult Passed(string ruleName) => new(false, ruleName, null, null, null);
    public static RuleResult ViolatedRule(string ruleName, string? reason = null, double? distanceMeters = null, string? zoneIdentifier = null) => new(true, ruleName, reason, distanceMeters, zoneIdentifier);
}
