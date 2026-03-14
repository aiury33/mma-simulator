using MmaSimulator.Core.Interfaces;
using Spectre.Console;

namespace MmaSimulator.Console.Flows;

public sealed class MainMenuFlow
{
    private readonly FightFlow _fightFlow;
    private readonly IFighterRepository _repository;

    public MainMenuFlow(FightFlow fightFlow, IFighterRepository repository)
    {
        _fightFlow = fightFlow;
        _repository = repository;
    }

    public void Run()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("MMA Simulator")
                .Centered()
                .Color(Color.Red1));
            AnsiConsole.MarkupLine("[bold grey]  The Ultimate Fight Simulator[/]\n");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Main Menu[/]")
                    .HighlightStyle(Style.Parse("bold red"))
                    .AddChoices(
                        "Simulate a Fight",
                        "View Fighter Roster",
                        "About",
                        "Exit"));

            switch (choice)
            {
                case "Simulate a Fight":
                    _fightFlow.Run();
                    break;
                case "View Fighter Roster":
                    ShowRoster();
                    break;
                case "About":
                    ShowAbout();
                    break;
                case "Exit":
                    AnsiConsole.MarkupLine("[bold grey]Thanks for using MMA Simulator![/]");
                    return;
            }
        }
    }

    private void ShowRoster()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold yellow]FIGHTER ROSTER[/]\n");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Fighter[/]")
            .AddColumn("[bold]Weight Class[/]")
            .AddColumn("[bold]Style[/]")
            .AddColumn("[bold]Record[/]")
            .AddColumn("[bold]Nationality[/]");

        foreach (var fighter in _repository.GetAll().OrderBy(f => f.WeightClass).ThenBy(f => f.LastName))
        {
            table.AddRow(
                fighter.FullName,
                fighter.WeightClass.ToString(),
                fighter.PrimaryStyle.ToString(),
                fighter.Record.Display,
                fighter.Nationality);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[dim]Press [bold]Enter[/] to return...[/]");
        System.Console.ReadLine();
    }

    private static void ShowAbout()
    {
        AnsiConsole.Clear();
        var panel = new Panel(
            new Markup(
                "[bold]MMA Simulator[/]\n\n" +
                "A realistic Mixed Martial Arts fight simulation engine built with [bold].NET 10[/] and [bold]C#[/].\n\n" +
                "Simulation features:\n" +
                "  [cyan]•[/] Striking algorithms (accuracy, power, reach, stance)\n" +
                "  [cyan]•[/] Grappling & submission system (takedowns, guard, mount, back control)\n" +
                "  [cyan]•[/] Stamina & fatigue modelling per round\n" +
                "  [cyan]•[/] 10-point must judge scoring with 3 independent judges\n" +
                "  [cyan]•[/] KO/TKO/Submission finish detection\n\n" +
                "[grey]Built with Spectre.Console and .NET 10[/]"))
        {
            Header = new PanelHeader("[bold red]ABOUT[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Red1),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.MarkupLine("\n[dim]Press [bold]Enter[/] to return...[/]");
        System.Console.ReadLine();
    }
}
