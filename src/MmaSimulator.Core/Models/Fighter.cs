using MmaSimulator.Core.Enums;
using MmaSimulator.Core.ValueObjects;

namespace MmaSimulator.Core.Models;

public sealed class Fighter
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Nickname { get; init; }
    public required string Nationality { get; init; }
    public required WeightClass WeightClass { get; init; }
    public required FightingStyle PrimaryStyle { get; init; }
    public FightingStyle? SecondaryStyle { get; init; }
    public required IReadOnlyList<StyleProfile> StyleProfiles { get; init; }
    public required Stance Stance { get; init; }
    public required PhysicalStats Physical { get; init; }
    public required StrikingStats Striking { get; init; }
    public required GrapplingStats Grappling { get; init; }
    public required AthleticStats Athletics { get; init; }
    public required FighterRecord Record { get; init; }
    public int FightIq { get; init; } = 75;

    /// <summary>
    /// Returns the fighter name formatted with nickname when available.
    /// </summary>
    public string FullName => string.IsNullOrWhiteSpace(Nickname)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} \"{Nickname}\" {LastName}";

    /// <summary>
    /// Returns the proficiency for the requested fighting style, falling back to primary and secondary styles.
    /// </summary>
    public int GetStyleProficiency(FightingStyle style)
    {
        var explicitProfile = StyleProfiles.FirstOrDefault(p => p.Style == style);
        if (explicitProfile is not null)
            return explicitProfile.Proficiency;

        if (PrimaryStyle == style) return 80;
        if (SecondaryStyle == style) return 65;
        return 0;
    }

    /// <summary>
    /// Returns the highest proficiency among style profiles that contain the requested specialty.
    /// </summary>
    public int GetSpecialtyProficiency(StyleSpecialty specialty) =>
        StyleProfiles
            .Where(profile => profile.HasSpecialty(specialty))
            .Select(profile => profile.Proficiency)
            .DefaultIfEmpty(0)
            .Max();

    /// <summary>
    /// Returns whether the fighter has the requested specialty in any style profile.
    /// </summary>
    public bool HasSpecialty(StyleSpecialty specialty) => GetSpecialtyProficiency(specialty) > 0;

    /// <summary>
    /// Builds a compact string representation of all style profiles and specialties.
    /// </summary>
    public string StyleSummary =>
        StyleProfiles.Count == 0
            ? PrimaryStyle.ToString()
            : string.Join(" | ", StyleProfiles.Select(profile => profile.Summary));
}
