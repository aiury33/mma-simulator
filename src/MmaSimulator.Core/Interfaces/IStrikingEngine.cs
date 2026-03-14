using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Core.Interfaces;

/// <summary>
/// Outcome of a single strike attempt, produced by <see cref="IStrikingEngine.ResolveStrike"/>.
/// </summary>
/// <param name="Landed">
/// <c>true</c> if the strike connected (including blocked).
/// <c>false</c> means it missed entirely.
/// </param>
/// <param name="Blocked">
/// <c>true</c> if the defender partially deflected the strike.
/// Blocked strikes deal 40% damage and do not trigger KO / stun checks.
/// </param>
/// <param name="DamageDealt">
/// Numeric damage applied to the defender's accumulated totals.
/// Head strikes accumulate in <see cref="FighterState.AccumulatedHeadDamage"/>;
/// body shots and blocked punches in <see cref="FighterState.AccumulatedBodyDamage"/>.
/// </param>
/// <param name="CausedKnockdown">
/// <c>true</c> when the strike floored the opponent.
/// The round simulator then evaluates a TKO stoppage probability.
/// </param>
/// <param name="CausedStun">
/// <c>true</c> when the strike hurt but did not floor the opponent.
/// </param>
/// <param name="NarrationText">Human-readable description for the UI layer.</param>
public sealed record StrikeOutcome(
    bool   Landed,
    bool   Blocked,
    double DamageDealt,
    bool   CausedKnockdown,
    bool   CausedStun,
    string NarrationText);

/// <summary>
/// Resolves the outcome of a single strike attempt.
///
/// <para>The engine runs a four-stage pipeline:</para>
/// <list type="number">
///   <item>Accuracy roll — influenced by attacker stamina, reach advantage, and stance matchup.</item>
///   <item>Defense roll — the defender may block, reducing damage to 40%.</item>
///   <item>Damage computation — base power × strike-type multiplier × position bonus ± 10% jitter.</item>
///   <item>KO / stun evaluation — only for clean head strikes, using an exponential danger model
///       that scales with accumulated damage, attacker weight class, and cross-division
///       weight differential.</item>
/// </list>
/// </summary>
public interface IStrikingEngine
{
    /// <summary>
    /// Resolves one strike attempt and returns the full outcome.
    /// </summary>
    /// <param name="attacker">Reads accuracy, power, weight, reach, and current stamina.</param>
    /// <param name="defender">Reads defense, chin, toughness, and accumulated head damage.</param>
    /// <param name="strikeType">Technique being thrown — determines the power multiplier.</param>
    /// <returns>A <see cref="StrikeOutcome"/> describing what happened.</returns>
    StrikeOutcome ResolveStrike(FighterState attacker, FighterState defender, StrikeType strikeType);
}
