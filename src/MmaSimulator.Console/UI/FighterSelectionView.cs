using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;
using Spectre.Console;

namespace MmaSimulator.Console.UI;

public sealed class FighterSelectionView
{
    private readonly IFighterRepository _repository;

    public FighterSelectionView(IFighterRepository repository)
    {
        _repository = repository;
    }

    public (Fighter FighterA, Fighter FighterB) SelectFighters()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("MMA Simulator").Color(Color.Red1));
        AnsiConsole.MarkupLine("[bold yellow]SELECT FIGHTERS[/]\n");

        var weightClass = AnsiConsole.Prompt(
            new SelectionPrompt<WeightClass>()
                .Title("[cyan]Choose weight class:[/]")
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
                .UseConverter(f => $"{f.FullName} ({f.Record.Display}) — {f.PrimaryStyle}")
                .AddChoices(fighters));

        var availableForB = fighters.Where(f => f.Id != fighterA.Id).ToList();

        var fighterB = AnsiConsole.Prompt(
            new SelectionPrompt<Fighter>()
                .Title("[cyan]Select [bold]Fighter B[/]:[/]")
                .UseConverter(f => $"{f.FullName} ({f.Record.Display}) — {f.PrimaryStyle}")
                .AddChoices(availableForB));

        return (fighterA, fighterB);
    }
}
