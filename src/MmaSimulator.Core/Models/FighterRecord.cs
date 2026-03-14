namespace MmaSimulator.Core.Models;

public sealed record FighterRecord(int Wins, int Losses, int Draws, int NoContests = 0)
{
    public string Display => $"{Wins}-{Losses}-{Draws}";
}
