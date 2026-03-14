using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Core.Interfaces;

public interface IFighterRepository
{
    IReadOnlyList<Fighter> GetAll();
    Fighter? GetById(Guid id);
    IReadOnlyList<Fighter> GetByWeightClass(WeightClass weightClass);
}
