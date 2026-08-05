using Adm.Core.Projects;

namespace Adm.Core.Tests;

public sealed class ProjectContractTests
{
    [Fact]
    public void ProjectIdIsAValueObject()
    {
        var first = new ProjectId("project-001");
        var second = new ProjectId("project-001");

        Assert.Equal(first, second);
        Assert.Equal("project-001", first.Value);
        Assert.Equal("project-001", first.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("project with spaces")]
    [InlineData("project/")]
    public void ProjectIdRejectsInvalidValues(string value)
    {
        Assert.Throws<ArgumentException>(() => new ProjectId(value));
    }

    [Fact]
    public void RegisteredProjectKeepsValidatedRootAndRegistrationState()
    {
        var registeredAt = new DateTimeOffset(2026, 8, 6, 1, 2, 3, TimeSpan.Zero);
        var project = new RegisteredProject(
            new ProjectId("project-001"),
            "日本語 Project",
            new ValidatedProjectRoot("C:\\Projects\\sample"),
            registeredAt,
            isSelected: true);

        Assert.Equal("日本語 Project", project.DisplayName);
        Assert.Equal("C:\\Projects\\sample", project.Root.CanonicalPath);
        Assert.Equal(registeredAt, project.RegisteredAtUtc);
        Assert.True(project.IsSelected);
    }

    [Fact]
    public void RawRootAndValidatedRootAreDifferentContracts()
    {
        var raw = new ProjectRootInput("C:\\Projects\\sample");
        var validated = new ValidatedProjectRoot(raw.Value);

        Assert.Equal(raw.Value, validated.CanonicalPath);
    }
}
