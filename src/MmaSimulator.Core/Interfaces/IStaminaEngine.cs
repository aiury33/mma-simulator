using MmaSimulator.Core.Models;

namespace MmaSimulator.Core.Interfaces;

/// <summary>
/// Manages stamina drain during a fight and recovery between rounds.
///
/// <para>Stamina is stored as a normalised value in [0.0, 1.0] on <see cref="FighterState.CurrentStamina"/>.
/// It affects accuracy, defense effectiveness, and damage output through the derived properties
/// on <see cref="FighterState"/>. Stamina never drops below 0.05 during a round.</para>
///
/// <para><b>Drain table (representative values per action):</b>
/// <list type="table">
///   <item><term>Strike thrown</term><description>0.003</description></item>
///   <item><term>Strike missed</term><description>0.004</description></item>
///   <item><term>Takedown attempt</term><description>0.018</description></item>
///   <item><term>Submission attempt</term><description>0.012</description></item>
/// </list>
/// </para>
///
/// <para><b>Between-round recovery formula:</b>
/// base 25% + cardio bonus − age penalty − body damage cap.
/// A fighter with elite cardio and no body damage recovers ~35% stamina per rest period.
/// </para>
/// </summary>
public interface IStaminaEngine
{
    /// <summary>
    /// Calculates how much stamina a fighter loses as a result of the given event.
    /// </summary>
    /// <param name="fighter">The fighter who performed the action (reads cardio and style).</param>
    /// <param name="fightEvent">The event that occurred — determines the base drain value from the drain table.</param>
    /// <returns>Stamina to subtract from <see cref="FighterState.CurrentStamina"/> (always non-negative).</returns>
    double CalculateStaminaDrain(FighterState fighter, FightEvent fightEvent);

    /// <summary>
    /// Calculates how much stamina a fighter recovers during the rest between rounds.
    /// </summary>
    /// <param name="fighter">The fighter recovering (reads cardio, age, and accumulated body damage).</param>
    /// <returns>Stamina to add to <see cref="FighterState.CurrentStamina"/>, before clamping to 1.0.</returns>
    double CalculateRoundRecovery(FighterState fighter);
}
