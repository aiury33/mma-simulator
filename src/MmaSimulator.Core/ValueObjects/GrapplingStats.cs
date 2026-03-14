namespace MmaSimulator.Core.ValueObjects;

public sealed record GrapplingStats(
    int TakedownAccuracy,
    int TakedownDefense,
    int SubmissionOffense,
    int SubmissionDefense,
    int GroundControl,
    int Clinchwork);
