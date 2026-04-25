using System;
using System.Diagnostics;

namespace YomogiTaskBar.Utilities
{
    /// <summary>
    /// Centralized logging utility for consistent error handling and debugging
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="category">Optional category for the message</param>
        public static void LogInfo(string message, string category = "General")
        {
            Debug.WriteLine($"[INFO] [{category}] {DateTime.Now:HH:mm:ss} - {message}");
        }
        
        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The warning message to log</param>
        /// <param name="category">Optional category for the message</param>
        public static void LogWarning(string message, string category = "General")
        {
            Debug.WriteLine($"[WARN] [{category}] {DateTime.Now:HH:mm:ss} - {message}");
        }
        
        /// <summary>
        /// Logs an error message with optional exception details
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <param name="exception">Optional exception object for additional details</param>
        /// <param name="category">Optional category for the message</param>
        public static void LogError(string message, Exception? exception = null, string category = "General")
        {
            var errorText = exception != null 
                ? $"{message}: {exception.Message}" 
                : message;
                
            Debug.WriteLine($"[ERROR] [{category}] {DateTime.Now:HH:mm:ss} - {errorText}");
            
            if (exception != null)
            {
                Debug.WriteLine($"[ERROR] [{category}] Stack Trace: {exception.StackTrace}");
            }
        }
        
        /// <summary>
        /// Logs a debug message (only compiled in Debug builds)
        /// </summary>
        /// <param name="message">The debug message to log</param>
        /// <param name="category">Optional category for the message</param>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void LogDebug(string message, string category = "General")
        {
            Debug.WriteLine($"[DEBUG] [{category}] {DateTime.Now:HH:mm:ss} - {message}");
        }
        
        /// <summary>
        /// Logs an operation start
        /// </summary>
        /// <param name="operation">The operation being started</param>
        /// <param name="category">Optional category for the message</param>
        public static void LogOperationStart(string operation, string category = "General")
        {
            Debug.WriteLine($"[START] [{category}] {DateTime.Now:HH:mm:ss} - {operation}");
        }
        
        /// <summary>
        /// Logs an operation completion
        /// </summary>
        /// <param name="operation">The operation that completed</param>
        /// <param name="category">Optional category for the message</param>
        public static void LogOperationComplete(string operation, string category = "General")
        {
            Debug.WriteLine($"[COMPLETE] [{category}] {DateTime.Now:HH:mm:ss} - {operation}");
        }
        
        /// <summary>
        /// Logs an operation failure
        /// </summary>
        /// <param name="operation">The operation that failed</param>
        /// <param name="exception">Optional exception object for additional details</param>
        /// <param name="category">Optional category for the message</param>
        public static void LogOperationFailed(string operation, Exception? exception = null, string category = "General")
        {
            var errorText = exception != null 
                ? $"{operation}: {exception.Message}" 
                : operation;
                
            Debug.WriteLine($"[FAILED] [{category}] {DateTime.Now:HH:mm:ss} - {errorText}");
        }
    }
}
