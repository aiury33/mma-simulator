using MmaSimulator.Console.UI;
using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;
using Spectre.Console;

namespace MmaSimulator.Console.Flows;

public sealed class FightFlow
{
    private readonly FighterSelectionView _selection;
    private readonly FighterStatsView _stats;
    private readonly FightSimulationView _simulation;
    private readonly FightResultView _result;
    private readonly IFightSimulator _simulator;

    public FightFlow(
        FighterSelectionView selection,
        FighterStatsView stats,
        FightSimulationView simulation,
        FightResultView result,
        IFightSimulator simulator)
    {
        _selection = selection;
        _stats = stats;
        _simulation = simulation;
        _result = result;
        _simulator = simulator;
    }

    public void Run()
    {
        var (fighterA, fighterB) = _selection.SelectFighters();

        _stats.ShowComparison(fighterA, fighterB);

        var isTitleFight = AnsiConsole.Confirm("[cyan]Is this a title fight? (5 rounds)[/]", defaultValue: false);
        var rounds = isTitleFight ? 5 : 3;

        AnsiConsole.WriteLine();
        if (!AnsiConsole.Confirm($"[bold]Start the fight: [cyan]{Markup.Escape(fighterA.FullName)}[/] vs [yellow]{Markup.Escape(fighterB.FullName)}[/]?[/]"))
            return;

        var fight = new Fight
        {
            Id = Guid.NewGuid(),
            FighterA = fighterA,
            FighterB = fighterB,
            NumberOfRounds = rounds,
            IsTitleFight = isTitleFight,
            WeightClass = fighterA.WeightClass
        };

        var options = new SimulationOptions(VerboseEvents: true, RandomnessFactor: 0.15);
        FightResult fightResult = null!;

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("red"))
            .Start("[bold]Simulating fight...[/]", _ =>
            {
                fightResult = _simulator.Simulate(fight, options);
                Thread.Sleep(1500);
            });

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("\n[bold red]══════════════════════════════════[/]");
        AnsiConsole.MarkupLine($"[bold cyan]{Markup.Escape(fighterA.FullName)}[/] [bold]vs[/] [bold yellow]{Markup.Escape(fighterB.FullName)}[/]");
        AnsiConsole.MarkupLine("[bold red]══════════════════════════════════[/]\n");

        Thread.Sleep(800);

        foreach (var round in fightResult.Rounds)
        {
            _simulation.ShowRound(round, fighterA, fighterB);
            if (round.Events.Any(e => e.Type == FightEventType.FightEnded))
                break;
            if (round.Number < fightResult.Rounds.Count)
            {
                AnsiConsole.MarkupLine("[dim]Press [bold]Enter[/] for the next round...[/]");
                System.Console.ReadLine();
            }
        }

        _result.ShowResult(fightResult);

        AnsiConsole.MarkupLine("[dim]Press [bold]Enter[/] to return to the main menu...[/]");
        System.Console.ReadLine();
    }
}
