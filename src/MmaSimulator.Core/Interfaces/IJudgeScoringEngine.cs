using MmaSimulator.Core.Models;

namespace MmaSimulator.Core.Interfaces;

/// <summary>
/// Implements the UFC 10-point must system for scoring rounds and tallying decisions.
///
/// <para>Three independent judges score each completed round (no finish).
/// Each judge has slightly different criteria weightings:</para>
/// <list type="table">
///   <item><term>Judge 1</term><description>Striking 40%, Grappling 35%, Aggression 15%, Control 10%</description></item>
///   <item><term>Judge 2</term><description>Striking 45%, Grappling 30%, Aggression 10%, Control 15%</description></item>
///   <item><term>Judge 3</term><description>Striking 35%, Grappling 40%, Aggression 15%, Control 10%</description></item>
/// </list>
///
/// <para>A 10-8 round is awarded when a knockdown occurred AND the scorer determines the round
/// was dominated. A 10-10 is possible on an extremely close round.
/// After all rounds, each judge's totals determine a Unanimous, Split, or Draw decision.</para>
/// </summary>
public interface IJudgeScoringEngine
{
    /// <summary>
    /// Scores a single completed round from one judge's perspective.
    /// </summary>
    /// <param name="judgeIndex">1, 2, or 3 — selects the judge's criteria weighting.</param>
    /// <param name="round">The completed round containing all events and per-fighter stats.</param>
    /// <param name="fighterA">Reference to Fighter A for stat lookups.</param>
    /// <param name="fighterB">Reference to Fighter B for stat lookups.</param>
    /// <returns>
    /// A <see cref="JudgeScorecard"/> with scores for both fighters and a brief rationale string.
    /// </returns>
    JudgeScorecard ScoreRound(int judgeIndex, Round round, Fighter fighterA, Fighter fighterB);

    /// <summary>
    /// Aggregates all per-round scorecards for one judge and determines their winner.
    /// </summary>
    /// <param name="judgeIndex">1, 2, or 3 — used to filter scorecards belonging to this judge.</param>
    /// <param name="roundScores">All scorecards from all rounds of the fight.</param>
    /// <param name="fighterA">Reference to Fighter A.</param>
    /// <param name="fighterB">Reference to Fighter B.</param>
    /// <returns>
    /// A <see cref="JudgeDecision"/> with the judge's total score and their chosen winner.
    /// </returns>
    JudgeDecision TallyDecision(int judgeIndex, IReadOnlyList<JudgeScorecard> roundScores,
        Fighter fighterA, Fighter fighterB);
}
