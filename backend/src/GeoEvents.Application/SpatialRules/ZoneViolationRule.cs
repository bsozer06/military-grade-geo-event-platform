using System.Threading;
using System.Threading.Tasks;
using GeoEvents.Domain.Entities;
using GeoEvents.Domain.ValueObjects;

namespace GeoEvents.Application.SpatialRules;

/// <summary>
/// Checks if a unit has entered a restricted zone.
/// NOTE: Infrastructure will provide precise PostGIS check; here we simulate with radius if available.
/// </summary>
public sealed class ZoneViolationRule : ISpatialRule
{
    public string Name => "ZoneViolation";

    private readonly IEnumerable<Zone> _zones;

    public ZoneViolationRule(IEnumerable<Zone> zones)
    {
        _zones = zones;
    }

    public Task<RuleResult> EvaluateAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        foreach (var zone in _zones)
        {
            if (!zone.IsRestricted || !zone.IsActive())
                continue;

            if (zone.CenterPoint is not null && zone.RadiusMeters is not null)
            {
                var distance = unit.Position.DistanceTo(zone.CenterPoint);
                if (distance <= zone.RadiusMeters.Value)
                {
                    return Task.FromResult(
                        RuleResult.ViolatedRule(Name, $"Unit entered zone {zone.Identifier}", distance, zone.Identifier));
                }
            }
            // Polygonal zone evaluation will be done via PostGIS in Infrastructure layer
        }

        return Task.FromResult(RuleResult.Passed(Name));
    }
}
