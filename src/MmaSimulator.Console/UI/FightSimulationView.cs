using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;
using Spectre.Console;

namespace MmaSimulator.Console.UI;

public sealed class FightSimulationView
{
    // Only show events that are meaningful to watch — filters out idle ticks
    private static readonly HashSet<FightEventType> DisplayableTypes =
    [
        FightEventType.StrikeLanded,
        FightEventType.KnockdownScored,
        FightEventType.TakedownLanded,
        FightEventType.TakedownDefended,
        FightEventType.SubmissionAttempted,
        FightEventType.SubmissionEscaped,
        FightEventType.SubmissionLocked,
        FightEventType.FighterStunned,
        FightEventType.PositionChange,
        FightEventType.FightEnded
    ];

    public void ShowRound(Round round, Fighter fighterA, Fighter fighterB)
    {
        AnsiConsole.MarkupLine($"\n[bold red]══════════  ROUND {round.Number}  ══════════[/]\n");

        foreach (var ev in round.Events.Where(IsDisplayable))
        {
            AnsiConsole.MarkupLine(FormatEvent(ev));
            Thread.Sleep(DelayFor(ev));
        }

        AnsiConsole.WriteLine();
        ShowRoundScorecard(round, fighterA, fighterB);
    }

    private static bool IsDisplayable(FightEvent ev) =>
        DisplayableTypes.Contains(ev.Type) && !string.IsNullOrWhiteSpace(ev.NarrationText);

    private static int DelayFor(FightEvent ev) => ev.Type switch
    {
        FightEventType.KnockdownScored => 2000,
        FightEventType.SubmissionLocked => 2000,
        FightEventType.FightEnded => 1500,
        FightEventType.TakedownLanded => 800,
        FightEventType.SubmissionAttempted => 900,
        FightEventType.SubmissionEscaped => 700,
        FightEventType.StrikeLanded when ev.IsSignificant => 600,
        _ => 400
    };

    private static string FormatEvent(FightEvent ev)
    {
        var text = Markup.Escape(ev.NarrationText);
        var time = ev.Timestamp == TimeSpan.Zero ? string.Empty : $"[dim]{ev.Timestamp:m\\:ss}[/] ";
        return ev.Type switch
        {
            FightEventType.KnockdownScored   => $"{time}[bold red on white] KNOCKDOWN! [/] [bold red]{text}[/]",
            FightEventType.SubmissionLocked  => $"{time}[bold magenta on white] TAP! [/] [bold magenta]{text}[/]",
            FightEventType.SubmissionAttempted => $"{time}[bold magenta]{text}[/]",
            FightEventType.SubmissionEscaped => $"{time}[grey]{text}[/]",
            FightEventType.TakedownLanded    => $"{time}[bold cyan]{text}[/]",
            FightEventType.TakedownDefended  => $"{time}[grey]{text}[/]",
            FightEventType.FightEnded        => $"\n[bold red on white] FIGHT OVER! [/] [bold]{text}[/]\n",
            FightEventType.StrikeLanded when ev.IsSignificant => $"{time}[bold yellow]{text}[/]",
            FightEventType.PositionChange    => $"{time}[cyan]{text}[/]",
            _ => $"{time}[white]{text}[/]"
        };
    }

    private static void ShowRoundScorecard(Round round, Fighter fighterA, Fighter fighterB)
    {
        if (round.Scorecards.Count == 0) return;

        var a = round.FighterAStats;
        var b = round.FighterBStats;
        var statsLine = $"[dim]Sig. strikes: [cyan]{a.SignificantStrikesLanded}[/]–[yellow]{b.SignificantStrikesLanded}[/]  " +
                        $"TDs: [cyan]{a.TakedownsLanded}[/]–[yellow]{b.TakedownsLanded}[/]  " +
                        $"Subs: [cyan]{a.SubmissionAttempts}[/]–[yellow]{b.SubmissionAttempts}[/][/]";

        var cardLines = round.Scorecards.Select(sc =>
            $"[grey]Judge {sc.JudgeIndex}:[/] [cyan]{sc.ScoreFighterA}[/] – [yellow]{sc.ScoreFighterB}[/]  [dim]{Markup.Escape(sc.Rationale)}[/]");

        var content = statsLine + "\n\n" + string.Join("\n", cardLines);

        var panel = new Panel(content)
        {
            Header = new PanelHeader($"[bold]END OF ROUND {round.Number}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    public void ShowFightStats(Round round, Fighter fighterA, Fighter fighterB)
    {
        var a = round.FighterAStats;
        var b = round.FighterBStats;

        AnsiConsole.MarkupLine($"  [grey]Sig. Strikes:[/] [cyan]{a.SignificantStrikesLanded}[/] — [yellow]{b.SignificantStrikesLanded}[/]");
        AnsiConsole.MarkupLine($"  [grey]Takedowns:   [/] [cyan]{a.TakedownsLanded}/{a.TakedownsAttempted}[/] — [yellow]{b.TakedownsLanded}/{b.TakedownsAttempted}[/]");
        AnsiConsole.MarkupLine($"  [grey]Submissions: [/] [cyan]{a.SubmissionAttempts}[/] — [yellow]{b.SubmissionAttempts}[/]");
    }
}
