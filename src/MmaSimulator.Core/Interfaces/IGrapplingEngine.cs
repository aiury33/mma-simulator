using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Core.Interfaces;

/// <summary>
/// Outcome of a single grappling action, produced by <see cref="IGrapplingEngine.ResolveGrappleAction"/>.
/// </summary>
/// <param name="Succeeded">
/// <c>true</c> if the action completed successfully
/// (takedown landed, guard passed, submission locked, etc.).
/// </param>
/// <param name="NewPosition">
/// The actor's resulting <see cref="FightPosition"/> after the action.
/// If the action failed, this typically equals the pre-action position.
/// </param>
/// <param name="DamageDealt">
/// Supplementary damage applied during the action (e.g., slam impact on a takedown).
/// Most grappling actions return 0.
/// </param>
/// <param name="SubmissionFinish">
/// <c>true</c> when the defender was forced to tap — the round ends immediately.
/// </param>
/// <param name="NarrationText">Human-readable description for the UI layer.</param>
public sealed record GrappleOutcome(
    bool          Succeeded,
    FightPosition NewPosition,
    double        DamageDealt,
    bool          SubmissionFinish,
    string        NarrationText);

/// <summary>
/// Resolves grappling interactions: takedowns, position transitions, and submissions.
///
/// <para><b>Takedowns</b> — success probability is derived from attacker
/// <c>TakedownAccuracy</c>, the style bonus (wrestlers +15%), defender
/// <c>TakedownDefense</c>, and both fighters' current stamina.</para>
///
/// <para><b>Position advancement</b> (guard passes, sweeps) — uses the attacker's
/// <c>GroundControl</c> versus the defender's scrambling ability.</para>
///
/// <para><b>Submissions</b> — each attempt independently rolls against the
/// defender's <c>SubmissionDefense</c> and stamina. The <c>SubmissionOffense</c>
/// stat and the attacker's <c>PrimaryStyle</c> (BJJ specialists get a strong bonus)
/// determine the lock probability.</para>
/// </summary>
public interface IGrapplingEngine
{
    /// <summary>
    /// Resolves a single grappling action and returns the outcome.
    /// </summary>
    /// <param name="actor">The fighter initiating the action.</param>
    /// <param name="opponent">The fighter defending against the action.</param>
    /// <param name="action">The specific grappling technique being attempted.</param>
    /// <returns>A <see cref="GrappleOutcome"/> describing success, new position, any damage, and narration.</returns>
    GrappleOutcome ResolveGrappleAction(FighterState actor, FighterState opponent, GrappleAction action);
}
