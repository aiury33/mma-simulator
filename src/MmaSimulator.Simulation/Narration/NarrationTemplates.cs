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
        [StrikeType.ElbowHorizontal] =
        [
            "{actor} opens a cut on {target} with a sharp elbow!",
            "A slicing elbow from {actor} rocks {target}!",
            "{actor} lands a devastating elbow in the clinch!"
        ],
        [StrikeType.ElbowUpward] =
        [
            "{actor} sneaks a brutal upward elbow through the middle on {target}!",
            "A rising elbow from {actor} splits the guard of {target}!",
            "{actor} clips {target} with a short upward elbow!"
        ],
        [StrikeType.SpinningElbow] =
        [
            "{actor} spins and lands a highlight-reel elbow on {target}!",
            "A spinning elbow from {actor} crashes into {target}!",
            "{target} walks into a spinning elbow from {actor}!"
        ],
        [StrikeType.KneeBody] =
        [
            "{actor} drives a knee into {target}'s midsection!",
            "A hard knee from {actor} in the clinch!",
            "{actor} lands a powerful knee strike!"
        ],
        [StrikeType.KneeHead] =
        [
            "{actor} rips a knee upstairs and catches {target} clean!",
            "A vicious flying knee from {actor} clips {target}!",
            "{actor} lands a crushing knee to the head of {target}!"
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
        ],
        [StrikeType.Teep] =
        [
            "{actor} stabs a teep into the body of {target}!",
            "{actor} uses a piston-like teep to keep {target} honest.",
            "A sharp teep from {actor} jolts {target} backward!"
        ],
        [StrikeType.BodyKick] =
        [
            "{actor} whips a body kick into the ribs of {target}!",
            "A slamming body kick from {actor} lands flush!",
            "{actor} hammers the midsection of {target} with a kick!"
        ],
        [StrikeType.LowKick] =
        [
            "{actor} slaps a low kick across the thigh of {target}!",
            "A heavy low kick from {actor} lands on the lead leg!",
            "{actor} chews up the leg of {target} with a kick!"
        ],
        [StrikeType.CalfKick] =
        [
            "{actor} hacks at the calf of {target} with a nasty kick!",
            "A calf kick from {actor} buckles the base of {target}!",
            "{actor} lands a painful calf kick on {target}!"
        ],
        [StrikeType.ObliqueKick] =
        [
            "{actor} stomps an oblique kick into the knee line of {target}!",
            "A nasty knee stomp from {actor} lands at range!",
            "{actor} attacks the lead knee of {target} with an oblique kick!"
        ],
        [StrikeType.Stomp] =
        [
            "{actor} lands a vicious stomp to the leg of {target} from range!",
            "{actor} stamps down hard on the knee line of {target}!",
            "A brutal stomping kick from {actor} catches {target}!"
        ],
        [StrikeType.GroundPunch] =
        [
            "{actor} postures up and lands a crushing ground-and-pound punch!",
            "{actor} unloads a heavy shot from top position on {target}!",
            "A nasty punch from the top gets through for {actor}!"
        ],
        [StrikeType.GroundElbow] =
        [
            "{actor} carves {target} up with a sharp elbow from top control!",
            "A slicing ground elbow from {actor} lands clean!",
            "{actor} opens up with brutal elbows on the mat!"
        ],
        [StrikeType.Hammerfist] =
        [
            "{actor} slams a hammerfist down on {target}!",
            "A heavy hammerfist from {actor} crashes through the guard!",
            "{actor} lands a punishing hammerfist from the top!"
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

    internal static readonly IReadOnlyDictionary<GrappleAction, string[]> GrappleActionTemplates = new Dictionary<GrappleAction, string[]>
    {
        [GrappleAction.SingleLegTakedown] =
        [
            "{actor} runs the pipe on a single-leg and dumps {target} down!",
            "{actor} snatches a single-leg and finishes the takedown!"
        ],
        [GrappleAction.DoubleLegTakedown] =
        [
            "{actor} blasts through with a double-leg on {target}!",
            "{actor} changes levels and drives {target} to the mat with a double-leg!"
        ],
        [GrappleAction.BodyLockTakedown] =
        [
            "{actor} locks the body and drags {target} down!",
            "A body-lock takedown from {actor} puts {target} on the canvas!"
        ],
        [GrappleAction.TripFromClinch] =
        [
            "{actor} hits a slick trip from the clinch on {target}!",
            "{actor} reaps the leg and sends {target} down from the clinch!"
        ],
        [GrappleAction.OutsideTrip] =
        [
            "{actor} lands an outside trip and topples {target}!",
            "Beautiful outside reap from {actor} — {target} goes down!"
        ],
        [GrappleAction.InsideTrip] =
        [
            "{actor} snags the inside trip and dumps {target}!",
            "A crafty inside trip from {actor} brings {target} to the mat!"
        ],
        [GrappleAction.SubmissionAttemptRearNakedChoke] =
        [
            "{actor} snakes the arm under the chin — rear naked choke attempt!",
            "{actor} is hunting the rear naked choke on {target}!"
        ],
        [GrappleAction.SubmissionAttemptGuillotine] =
        [
            "{actor} jumps on a guillotine as {target} shoots in!",
            "{actor} wraps the neck for a guillotine choke!"
        ],
        [GrappleAction.SubmissionAttemptDarce] =
        [
            "{actor} threads the arm through for a darce choke!",
            "{actor} is squeezing on a nasty darce!"
        ],
        [GrappleAction.SubmissionAttemptAnaconda] =
        [
            "{actor} rolls through on an anaconda choke attempt!",
            "{actor} is locking up the anaconda on {target}!"
        ],
        [GrappleAction.SubmissionAttemptArmTriangle] =
        [
            "{actor} traps the arm and moves to an arm-triangle choke!",
            "{actor} is trying to finish an arm-triangle from top!"
        ],
        [GrappleAction.SubmissionAttemptTriangle] =
        [
            "{actor} throws the legs high for a triangle choke!",
            "{actor} locks the figure-four for a triangle attempt!"
        ],
        [GrappleAction.SubmissionAttemptArmbar] =
        [
            "{actor} swings for the armbar on {target}!",
            "{actor} isolates the arm — armbar attempt!"
        ],
        [GrappleAction.SubmissionAttemptKimura] =
        [
            "{actor} secures a kimura grip on {target}!",
            "{actor} cranks on a kimura attempt!"
        ],
        [GrappleAction.SubmissionAttemptHeelHook] =
        [
            "{actor} dives on the heel hook and torques the leg!",
            "{actor} attacks the heel for a dangerous heel hook!"
        ],
        [GrappleAction.KneeCutPass] =
        [
            "{actor} slices through with a knee-cut pass!",
            "{actor} shreds the guard with a sharp knee-cut!"
        ],
        [GrappleAction.StackPass] =
        [
            "{actor} stacks {target} up and begins to pass!",
            "{actor} crushes through with a stack pass!"
        ],
        [GrappleAction.ButterflySweep] =
        [
            "{actor} elevates and hits a butterfly sweep!",
            "{actor} scoops the butterfly hooks and reverses position!"
        ],
        [GrappleAction.BackTake] =
        [
            "{actor} slips around and takes the back of {target}!",
            "{actor} secures back control in transition!"
        ]
    };

    /// <summary>
    /// Replaces actor and target placeholders in a narration template.
    /// </summary>
    internal static string Format(string template, string actorName, string targetName = "") =>
        template
            .Replace("{actor}", actorName)
            .Replace("{target}", targetName);
}
