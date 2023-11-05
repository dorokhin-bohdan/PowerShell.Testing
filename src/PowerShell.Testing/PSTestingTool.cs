using PowerShell.Testing.Exceptions;
using PowerShell.Testing.Models;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace PowerShell.Testing;

public class PSTestingTool : IDisposable
{
    private readonly Runspace _runSpace;
    private readonly System.Management.Automation.PowerShell _instance;
    private readonly ISubject<MessageModel> _dataAddedSubject = new Subject<MessageModel>();

    /// <summary>
    ///  Constructs a PSTestingTool instance.
    /// </summary>
    /// <returns>Instance of <see cref="PSTestingTool"/>.</returns>
    public static PSTestingTool Create() => new();

    private PSTestingTool()
    {
        _runSpace = RunspaceFactory.CreateRunspace();
        _runSpace.Open();

        _instance = System.Management.Automation.PowerShell.Create(_runSpace);
        SubscribeOnStreamEvents();
    }

    /// <summary>
    /// Occurs when data was received.
    /// </summary>
    public IObservable<MessageModel> OnDataAdded => _dataAddedSubject.AsObservable();

    /// <summary>
    /// Disposes this tool instance.
    /// </summary>
    public void Dispose()
    {
        _dataAddedSubject.OnCompleted();
        _instance.Dispose();
        _runSpace.Dispose();
    }

    /// <summary>
    /// Executes provided <paramref name="script"/> in PowerShell.
    /// </summary>
    /// <param name="script">The command.</param>
    /// <returns>Instance of <see cref="Task"/>.</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="TestingToolException"></exception>
    public async Task ExecuteScriptAsync(string script)
    {
        if (string.IsNullOrWhiteSpace(script))
            throw new ArgumentException($"The '{nameof(script)}' cannot be null or empty.");

        Clear();

        using var inputCollection = new PSDataCollection<PSObject>();
        using var outputCollection = new PSDataCollection<PSObject>();

        SubscribeOnOutputEvents(outputCollection);

        try
        {
            _instance.AddScript(script);
            await _instance.InvokeAsync(inputCollection, outputCollection).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new TestingToolException($"Unable to invoke '{script}' command.", ex);
        }
    }

    /// <summary>
    /// Stops the currently running command.
    /// </summary>
    public void Stop()
    {
        _instance.Stop();
    }

    /// <summary>
    /// Clears commands and streams.
    /// </summary>
    public void Clear()
    {
        _instance.Streams.ClearStreams();
        _instance.Commands.Clear();
    }

    private void ConsumeData(object? sender, DataAddedEventArgs evtArgs)
    {
        var data = (sender as IList)?[evtArgs.Index];
        var message = new MessageModel(data?.ToString());

        _dataAddedSubject.OnNext(message);
    }

    private void SubscribeOnStreamEvents()
    {
        _instance.Streams.Error.DataAdded += ConsumeData;
        _instance.Streams.Progress.DataAdded += ConsumeData;
        _instance.Streams.Verbose.DataAdded += ConsumeData;
        _instance.Streams.Debug.DataAdded += ConsumeData;
        _instance.Streams.Warning.DataAdded += ConsumeData;
        _instance.Streams.Information.DataAdded += ConsumeData;
    }

    private void SubscribeOnOutputEvents(PSDataCollection<PSObject> output)
    {
        output.DataAdded += ConsumeData;
    }
}
