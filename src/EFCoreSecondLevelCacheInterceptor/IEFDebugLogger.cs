namespace EFCoreSecondLevelCacheInterceptor;

/// <summary>
///     Formats and writes a debug log message.
/// </summary>
public interface IEFDebugLogger
{
    /// <summary>
    ///     Determines whether the debug logger is enabled.
    /// </summary>
    bool IsLoggerEnabled { get; }
}