using PowerShell.Testing.Exceptions;

namespace PowerShell.Testing.Tests;

public class PSTestingToolTests
{
    [Fact]
    public void Create_ShouldReturnInstance()
    {
        // Arrange

        // Act
        using var result = PSTestingTool.Create();

        // Assert
        result.Should().NotBeNull()
            .And.BeOfType<PSTestingTool>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ExecuteScriptAsync_NullOrWhiteSpaceScript_ShouldThrowTestingToolException(string script)
    {
        // Arrange
        using var tool = PSTestingTool.Create();

        // Act
        Func<Task> action = async () => await tool.ExecuteScriptAsync(script);

        // Assert
        action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("The 'script' cannot be null or empty.");
    }

    [Fact]
    public async Task ExecuteScriptAsync_MultiCommand_ShouldNotDuplicateCommands()
    {
        // Arrange
        const string firstCommandText = "First command";
        const string secondCommandText = "Second command";
        using var tool = PSTestingTool.Create();
        await tool.ExecuteScriptAsync($"Write-Host \"{firstCommandText}\"");
        var addedMessages = new List<string?>();
        using var _ = tool.OnDataAdded.Subscribe(x => addedMessages.Add(x.Message));

        // Act
        await tool.ExecuteScriptAsync($"Write-Host \"{secondCommandText}\"");

        // Assert
        addedMessages.Should().Contain(secondCommandText)
            .And.NotContain(firstCommandText);
    }

    [Fact]
    public async Task ExecuteScriptAsync_ErrorStream_ShouldConsumeErrorStream()
    {
        // Arrange
        const string errorText = "Some error here";
        using var tool = PSTestingTool.Create();

        var addedMessages = new List<string?>();
        using var _ = tool.OnDataAdded.Subscribe(x => addedMessages.Add(x.Message));

        // Act
        await tool.ExecuteScriptAsync($"Write-Error \"{errorText}\"");

        // Assert
        addedMessages.Should().Contain(errorText);
    }

    [Fact]
    public async Task ExecuteScriptAsync_ProgressStream_ShouldConsumeProgressStream()
    {
        // Arrange
        const int operationCount = 10;
        const string operationName = "Search in Progress";
        using var tool = PSTestingTool.Create();

        var addedMessages = new List<string?>();
        using var _ = tool.OnDataAdded.Subscribe(x => addedMessages.Add(x.Message));

        // Act
        await tool.ExecuteScriptAsync($"for ($i = 1; $i -le {operationCount}; $i++ ) {{ Write-Progress -Activity \"{operationName}\" -Status \"$i% Complete:\" -PercentComplete $i; Start-Sleep -Milliseconds 100; }}");

        // Assert
        addedMessages.Should().HaveCount(operationCount)
            .And.AllSatisfy(x => x.Contains(operationName));
    }

    [Fact]
    public async Task ExecuteScriptAsync_VerboseStream_ShouldConsumeVerboseStream()
    {
        // Arrange
        const string verboseText = "Some verbose here";
        using var tool = PSTestingTool.Create();

        var addedMessages = new List<string?>();
        using var _ = tool.OnDataAdded.Subscribe(x => addedMessages.Add(x.Message));

        // Act
        await tool.ExecuteScriptAsync($"$VerbosePreference = \"Continue\"; Write-Verbose \"{verboseText}\"");

        // Assert
        addedMessages.Should().Contain(verboseText);
    }

    [Fact]
    public async Task ExecuteScriptAsync_DebugStream_ShouldConsumeDebugStream()
    {
        // Arrange
        const string debugText = "Some debug here";
        using var tool = PSTestingTool.Create();

        var addedMessages = new List<string?>();
        using var _ = tool.OnDataAdded.Subscribe(x => addedMessages.Add(x.Message));

        // Act
        await tool.ExecuteScriptAsync($"$DebugPreference = \"Continue\"; Write-Debug \"{debugText}\"");

        // Assert
        addedMessages.Should().Contain(debugText);
    }

    [Fact]
    public async Task ExecuteScriptAsync_WarningStream_ShouldConsumeWarningStream()
    {
        // Arrange
        const string warningText = "Some warning here";
        using var tool = PSTestingTool.Create();

        var addedMessages = new List<string?>();
        using var _ = tool.OnDataAdded.Subscribe(x => addedMessages.Add(x.Message));

        // Act
        await tool.ExecuteScriptAsync($"WarningPreference = \"Continue\"; Write-Warning \"{warningText}\"");

        // Assert
        addedMessages.Should().Contain(warningText);
    }

    [Fact]
    public async Task ExecuteScriptAsync_InformationStream_ShouldConsumeInformationStream()
    {
        // Arrange
        const string infoText = "Some info here";
        using var tool = PSTestingTool.Create();

        var addedMessages = new List<string?>();
        using var _ = tool.OnDataAdded.Subscribe(x => addedMessages.Add(x.Message));

        // Act
        await tool.ExecuteScriptAsync($"Write-Information \"{infoText}\"");

        // Assert
        addedMessages.Should().Contain(infoText);
    }

    [Fact]
    public async Task ExecuteScriptAsync_Output_ShouldConsumeOutputData()
    {
        // Arrange
        const string dateFormat = "yyyyMMdd";
        var expectedDate = new DateTime(2023, 12, 31).ToString(dateFormat);
        using var tool = PSTestingTool.Create();

        var addedMessages = new List<string?>();
        using var _ = tool.OnDataAdded.Subscribe(x => addedMessages.Add(x.Message));

        // Act
        await tool.ExecuteScriptAsync($"Get-Date -Year 2023 -Month 12 -Day 31 -Format \"{dateFormat}\"");

        // Assert
        addedMessages.Should().Contain(expectedDate);
    }

    [Fact]
    public void ExecuteScriptAsync_InvalidCommand_ShouldThrowTestingToolException()
    {
        // Arrange
        const string invalidCommand = "INVALID_COMMAND";
        using var tool = PSTestingTool.Create();

        // Act
        Func<Task> action = async () => await tool.ExecuteScriptAsync(invalidCommand);

        // Assert
        action.Should().ThrowAsync<TestingToolException>()
            .WithMessage($"Unable to invoke '{invalidCommand}' command.");
    }

    [Fact]
    public void Dispose_ShouldCompleteSubject()
    {
        // Arrange
        using var tool = PSTestingTool.Create();
        var completed = false;
        using var _ = tool.OnDataAdded.Subscribe(_ => { }, () => { completed = true; });

        // Act
        tool.Dispose();

        // Assert
        completed.Should().BeTrue();
    }

    [Fact]
    public async Task Stop_ShouldStopCurrentCommand()
    {
        // Arrange
        const int operationCount = 10;
        const int delay = 500;
        const string operationName = "Heavy operation";
        using var tool = PSTestingTool.Create();

        var addedMessages = new List<string?>();
        _ = tool.OnDataAdded.Subscribe(x => addedMessages.Add(x.Message));
        _ = tool.ExecuteScriptAsync($"for ($i = 1; $i -le {operationCount}; $i++ ) {{ Write-Progress -Activity \"{operationName}\" -Status \"$i% Complete:\" -PercentComplete $i; Start-Sleep -Milliseconds 100; }}");

        await Task.Delay(delay);

        // Act
        tool.Stop();

        // Assert
        addedMessages.Should().HaveCountLessThan(operationCount);
    }
}