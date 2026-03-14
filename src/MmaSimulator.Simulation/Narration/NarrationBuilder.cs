using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Simulation.Narration;

public sealed class NarrationBuilder
{
    private readonly IRandomProvider _random;

    public NarrationBuilder(IRandomProvider random)
    {
        _random = random;
    }

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

    public string BuildForGrapple(FightEvent ev)
    {
        var actor = ev.Actor.FullName;
        var target = ev.Target?.FullName ?? string.Empty;

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
