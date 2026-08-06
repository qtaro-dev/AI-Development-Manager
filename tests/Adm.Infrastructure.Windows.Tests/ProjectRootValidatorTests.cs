using System.Diagnostics;
using Adm.Application.Projects;
using Adm.Core.Projects;
using Adm.Infrastructure.Windows.Projects;

namespace Adm.Infrastructure.Windows.Tests;

public sealed class ProjectRootValidatorTests
{
    [Fact]
    public async Task AcceptsExistingLocalNftsDirectoryWithJapaneseAndSpaces()
    {
        var root = CreateDirectory("日本語 Project");
        try
        {
            var result = await new WindowsProjectRootValidator().ValidateAsync(new ProjectRootInput(root));

            Assert.True(result.IsValid);
            Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)), result.Root!.Value.CanonicalPath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("relative-project")]
    [InlineData("\\\\server\\share")]
    [InlineData("\\\\?\\C:\\project")]
    [InlineData("C:\\project:stream")]
    [InlineData("C:\\CON")]
    [InlineData("C:\\project. ")]
    public async Task RejectsPathsOutsideTheLocalRootContract(string path)
    {
        var result = await new WindowsProjectRootValidator().ValidateAsync(new ProjectRootInput(path));

        Assert.False(result.IsValid);
        Assert.Equal(ProjectErrorCode.InvalidRoot, result.Error!.Code);
    }

    [Fact]
    public async Task RejectsMissingPathAndFilePath()
    {
        var validator = new WindowsProjectRootValidator();
        var missing = await validator.ValidateAsync(new ProjectRootInput(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        await File.WriteAllTextAsync(file, "fixture");
        try
        {
            var fileResult = await validator.ValidateAsync(new ProjectRootInput(file));

            Assert.Equal(ProjectErrorCode.InvalidRoot, missing.Error!.Code);
            Assert.Equal(ProjectErrorCode.InvalidRoot, fileResult.Error!.Code);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task RejectsAReparsePointRoot()
    {
        var basePath = Path.Combine(Path.GetTempPath(), $"Adm P2-002 Reparse {Guid.NewGuid():N}");
        var target = Path.Combine(basePath, "target");
        var junction = Path.Combine(basePath, "junction");
        Directory.CreateDirectory(target);
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c mklink /J \"{junction}\" \"{target}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            Assert.NotNull(process);
            await process!.WaitForExitAsync();
            Assert.Equal(0, process.ExitCode);

            var result = await new WindowsProjectRootValidator().ValidateAsync(new ProjectRootInput(junction));

            Assert.False(result.IsValid);
            Assert.Equal(ProjectErrorCode.InvalidRoot, result.Error!.Code);
        }
        finally
        {
            if (Directory.Exists(junction)) Directory.Delete(junction);
            if (Directory.Exists(basePath)) Directory.Delete(basePath, recursive: true);
        }
    }

    [Fact]
    public async Task CancellationIsPropagatedBeforeFilesystemAccess()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new WindowsProjectRootValidator().ValidateAsync(
                new ProjectRootInput(Path.GetTempPath()), cancellation.Token));
    }

    private static string CreateDirectory(string name)
    {
        var root = Path.Combine(Path.GetTempPath(), $"Adm P2-002 {Guid.NewGuid():N}", name);
        Directory.CreateDirectory(root);
        return root;
    }
}
