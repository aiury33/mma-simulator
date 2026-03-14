using FluentAssertions;
using MmaSimulator.Core.Tests.Fixtures;

namespace MmaSimulator.Core.Tests.Models;

public sealed class FighterTests
{
    [Fact]
    public void FullName_WithNickname_IncludesNicknameInQuotes()
    {
        var fighter = FighterFixtures.CreateStriker();

        fighter.FullName.Should().Contain("\"The Puncher\"");
        fighter.FullName.Should().StartWith("Test");
        fighter.FullName.Should().EndWith("Striker");
    }

    [Fact]
    public void FullName_WithNickname_IsNotEmpty()
    {
        var fighter = FighterFixtures.CreateStriker();

        fighter.FullName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Fighter_Record_DisplayFormatsCorrectly()
    {
        var fighter = FighterFixtures.CreateStriker();

        fighter.Record.Display.Should().Be("10-2-0");
    }

    [Fact]
    public void Fighter_HasUniqueId_WhenCreated()
    {
        var f1 = FighterFixtures.CreateStriker();
        var f2 = FighterFixtures.CreateStriker();

        f1.Id.Should().NotBe(f2.Id);
    }
}
