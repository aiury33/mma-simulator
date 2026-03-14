using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Core.Interfaces;

public sealed record GrappleOutcome(
    bool Succeeded,
    FightPosition NewPosition,
    double DamageDealt,
    bool SubmissionFinish,
    string NarrationText);

public interface IGrapplingEngine
{
    GrappleOutcome ResolveGrappleAction(FighterState actor, FighterState opponent, GrappleAction action);
}
