using MmaSimulator.Core.Models;

namespace MmaSimulator.Core.Interfaces;

public interface IJudgeScoringEngine
{
    JudgeScorecard ScoreRound(int judgeIndex, Round round, Fighter fighterA, Fighter fighterB);
    JudgeDecision TallyDecision(int judgeIndex, IReadOnlyList<JudgeScorecard> roundScores, Fighter fighterA, Fighter fighterB);
}
