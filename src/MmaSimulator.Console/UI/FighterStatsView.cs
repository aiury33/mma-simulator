using MmaSimulator.Core.Models;
using Spectre.Console;

namespace MmaSimulator.Console.UI;

public sealed class FighterStatsView
{
    public void ShowComparison(Fighter a, Fighter b)
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("\n[bold red]FIGHT MATCHUP[/]\n");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Red1)
            .AddColumn(new TableColumn("[bold grey]Attribute[/]"))
            .AddColumn(new TableColumn($"[bold cyan]{a.FullName}[/]").Centered())
            .AddColumn(new TableColumn($"[bold yellow]{b.FullName}[/]").Centered());

        table.AddRow("Record", $"[cyan]{a.Record.Display}[/]", $"[yellow]{b.Record.Display}[/]");
        table.AddRow("Style", $"[cyan]{a.PrimaryStyle}[/]", $"[yellow]{b.PrimaryStyle}[/]");
        table.AddRow("Stance", $"[cyan]{a.Stance}[/]", $"[yellow]{b.Stance}[/]");
        table.AddRow("Nationality", $"[cyan]{a.Nationality}[/]", $"[yellow]{b.Nationality}[/]");
        table.AddRow("Height", $"[cyan]{a.Physical.HeightCm}cm[/]", $"[yellow]{b.Physical.HeightCm}cm[/]");
        table.AddRow("Reach", $"[cyan]{a.Physical.ReachCm}cm[/]", $"[yellow]{b.Physical.ReachCm}cm[/]");
        table.AddRow("Age", $"[cyan]{a.Physical.Age}[/]", $"[yellow]{b.Physical.Age}[/]");
        table.AddEmptyRow();
        AddStatRow(table, "Strike Accuracy", a.Striking.Accuracy, b.Striking.Accuracy);
        AddStatRow(table, "Strike Power", a.Striking.Power, b.Striking.Power);
        AddStatRow(table, "Strike Speed", a.Striking.Speed, b.Striking.Speed);
        AddStatRow(table, "Strike Defense", a.Striking.Defense, b.Striking.Defense);
        AddStatRow(table, "Chin Durability", a.Striking.ChinDurability, b.Striking.ChinDurability);
        table.AddEmptyRow();
        AddStatRow(table, "Takedown Accuracy", a.Grappling.TakedownAccuracy, b.Grappling.TakedownAccuracy);
        AddStatRow(table, "Takedown Defense", a.Grappling.TakedownDefense, b.Grappling.TakedownDefense);
        AddStatRow(table, "Submission Offense", a.Grappling.SubmissionOffense, b.Grappling.SubmissionOffense);
        AddStatRow(table, "Submission Defense", a.Grappling.SubmissionDefense, b.Grappling.SubmissionDefense);
        AddStatRow(table, "Ground Control", a.Grappling.GroundControl, b.Grappling.GroundControl);
        table.AddEmptyRow();
        AddStatRow(table, "Stamina", a.Athletics.Stamina, b.Athletics.Stamina);
        AddStatRow(table, "Strength", a.Athletics.Strength, b.Athletics.Strength);
        AddStatRow(table, "Cardio", a.Athletics.Cardio, b.Athletics.Cardio);
        AddStatRow(table, "Toughness", a.Athletics.Toughness, b.Athletics.Toughness);

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void AddStatRow(Table table, string label, int valueA, int valueB)
    {
        var barA = BuildBar(valueA, "[cyan]");
        var barB = BuildBar(valueB, "[yellow]");
        table.AddRow(label, $"{barA} [grey]{valueA}[/]", $"{barB} [grey]{valueB}[/]");
    }

    private static string BuildBar(int value, string color)
    {
        var filled = (int)Math.Round(value / 10.0);
        var empty = 10 - filled;
        return $"{color}{new string('█', filled)}[/][grey]{new string('░', empty)}[/]";
    }
}
