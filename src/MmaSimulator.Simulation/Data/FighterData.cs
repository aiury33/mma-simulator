using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;
using MmaSimulator.Core.ValueObjects;

namespace MmaSimulator.Simulation.Data;

// Roster: UFC top 15 per weight class as of March 2026
// Stats guide:  StrikingStats(accuracy, power, speed, defense, chinDurability, bodyDurability)
//               GrapplingStats(takedownAcc, takedownDef, subOffense, subDefense, groundControl, clinchwork)
//               AthleticStats(stamina, strength, agility, cardio, toughness)
internal static class FighterData
{
    internal static readonly IReadOnlyList<Fighter> All =
    [
        // ──────────────── FLYWEIGHT (125 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000001"),
            FirstName = "Alexandre", LastName = "Pantoja", Nickname = "The Cannibal",
            Nationality = "Brazil", WeightClass = WeightClass.Flyweight,
            PrimaryStyle = FightingStyle.BJJPractitioner, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(168, 125, 168, 33),
            Striking  = new StrikingStats(76, 72, 84, 74, 78, 76),
            Grappling = new GrapplingStats(78, 76, 92, 84, 86, 78),
            Athletics = new AthleticStats(88, 74, 86, 90, 82),
            Record = new FighterRecord(27, 5, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000002"),
            FirstName = "Brandon", LastName = "Royval", Nickname = "Raw Dawg",
            Nationality = "USA", WeightClass = WeightClass.Flyweight,
            PrimaryStyle = FightingStyle.BJJPractitioner, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(175, 125, 178, 31),
            Striking  = new StrikingStats(74, 76, 86, 68, 72, 74),
            Grappling = new GrapplingStats(76, 72, 88, 80, 80, 74),
            Athletics = new AthleticStats(88, 72, 88, 88, 76),
            Record = new FighterRecord(16, 6, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000003"),
            FirstName = "Steve", LastName = "Erceg", Nickname = "The Hammer",
            Nationality = "Australia", WeightClass = WeightClass.Flyweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(170, 125, 170, 29),
            Striking  = new StrikingStats(74, 72, 80, 72, 76, 74),
            Grappling = new GrapplingStats(74, 72, 76, 74, 74, 72),
            Athletics = new AthleticStats(84, 72, 80, 84, 80),
            Record = new FighterRecord(11, 2, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000004"),
            FirstName = "Amir", LastName = "Albazi", Nickname = "The Prince",
            Nationality = "Sweden", WeightClass = WeightClass.Flyweight,
            PrimaryStyle = FightingStyle.BJJPractitioner, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(170, 125, 175, 30),
            Striking  = new StrikingStats(72, 68, 78, 74, 76, 74),
            Grappling = new GrapplingStats(80, 78, 86, 82, 82, 76),
            Athletics = new AthleticStats(86, 74, 84, 86, 80),
            Record = new FighterRecord(17, 1, 0)
        },

        // ──────────────── BANTAMWEIGHT (135 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000005"),
            FirstName = "Merab", LastName = "Dvalishvili", Nickname = "The Machine",
            Nationality = "Georgia", WeightClass = WeightClass.Bantamweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(175, 135, 173, 33),
            Striking  = new StrikingStats(68, 65, 80, 70, 80, 76),
            Grappling = new GrapplingStats(92, 84, 72, 82, 90, 84),
            Athletics = new AthleticStats(98, 80, 86, 100, 86),
            Record = new FighterRecord(18, 4, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000006"),
            FirstName = "Sean", LastName = "O'Malley", Nickname = "Sugar",
            Nationality = "USA", WeightClass = WeightClass.Bantamweight,
            PrimaryStyle = FightingStyle.Striker, Stance = Stance.Southpaw,
            Physical = new PhysicalStats(178, 135, 180, 29),
            Striking  = new StrikingStats(86, 82, 88, 76, 70, 72),
            Grappling = new GrapplingStats(54, 62, 52, 60, 54, 58),
            Athletics = new AthleticStats(82, 68, 92, 84, 72),
            Record = new FighterRecord(17, 2, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000007"),
            FirstName = "Umar", LastName = "Nurmagomedov", Nickname = "The Predator",
            Nationality = "Russia", WeightClass = WeightClass.Bantamweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(173, 135, 175, 27),
            Striking  = new StrikingStats(74, 72, 78, 76, 86, 82),
            Grappling = new GrapplingStats(90, 88, 80, 86, 90, 84),
            Athletics = new AthleticStats(92, 88, 82, 92, 88),
            Record = new FighterRecord(18, 0, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000008"),
            FirstName = "Cory", LastName = "Sandhagen", Nickname = "The Sandman",
            Nationality = "USA", WeightClass = WeightClass.Bantamweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(183, 135, 183, 32),
            Striking  = new StrikingStats(82, 74, 86, 76, 74, 76),
            Grappling = new GrapplingStats(68, 70, 64, 68, 66, 66),
            Athletics = new AthleticStats(88, 74, 88, 90, 80),
            Record = new FighterRecord(17, 4, 0)
        },

        // ──────────────── FEATHERWEIGHT (145 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000009"),
            FirstName = "Ilia", LastName = "Topuria", Nickname = "El Matador",
            Nationality = "Georgia", WeightClass = WeightClass.Featherweight,
            PrimaryStyle = FightingStyle.Boxer, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(170, 145, 170, 27),
            Striking  = new StrikingStats(86, 94, 86, 80, 84, 82),
            Grappling = new GrapplingStats(72, 78, 74, 76, 72, 76),
            Athletics = new AthleticStats(88, 86, 88, 88, 88),
            Record = new FighterRecord(16, 0, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000010"),
            FirstName = "Max", LastName = "Holloway", Nickname = "Blessed",
            Nationality = "USA", WeightClass = WeightClass.Featherweight,
            PrimaryStyle = FightingStyle.Boxer, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(180, 145, 175, 33),
            Striking  = new StrikingStats(82, 72, 90, 74, 76, 78),
            Grappling = new GrapplingStats(68, 74, 64, 68, 66, 70),
            Athletics = new AthleticStats(94, 72, 88, 96, 84),
            Record = new FighterRecord(25, 7, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000011"),
            FirstName = "Brian", LastName = "Ortega", Nickname = "T-City",
            Nationality = "USA", WeightClass = WeightClass.Featherweight,
            PrimaryStyle = FightingStyle.BJJPractitioner, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(175, 145, 180, 33),
            Striking  = new StrikingStats(76, 78, 74, 72, 84, 76),
            Grappling = new GrapplingStats(76, 72, 94, 86, 86, 78),
            Athletics = new AthleticStats(82, 76, 76, 82, 86),
            Record = new FighterRecord(16, 2, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000012"),
            FirstName = "Alexander", LastName = "Volkanovski", Nickname = "The Great",
            Nationality = "Australia", WeightClass = WeightClass.Featherweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(168, 145, 182, 36),
            Striking  = new StrikingStats(84, 78, 84, 80, 82, 82),
            Grappling = new GrapplingStats(84, 82, 72, 78, 80, 80),
            Athletics = new AthleticStats(92, 84, 86, 90, 88),
            Record = new FighterRecord(26, 4, 0)
        },

        // ──────────────── LIGHTWEIGHT (155 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000013"),
            FirstName = "Islam", LastName = "Makhachev", Nickname = "The Eagle's Heir",
            Nationality = "Russia", WeightClass = WeightClass.Lightweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(178, 155, 178, 33),
            Striking  = new StrikingStats(74, 74, 74, 76, 84, 82),
            Grappling = new GrapplingStats(94, 90, 84, 88, 93, 88),
            Athletics = new AthleticStats(94, 92, 82, 94, 90),
            Record = new FighterRecord(26, 1, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000014"),
            FirstName = "Charles", LastName = "Oliveira", Nickname = "Do Bronx",
            Nationality = "Brazil", WeightClass = WeightClass.Lightweight,
            PrimaryStyle = FightingStyle.BJJPractitioner, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(178, 155, 185, 35),
            Striking  = new StrikingStats(78, 80, 78, 68, 70, 70),
            Grappling = new GrapplingStats(82, 76, 96, 84, 88, 82),
            Athletics = new AthleticStats(86, 78, 82, 88, 78),
            Record = new FighterRecord(34, 9, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000015"),
            FirstName = "Arman", LastName = "Tsarukyan", Nickname = "Ahalkalakets",
            Nationality = "Armenia", WeightClass = WeightClass.Lightweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(175, 155, 178, 28),
            Striking  = new StrikingStats(76, 76, 80, 74, 82, 80),
            Grappling = new GrapplingStats(86, 82, 74, 80, 84, 80),
            Athletics = new AthleticStats(90, 84, 82, 92, 84),
            Record = new FighterRecord(22, 3, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000016"),
            FirstName = "Dustin", LastName = "Poirier", Nickname = "The Diamond",
            Nationality = "USA", WeightClass = WeightClass.Lightweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Southpaw,
            Physical = new PhysicalStats(175, 155, 182, 36),
            Striking  = new StrikingStats(82, 84, 80, 72, 74, 80),
            Grappling = new GrapplingStats(72, 70, 74, 76, 72, 74),
            Athletics = new AthleticStats(86, 80, 82, 86, 86),
            Record = new FighterRecord(30, 8, 0)
        },

        // ──────────────── WELTERWEIGHT (170 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000017"),
            FirstName = "Belal", LastName = "Muhammad", Nickname = "Remember The Name",
            Nationality = "USA", WeightClass = WeightClass.Welterweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(178, 170, 183, 36),
            Striking  = new StrikingStats(70, 68, 74, 76, 82, 80),
            Grappling = new GrapplingStats(88, 82, 70, 78, 86, 82),
            Athletics = new AthleticStats(94, 82, 78, 96, 84),
            Record = new FighterRecord(24, 3, 1)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000018"),
            FirstName = "Leon", LastName = "Edwards", Nickname = "Rocky",
            Nationality = "UK", WeightClass = WeightClass.Welterweight,
            PrimaryStyle = FightingStyle.Striker, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(183, 170, 185, 32),
            Striking  = new StrikingStats(80, 76, 80, 78, 80, 76),
            Grappling = new GrapplingStats(72, 76, 65, 72, 68, 74),
            Athletics = new AthleticStats(88, 78, 86, 90, 82),
            Record = new FighterRecord(22, 3, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000019"),
            FirstName = "Shavkat", LastName = "Rakhmonov", Nickname = "Nomad",
            Nationality = "Kazakhstan", WeightClass = WeightClass.Welterweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(183, 170, 185, 29),
            Striking  = new StrikingStats(80, 88, 82, 74, 84, 80),
            Grappling = new GrapplingStats(82, 80, 84, 80, 82, 80),
            Athletics = new AthleticStats(90, 88, 84, 90, 88),
            Record = new FighterRecord(18, 0, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000020"),
            FirstName = "Jack", LastName = "Della Maddalena", Nickname = "The Highlight",
            Nationality = "Australia", WeightClass = WeightClass.Welterweight,
            PrimaryStyle = FightingStyle.Boxer, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(183, 170, 185, 27),
            Striking  = new StrikingStats(82, 90, 82, 72, 80, 76),
            Grappling = new GrapplingStats(64, 68, 58, 64, 60, 62),
            Athletics = new AthleticStats(86, 84, 82, 86, 80),
            Record = new FighterRecord(16, 2, 0)
        },

        // ──────────────── MIDDLEWEIGHT (185 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000021"),
            FirstName = "Dricus", LastName = "du Plessis", Nickname = "Stillknocks",
            Nationality = "South Africa", WeightClass = WeightClass.Middleweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(183, 185, 186, 30),
            Striking  = new StrikingStats(78, 84, 80, 76, 88, 86),
            Grappling = new GrapplingStats(76, 78, 78, 76, 78, 80),
            Athletics = new AthleticStats(90, 86, 82, 90, 92),
            Record = new FighterRecord(22, 2, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000022"),
            FirstName = "Israel", LastName = "Adesanya", Nickname = "The Last Stylebender",
            Nationality = "Nigeria", WeightClass = WeightClass.Middleweight,
            PrimaryStyle = FightingStyle.Kickboxer, Stance = Stance.Switch,
            Physical = new PhysicalStats(193, 185, 203, 34),
            Striking  = new StrikingStats(90, 78, 92, 86, 74, 70),
            Grappling = new GrapplingStats(52, 68, 58, 62, 56, 60),
            Athletics = new AthleticStats(82, 70, 94, 84, 76),
            Record = new FighterRecord(24, 4, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000023"),
            FirstName = "Khamzat", LastName = "Chimaev", Nickname = "Borz",
            Nationality = "Sweden", WeightClass = WeightClass.Middleweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(188, 185, 190, 30),
            Striking  = new StrikingStats(80, 88, 82, 78, 88, 86),
            Grappling = new GrapplingStats(92, 82, 80, 78, 90, 86),
            Athletics = new AthleticStats(90, 94, 82, 90, 92),
            Record = new FighterRecord(14, 0, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000024"),
            FirstName = "Sean", LastName = "Strickland", Nickname = "Tarzan",
            Nationality = "USA", WeightClass = WeightClass.Middleweight,
            PrimaryStyle = FightingStyle.Boxer, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(185, 185, 193, 33),
            Striking  = new StrikingStats(78, 72, 86, 74, 78, 76),
            Grappling = new GrapplingStats(62, 68, 58, 66, 60, 64),
            Athletics = new AthleticStats(90, 76, 84, 92, 88),
            Record = new FighterRecord(29, 6, 0)
        },

        // ──────────────── LIGHT HEAVYWEIGHT (205 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000025"),
            FirstName = "Alex", LastName = "Pereira", Nickname = "Poatan",
            Nationality = "Brazil", WeightClass = WeightClass.LightHeavyweight,
            PrimaryStyle = FightingStyle.Kickboxer, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(193, 205, 198, 37),
            Striking  = new StrikingStats(78, 98, 80, 72, 82, 78),
            Grappling = new GrapplingStats(58, 64, 56, 60, 58, 68),
            Athletics = new AthleticStats(82, 92, 80, 82, 88),
            Record = new FighterRecord(12, 2, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000026"),
            FirstName = "Jiri", LastName = "Prochazka", Nickname = "Denisa",
            Nationality = "Czech Republic", WeightClass = WeightClass.LightHeavyweight,
            PrimaryStyle = FightingStyle.Striker, Stance = Stance.Southpaw,
            Physical = new PhysicalStats(191, 205, 201, 32),
            Striking  = new StrikingStats(76, 90, 84, 68, 74, 72),
            Grappling = new GrapplingStats(62, 66, 62, 64, 60, 64),
            Athletics = new AthleticStats(88, 82, 90, 86, 84),
            Record = new FighterRecord(30, 4, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000027"),
            FirstName = "Magomed", LastName = "Ankalaev", Nickname = "Iron",
            Nationality = "Russia", WeightClass = WeightClass.LightHeavyweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(188, 205, 193, 31),
            Striking  = new StrikingStats(78, 80, 76, 78, 84, 80),
            Grappling = new GrapplingStats(84, 80, 72, 76, 80, 76),
            Athletics = new AthleticStats(88, 84, 80, 88, 86),
            Record = new FighterRecord(20, 1, 1)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000028"),
            FirstName = "Khalil", LastName = "Rountree Jr.", Nickname = "The War Horse",
            Nationality = "USA", WeightClass = WeightClass.LightHeavyweight,
            PrimaryStyle = FightingStyle.Kickboxer, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(185, 205, 188, 34),
            Striking  = new StrikingStats(74, 96, 78, 68, 76, 74),
            Grappling = new GrapplingStats(60, 62, 54, 58, 56, 58),
            Athletics = new AthleticStats(82, 88, 78, 80, 80),
            Record = new FighterRecord(13, 5, 1)
        },

        // ──────────────── HEAVYWEIGHT (265 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000029"),
            FirstName = "Jon", LastName = "Jones", Nickname = "Bones",
            Nationality = "USA", WeightClass = WeightClass.Heavyweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(193, 240, 215, 38),
            Striking  = new StrikingStats(84, 88, 78, 84, 86, 84),
            Grappling = new GrapplingStats(86, 86, 80, 82, 88, 84),
            Athletics = new AthleticStats(86, 90, 84, 84, 90),
            Record = new FighterRecord(27, 1, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000030"),
            FirstName = "Tom", LastName = "Aspinall", Nickname = "The Machine",
            Nationality = "UK", WeightClass = WeightClass.Heavyweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(193, 245, 201, 31),
            Striking  = new StrikingStats(82, 90, 84, 76, 78, 76),
            Grappling = new GrapplingStats(78, 74, 76, 74, 74, 76),
            Athletics = new AthleticStats(88, 92, 84, 88, 80),
            Record = new FighterRecord(15, 3, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000031"),
            FirstName = "Ciryl", LastName = "Gane", Nickname = "Bon Gamin",
            Nationality = "France", WeightClass = WeightClass.Heavyweight,
            PrimaryStyle = FightingStyle.Kickboxer, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(196, 250, 211, 34),
            Striking  = new StrikingStats(82, 80, 82, 82, 78, 76),
            Grappling = new GrapplingStats(68, 70, 62, 66, 64, 68),
            Athletics = new AthleticStats(82, 80, 86, 84, 76),
            Record = new FighterRecord(12, 2, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000032"),
            FirstName = "Sergei", LastName = "Pavlovich", Nickname = "The Bulldozer",
            Nationality = "Russia", WeightClass = WeightClass.Heavyweight,
            PrimaryStyle = FightingStyle.Striker, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(191, 243, 196, 31),
            Striking  = new StrikingStats(72, 96, 80, 64, 74, 70),
            Grappling = new GrapplingStats(64, 66, 56, 62, 60, 60),
            Athletics = new AthleticStats(76, 88, 76, 76, 78),
            Record = new FighterRecord(18, 2, 0)
        },

        // ──────────────── WOMEN'S STRAWWEIGHT (115 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000033"),
            FirstName = "Zhang", LastName = "Weili", Nickname = "Magnum",
            Nationality = "China", WeightClass = WeightClass.WomensStrawweight,
            PrimaryStyle = FightingStyle.Kickboxer, Stance = Stance.Southpaw,
            Physical = new PhysicalStats(163, 115, 163, 35),
            Striking  = new StrikingStats(84, 80, 88, 80, 82, 80),
            Grappling = new GrapplingStats(80, 76, 72, 74, 76, 78),
            Athletics = new AthleticStats(92, 78, 90, 92, 86),
            Record = new FighterRecord(24, 3, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000034"),
            FirstName = "Tatiana", LastName = "Suarez", Nickname = "The Lioness",
            Nationality = "USA", WeightClass = WeightClass.WomensStrawweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(163, 115, 165, 32),
            Striking  = new StrikingStats(68, 64, 72, 70, 78, 74),
            Grappling = new GrapplingStats(92, 80, 76, 78, 88, 78),
            Athletics = new AthleticStats(90, 76, 82, 90, 84),
            Record = new FighterRecord(11, 1, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000035"),
            FirstName = "Marina", LastName = "Rodriguez", Nickname = "Xena",
            Nationality = "Brazil", WeightClass = WeightClass.WomensStrawweight,
            PrimaryStyle = FightingStyle.MuayThai, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(168, 115, 170, 33),
            Striking  = new StrikingStats(80, 76, 84, 76, 78, 76),
            Grappling = new GrapplingStats(64, 66, 60, 62, 60, 62),
            Athletics = new AthleticStats(86, 70, 82, 88, 80),
            Record = new FighterRecord(17, 4, 0)
        },

        // ──────────────── WOMEN'S FLYWEIGHT (125 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000036"),
            FirstName = "Alexa", LastName = "Grasso", Nickname = "The Mexican Queen",
            Nationality = "Mexico", WeightClass = WeightClass.WomensFlyweight,
            PrimaryStyle = FightingStyle.BJJPractitioner, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(160, 125, 163, 31),
            Striking  = new StrikingStats(80, 72, 82, 76, 80, 78),
            Grappling = new GrapplingStats(72, 70, 84, 78, 76, 74),
            Athletics = new AthleticStats(86, 68, 86, 88, 82),
            Record = new FighterRecord(16, 3, 1)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000037"),
            FirstName = "Valentina", LastName = "Shevchenko", Nickname = "Bullet",
            Nationality = "Kyrgyzstan", WeightClass = WeightClass.WomensFlyweight,
            PrimaryStyle = FightingStyle.MuayThai, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(165, 125, 168, 36),
            Striking  = new StrikingStats(88, 76, 86, 86, 82, 82),
            Grappling = new GrapplingStats(74, 82, 74, 76, 74, 76),
            Athletics = new AthleticStats(90, 72, 90, 92, 88),
            Record = new FighterRecord(24, 4, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000038"),
            FirstName = "Manon", LastName = "Fiorot", Nickname = "La Lionne",
            Nationality = "France", WeightClass = WeightClass.WomensFlyweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            Physical = new PhysicalStats(163, 125, 163, 31),
            Striking  = new StrikingStats(78, 74, 82, 74, 78, 76),
            Grappling = new GrapplingStats(74, 72, 68, 70, 68, 68),
            Athletics = new AthleticStats(88, 70, 82, 88, 80),
            Record = new FighterRecord(11, 1, 0)
        }
    ];
}
