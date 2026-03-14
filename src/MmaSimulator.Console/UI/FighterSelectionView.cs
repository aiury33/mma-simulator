using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;
using Spectre.Console;

namespace MmaSimulator.Console.UI;

/// <summary>
/// Handles the fighter selection flow, including optional cross-division superfights.
/// </summary>
public sealed class FighterSelectionView
{
    private readonly IFighterRepository _repository;

    /// <summary>Creates a new selection view backed by the given fighter repository.</summary>
    public FighterSelectionView(IFighterRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Runs the interactive fighter selection flow and returns the two chosen fighters.
    ///
    /// <para>Fighter A is always chosen from a specific weight class.
    /// After selecting Fighter A, the user may choose to open the roster to
    /// <b>all weight classes</b> for a cross-division superfight — in which case the
    /// simulation will apply an appropriate weight-differential danger multiplier.</para>
    /// </summary>
    /// <returns>
    /// A tuple of (FighterA, FighterB) ready to be passed to <see cref="IFightSimulator"/>.
    /// </returns>
    public (Fighter FighterA, Fighter FighterB) SelectFighters()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("MMA Simulator").Color(Color.Red1));
        AnsiConsole.MarkupLine("[bold yellow]SELECT FIGHTERS[/]\n");

        var weightClass = AnsiConsole.Prompt(
            new SelectionPrompt<WeightClass>()
                .Title("[cyan]Choose weight class for Fighter A:[/]")
                .AddChoices(Enum.GetValues<WeightClass>()));

        var fighters = _repository.GetByWeightClass(weightClass);

        if (fighters.Count < 2)
        {
            AnsiConsole.MarkupLine($"[red]Not enough fighters in {weightClass}. Using all fighters.[/]");
            fighters = _repository.GetAll();
        }

        var fighterA = AnsiConsole.Prompt(
            new SelectionPrompt<Fighter>()
                .Title("[cyan]Select [bold]Fighter A[/]:[/]")
                .UseConverter(f => FormatFighter(f))
                .AddChoices(fighters));

        // Cross-division superfight option
        var crossDivision = AnsiConsole.Confirm(
            "[dim]Open Fighter B selection to [bold]all weight classes[/] (cross-division superfight)?[/]",
            defaultValue: false);

        var bPool = crossDivision
            ? _repository.GetAll().Where(f => f.Id != fighterA.Id).ToList()
            : fighters.Where(f => f.Id != fighterA.Id).ToList();

        if (crossDivision)
            AnsiConsole.MarkupLine("[bold yellow]⚡ Cross-division superfight — weight differential will affect KO danger![/]\n");

        var fighterB = AnsiConsole.Prompt(
            new SelectionPrompt<Fighter>()
                .Title("[cyan]Select [bold]Fighter B[/]:[/]")
                .UseConverter(f => FormatFighter(f))
                .AddChoices(bPool));

        return (fighterA, fighterB);
    }

    private static string FormatFighter(Fighter f)
    {
        var name   = Markup.Escape(f.FullName);
        var record = Markup.Escape(f.Record.Display);
        var wc     = Markup.Escape(f.WeightClass.ToString());
        var style  = Markup.Escape(f.PrimaryStyle.ToString());
        return $"{name} ({record}) [{wc}] — {style}";
    }
}
