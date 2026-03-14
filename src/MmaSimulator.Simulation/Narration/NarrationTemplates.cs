using MmaSimulator.Core.Enums;

namespace MmaSimulator.Simulation.Narration;

internal static class NarrationTemplates
{
    internal static readonly IReadOnlyDictionary<StrikeType, string[]> StrikeLanded = new Dictionary<StrikeType, string[]>
    {
        [StrikeType.Jab] =
        [
            "{actor} snaps a sharp jab that catches {target} flush.",
            "{actor} peppers {target} with a quick jab.",
            "A clean jab from {actor} keeps {target} at distance."
        ],
        [StrikeType.Cross] =
        [
            "{actor} lands a powerful straight right on {target}!",
            "{actor} fires a cross that rocks {target}!",
            "A thunderous cross from {actor} stuns {target}!"
        ],
        [StrikeType.Hook] =
        [
            "{actor} lands a HUGE left hook on {target}!",
            "{actor} clips {target} with a hook to the head!",
            "A vicious hook from {actor} sends {target} stumbling!"
        ],
        [StrikeType.Uppercut] =
        [
            "{actor} sneaks in a tight uppercut on {target}!",
            "A picture-perfect uppercut from {actor} snaps {target}'s head back!",
            "{actor} lands a devastating uppercut!"
        ],
        [StrikeType.BodyShot] =
        [
            "{actor} digs a body shot into {target}'s ribs!",
            "{actor} works the body of {target} with a hard shot.",
            "A thudding body shot from {actor} makes {target} wince!"
        ],
        [StrikeType.Overhand] =
        [
            "{actor} lands a looping overhand right on {target}!",
            "A big overhand from {actor} catches {target} on the temple!",
            "{actor} uncorks an overhand that lands clean!"
        ],
        [StrikeType.Elbow] =
        [
            "{actor} opens a cut on {target} with a sharp elbow!",
            "A slicing elbow from {actor} rocks {target}!",
            "{actor} lands a devastating elbow in the clinch!"
        ],
        [StrikeType.Knee] =
        [
            "{actor} drives a knee into {target}'s midsection!",
            "A hard knee from {actor} in the clinch!",
            "{actor} lands a powerful knee strike!"
        ],
        [StrikeType.Roundhouse] =
        [
            "{actor} lands a crisp roundhouse kick to the leg of {target}!",
            "A solid roundhouse from {actor} lands on {target}'s thigh!",
            "{actor} checks the distance and lands a roundhouse!"
        ],
        [StrikeType.HeadKick] =
        [
            "{actor} LANDS A HEAD KICK on {target}! This could be over!",
            "A HIGHLIGHT-REEL HEAD KICK from {actor} sends {target} crashing to the canvas!",
            "{actor} lands a perfectly timed head kick on {target}!"
        ],
        [StrikeType.SpinningBackKick] =
        [
            "{actor} spins and lands a back kick right into {target}'s solar plexus!",
            "A spinning back kick from {actor} catches {target} off guard!",
            "{actor} lands a technical spinning kick on {target}!"
        ],
        [StrikeType.FrontKick] =
        [
            "{actor} extends a front kick that pushes {target} back.",
            "A teep kick from {actor} keeps {target} at the end of the range.",
            "{actor} lands a front kick on {target}'s midsection!"
        ]
    };

    internal static readonly string[] StrikeMissed =
    [
        "{actor} throws wildly but {target} slips out of range.",
        "{actor} swings and misses as {target} ducks under.",
        "{target} steps back and makes {actor} miss badly.",
        "{actor} overcommits on the punch and loses balance."
    ];

    internal static readonly string[] StrikeBlocked =
    [
        "{target} raises the guard and blocks the shot from {actor}.",
        "{actor}'s punch is deflected by {target}'s tight guard.",
        "{target} rolls with the punch from {actor}, absorbing most of the damage."
    ];

    internal static readonly string[] TakedownLanded =
    [
        "{actor} shoots in deep and DRIVES {target} to the mat!",
        "A beautiful double-leg from {actor} puts {target} on the canvas!",
        "{actor} clinches up and trips {target} to the ground!",
        "Textbook wrestling from {actor} — {target} is taken down!"
    ];

    internal static readonly string[] TakedownDefended =
    [
        "{target} sprawls and stuffs the takedown from {actor}!",
        "{target} shows excellent takedown defense, stopping {actor} cold.",
        "{actor} shoots but {target} uses the cage to defend!",
        "Great defense from {target} — the takedown is denied!"
    ];

    internal static readonly string[] KnockdownScored =
    [
        "{actor} DROPS {target}! {target} is DOWN on the canvas!",
        "A MASSIVE SHOT from {actor} sends {target} to the floor!",
        "{target} hits the mat hard from a punch by {actor}! The referee steps in for a count!"
    ];

    internal static readonly string[] SubmissionAttempted =
    [
        "{actor} is looking for the submission! {target} is in danger!",
        "{actor} locks up the arm — this could be an armbar!",
        "{actor} sinks in the rear naked choke! {target} has to escape!"
    ];

    internal static readonly string[] SubmissionEscaped =
    [
        "Incredible defense from {target}! The submission attempt is escaped!",
        "{target} scratches and claws their way out of the submission!",
        "Great grappling IQ from {target} — the submission is defended!"
    ];

    internal static readonly string[] SubmissionFinish =
    [
        "{target} TAPS OUT! {actor} wins by submission!",
        "The referee stops it — {target} could not escape the submission from {actor}!",
        "{actor} locks in the finish and {target} has no choice but to tap!"
    ];

    internal static readonly string[] GuardPass =
    [
        "{actor} passes the guard! Dominant position secured!",
        "{actor} advances position over {target}!",
        "Great ground work from {actor} — the guard is passed!"
    ];

    internal static string Format(string template, string actorName, string targetName = "") =>
        template
            .Replace("{actor}", actorName)
            .Replace("{target}", targetName);
}
