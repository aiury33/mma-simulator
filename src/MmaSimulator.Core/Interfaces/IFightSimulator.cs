using MmaSimulator.Core.Models;

namespace MmaSimulator.Core.Interfaces;

/// <summary>
/// Top-level orchestrator that runs a complete MMA fight simulation.
///
/// <para>Implementations are responsible for:</para>
/// <list type="bullet">
///   <item>Creating mutable <see cref="FighterState"/> instances from the immutable
///       <see cref="Fighter"/> domain objects.</item>
///   <item>Delegating each round to <see cref="IRoundSimulator"/> and detecting finish events.</item>
///   <item>Applying between-round stamina recovery via <see cref="IStaminaEngine"/>.</item>
///   <item>Tallying the three judges' scorecards via <see cref="IJudgeScoringEngine"/> when
///       the fight goes to a decision.</item>
///   <item>Building and returning a fully populated <see cref="FightResult"/>.</item>
/// </list>
/// </summary>
public interface IFightSimulator
{
    /// <summary>
    /// Simulates every round of <paramref name="fight"/> and returns the complete result.
    /// </summary>
    /// <param name="fight">
    /// Immutable description of the bout: the two fighters, number of rounds, title-fight flag,
    /// and scheduled weight class.
    /// </param>
    /// <param name="options">
    /// Simulation controls including optional random seed (for reproducibility), verbosity flag,
    /// and a randomness noise factor.
    /// </param>
    /// <returns>
    /// A <see cref="FightResult"/> containing the winner, finish method, round-by-round events,
    /// per-round statistics, judge scorecards (if applicable), and aggregate stats summary.
    /// </returns>
    FightResult Simulate(Fight fight, SimulationOptions options);
}
