namespace Compiler.IO
{
    /// <summary>
    /// Reads a file character by character
    /// </summary>
    public interface IFileReader
    {
        /// <summary>
        /// The position of the current character in the file
        /// </summary>
        /// <remarks>Lines and columns both start from 1, not 0</remarks>
        Position CurrentPosition { get; }

        /// <summary>
        /// The current character in the file
        /// </summary>
        /// <remarks>If the end of file has been reached then returns default(char)</remarks>
        char Current { get; }

        /// <summary>
        /// Move to the next character in the file
        /// </summary>
        /// <returns>True if there is another character in the file or false if the end of file has been reached</returns>
        bool MoveNext();

        /// <summary>
        /// Peeks at the next character in the file without advancing the position
        /// </summary>
        /// <returns>The next character or '\0' if the end of file has been reached</returns>
        char PeekNext();

        /// <summary>
        /// Skips forward to the beginning of the next line
        /// </summary>
        void SkipRestOfLine();

        /// <summary>
        /// Closes the file
        /// </summary>
        void Close();
    }
}