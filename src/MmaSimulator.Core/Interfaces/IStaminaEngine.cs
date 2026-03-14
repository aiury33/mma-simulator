using MmaSimulator.Core.Models;

namespace MmaSimulator.Core.Interfaces;

public interface IStaminaEngine
{
    double CalculateStaminaDrain(FighterState fighter, FightEvent fightEvent);
    double CalculateRoundRecovery(FighterState fighter);
}
