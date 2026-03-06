namespace CategorizedLogging
{
    public interface ILogModifier
    {
        LogEntry Modify(in LogEntry logEntry);
    }
}