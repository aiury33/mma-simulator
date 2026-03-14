using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Core.Interfaces;

/// <summary>
/// Read-only data access layer for the built-in fighter roster.
///
/// <para>All returned collections are backed by in-memory data — no I/O occurs.
/// The roster contains the current UFC top-15 fighters per weight class (March 2026)
/// across all 10 divisions (8 men's + 2 women's).</para>
/// </summary>
public interface IFighterRepository
{
    /// <summary>Returns every fighter in the roster regardless of weight class.</summary>
    IReadOnlyList<Fighter> GetAll();

    /// <summary>
    /// Finds a single fighter by their unique identifier.
    /// </summary>
    /// <param name="id">The fighter's <see cref="Fighter.Id"/>.</param>
    /// <returns>The matching <see cref="Fighter"/>, or <c>null</c> if not found.</returns>
    Fighter? GetById(Guid id);

    /// <summary>
    /// Returns all fighters assigned to the specified weight class.
    /// </summary>
    /// <param name="weightClass">The division to filter by.</param>
    /// <returns>
    /// An ordered list of fighters; may be empty if no fighters are registered for that class.
    /// Cross-division superfights are handled at the simulation layer — this method always
    /// groups fighters by their registered division.
    /// </returns>
    IReadOnlyList<Fighter> GetByWeightClass(WeightClass weightClass);
}
