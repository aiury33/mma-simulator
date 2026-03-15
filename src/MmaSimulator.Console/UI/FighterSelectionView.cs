using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;
using Spectre.Console;

namespace MmaSimulator.Console.UI;

/// <summary>
/// Handles fighter selection, including searchable prompts, back navigation, and optional superfights.
/// </summary>
public sealed class FighterSelectionView
{
    private const string BackOption = "<< Back";

    private readonly IFighterRepository _repository;

    /// <summary>Creates a new selection view backed by the given fighter repository.</summary>
    public FighterSelectionView(IFighterRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Runs the interactive fighter selection flow and returns the selected matchup, or <see langword="null"/> if cancelled.
    /// </summary>
    public (Fighter FighterA, Fighter FighterB)? SelectFighters()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("MMA Simulator").Color(Color.Red1));
            AnsiConsole.MarkupLine("[bold yellow]SELECT FIGHTERS[/]\n");

            var weightClass = PromptWeightClass();
            if (weightClass is null)
                return null;

            var fighters = _repository.GetByWeightClass(weightClass.Value);
            if (fighters.Count < 2)
                fighters = _repository.GetAll();

            var sortedDivisionFighters = fighters
                .OrderBy(f => f.LastName)
                .ThenBy(f => f.FirstName)
                .ToList();

            var fighterA = PromptFighter(
                sortedDivisionFighters,
                $"[cyan]Select [bold]Fighter A[/] ([grey]{sortedDivisionFighters.Count} options[/]):[/]",
                $"[dim]{sortedDivisionFighters.Count} fighters available for {Markup.Escape(weightClass.Value.ToString())}.[/]");

            if (fighterA is null)
                continue;

            var crossDivision = PromptCrossDivision(fighterA);
            if (crossDivision is null)
                continue;

            var bPool = crossDivision.Value
                ? _repository.GetAll()
                    .Where(f => f.Id != fighterA.Id)
                    .OrderBy(f => f.WeightClass)
                    .ThenBy(f => f.LastName)
                    .ThenBy(f => f.FirstName)
                    .ToList()
                : sortedDivisionFighters
                    .Where(f => f.Id != fighterA.Id)
                    .ToList();

            var helperText = crossDivision.Value
                ? $"[bold yellow]{bPool.Count} cross-division opponents available. Weight differential will affect KO danger.[/]"
                : $"[dim]{bPool.Count} opponents available in {Markup.Escape(fighterA.WeightClass.ToString())}.[/]";

            var fighterB = PromptFighter(
                bPool,
                $"[cyan]Select [bold]Fighter B[/] ([grey]{bPool.Count} options[/]):[/]",
                helperText);

            if (fighterB is null)
                continue;

            return (fighterA, fighterB);
        }
    }

    /// <summary>
    /// Prompts for a weight class or returns <see langword="null"/> when the user chooses to go back.
    /// </summary>
    private static WeightClass? PromptWeightClass()
    {
        var labels = Enum.GetValues<WeightClass>()
            .Select(wc => wc.ToString())
            .Append(BackOption)
            .ToList();

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Choose weight class for Fighter A:[/]")
                .PageSize(12)
                .AddChoices(labels));

        return selected == BackOption
            ? null
            : Enum.Parse<WeightClass>(selected);
    }

    /// <summary>
    /// Prompts for a fighter from the supplied pool or returns <see langword="null"/> when the user chooses to go back.
    /// </summary>
    private static Fighter? PromptFighter(IReadOnlyList<Fighter> fighters, string title, string helperText)
    {
        AnsiConsole.MarkupLine(helperText);

        var options = fighters
            .Select(fighter => new FighterOption(FormatFighter(fighter), fighter))
            .Append(new FighterOption(BackOption, null))
            .ToList();

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<FighterOption>()
                .Title(title)
                .PageSize(20)
                .MoreChoicesText("[grey](scroll for more, use arrows/PageUp/PageDown, or type to search by name)[/]")
                .EnableSearch()
                .UseConverter(option => option.Label)
                .AddChoices(options));

        return selected.Fighter;
    }

    /// <summary>
    /// Prompts whether fighter B selection should include every division, or returns <see langword="null"/> to go back.
    /// </summary>
    private static bool? PromptCrossDivision(Fighter fighterA)
    {
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[cyan]How should [bold]{Markup.Escape(fighterA.FullName)}[/] find an opponent?[/]")
                .AddChoices(
                    "Same weight class",
                    "All weight classes",
                    BackOption));

        return selected switch
        {
            "Same weight class" => false,
            "All weight classes" => true,
            _ => null
        };
    }

    /// <summary>
    /// Formats a fighter entry for selection prompts.
    /// </summary>
    private static string FormatFighter(Fighter fighter)
    {
        return $"{fighter.FullName} ({fighter.Record.Display}) - {fighter.WeightClass}";
    }

    private sealed record FighterOption(string Label, Fighter? Fighter);
}
