using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;
using Spectre.Console;

namespace MmaSimulator.Console.UI;

public sealed class FightSimulationView
{
    // Only show events that are meaningful to watch - filters out idle ticks.
    private static readonly HashSet<FightEventType> DisplayableTypes =
    [
        FightEventType.StrikeLanded,
        FightEventType.StrikeBlocked,
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

    /// <summary>
    /// Displays the narrated playback for a single round, followed by the round scorecard.
    /// </summary>
    /// <param name="round">The round to render.</param>
    /// <param name="fighterA">The fight's first fighter.</param>
    /// <param name="fighterB">The fight's second fighter.</param>
    public void ShowRound(Round round, Fighter fighterA, Fighter fighterB, FightHealthTracker? healthTracker = null)
    {
        var health = healthTracker ?? new FightHealthTracker(fighterA, fighterB);

        AnsiConsole.MarkupLine($"\n[bold red]==========  ROUND {round.Number}  ==========[/]\n");

        foreach (var ev in round.Events.Where(IsDisplayable))
        {
            AnsiConsole.MarkupLine(FormatEvent(ev));
            health.Apply(ev);
            AnsiConsole.MarkupLine(health.FormatStatus());
            Thread.Sleep(DelayFor(ev));
        }

        AnsiConsole.WriteLine();
        ShowRoundScorecard(round, fighterA, fighterB);
    }

    /// <summary>
    /// Returns whether an event is meaningful enough to include in narrated playback.
    /// </summary>
    private static bool IsDisplayable(FightEvent ev) =>
        DisplayableTypes.Contains(ev.Type) && !string.IsNullOrWhiteSpace(ev.NarrationText);

    /// <summary>
    /// Returns the playback delay for a visible event type.
    /// </summary>
    private static int DelayFor(FightEvent ev) => ev.Type switch
    {
        FightEventType.KnockdownScored => 3000,
        FightEventType.SubmissionLocked => 3000,
        FightEventType.FightEnded => 3000,
        FightEventType.TakedownLanded => 3000,
        FightEventType.SubmissionAttempted => 3000,
        FightEventType.SubmissionEscaped => 1000,
        FightEventType.StrikeLanded when ev.IsSignificant => 1000,
        _ => 1000
    };

    /// <summary>
    /// Formats a fight event for display with timestamp, color, and emphasis.
    /// </summary>
    private static string FormatEvent(FightEvent ev)
    {
        var text = Markup.Escape(ev.NarrationText);
        var time = ev.Timestamp == TimeSpan.Zero ? string.Empty : $"[dim]{ev.Timestamp:m\\:ss}[/] ";
        return ev.Type switch
        {
            FightEventType.KnockdownScored => $"{time}[bold red on white] KNOCKDOWN! [/] [bold red]{text}[/]",
            FightEventType.SubmissionLocked => $"{time}[bold magenta on white] TAP! [/] [bold magenta]{text}[/]",
            FightEventType.SubmissionAttempted => $"{time}[bold magenta]{text}[/]",
            FightEventType.SubmissionEscaped => $"{time}[grey]{text}[/]",
            FightEventType.StrikeBlocked => $"{time}[grey]{text}[/]",
            FightEventType.TakedownLanded => $"{time}[bold cyan]{text}[/]",
            FightEventType.TakedownDefended => $"{time}[grey]{text}[/]",
            FightEventType.FightEnded => $"\n[bold red on white] FIGHT OVER! [/] [bold]{text}[/]\n",
            FightEventType.StrikeLanded when ev.IsSignificant => $"{time}[bold yellow]{text}[/]",
            FightEventType.PositionChange => $"{time}[cyan]{text}[/]",
            _ => $"{time}[white]{text}[/]"
        };
    }

    /// <summary>
    /// Classifies the body zone hit by a strike event, if any.
    /// </summary>
    private static HealthZone? GetHealthZone(FightEvent ev) => ev.StrikeType switch
    {
        StrikeType.Jab or
        StrikeType.Cross or
        StrikeType.Hook or
        StrikeType.Uppercut or
        StrikeType.Overhand or
        StrikeType.ElbowHorizontal or
        StrikeType.ElbowUpward or
        StrikeType.SpinningElbow or
        StrikeType.KneeHead or
        StrikeType.HeadKick or
        StrikeType.GroundPunch or
        StrikeType.GroundElbow or
        StrikeType.Hammerfist => HealthZone.Head,

        StrikeType.BodyShot or
        StrikeType.KneeBody or
        StrikeType.FrontKick or
        StrikeType.Teep or
        StrikeType.BodyKick or
        StrikeType.SpinningBackKick => HealthZone.Body,

        StrikeType.LowKick or
        StrikeType.CalfKick or
        StrikeType.ObliqueKick or
        StrikeType.Roundhouse or
        StrikeType.Stomp => HealthZone.Legs,

        _ => null
    };

    /// <summary>
    /// Displays the judges' round scorecards and summary stats when available.
    /// </summary>
    private static void ShowRoundScorecard(Round round, Fighter fighterA, Fighter fighterB)
    {
        if (round.Scorecards.Count == 0) return;

        var a = round.FighterAStats;
        var b = round.FighterBStats;
        var statsLine = $"[dim]Sig. strikes: [cyan]{a.SignificantStrikesLanded}[/]-[yellow]{b.SignificantStrikesLanded}[/]  " +
                        $"TDs: [cyan]{a.TakedownsLanded}[/]-[yellow]{b.TakedownsLanded}[/]  " +
                        $"Subs: [cyan]{a.SubmissionAttempts}[/]-[yellow]{b.SubmissionAttempts}[/][/]";

        var cardLines = round.Scorecards.Select(sc =>
            $"[grey]Judge {sc.JudgeIndex}:[/] [cyan]{sc.ScoreFighterA}[/] - [yellow]{sc.ScoreFighterB}[/]  [dim]{Markup.Escape(sc.Rationale)}[/]");

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

    /// <summary>
    /// Displays the condensed round statistics for both fighters.
    /// </summary>
    public void ShowFightStats(Round round, Fighter fighterA, Fighter fighterB)
    {
        var a = round.FighterAStats;
        var b = round.FighterBStats;

        AnsiConsole.MarkupLine($"  [grey]Sig. Strikes:[/] [cyan]{a.SignificantStrikesLanded}[/] - [yellow]{b.SignificantStrikesLanded}[/]");
        AnsiConsole.MarkupLine($"  [grey]Takedowns:   [/] [cyan]{a.TakedownsLanded}/{a.TakedownsAttempted}[/] - [yellow]{b.TakedownsLanded}/{b.TakedownsAttempted}[/]");
        AnsiConsole.MarkupLine($"  [grey]Submissions: [/] [cyan]{a.SubmissionAttempts}[/] - [yellow]{b.SubmissionAttempts}[/]");
    }

    private enum HealthZone
    {
        Head,
        Body,
        Legs
    }

    public sealed class FightHealthTracker
    {
        private const double HeadPool = 24.0;
        private const double BodyPool = 30.0;
        private const double LegPool = 26.0;

        private readonly Fighter _fighterA;
        private readonly Fighter _fighterB;

        private double _fighterAHead = 100.0;
        private double _fighterABody = 100.0;
        private double _fighterALegs = 100.0;
        private double _fighterBHead = 100.0;
        private double _fighterBBody = 100.0;
        private double _fighterBLegs = 100.0;

        /// <summary>
        /// Creates a round-level health tracker for both fighters.
        /// </summary>
        public FightHealthTracker(Fighter fighterA, Fighter fighterB)
        {
            _fighterA = fighterA;
            _fighterB = fighterB;
        }

        /// <summary>
        /// Applies the visible event to the tracked health pools.
        /// </summary>
        public void Apply(FightEvent ev)
        {
            if (ev.Target is null || ev.DamageDealt <= 0)
                return;

            var zone = GetHealthZone(ev);
            if (zone is null)
                return;

            var percentDamage = zone switch
            {
                HealthZone.Head => ev.DamageDealt / HeadPool * 100.0,
                HealthZone.Body => ev.DamageDealt / BodyPool * 100.0,
                HealthZone.Legs => ev.DamageDealt / LegPool * 100.0,
                _ => 0.0
            };

            if (ev.Type == FightEventType.KnockdownScored && zone == HealthZone.Head)
                percentDamage += 8.0;

            if (ev.Target.Id == _fighterA.Id)
                ApplyToFighterA(zone.Value, percentDamage);
            else if (ev.Target.Id == _fighterB.Id)
                ApplyToFighterB(zone.Value, percentDamage);
        }

        /// <summary>
        /// Formats the current body-part health status for both fighters.
        /// </summary>
        public string FormatStatus()
        {
            var fighterALabel = Markup.Escape(_fighterA.LastName);
            var fighterBLabel = Markup.Escape(_fighterB.LastName);

            return
                $"[dim]{fighterALabel}[/] [grey]H:[/] {FormatPercent(_fighterAHead)} [grey]B:[/] {FormatPercent(_fighterABody)} [grey]L:[/] {FormatPercent(_fighterALegs)}" +
                $"    [dim]{fighterBLabel}[/] [grey]H:[/] {FormatPercent(_fighterBHead)} [grey]B:[/] {FormatPercent(_fighterBBody)} [grey]L:[/] {FormatPercent(_fighterBLegs)}";
        }

        /// <summary>
        /// Applies percentage damage to fighter A in the given zone.
        /// </summary>
        private void ApplyToFighterA(HealthZone zone, double percentDamage)
        {
            switch (zone)
            {
                case HealthZone.Head:
                    _fighterAHead = ClampHealth(_fighterAHead - percentDamage);
                    break;
                case HealthZone.Body:
                    _fighterABody = ClampHealth(_fighterABody - percentDamage);
                    break;
                case HealthZone.Legs:
                    _fighterALegs = ClampHealth(_fighterALegs - percentDamage);
                    break;
            }
        }

        /// <summary>
        /// Applies percentage damage to fighter B in the given zone.
        /// </summary>
        private void ApplyToFighterB(HealthZone zone, double percentDamage)
        {
            switch (zone)
            {
                case HealthZone.Head:
                    _fighterBHead = ClampHealth(_fighterBHead - percentDamage);
                    break;
                case HealthZone.Body:
                    _fighterBBody = ClampHealth(_fighterBBody - percentDamage);
                    break;
                case HealthZone.Legs:
                    _fighterBLegs = ClampHealth(_fighterBLegs - percentDamage);
                    break;
            }
        }

        /// <summary>
        /// Formats one health percentage with color thresholds.
        /// </summary>
        private static string FormatPercent(double value)
        {
            var rounded = Math.Round(value);
            var color = rounded switch
            {
                >= 70 => "green",
                >= 40 => "yellow",
                _ => "red"
            };

            return $"[{color}]{rounded,3}%[/]";
        }

        /// <summary>
        /// Clamps tracked health values to the visible percentage range.
        /// </summary>
        private static double ClampHealth(double value) => Math.Clamp(value, 0.0, 100.0);
    }
}
