using MmaSimulator.Core.Models;

namespace MmaSimulator.Core.Interfaces;

public interface IFightSimulator
{
    FightResult Simulate(Fight fight, SimulationOptions options);
}
