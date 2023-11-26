using static System.Console;

namespace Compiler.IO
{
    /// <summary>
    /// An object for reporting errors in the compilation process
    /// </summary>
    public class ErrorReporter
    {
        /// <summary>
        /// The number of errors encountered so far
        /// </summary>
        public int ErrorCount { get; private set; } = 0;

        /// <summary>
        /// Whether or not any errors have been encounter
        /// </summary>
        public bool HasErrors { get { return ErrorCount > 0; } }

        /// <summary>
        /// Reports an error
        /// </summary>
        /// <param name="message">The message to display</param>
        public void ReportError(string message)
        {
            ErrorCount += 1;
            WriteLine($"ERROR: {message}");
        }
    }
}