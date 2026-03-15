using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Simulation.Narration;

public sealed class NarrationBuilder
{
    private readonly IRandomProvider _random;

    /// <summary>
    /// Creates the narration builder with the random provider used to pick templates.
    /// </summary>
    public NarrationBuilder(IRandomProvider random)
    {
        _random = random;
    }

    /// <summary>
    /// Builds narration text for a striking event.
    /// </summary>
    public string BuildForStrike(FightEvent ev)
    {
        var actor = ev.Actor.FullName;
        var target = ev.Target?.FullName ?? string.Empty;

        if (ev.Type == FightEventType.StrikeMissed)
            return NarrationTemplates.Format(_random.Choose(NarrationTemplates.StrikeMissed), actor, target);

        if (ev.Type == FightEventType.StrikeBlocked)
            return NarrationTemplates.Format(_random.Choose(NarrationTemplates.StrikeBlocked), actor, target);

        if (ev.Type == FightEventType.KnockdownScored)
            return NarrationTemplates.Format(_random.Choose(NarrationTemplates.KnockdownScored), actor, target);

        if (ev.StrikeType.HasValue && NarrationTemplates.StrikeLanded.TryGetValue(ev.StrikeType.Value, out var templates))
            return NarrationTemplates.Format(_random.Choose(templates), actor, target);

        return $"{actor} lands a strike on {target}.";
    }

    /// <summary>
    /// Builds narration text for a grappling event.
    /// </summary>
    public string BuildForGrapple(FightEvent ev)
    {
        var actor = ev.Actor.FullName;
        var target = ev.Target?.FullName ?? string.Empty;

        if (ev.GrappleAction.HasValue && NarrationTemplates.GrappleActionTemplates.TryGetValue(ev.GrappleAction.Value, out var actionTemplates))
            return NarrationTemplates.Format(_random.Choose(actionTemplates), actor, target);

        return ev.Type switch
        {
            FightEventType.TakedownLanded => NarrationTemplates.Format(_random.Choose(NarrationTemplates.TakedownLanded), actor, target),
            FightEventType.TakedownDefended => NarrationTemplates.Format(_random.Choose(NarrationTemplates.TakedownDefended), actor, target),
            FightEventType.SubmissionAttempted => NarrationTemplates.Format(_random.Choose(NarrationTemplates.SubmissionAttempted), actor, target),
            FightEventType.SubmissionEscaped => NarrationTemplates.Format(_random.Choose(NarrationTemplates.SubmissionEscaped), actor, target),
            FightEventType.SubmissionLocked => NarrationTemplates.Format(_random.Choose(NarrationTemplates.SubmissionFinish), actor, target),
            FightEventType.PositionChange => NarrationTemplates.Format(_random.Choose(NarrationTemplates.GuardPass), actor, target),
            _ => $"{actor} attempts a grappling move on {target}."
        };
    }
}
