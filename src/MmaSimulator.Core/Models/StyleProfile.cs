using MmaSimulator.Core.Enums;

namespace MmaSimulator.Core.Models;

public sealed record StyleProfile(
    FightingStyle Style,
    int Proficiency,
    IReadOnlyList<StyleSpecialty> Specialties)
{
    /// <summary>
    /// Returns whether the style profile includes the requested specialty.
    /// </summary>
    public bool HasSpecialty(StyleSpecialty specialty) => Specialties.Contains(specialty);

    /// <summary>
    /// Builds a compact display string for the style profile and its specialties.
    /// </summary>
    public string Summary => Specialties.Count == 0
        ? $"{Style} {Proficiency}"
        : $"{Style} {Proficiency} ({string.Join(", ", Specialties)})";
}
