using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Simulation.Data;

public sealed class FighterRepository : IFighterRepository
{
    /// <summary>
    /// Returns the full in-memory roster.
    /// </summary>
    public IReadOnlyList<Fighter> GetAll() => FighterData.All;

    /// <summary>
    /// Returns the fighter with the requested identifier, or <see langword="null"/> when not found.
    /// </summary>
    public Fighter? GetById(Guid id) => FighterData.All.FirstOrDefault(f => f.Id == id);

    /// <summary>
    /// Returns all fighters that belong to the requested weight class.
    /// </summary>
    public IReadOnlyList<Fighter> GetByWeightClass(WeightClass weightClass) =>
        FighterData.All.Where(f => f.WeightClass == weightClass).ToList();
}
