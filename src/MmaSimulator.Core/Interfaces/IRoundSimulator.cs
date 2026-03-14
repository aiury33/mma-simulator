using MmaSimulator.Core.Models;

namespace MmaSimulator.Core.Interfaces;

/// <summary>
/// Simulates a single round of an MMA fight using a tick-based loop.
///
/// <para>Each round is divided into discrete time ticks (typically 300 ticks = 300 seconds = 5 minutes).
/// On each tick a random actor is selected, an action is resolved based on the current
/// fight position and fighter styles, and the resulting <see cref="FightEvent"/> is recorded.</para>
///
/// <para>The round ends early if a finish event (KO, TKO, or submission) occurs.
/// Otherwise all ticks elapse and three judge scorecards are computed.</para>
/// </summary>
public interface IRoundSimulator
{
    /// <summary>
    /// Simulates one full round and returns all events plus per-fighter statistics.
    /// </summary>
    /// <param name="roundNumber">1-based round index within the fight.</param>
    /// <param name="stateA">
    /// Mutable state for Fighter A — carries accumulated damage, stamina, and position
    /// <b>into</b> the round and is updated in-place as events resolve.
    /// </param>
    /// <param name="stateB">Mutable state for Fighter B (same semantics as <paramref name="stateA"/>).</param>
    /// <param name="options">Simulation settings such as randomness factor and verbosity.</param>
    /// <returns>
    /// A <see cref="Round"/> containing the ordered event list, per-fighter round stats,
    /// and (if no finish) three judge scorecards.
    /// </returns>
    Round SimulateRound(int roundNumber, FighterState stateA, FighterState stateB, SimulationOptions options);
}
