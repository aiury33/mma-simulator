using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Simulation.Data;

public sealed class FighterRepository : IFighterRepository
{
    public IReadOnlyList<Fighter> GetAll() => FighterData.All;

    public Fighter? GetById(Guid id) => FighterData.All.FirstOrDefault(f => f.Id == id);

    public IReadOnlyList<Fighter> GetByWeightClass(WeightClass weightClass) =>
        FighterData.All.Where(f => f.WeightClass == weightClass).ToList();
}
