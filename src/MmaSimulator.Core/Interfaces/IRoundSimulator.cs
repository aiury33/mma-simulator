using MmaSimulator.Core.Models;

namespace MmaSimulator.Core.Interfaces;

public interface IRoundSimulator
{
    Round SimulateRound(int roundNumber, FighterState stateA, FighterState stateB, SimulationOptions options);
}
