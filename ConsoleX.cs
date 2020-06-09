using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace IbGatewayHealthChecker
{
    /// <summary>
    /// Describes some useful methods for outputting to the console.
    /// </summary>
    internal static class ConsoleX
    {
        /// <summary>
        /// Writes the specified string value, followed by the current line terminator, to
        /// the standard output stream asyncronously.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="token">The token to monitor for cancellation requests. The
        /// default value is System.Threading.CancellationToken.None.</param>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        public static async Task WriteLineAsync(
            string value, CancellationToken token = default)
        {
            await Console.Out.WriteLineAsync(new StringBuilder(value), token);
        }

        /// <summary>
        /// Writes the specified string value, followed by the current line terminator, to
        /// the standard error stream asyncronously.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="token">The token to monitor for cancellation requests. The
        /// default value is System.Threading.CancellationToken.None.</param>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        public static async Task WriteErrorLineAsync(
            string value, CancellationToken token = default)
        {
            await Console.Error.WriteLineAsync(new StringBuilder(value), token);
        }
    }
}
