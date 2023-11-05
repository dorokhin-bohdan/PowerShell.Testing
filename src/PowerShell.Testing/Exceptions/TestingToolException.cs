namespace PowerShell.Testing.Exceptions;

public class TestingToolException : Exception
{
    public TestingToolException(string message, Exception innerException) : base(message, innerException) { }
}
