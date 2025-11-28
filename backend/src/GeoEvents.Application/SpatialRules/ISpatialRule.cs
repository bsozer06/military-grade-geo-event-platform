using System.Threading;
using System.Threading.Tasks;
using GeoEvents.Domain.Entities;

namespace GeoEvents.Application.SpatialRules;

/// <summary>
/// A spatial rule that evaluates a unit position against zones or other spatial data.
/// </summary>
public interface ISpatialRule
{
    string Name { get; }

    /// <summary>
    /// Evaluates the rule for a given unit and its current position.
    /// </summary>
    Task<RuleResult> EvaluateAsync(Unit unit, CancellationToken cancellationToken = default);
}
