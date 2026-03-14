using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Core.Interfaces;

public sealed record StrikeOutcome(
    bool Landed,
    bool Blocked,
    double DamageDealt,
    bool CausedKnockdown,
    bool CausedStun,
    string NarrationText);

public interface IStrikingEngine
{
    StrikeOutcome ResolveStrike(FighterState attacker, FighterState defender, StrikeType strikeType);
}
