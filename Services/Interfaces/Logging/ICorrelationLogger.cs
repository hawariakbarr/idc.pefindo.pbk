using idc.pefindo.pbk.Models.Logging;

namespace idc.pefindo.pbk.Services.Interfaces.Logging
{
    public interface ICorrelationLogger
    {
        /// <summary>
        /// Logs the start of a process to the master correlation log
        /// </summary>
        /// <param name="correlationId">The correlation ID for the request</param>
        /// <param name="requestId">The request ID</param>
        /// <param name="processName">The name of the process (e.g., "IndividualProcessing")</param>
        /// <param name="userId">Optional user ID</param>
        /// <param name="sessionId">Optional session ID</param>
        /// <returns>Task representing the async operation</returns>
        Task LogProcessStartAsync(string correlationId, string requestId, string processName, string? userId = null, string? sessionId = null);

        /// <summary>
        /// Logs the successful completion of a process
        /// </summary>
        /// <param name="correlationId">The correlation ID for the request</param>
        /// <param name="status">The final status (Success, Failed, etc.)</param>
        /// <returns>Task representing the async operation</returns>
        Task LogProcessCompleteAsync(string correlationId, string status = "Success");

        /// <summary>
        /// Logs the failure of a process
        /// </summary>
        /// <param name="correlationId">The correlation ID for the request</param>
        /// <param name="status">The failure status (Failed, Error, etc.)</param>
        /// <param name="errorMessage">Optional error message</param>
        /// <returns>Task representing the async operation</returns>
        Task LogProcessFailAsync(string correlationId, string status = "Failed", string? errorMessage = null);

        /// <summary>
        /// Updates the status of an existing process log entry
        /// </summary>
        /// <param name="correlationId">The correlation ID for the request</param>
        /// <param name="status">The new status</param>
        /// <returns>Task representing the async operation</returns>
        Task UpdateProcessStatusAsync(string correlationId, string status);

        /// <summary>
        /// Gets a log entry by correlation ID
        /// </summary>
        /// <param name="correlationId">The correlation ID</param>
        /// <returns>The log entry or null if not found</returns>
        Task<LogEntry?> GetLogEntryAsync(string correlationId);
    }
}