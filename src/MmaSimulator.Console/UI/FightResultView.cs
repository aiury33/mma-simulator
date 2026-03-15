using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;
using Spectre.Console;

namespace MmaSimulator.Console.UI;

public sealed class FightResultView
{
    /// <summary>
    /// Displays the complete post-fight result sequence, including decisions and aggregate stats.
    /// </summary>
    /// <param name="result">The fight result to render.</param>
    public void ShowResult(FightResult result)
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("yellow"))
            .Start("Awaiting judges' scorecards...", _ => Thread.Sleep(2000));

        AnsiConsole.Clear();

        if (result.Method is FightResultMethod.DecisionUnanimous
            or FightResultMethod.DecisionSplit
            or FightResultMethod.DecisionMajority)
        {
            ShowJudgeDecisions(result);
        }

        if (result.Fight.IsTitleFight)
        {
            ShowTitleCelebration(result);
        }

        ShowFinalResult(result);
        ShowFightStats(result);
    }

    /// <summary>
    /// Displays the judges' totals when the fight reaches a decision.
    /// </summary>
    /// <param name="result">The fight result to render.</param>
    private static void ShowJudgeDecisions(FightResult result)
    {
        AnsiConsole.MarkupLine("\n[bold yellow]JUDGES' SCORECARDS[/]\n");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Yellow)
            .AddColumn("[bold grey]Judge[/]")
            .AddColumn($"[bold cyan]{Markup.Escape(result.Fight.FighterA.LastName)}[/]")
            .AddColumn($"[bold yellow]{Markup.Escape(result.Fight.FighterB.LastName)}[/]")
            .AddColumn("[bold grey]Winner[/]");

        foreach (var decision in result.JudgeDecisions)
        {
            var winnerColor = decision.PickedWinner.Id == result.Fight.FighterA.Id ? "cyan" : "yellow";
            table.AddRow(
                $"Judge {decision.JudgeIndex}",
                $"[cyan]{decision.ScoreFighterA}[/]",
                $"[yellow]{decision.ScoreFighterB}[/]",
                $"[{winnerColor}]{Markup.Escape(decision.PickedWinner.LastName)}[/]");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        Thread.Sleep(1500);
    }

    /// <summary>
    /// Displays the winner, method, round, and finish time summary panel.
    /// </summary>
    /// <param name="result">The fight result to render.</param>
    private static void ShowFinalResult(FightResult result)
    {
        var methodDisplay = result.Method switch
        {
            FightResultMethod.KO => "KO",
            FightResultMethod.TKO => "TKO (Referee Stoppage)",
            FightResultMethod.Submission => "Submission",
            FightResultMethod.DecisionUnanimous => "Unanimous Decision",
            FightResultMethod.DecisionSplit => "Split Decision",
            FightResultMethod.DecisionMajority => "Majority Decision",
            FightResultMethod.Draw => "Draw",
            _ => result.Method.ToString()
        };

        var content = new Markup(
            $"[bold white]WINNER:[/] [bold red]{Markup.Escape(result.Winner.FullName)}[/]\n" +
            $"[bold white]METHOD:[/] [bold yellow]{methodDisplay}[/]\n" +
            $"[bold white]ROUND: [/] [bold]{result.FinishRound}[/]  " +
            $"[bold white]TIME:[/] [bold]{result.FinishTime:m\\:ss}[/]");

        var panel = new Panel(content)
        {
            Header = new PanelHeader("[bold red on white] FIGHT RESULT [/]"),
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Red1),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays a title-fight celebration HUD with an ASCII-style championship belt.
    /// </summary>
    /// <param name="result">The title-fight result to render.</param>
    private static void ShowTitleCelebration(FightResult result)
    {
        var winnerName = Markup.Escape(result.Winner.FullName.ToUpperInvariant());
        var division = Markup.Escape(result.Fight.WeightClass.ToString().ToUpperInvariant());

        var beltArt = string.Join('\n', new[]
        {
            "[yellow]============================================================[/]",
            "[yellow]==================== ########## ====================[/]",
            "[yellow]=============== ###################### =============[/]",
            "[black on yellow]==================== UFC WORLD CHAMPION ====================[/]",
            "[black on yellow]======================= GOLD BELT ========================[/]",
            $"[black on yellow]==================== {division,-30} ====================[/]",
            "[yellow]=============== ###################### =============[/]",
            "[yellow]==================== ########## ====================[/]",
            "[yellow]============================================================[/]"
        });

        var content = new Markup(
            beltArt + "\n\n" +
            $"[bold yellow]{winnerName}[/]\n" +
            "[bold white]WINS THE BELT![/]\n" +
            "[yellow]The arena erupts as the new champion celebrates![/]");

        var panel = new Panel(content)
        {
            Header = new PanelHeader("[bold yellow on black] TITLE FIGHT [/]", Justify.Center),
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Yellow),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
        Thread.Sleep(1400);
    }

    /// <summary>
    /// Displays aggregate fight statistics for both competitors.
    /// </summary>
    /// <param name="result">The fight result to render.</param>
    private static void ShowFightStats(FightResult result)
    {
        var s = result.StatsSummary;
        var a = result.Fight.FighterA.LastName;
        var b = result.Fight.FighterB.LastName;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("[bold grey]Statistic[/]")
            .AddColumn($"[bold cyan]{Markup.Escape(a)}[/]")
            .AddColumn($"[bold yellow]{Markup.Escape(b)}[/]");

        table.AddRow("Significant Strikes", $"[cyan]{s.TotalSignificantStrikesA}[/]", $"[yellow]{s.TotalSignificantStrikesB}[/]");
        table.AddRow("Takedowns", $"[cyan]{s.TakedownsA}[/]", $"[yellow]{s.TakedownsB}[/]");
        table.AddRow("Submission Attempts", $"[cyan]{s.SubmissionAttemptsA}[/]", $"[yellow]{s.SubmissionAttemptsB}[/]");
        table.AddRow("Knockdowns", $"[cyan]{s.KnockdownsA}[/]", $"[yellow]{s.KnockdownsB}[/]");

        AnsiConsole.MarkupLine("[bold grey]FIGHT STATS[/]");
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}
