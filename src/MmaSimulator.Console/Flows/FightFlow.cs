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

    /// <summary>
    /// Creates the interactive fight flow and its dependent views.
    /// </summary>
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

    /// <summary>
    /// Runs the full interactive fight experience from selection to final result.
    /// </summary>
    public void Run()
    {
        var selection = _selection.SelectFighters();
        if (selection is null)
            return;

        var (fighterA, fighterB) = selection.Value;

        _stats.ShowComparison(fighterA, fighterB);

        var isTitleFight = AnsiConsole.Confirm("[cyan]Is this a title fight? (5 rounds)[/]", defaultValue: false);
        var rounds = isTitleFight ? 5 : 3;

        AnsiConsole.WriteLine();
        if (!AnsiConsole.Confirm($"[bold]Start the fight: [cyan]{Markup.Escape(fighterA.FullName)}[/] vs [yellow]{Markup.Escape(fighterB.FullName)}[/]?[/]"))
            return;

        var options = new SimulationOptions(VerboseEvents: true, RandomnessFactor: 0.15);

        while (true)
        {
            var fight = new Fight
            {
                Id = Guid.NewGuid(),
                FighterA = fighterA,
                FighterB = fighterB,
                NumberOfRounds = rounds,
                IsTitleFight = isTitleFight,
                WeightClass = fighterA.WeightClass
            };

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
            AnsiConsole.MarkupLine("\n[bold red]==================================[/]");
            AnsiConsole.MarkupLine($"[bold cyan]{Markup.Escape(fighterA.FullName)}[/] [bold]vs[/] [bold yellow]{Markup.Escape(fighterB.FullName)}[/]");
            AnsiConsole.MarkupLine("[bold red]==================================[/]\n");

            Thread.Sleep(800);

            var healthTracker = new FightSimulationView.FightHealthTracker(fighterA, fighterB);

            foreach (var round in fightResult.Rounds)
            {
                _simulation.ShowRound(round, fighterA, fighterB, healthTracker);
                if (round.Events.Any(e => e.Type == FightEventType.FightEnded))
                    break;

                if (round.Number < fightResult.Rounds.Count)
                {
                    AnsiConsole.MarkupLine("[dim]Press [bold]Enter[/] for the next round...[/]");
                    System.Console.ReadLine();
                }
            }

            AnsiConsole.MarkupLine("[dim]Press [bold]Enter[/] to see the official result...[/]");
            System.Console.ReadLine();

            _result.ShowResult(fightResult);

            if (!AnsiConsole.Confirm("[cyan]Repeat this fight with the same matchup?[/]", defaultValue: false))
                break;
        }

        AnsiConsole.MarkupLine("[dim]Press [bold]Enter[/] to return to the main menu...[/]");
        System.Console.ReadLine();
    }
}
