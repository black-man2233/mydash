using FluentAssertions;
using MyDash.Domain.Entities;

namespace MyDash.Domain.Tests;

public class ServerTests
{
    [Fact]
    public void NewEnrolled_sets_correct_fields()
    {
        var server = Server.NewEnrolled("hp", "HP ProLiant", "hp.tail.ts.net", "Ubuntu 24.04");

        server.Id.Should().NotBeEmpty();
        server.Name.Should().Be("hp");
        server.FullName.Should().Be("HP ProLiant");
        server.TailscaleHost.Should().Be("hp.tail.ts.net");
        server.OS.Should().Be("Ubuntu 24.04");
        server.InitialChar.Should().Be("H");
        server.Status.Should().Be(ServerStatus.Unknown);
        server.EnrolledAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("hp", "#0096D6")]
    [InlineData("huawei", "#C7000B")]
    [InlineData("cool", "#3DB2FF")]
    [InlineData("random", "#594AE2")]
    public void NewEnrolled_assigns_brand_colors(string name, string expectedColor)
    {
        var server = Server.NewEnrolled(name, name, "host", "Linux");
        server.Color.Should().Be(expectedColor);
    }

    [Fact]
    public void NewEnrolled_generates_unique_ids()
    {
        var s1 = Server.NewEnrolled("a", "A", "a.ts.net", "Linux");
        var s2 = Server.NewEnrolled("b", "B", "b.ts.net", "Linux");
        s1.Id.Should().NotBe(s2.Id);
    }
}
